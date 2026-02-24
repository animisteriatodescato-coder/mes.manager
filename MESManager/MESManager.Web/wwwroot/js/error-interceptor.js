/**
 * MESManager — Error Interceptor
 * Cattura automaticamente errori JavaScript e HTTP dal browser e li invia
 * all'endpoint /api/IssueLog/error per salvarli come TechnicalIssue.
 *
 * Cattura:
 *  - window.onerror         → errori JS non gestiti
 *  - window.onunhandledrejection → Promise rejection non gestite
 *  - fetch monkey-patch     → risposte HTTP 4xx / 5xx
 *
 * Deduplicazione lato client: stesso errore nei 60 secondi precedenti viene ignorato.
 * Filtri: nomi file CDN esterni, favicon, health-check, issue-log stesso.
 */
(function () {
    'use strict';

    const ENDPOINT = '/api/IssueLog/error';
    const DEDUP_TTL_MS = 60_000; // 60 secondi
    const IGNORE_URL_PATTERNS = [
        /cdn\.jsdelivr/i,
        /unpkg\.com/i,
        /fonts\.googleapis/i,
        /favicon/i,
        /health/i,
        /api\/IssueLog/i,       // evita loop
        /blazor\.web\.js/i,
        /MudBlazor\.min/i,
        /ag-grid-community/i,
        /vis-timeline/i
    ];
    const IGNORE_MSG_PATTERNS = [
        /Script error/i,                // errori cross-origin opachi
        /ResizeObserver loop/i,         // warning noto e innocuo di browser
        /Non-Error promise rejection/i,
        /enableRangeSelection/i,        // AG Grid warning enterprise
        /deprecated/i                   // warning deprecation AG Grid
    ];

    // Cache locale per deduplicazione
    const _recentErrors = new Map(); // key → timestamp

    function isDuplicate(key) {
        const ts = _recentErrors.get(key);
        if (!ts) return false;
        return (Date.now() - ts) < DEDUP_TTL_MS;
    }

    function markSeen(key) {
        _recentErrors.set(key, Date.now());
        // Pulizia mappa per evitare memory leak
        if (_recentErrors.size > 100) {
            const cutoff = Date.now() - DEDUP_TTL_MS;
            for (const [k, v] of _recentErrors) {
                if (v < cutoff) _recentErrors.delete(k);
            }
        }
    }

    function shouldIgnore(message, sourceUrl) {
        const msg = message || '';
        const url = sourceUrl || '';

        for (const pat of IGNORE_URL_PATTERNS) {
            if (pat.test(url)) return true;
        }
        for (const pat of IGNORE_MSG_PATTERNS) {
            if (pat.test(msg)) return true;
        }

        // Non catturare se siamo sulla pagina issue-log stessa
        if (window.location.pathname.includes('issue-log')) return true;

        return false;
    }

    function send(payload) {
        const key = `${payload.errorType}|${payload.message?.substring(0, 120)}|${payload.statusCode ?? ''}`;
        if (isDuplicate(key)) return;
        markSeen(key);

        // Fire-and-forget silenzioso
        fetch(ENDPOINT, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload),
            keepalive: true
        }).catch(() => { /* silenzioso: non generare loop di errori */ });
    }

    // ─────────────────────────────────────────────────────────────
    // 1. window.onerror — errori JS non gestiti
    // ─────────────────────────────────────────────────────────────
    const _origOnerror = window.onerror;
    window.onerror = function (message, source, lineno, colno, error) {
        if (!shouldIgnore(message, source)) {
            send({
                errorType: 'js_error',
                message: String(message),
                stack: error?.stack ?? `${source}:${lineno}:${colno}`,
                sourceUrl: window.location.href,
                statusCode: null
            });
        }
        if (typeof _origOnerror === 'function') {
            return _origOnerror.call(this, message, source, lineno, colno, error);
        }
        return false;
    };

    // ─────────────────────────────────────────────────────────────
    // 2. window.onunhandledrejection — Promise rejection non gestite
    // ─────────────────────────────────────────────────────────────
    window.addEventListener('unhandledrejection', function (event) {
        const reason = event.reason;
        const message = reason instanceof Error
            ? reason.message
            : (typeof reason === 'string' ? reason : JSON.stringify(reason));
        const stack = reason instanceof Error ? reason.stack : undefined;

        if (!shouldIgnore(message, window.location.href)) {
            send({
                errorType: 'promise_rejection',
                message: message?.substring(0, 300) ?? 'Unhandled promise rejection',
                stack: stack ?? null,
                sourceUrl: window.location.href,
                statusCode: null
            });
        }
    });

    // ─────────────────────────────────────────────────────────────
    // 3. fetch monkey-patch — risposte HTTP 4xx / 5xx
    // Solo per chiamate allo stesso origin (API interne)
    // ─────────────────────────────────────────────────────────────
    const _origFetch = window.fetch;
    window.fetch = async function (input, init) {
        const url = typeof input === 'string' ? input
            : input instanceof Request ? input.url
            : String(input);

        const response = await _origFetch.call(this, input, init);

        // Controlla solo risposte con status code di errore
        if (response.status >= 400 && !response.ok) {
            // Non intercettare CDN o URL esterni
            const isLocal = url.startsWith('/') || url.startsWith(window.location.origin);
            if (isLocal && !shouldIgnore(url, url)) {
                const message = `${response.status} ${response.statusText} — ${url}`;
                send({
                    errorType: 'fetch_error',
                    message: message,
                    stack: null,
                    sourceUrl: window.location.href,
                    statusCode: response.status
                });
            }
        }

        return response;
    };

    // ─────────────────────────────────────────────────────────────
    // 4. console.error intercept — solo messaggi di errore espliciti
    //    (cattura quelli che log manuale nell'app)
    // ─────────────────────────────────────────────────────────────
    const _origConsoleError = console.error;
    console.error = function (...args) {
        _origConsoleError.apply(console, args);

        // Ricostruisce messaggio
        const message = args.map(a => {
            if (typeof a === 'string') return a;
            if (a instanceof Error) return a.message;
            try { return JSON.stringify(a); } catch { return String(a); }
        }).join(' ').substring(0, 300);

        if (message && !shouldIgnore(message, window.location.href)) {
            // Solo errori espliciti dell'app (contengono parole chiave di errore reale)
            const isRealError = /error|failed|exception|cannot|undefined is not|null is not/i.test(message);
            if (isRealError) {
                send({
                    errorType: 'console_error',
                    message: message,
                    stack: null,
                    sourceUrl: window.location.href,
                    statusCode: null
                });
            }
        }
    };

    console.info('[MESManager] Error interceptor attivo');
})();
