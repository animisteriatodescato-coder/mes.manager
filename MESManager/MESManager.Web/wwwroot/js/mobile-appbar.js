/**
 * mobile-appbar.js — Auto-hide AppBar su scroll (mobile UX)
 *
 * Comportamento:
 * - Scroll verso il basso (> 60px dal top): aggiunge `mes-appbar-hidden` al body → AppBar scompare con slide-up
 * - Scroll verso l'alto: rimuove la classe → AppBar riappare con slide-down
 * - A fine pagina (bottom): rimuove la classe (sempre visibile a fondo pagina)
 * - Su touch (swipe up per mostrare) — già coperto dalla logica scroll
 *
 * CSS: .mud-appbar gestisce la transizione con transform in app.css
 *
 * Attivazione: solo se schermo < 992px (mobile/tablet). Su desktop nessuna modifica.
 */
(function () {
    'use strict';

    var THRESHOLD = 60;       // px dal top oltre cui nascondere
    var DELTA     = 8;         // px minimi di scroll per reagire (evita microvibrazione)
    var lastScrollY = 0;
    var ticking    = false;

    function isMobile() {
        return window.innerWidth < 992;
    }

    function onScroll() {
        if (!ticking) {
            window.requestAnimationFrame(function () {
                var currentY = window.scrollY;
                var diff     = currentY - lastScrollY;

                if (Math.abs(diff) > DELTA) {
                    var atBottom = (window.innerHeight + currentY) >= (document.body.scrollHeight - 10);

                    if (isMobile()) {
                        if (currentY > THRESHOLD && diff > 0 && !atBottom) {
                            // scrolling DOWN → nascondi AppBar
                            document.body.classList.add('mes-appbar-hidden');
                        } else {
                            // scrolling UP o quasi top o fine pagina → mostra AppBar
                            document.body.classList.remove('mes-appbar-hidden');
                        }
                    } else {
                        document.body.classList.remove('mes-appbar-hidden');
                    }

                    lastScrollY = currentY;
                }

                ticking = false;
            });
            ticking = true;
        }
    }

    // Rimuovi la classe nascosta quando il viewport cambia dimensione (es. rotazione schermo)
    window.addEventListener('resize', function () {
        if (!isMobile()) {
            document.body.classList.remove('mes-appbar-hidden');
        }
    }, { passive: true });

    window.addEventListener('scroll', onScroll, { passive: true });

    // API esposta: Blazor può chiamarla dopo la navigazione per resettare lo stato
    window.mesMobile = window.mesMobile || {};
    window.mesMobile.showAppBar = function () {
        document.body.classList.remove('mes-appbar-hidden');
        lastScrollY = 0;
    };
})();
