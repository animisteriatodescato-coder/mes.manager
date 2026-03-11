/**
 * theme-vars.js — MESManager
 *
 * Applica le CSS custom properties (--mes-*) a :root via JavaScript Interop.
 * Chiamato da ThemeCssService.ApplyAsync() dopo ogni cambio di tema.
 * Garantisce live-preview senza re-render Blazor: aggiorna solo le vars nel DOM.
 */
window.mesTheme = {
    apply: function (vars) {
        const root = document.documentElement;
        for (const [key, value] of Object.entries(vars)) {
            root.style.setProperty(key, value);
        }
    }
};
