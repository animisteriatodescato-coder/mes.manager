window.preferencesInterop = {
    getItem: function (key) {
        return localStorage.getItem(key);
    },
    setItem: function (key, value) {
        localStorage.setItem(key, value);
    },
    removeItem: function (key) {
        localStorage.removeItem(key);
    }
};

/* ── Fullscreen helpers — usati da MainLayout.razor.cs ── */
window.mesFullscreen = {
    request: function () {
        // Prova Fullscreen API nativa (desktop Chrome, Android Chrome)
        if (document.documentElement.requestFullscreen) {
            document.documentElement.requestFullscreen().catch(function () {
                // Fallback CSS per iOS Safari e browser senza supporto nativo
                document.body.classList.add('mes-pseudo-fullscreen');
            });
            return;
        }
        // WebKit prefixed (Safari macOS / vecchi browser)
        if (document.documentElement.webkitRequestFullscreen) {
            try { document.documentElement.webkitRequestFullscreen(); return; } catch (e) { }
        }
        // Fallback puro CSS (iOS Safari)
        document.body.classList.add('mes-pseudo-fullscreen');
    },
    exit: function () {
        document.body.classList.remove('mes-pseudo-fullscreen');
        try {
            if (document.exitFullscreen && document.fullscreenElement) document.exitFullscreen();
            else if (document.webkitExitFullscreen && document.webkitFullscreenElement) document.webkitExitFullscreen();
        } catch (e) { }
    },
    isActive: function () {
        return !!(document.fullscreenElement || document.webkitFullscreenElement || document.body.classList.contains('mes-pseudo-fullscreen'));
    }
};
