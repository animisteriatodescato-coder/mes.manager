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
                    // Cella vuota: cliccabile per aprire il dialog di importazione
                    const codiceArticolo = params.data[codiceArticoloField];
                    const onClickHandler = codiceArticolo
                        ? `window.${config.gridNamespace}.openImportaRicetta('${codiceArticolo}')`
                        : '';
                    const style = codiceArticolo
                        ? 'cursor: pointer; display: flex; align-items: center; height: 100%;'
                        : 'display: flex; align-items: center; height: 100%;';
                    const tooltip = codiceArticolo ? 'Clicca per importare ricetta da macchina' : 'Nessuna ricetta';

                    return `<div style="${style}" onclick="${onClickHandler}" title="${tooltip}">
                        <span style="color: #bbb; font-size: 11px; text-decoration: underline dotted; text-underline-offset: 2px;">— importa</span>
                    </div>`;
                }
            }
        };
    }
    
    /**
     * Funzione helper per aprire dialog ricetta (cella verde con parametri)
     * @param {string} codiceArticolo
     * @param {Object} dotNetRef - Reference Blazor
     * @param {string} gridName
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

    /**
     * Funzione helper per aprire dialog importa ricetta (cella vuota)
     * @param {string} codiceArticolo
     * @param {Object} dotNetRef - Reference Blazor
     * @param {string} gridName
     */
    function openImportaRicettaDialog(codiceArticolo, dotNetRef, gridName) {
        if (!codiceArticolo) {
            console.warn(`[${gridName}] openImportaRicetta chiamato senza codiceArticolo`);
            return;
        }
        console.log(`[${gridName}] Importa ricetta per:`, codiceArticolo);
        if (dotNetRef) {
            dotNetRef.invokeMethodAsync('ImportaRicetta', codiceArticolo);
        } else {
            console.error(`[${gridName}] DotNetRef non registrato per ImportaRicetta`);
        }
    }
    
    return {
        createColumnDef: createColumnDef,
        openRicettaDialog: openRicettaDialog,
        openImportaRicettaDialog: openImportaRicettaDialog
    };
})();
