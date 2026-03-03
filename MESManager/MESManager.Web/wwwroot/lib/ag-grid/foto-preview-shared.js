/**
 * Colonna Anteprima Foto condivisa per tutte le griglie AG Grid
 * =============================================================
 * - Immagine si adatta all'altezza della riga
 * - Hover > 1 secondo: popup a 3x con didascalia codice sotto (no tooltip nativo)
 */
window.fotoPreviewShared = (function () {

    var _popupEl  = null;
    var _hoverTimer = null;

    function removePopup() {
        if (_hoverTimer) { clearTimeout(_hoverTimer); _hoverTimer = null; }
        if (_popupEl && _popupEl.parentNode) { _popupEl.parentNode.removeChild(_popupEl); }
        _popupEl = null;
    }

    function showPopup(src, codice, anchorEl) {
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
            'padding:6px',
            'pointer-events:none',
            'transition:opacity 0.15s',
            'opacity:0',
            'display:flex',
            'flex-direction:column',
            'align-items:center',
            'gap:4px',
            'max-width:222px'
        ].join(';');

        var img = document.createElement('img');
        img.src = src;
        img.style.cssText = 'max-width:210px;max-height:210px;object-fit:contain;display:block;border-radius:4px;';
        img.onerror = function () { removePopup(); };

        // Didascalia SOTTO l'immagine — nessun testo sovrapposto
        var caption = document.createElement('span');
        caption.textContent = codice;
        caption.style.cssText = 'font-size:11px;color:#555;text-align:center;word-break:break-all;max-width:210px;line-height:1.3;';

        popup.appendChild(img);
        popup.appendChild(caption);
        document.body.appendChild(popup);
        _popupEl = popup;

        // Posiziona vicino alla cella, evitando uscita dallo schermo
        var popupW = 226;
        var popupH = 260; // img 210 + caption ~30 + padding
        var left = rect.right + 8;
        var top  = rect.top - 4;
        if (left + popupW > window.innerWidth)  { left = rect.left - popupW - 4; }
        if (top  + popupH > window.innerHeight) { top  = window.innerHeight - popupH - 4; }
        if (top  < 4) { top = 4; }
        if (left < 4) { left = 4; }
        popup.style.left = left + 'px';
        popup.style.top  = top  + 'px';

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
                var src = '/api/AllegatiAnima/preview-foto/' + encodeURIComponent(codice) + '?n=' + photoIndex + '&_t=' + Date.now();

                var img = document.createElement('img');
                img.src = src;
                // NIENTE img.title → nessun tooltip nativo del browser che si sovrappone al popup
                img.style.cssText = 'height:100%;width:100%;object-fit:contain;border-radius:2px;display:block;cursor:zoom-in;';
                img.onerror = function () { window.fotoPreviewShared.onImgError(this); };

                img.addEventListener('mouseenter', function () {
                    var self = this;
                    _hoverTimer = setTimeout(function () { showPopup(src, codice, self); }, 1000);
                });
                img.addEventListener('mouseleave', removePopup);

                var wrapper = document.createElement('div');
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

console.log('[foto-preview-shared v1.50.4] Module loaded');
