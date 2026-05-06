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
    request: async function () {
        try {
            if (document.documentElement.requestFullscreen) {
                await document.documentElement.requestFullscreen();
                return true;
            }
            if (document.documentElement.webkitRequestFullscreen) {
                document.documentElement.webkitRequestFullscreen();
                return true;
            }
        } catch (e) { }
        return false;
    },
    exit: function () {
        if (document.exitFullscreen) document.exitFullscreen();
        else if (document.webkitExitFullscreen) document.webkitExitFullscreen();
    },
    isActive: function () {
        return !!(document.fullscreenElement || document.webkitFullscreenElement);
    }
};
