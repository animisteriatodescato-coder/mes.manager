/**
 * Colonna Ricetta condivisa per tutte le griglie AG Grid
 * Centralizza logica di rendering e apertura dialog ricetta
 */
window.ricettaColumnShared = (function() {
    
    /**
     * Crea la definizione della colonna Ricetta
     * @param {Object} config - Configurazione
     * @param {string} config.fieldPrefix - 'camelCase' | 'PascalCase' (default: camelCase)
     * @param {string} config.gridNamespace - Nome namespace grid (es: 'animeGrid', 'commesseGrid')
     * @param {string} config.codiceArticoloField - Nome campo codice articolo
     * @returns {Object} Definizione colonna AG Grid
     */
    function createColumnDef(config) {
        const usePascal = config.fieldPrefix === 'PascalCase';
        const hasRicettaField = usePascal ? 'HasRicetta' : 'hasRicetta';
        const numeroParametriField = usePascal ? 'NumeroParametri' : 'numeroParametri';
        const dataModificaField = usePascal ? 'RicettaUltimaModifica' : 'ricettaUltimaModifica';
        const codiceArticoloField = config.codiceArticoloField || (usePascal ? 'ArticoloCodice' : 'articoloCodice');
        
        return {
            field: hasRicettaField,
            headerName: 'Ricetta',
            sortable: true,
            filter: true,
            width: 100,
            cellRenderer: params => {
                if (!params.data) return '';
                
                const hasRicetta = params.data[hasRicettaField];
                const numParametri = params.data[numeroParametriField] || 0;
                const dataModifica = params.data[dataModificaField];
                
                if (hasRicetta && numParametri > 0) {
                    const tooltip = dataModifica 
                        ? `${numParametri} parametri - Agg: ${new Date(dataModifica).toLocaleDateString('it-IT')}`
                        : `${numParametri} parametri`;
                    
                    const codiceArticolo = params.data[codiceArticoloField];
                    const onClickHandler = `window.${config.gridNamespace}.openRicetta('${codiceArticolo}')`;
                    
                    return `<div style="display: flex; align-items: center; height: 100%; cursor: pointer;" 
                                 onclick="${onClickHandler}" 
                                 title="${tooltip}">
                        <span style="background-color: #4caf50; color: white; padding: 2px 8px; border-radius: 12px; font-size: 11px; font-weight: 600;">
                            ✓ ${numParametri}
                        </span>
                    </div>`;
                } else {
                    return `<div style="display: flex; align-items: center; height: 100%;" title="Nessuna ricetta">
                        <span style="color: #999; font-size: 11px;">—</span>
                    </div>`;
                }
            }
        };
    }
    
    /**
     * Funzione helper per aprire dialog ricetta
     * Da chiamare dai metodi openRicetta() delle singole griglie
     * 
     * @param {string} codiceArticolo - Codice articolo
     * @param {Object} dotNetRef - Reference Blazor
     * @param {string} gridName - Nome griglia per log
     */
    function openRicettaDialog(codiceArticolo, dotNetRef, gridName) {
        if (!codiceArticolo) {
            console.warn(`[${gridName}] openRicetta chiamato senza codiceArticolo`);
            return;
        }
        
        console.log(`[${gridName}] Apertura ricetta per:`, codiceArticolo);
        
        if (dotNetRef) {
            dotNetRef.invokeMethodAsync('ViewRicetta', codiceArticolo);
        } else {
            console.error(`[${gridName}] DotNetRef non registrato`);
        }
    }
    
    return {
        createColumnDef: createColumnDef,
        openRicettaDialog: openRicettaDialog
    };
})();
