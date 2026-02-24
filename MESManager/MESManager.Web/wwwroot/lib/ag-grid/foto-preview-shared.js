/**
 * Colonna Anteprima Foto condivisa per tutte le griglie AG Grid
 * =============================================================
 * - Immagine si adatta all'altezza della riga
 * - Hover > 1 secondo: popup a 3x per l'ispezione visiva
 */
window.fotoPreviewShared = (function () {

    // ID del popup attivo (per evitare duplicati)
    var _popupEl = null;
    var _hoverTimer = null;

    function removePopup() {
        if (_hoverTimer) { clearTimeout(_hoverTimer); _hoverTimer = null; }
        if (_popupEl && _popupEl.parentNode) { _popupEl.parentNode.removeChild(_popupEl); }
        _popupEl = null;
    }

    function showPopup(src, anchorEl) {
        removePopup();
        var rect = anchorEl.getBoundingClientRect();
        var popup = document.createElement('div');
        popup.style.cssText = [
            'position:fixed',
            'z-index:99999',
            'background:#fff',
            'border:2px solid #1976d2',
            'border-radius:6px',
            'box-shadow:0 8px 32px rgba(0,0,0,0.35)',
            'padding:4px',
            'pointer-events:none',
            'transition:opacity 0.15s',
            'opacity:0'
        ].join(';');

        var img = document.createElement('img');
        img.src = src;
        img.style.cssText = 'max-width:210px;max-height:210px;object-fit:contain;display:block;border-radius:4px;';
        img.onerror = function () { removePopup(); };
        popup.appendChild(img);
        document.body.appendChild(popup);
        _popupEl = popup;

        // Posiziona vicino alla cella, evitando di uscire dallo schermo
        var left = rect.right + 8;
        var top  = rect.top - 4;
        if (left + 226 > window.innerWidth)  { left = rect.left - 226; }
        if (top  + 218 > window.innerHeight) { top  = window.innerHeight - 222; }
        if (top < 4) { top = 4; }
        popup.style.left = left + 'px';
        popup.style.top  = top  + 'px';

        // fade in
        requestAnimationFrame(function () { popup.style.opacity = '1'; });
    }

    function onImgError(img) {
        try {
            img.style.display = 'none';
            var w = img.parentElement;
            if (w) w.innerHTML = '<span style="color:#ccc;font-size:11px;">—</span>';
        } catch (e) { /* silent */ }
    }

    function createColumnDef(config) {
        config = config || {};
        var codiceField = config.codiceArticoloField || 'articoloCodice';
        var photoIndex  = config.photoIndex           || 2;
        var hide        = config.hide !== undefined   ? config.hide : false;

        return {
            field: 'fotoPreview',
            headerName: 'Foto',
            sortable: false,
            filter: false,
            width: 70,
            hide: hide,
            suppressMenu: true,
            resizable: true,
            cellRenderer: function (params) {
                if (!params.data) return '';
                var codice = params.data[codiceField];
                if (!codice) {
                    return '<span style="color:#ccc;font-size:11px;">—</span>';
                }
                var src = '/api/AllegatiAnima/preview-foto/' + encodeURIComponent(codice) + '?n=' + photoIndex;

                var img = document.createElement('img');
                img.src = src;
                img.title = 'Foto ' + photoIndex + ': ' + codice;
                // Altezza 100% = si adatta alla riga; larghezza massima = larghezza colonna - 8px padding
                img.style.cssText = 'height:100%;width:100%;object-fit:contain;border-radius:2px;display:block;cursor:zoom-in;';
                img.onerror = function () { window.fotoPreviewShared.onImgError(this); };

                // Hover con ritardo 1 secondo → popup 3x
                img.addEventListener('mouseenter', function () {
                    var self = this;
                    _hoverTimer = setTimeout(function () { showPopup(src, self); }, 1000);
                });
                img.addEventListener('mouseleave', removePopup);

                var wrapper = document.createElement('div');
                // overflow:hidden taglia l'immagine che deborda; height:100% la vincola alla riga
                wrapper.style.cssText = 'display:flex;align-items:center;justify-content:center;height:100%;width:100%;padding:2px;box-sizing:border-box;overflow:hidden;';
                wrapper.appendChild(img);
                return wrapper;
            }
        };
    }

    return {
        createColumnDef: createColumnDef,
        onImgError: onImgError
    };

})();

console.log('[foto-preview-shared v1.50.3] Module loaded');
