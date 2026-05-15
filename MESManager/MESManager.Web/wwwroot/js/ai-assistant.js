// AI Assistant Panel — scroll + drawer resize helpers
window.AiAssistant = {
    scrollToBottom: function (elementId) {
        var el = document.getElementById(elementId);
        if (el) el.scrollTop = el.scrollHeight;
    },

    startDrawerResize: function (event, handle) {
        handle = handle || event.currentTarget;
        var drawer = handle
            ? (handle.closest('.mud-drawer') || handle.closest('.ai-assistant-drawer') || handle.closest('[data-testid="ai-assistant-drawer"]'))
            : document.querySelector('.ai-assistant-drawer, [data-testid="ai-assistant-drawer"]');
        if (!drawer) return;

        event.preventDefault();
        event.stopPropagation();

        var storageKey = 'mes-ai-assistant-width';
        var minWidth = 360;
        var defaultWidth = 520;
        var startX = event.clientX;
        var startWidth = drawer.getBoundingClientRect().width || defaultWidth;
        var moveEventName = event.type === 'mousedown' ? 'mousemove' : 'pointermove';
        var upEventName = event.type === 'mousedown' ? 'mouseup' : 'pointerup';

        function clamp(width) {
            var maxWidth = Math.max(300, window.innerWidth - 72);
            var lower = Math.min(minWidth, maxWidth);
            return Math.min(Math.max(width, lower), maxWidth);
        }

        function applyWidth(width) {
            var next = clamp(width || defaultWidth);
            drawer.style.width = next + 'px';
            drawer.style.minWidth = Math.min(minWidth, next) + 'px';
            drawer.style.maxWidth = 'calc(100vw - 72px)';
            drawer.style.setProperty('--ai-assistant-drawer-width', next + 'px');
            return next;
        }

        function stopResize() {
            document.body.classList.remove('ai-drawer-resizing');
            document.removeEventListener(moveEventName, onMove);
            document.removeEventListener(upEventName, stopResize);
            localStorage.setItem(storageKey, String(Math.round(drawer.getBoundingClientRect().width)));
        }

        function onMove(moveEvent) {
            var delta = startX - moveEvent.clientX;
            applyWidth(startWidth + delta);
        }

        document.body.classList.add('ai-drawer-resizing');
        document.addEventListener(moveEventName, onMove);
        document.addEventListener(upEventName, stopResize);
    },

    initResizableDrawer: function () {
        var handle = document.querySelector('.ai-drawer-resize-handle');
        var drawer = handle
            ? (handle.closest('.mud-drawer') || handle.closest('.ai-assistant-drawer') || handle.closest('[data-testid="ai-assistant-drawer"]'))
            : document.querySelector('.ai-assistant-drawer, [data-testid="ai-assistant-drawer"]');
        if (!drawer) return;

        var storageKey = 'mes-ai-assistant-width';
        var minWidth = 360;
        var defaultWidth = 520;

        function clamp(width) {
            var maxWidth = Math.max(300, window.innerWidth - 72);
            var lower = Math.min(minWidth, maxWidth);
            return Math.min(Math.max(width, lower), maxWidth);
        }

        function applyWidth(width) {
            var next = clamp(width || defaultWidth);
            drawer.style.width = next + 'px';
            drawer.style.minWidth = Math.min(minWidth, next) + 'px';
            drawer.style.maxWidth = 'calc(100vw - 72px)';
            drawer.style.setProperty('--ai-assistant-drawer-width', next + 'px');
            return next;
        }

        var saved = parseInt(localStorage.getItem(storageKey) || '', 10);
        applyWidth(Number.isFinite(saved) ? saved : defaultWidth);

        handle = handle || drawer.querySelector('.ai-drawer-resize-handle');
        if (!handle) return;
        if (handle.dataset.aiResizeReady === '1') return;
        handle.dataset.aiResizeReady = '1';

        var startX = 0;
        var startWidth = 0;

        function stopResize() {
            document.body.classList.remove('ai-drawer-resizing');
            document.removeEventListener('pointermove', onMove);
            document.removeEventListener('pointerup', stopResize);
            localStorage.setItem(storageKey, String(Math.round(drawer.getBoundingClientRect().width)));
        }

        function onMove(event) {
            var delta = startX - event.clientX;
            applyWidth(startWidth + delta);
        }

        handle.addEventListener('pointerdown', function (event) {
            startX = event.clientX;
            startWidth = drawer.getBoundingClientRect().width;
            event.preventDefault();
            document.body.classList.add('ai-drawer-resizing');
            document.addEventListener('pointermove', onMove);
            document.addEventListener('pointerup', stopResize);
        });

        handle.addEventListener('dblclick', function () {
            localStorage.removeItem(storageKey);
            applyWidth(defaultWidth);
        });

        window.addEventListener('resize', function () {
            var current = drawer.getBoundingClientRect().width || defaultWidth;
            var next = applyWidth(current);
            localStorage.setItem(storageKey, String(Math.round(next)));
        });
    }
};

(function () {
    if (window.__aiAssistantResizeDelegated) return;
    window.__aiAssistantResizeDelegated = true;

    document.addEventListener('pointerdown', function (event) {
        if (event.defaultPrevented || !window.AiAssistant) return;
        var target = event.target;
        var handle = target && target.closest ? target.closest('.ai-drawer-resize-handle') : null;
        if (!handle) return;
        window.AiAssistant.startDrawerResize(event, handle);
    });

    document.addEventListener('mousedown', function (event) {
        if (event.defaultPrevented || !window.AiAssistant) return;
        var target = event.target;
        var handle = target && target.closest ? target.closest('.ai-drawer-resize-handle') : null;
        if (!handle) return;
        window.AiAssistant.startDrawerResize(event, handle);
    });
})();
