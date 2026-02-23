/**
 * Definizioni condivise delle colonne anime
 * Usate da commesse-aperte-grid.js e programma-macchine-grid.js
 * Fonte unica per evitare duplicazioni - prese dal catalogo anime
 */
window.animeColumnsShared = (function() {
    
    /**
     * Colonne anime per visualizzazione in griglie commesse
     * Tutte nascoste di default (hide: true) - l'utente può mostrarle dal menu colonne
     */
    const animeColumns = [
        // RICETTA - Badge con numero parametri (CLICCABILE per aprire ricetta)
        { 
            field: 'hasRicetta', 
            headerName: 'Ricetta', 
            sortable: true, 
            filter: true, 
            width: 100,
            hide: false,
            editable: false,
            cellRenderer: params => {
                if (!params.data) return '';
                
                const hasRicetta = params.data.hasRicetta;
                const numParametri = params.data.numeroParametri || 0;
                const dataModifica = params.data.ricettaUltimaModifica;
                
                if (hasRicetta && numParametri > 0) {
                    const tooltip = dataModifica 
                        ? `${numParametri} parametri - Agg: ${new Date(dataModifica).toLocaleDateString('it-IT')}`
                        : `${numParametri} parametri`;
                    
                    // Usa il grid specifico se disponibile, fallback ad animeGrid
                    const gridNamespace = window.commesseAperteGrid ? 'commesseAperteGrid' : 
                                         window.commesseGrid ? 'commesseGrid' : 'animeGrid';
                    const codArticolo = params.data.codiceArticolo || params.data.articoloCodice;
                    
                    return `<div style="display: flex; align-items: center; height: 100%; cursor: pointer;" 
                                 onclick="window.${gridNamespace}.openRicetta('${codArticolo}')" 
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
        },
        
        // Dimensioni
        { field: 'larghezza', headerName: 'Larghezza', sortable: true, filter: 'agNumberColumnFilter', width: 100, hide: true },
        { field: 'altezza', headerName: 'Altezza', sortable: true, filter: 'agNumberColumnFilter', width: 100, hide: true },
        { field: 'profondita', headerName: 'Profondità', sortable: true, filter: 'agNumberColumnFilter', width: 100, hide: true },
        
        // Ubicazione e codici
        { field: 'ubicazione', headerName: 'Ubicazione', sortable: true, filter: true, width: 150, hide: true },
        { field: 'codiceCassa', headerName: 'Codice Cassa', sortable: true, filter: true, width: 120, hide: true },
        { field: 'codiceAnime', headerName: 'Codice Anime', sortable: true, filter: true, width: 120, hide: true },
        { field: 'unitaMisura', headerName: 'U.M. Anime', sortable: true, filter: true, width: 100, hide: true },
        
        // Imballo - IMPORTANTE: usa descrizione, non numero!
        { field: 'imballoDescrizione', headerName: 'Imballo', sortable: true, filter: true, width: 150, hide: true },
        
        // Materiali con descrizioni
        { field: 'sabbiaDescrizione', headerName: 'Sabbia', sortable: true, filter: true, width: 120, hide: true },
        { field: 'verniceDescrizione', headerName: 'Vernice', sortable: true, filter: true, width: 150, hide: true },
        { field: 'collaDescrizione', headerName: 'Colla', sortable: true, filter: true, width: 120, hide: true },
        
        // Macchine - descrizione contiene i nomi macchine
        { field: 'macchineSuDisponibiliDescrizione', headerName: 'Macchine Disponibili', sortable: true, filter: true, width: 180, hide: true },
        
        // Quantità
        { field: 'quantitaPiano', headerName: 'Qtà Piano', sortable: true, filter: 'agNumberColumnFilter', width: 100, hide: true },
        { field: 'numeroPiani', headerName: 'N. Piani', sortable: true, filter: 'agNumberColumnFilter', width: 100, hide: true },
        { 
            field: 'quantitaEtichetta', 
            headerName: 'Qtà Etichetta', 
            sortable: true, 
            filter: 'agNumberColumnFilter', 
            width: 120, 
            hide: true,
            valueGetter: params => {
                const qp = params.data?.quantitaPiano || 0;
                const np = params.data?.numeroPiani || 0;
                return qp * np;
            }
        },
        
        // Altri campi produzione
        { field: 'ciclo', headerName: 'Ciclo', sortable: true, filter: true, width: 150, hide: true },
        { field: 'peso', headerName: 'Peso', sortable: true, filter: true, width: 100, hide: true },
        { field: 'figure', headerName: 'Figure', sortable: true, filter: true, width: 120, hide: true },
        { field: 'maschere', headerName: 'Maschere', sortable: true, filter: true, width: 120, hide: true },
        { field: 'assemblata', headerName: 'Assemblata', sortable: true, filter: true, width: 120, hide: true },
        { field: 'armataL', headerName: 'Armata L', sortable: true, filter: true, width: 120, hide: true },
        { 
            field: 'togliereSparo', 
            headerName: 'Tog. Sparo', 
            sortable: true, 
            filter: true, 
            width: 100, 
            hide: true,
            valueFormatter: params => params.value === '1' ? 'Sì' : (params.value === '0' ? 'No' : '')
        },
        
        // Note e allegati
        { field: 'noteAnime', headerName: 'Note Anime', sortable: true, filter: true, width: 200, hide: true },
        { field: 'allegato', headerName: 'Allegato', sortable: true, filter: true, width: 150, hide: true },
        
        // Flag
        { 
            field: 'trasmettiTutto', 
            headerName: 'Trasmetti Tutto', 
            sortable: true, 
            filter: true, 
            width: 120,
            hide: true,
            valueFormatter: params => params.value === true ? 'Sì' : params.value === false ? 'No' : ''
        }
    ];

    /**
     * Restituisce una copia delle colonne anime
     * @returns {Array} Array di definizioni colonne
     */
    function getAnimeColumns() {
        // Ritorna una copia profonda per evitare modifiche accidentali
        return JSON.parse(JSON.stringify(animeColumns));
    }

    /**
     * Restituisce le colonne anime con opzioni personalizzate
     * @param {Object} options - Opzioni per personalizzare le colonne
     * @param {boolean} options.showAll - Se true, tutte le colonne sono visibili
     * @param {Array<string>} options.visible - Lista dei field da mostrare
     * @returns {Array} Array di definizioni colonne personalizzate
     */
    function getAnimeColumnsWithOptions(options = {}) {
        const columns = getAnimeColumns();
        
        if (options.showAll) {
            columns.forEach(col => col.hide = false);
        } else if (options.visible && Array.isArray(options.visible)) {
            columns.forEach(col => {
                col.hide = !options.visible.includes(col.field);
            });
        }
        
        return columns;
    }

    return {
        getAnimeColumns,
        getAnimeColumnsWithOptions,
        animeColumns // Esposto per reference
    };
})();

// Debug: Verifica caricamento modulo
console.log('[anime-columns-shared v1.45.2] Module loaded successfully with', window.animeColumnsShared.animeColumns.length, 'column definitions');
