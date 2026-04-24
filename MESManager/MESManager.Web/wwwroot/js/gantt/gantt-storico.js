// Gantt Storico Macchine — Vis-Timeline read-only per stati macchina PLC
// Versione: 1 — v1.56.0
// Dati da: GET /api/Plc/gantt-storico  |  Colori da: MesDesignTokens.PlcStatoColore() (C# controller)
// NO drag & drop, NO modifica — solo visualizzazione + tooltip + export CSV

window.GanttStorico = (function () {

    var _timeline = null;
    var _segmenti = [];

    // ─── Sanitizzazione HTML (OWASP XSS prevention) ──────────────────────────
    function _esc(str) {
        if (str == null) return '';
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    // ─── Formatta durata in ore/minuti/secondi ───────────────────────────
    function _formatDurata(minuti) {
        var totSec = Math.round(minuti * 60);
        var sec = totSec % 60;
        var totMin = Math.floor(totSec / 60);
        var min = totMin % 60;
        var ore = Math.floor(totMin / 60);
        if (ore > 0) return ore + 'h ' + String(min).padStart(2, '0') + 'm ' + String(sec).padStart(2, '0') + 's';
        if (min > 0) return min + 'm ' + String(sec).padStart(2, '0') + 's';
        return sec + 's';
    }

    // ─── Contenuto breve dell'item (visibile sulla barra) ─────────────────────
    // Formato: Op: NOME • durata [• N pz]  —  stato rimosso (coperto dalla legenda colori)
    function _buildContent(s) {
        var parts = [];
        if (s.nomeOperatore) parts.push('Op: ' + _esc(s.nomeOperatore));
        parts.push(_formatDurata(s.durataMinuti));
        if (s.cicliFatti > 0) parts.push(s.cicliFatti + ' pz');
        return parts.join(' \u2022 ');
    }

    // ─── Formatta tempo ciclo medio (secondi → "Xm Ys" o "Xs") ─────────────────
    function _formatTempoCiclo(secondi) {
        var s = Math.round(secondi);
        if (s <= 0) return '—';
        var m = Math.floor(s / 60);
        var r = s % 60;
        if (m > 0) return m + 'm ' + String(r).padStart(2, '0') + 's';
        return r + 's';
    }

    // ─── Tooltip HTML (Vis-Timeline usa innerHTML) ────────────────────────────
    function _buildTooltip(s) {
        var d     = new Date(s.inizio);
        var dFine = new Date(s.fine);
        var locale = 'it-IT';
        var tOpts = { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' };

        var html = '<div style="min-width:220px;padding:4px 0">';
        html += '<b style="font-size:1em">' + _esc(s.statoMacchina) + '</b><br>';
        html += '<span style="color:#888">Macchina: </span>' + _esc(s.macchianaNome) + '<br>';
        html += '<span style="color:#888">Da: </span>' + d.toLocaleString(locale, tOpts) + '<br>';
        html += '<span style="color:#888">A: </span>' + dFine.toLocaleString(locale, tOpts) + '<br>';
        html += '<span style="color:#888">Durata: </span><b>' + _formatDurata(s.durataMinuti) + '</b><br>';
        if (s.nomeOperatore) {
            html += '<span style="color:#888">Operatore: </span>' + _esc(s.nomeOperatore) + '<br>';
        }
        if (s.cicliFatti > 0) {
            html += '<span style="color:#888">Pezzi fatti: </span><b>' + s.cicliFatti + '</b><br>';
            // Preferisce il valore hardware PLC (tempoMedioRilevato); fallback su durata/pezzi
            var tempoCicloMedioSec = (s.tempoMedioRilevato && s.tempoMedioRilevato > 0)
                ? s.tempoMedioRilevato
                : (s.durataMinuti * 60) / s.cicliFatti;
            html += '<span style="color:#888">Tempo ciclo medio: </span><b>' + _formatTempoCiclo(tempoCicloMedioSec) + '</b><br>';
        } else if (s.tempoMedioRilevato && s.tempoMedioRilevato > 0) {
            // Ha tempo medio rilevato anche senza conteggio pezzi visibile
            html += '<span style="color:#888">Tempo ciclo medio: </span><b>' + _formatTempoCiclo(s.tempoMedioRilevato) + '</b><br>';
        }
        if (s.barcodeLavorazione > 0) {
            html += '<span style="color:#888">Barcode: </span>' + _esc(String(s.barcodeLavorazione)) + '<br>';
        }
        html += '</div>';
        return html;
    }

    // ─── Costruisce Items DataSet da segmenti ─────────────────────────────────
    function _buildItems(segmenti) {
        return new vis.DataSet(segmenti.map(function (s, idx) {
            var colore = s.colore || '#9E9E9E';
            // Colore bordo: versione leggermente più scura del fill
            var borderDarker = colore; // il controller manda già il colore corretto
            var isNonConnessa = s.statoMacchina && s.statoMacchina.toUpperCase().indexOf('NON CONNESSA') >= 0;
            return {
                id: idx,
                group: s.macchinaId,
                start: new Date(s.inizio),
                end: new Date(s.fine),
                content: _buildContent(s),
                title: _buildTooltip(s),
                style: 'background-color:' + colore + ';border-color:' + borderDarker + ';',
                className: 'vis-storico-item' + (isNonConnessa ? ' vis-stato-non-connessa' : '')
            };
        }));
    }

    // ─── Costruisce Groups DataSet da segmenti (macchine distinte) ────────────
    function _buildGroups(segmenti) {
        var seen = {};
        var groups = [];
        segmenti.forEach(function (s) {
            if (!seen[s.macchinaId]) {
                seen[s.macchinaId] = true;
                groups.push({ id: s.macchinaId, content: _esc(s.macchianaNome) });
            }
        });
        // Ordina per nome macchina
        groups.sort(function (a, b) { return a.content.localeCompare(b.content); });
        return new vis.DataSet(groups);
    }

    return {

        // ── initialize(elementId, segmenti) ──────────────────────────────────
        // Crea o ricrea la timeline Vis con i segmenti forniti.
        // Chiamato da GanttStoricoMacchine.razor via JS Interop.
        initialize: function (elementId, segmenti) {
            var container = document.getElementById(elementId);
            if (!container) {
                console.warn('[GanttStorico] Elemento non trovato:', elementId);
                return;
            }

            _segmenti = segmenti || [];

            if (!_segmenti.length) {
                if (_timeline) { _timeline.destroy(); _timeline = null; }
                return;
            }

            var items  = _buildItems(_segmenti);
            var groups = _buildGroups(_segmenti);

            var options = {
                editable: false,
                selectable: true,
                stack: false,
                orientation: 'top',
                groupOrder: 'content',
                tooltip: { followMouse: true, overflowMethod: 'flip' },
                margin: { item: { horizontal: 0, vertical: 2 }, axis: 3 },
                zoomMin: 1000 * 30,                    // 30 secondi — permette zoom ai secondi
                zoomMax: 1000 * 60 * 60 * 24 * 60,    // 60 giorni
                showCurrentTime: true,
                format: {
                    minorLabels: {
                        millisecond: 'SSS[ms]',
                        second:      's[s]',
                        minute:      'HH:mm',
                        hour:        'HH:mm',
                        weekday:     'ddd D',
                        day:         'D',
                        week:        'w',
                        month:       'MMM',
                        year:        'YYYY'
                    },
                    majorLabels: {
                        millisecond: 'HH:mm:ss',
                        second:      'D/MM HH:mm',
                        minute:      'ddd D MMM',
                        hour:        'ddd D MMM',
                        weekday:     'MMMM YYYY',
                        day:         'MMMM YYYY',
                        week:        'MMMM YYYY',
                        month:       'YYYY',
                        year:        ''
                    }
                }
            };

            if (_timeline) {
                _timeline.destroy();
            }

            _timeline = new vis.Timeline(container, items, groups, options);

            // Adatta la finestra temporale ai dati
            _timeline.fit({ animation: false });
        },

        // ── exportCsv(segmenti) ───────────────────────────────────────────────
        // Genera e scarica un file CSV con i segmenti correnti.
        // BOM UTF-8 (\uFEFF) per compatibilità Excel italiana.
        exportCsv: function (segmenti) {
            var locale = 'it-IT';
            var tOpts = { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' };

            var header = ['Macchina', 'Stato', 'Inizio', 'Fine', 'Durata (min)', 'Operatore', 'Cicli Fatti', 'Barcode'];
            var rows = (segmenti || []).map(function (s) {
                return [
                    s.macchianaNome,
                    s.statoMacchina,
                    new Date(s.inizio).toLocaleString(locale, tOpts),
                    new Date(s.fine).toLocaleString(locale, tOpts),
                    s.durataMinuti.toFixed(1),
                    s.nomeOperatore || '',
                    s.cicliFatti,
                    s.barcodeLavorazione
                ];
            });

            var csvContent = [header].concat(rows)
                .map(function (row) {
                    return row.map(function (cell) {
                        // Escaping CSV: ogni cella tra virgolette, le virgolette interne raddoppiate
                        return '"' + String(cell).replace(/"/g, '""') + '"';
                    }).join(',');
                }).join('\r\n');

            var blob = new Blob(['\uFEFF' + csvContent], { type: 'text/csv;charset=utf-8;' });
            var url  = URL.createObjectURL(blob);
            var a    = document.createElement('a');
            a.href     = url;
            a.download = 'gantt-storico-' + new Date().toISOString().slice(0, 10) + '.csv';
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            URL.revokeObjectURL(url);
        },

        // ── destroy() ────────────────────────────────────────────────────────
        // Chiamato da DisposeAsync di GanttStoricoMacchine.razor
        destroy: function () {
            if (_timeline) {
                _timeline.destroy();
                _timeline = null;
            }
            _segmenti = [];
        }
    };
}());
