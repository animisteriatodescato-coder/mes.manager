/**
 * Colonna Anteprima Foto condivisa per tutte le griglie AG Grid
 * =============================================================
 * UN UNICO punto di modifica per il rendering dell'anteprima immagine.
 * Si propaga automaticamente a tutte le griglie che la usano:
 *   - animeGrid, commesseGrid, commesseAperteGrid, programmaMacchineGrid
 *
 * Utilizza l'endpoint: GET /api/AllegatiAnima/preview-foto/{codiceArticolo}?n=2
 * Il browser gestisce automaticamente caching e richieste HTTP.
 * Se l'immagine non esiste (404), viene nascosta silenziosamente.
 *
 * Utilizzo in ogni file grid:
 *   window.fotoPreviewShared.createColumnDef({
 *       codiceArticoloField: 'articoloCodice',  // nome campo nel rowData
 *       photoIndex: 2,                           // numero foto (default: 2)
 *       hide: false                              // visibile di default
 *   })
 */
window.fotoPreviewShared = (function () {

    /**
     * Chiamata dall'attributo onerror delle img di anteprima.
     * Nasconde l'immagine e mostra un trattino se il file non esiste.
     * Funzione named per evitare escaping HTML complesso nel cellRenderer.
     */
    function onImgError(img) {
        try {
            img.style.display = 'none';
            var wrapper = img.parentElement;
            if (wrapper) {
                wrapper.innerHTML = '<span style="color:#ccc;font-size:11px;">—</span>';
            }
        } catch (e) { /* silent */ }
    }

    /**
     * Crea la definizione della colonna Anteprima Foto
     * @param {Object} config - Configurazione
     * @param {string} config.codiceArticoloField - Nome del campo codice articolo nel row data (default: 'articoloCodice')
     * @param {number} config.photoIndex          - Numero della foto da mostrare, 1=prima, 2=seconda (default: 2)
     * @param {boolean} config.hide               - Se nascondere la colonna di default (default: false)
     * @returns {Object} Definizione colonna AG Grid
     */
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
                img.src   = src;
                img.title = 'Foto ' + photoIndex + ': ' + codice;
                img.style.cssText = 'max-height:calc(100% - 4px);max-width:62px;object-fit:contain;border-radius:2px;display:block;';
                img.onerror = function () { window.fotoPreviewShared.onImgError(this); };
                var wrapper = document.createElement('div');
                wrapper.style.cssText = 'display:flex;align-items:center;justify-content:center;height:100%;padding:1px 2px;';
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

console.log('[foto-preview-shared v1.50.1] Module loaded');
