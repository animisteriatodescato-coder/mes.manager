window.commesseAperteGrid = (function() {
    let gridApi = null;
        function safeApiCall(action) {
            if (!gridApi) return;
            setTimeout(() => {
                try {
                    action();
                } catch (err) {
                    console.warn('safeApiCall failed:', err);
                }
            }, 0);
        }

    let dotNetHelper = null;
    let currentUserId = null;

    function getStorageKey() {
        return currentUserId 
            ? `commesse-aperte-grid-columnState-${currentUserId}`
            : 'commesse-aperte-grid-columnState';
    }

    function setCurrentUser(userId) {
        currentUserId = userId;
        console.log('Grid user set to:', userId);
    }

    // Funzione per verificare se i dati etichetta sono completi
    function hasDatiEtichettaCompleti(data) {
        return data && 
               data.codiceAnime && 
               (data.clienteRagioneSociale || data.companyName); // Fallback intelligente
    }

    const columnDefs = [
        {
            field: 'stampaEtichetta',
            headerName: '',
            width: 50,
            pinned: 'left',
            sortable: false,
            filter: false,
            suppressMenu: true,
            cellRenderer: params => {
                const hasData = hasDatiEtichettaCompleti(params.data);
                const icon = hasData ? '🖨️' : '⚠️';
                const title = hasData ? 'Stampa Etichetta' : 'Dati incompleti - Clicca per dettagli';
                const color = hasData ? '#1976d2' : '#ff9800';
                return `<button class="print-label-btn" style="border:none;background:transparent;cursor:pointer;font-size:18px;color:${color}" title="${title}">${icon}</button>`;
            },
            onCellClicked: params => {
                if (dotNetHelper) {
                    dotNetHelper.invokeMethodAsync('OnPrintLabelClick', params.data);
                }
            }
        },
        {
            field: 'storico',
            headerName: '',
            width: 50,
            pinned: 'left',
            sortable: false,
            filter: false,
            suppressMenu: true,
            cellRenderer: params => {
                return `<button class="storico-btn" style="border:none;background:transparent;cursor:pointer;font-size:16px;color:#9c27b0" title="Visualizza Storico Programmazione">📋</button>`;
            },
            onCellClicked: params => {
                showStoricoProgrammazione(params.data.id, params.data.numeroCommessa);
            }
        },
        { field: 'codice', headerName: 'Codice', sortable: true, filter: true, width: 180, pinned: 'left' },
        { field: 'internalOrdNo', headerName: 'Num. Ordine', sortable: true, filter: true, width: 130 },
        { field: 'externalOrdNo', headerName: 'Ordine Esterno', sortable: true, filter: true, width: 150 },
        { field: 'line', headerName: 'Linea', sortable: true, filter: true, width: 80 },
        { 
            field: 'articoloCodice', 
            headerName: 'Cod. Articolo', 
            sortable: true, 
            filter: true, 
            width: 150 
        },
        // RICETTA - Badge con numero parametri (shared component)
        window.ricettaColumnShared.createColumnDef({
            fieldPrefix: 'camelCase',
            gridNamespace: 'commesseAperteGrid',
            codiceArticoloField: 'articoloCodice'
        }),
        { 
            field: 'description', 
            headerName: 'Descrizione', 
            sortable: true, 
            filter: true, 
            width: 300 
        },
        { 
            field: 'articoloPrezzo', 
            headerName: 'Prezzo €', 
            sortable: true, 
            filter: 'agNumberColumnFilter', 
            width: 120,
            type: 'numericColumn',
            valueFormatter: params => params.value != null ? '€ ' + params.value.toFixed(2) : ''
        },
        { 
            field: 'clienteDisplay', 
            headerName: 'Cliente', 
            sortable: true, 
            filter: true, 
            width: 250 
        },
        { 
            field: 'quantitaRichiesta', 
            headerName: 'Quantità', 
            sortable: true, 
            filter: true, 
            width: 100,
            type: 'numericColumn'
        },
        { field: 'uoM', headerName: 'U.M.', sortable: true, filter: true, width: 80 },
        { 
            field: 'dataConsegna', 
            headerName: 'Data Consegna', 
            sortable: true, 
            filter: true, 
            width: 120,
            valueFormatter: params => params.value ? new Date(params.value).toLocaleDateString('it-IT') : ''
        },
        { 
            field: 'stato', 
            headerName: 'Stato', 
            sortable: true, 
            filter: true, 
            width: 120,
            cellStyle: params => {
                if (params.value === 'Aperta') return { backgroundColor: '#e8f5e9', color: '#2e7d32' };
                if (params.value === 'Chiusa') return { backgroundColor: '#fce4ec', color: '#c2185b' };
                return null;
            }
        },
        { 
            field: 'statoProgramma', 
            headerName: 'Stato Programma', 
            sortable: true, 
            filter: true, 
            width: 180,
            cellRenderer: params => {
                const stati = [
                    { value: 'NonProgrammata', label: 'Non Programmata', color: '#9e9e9e', bg: '#f5f5f5' },
                    { value: 'Programmata', label: 'Programmata', color: '#1976d2', bg: '#e3f2fd' },
                    { value: 'InProduzione', label: 'In Produzione', color: '#ff9800', bg: '#fff3e0' },
                    { value: 'Completata', label: 'Completata', color: '#4caf50', bg: '#e8f5e9' },
                    { value: 'Archiviata', label: 'Archiviata', color: '#616161', bg: '#eeeeee' }
                ];
                
                const currentValue = params.value || 'NonProgrammata';
                const currentStato = stati.find(s => s.value === currentValue) || stati[0];
                
                // Se è archiviata, mostra pulsante Ripristina
                if (currentValue === 'Archiviata') {
                    return `<span style="display:flex; align-items:center; justify-content:space-between; width:100%; height:100%; background:${currentStato.bg}; padding: 0 5px;">
                        <span style="color:${currentStato.color}; font-weight:bold; font-size:inherit;">
                            ${currentStato.label}
                        </span>
                        <button class="btn-ripristina" 
                                style="background:#1976d2; color:white; border:none; border-radius:3px; 
                                       padding:2px 8px; cursor:pointer; font-size:11px; font-weight:bold;">
                            Ripristina
                        </button>
                    </span>`;
                }
                
                // Solo visualizzazione - lo stato si gestisce da Programma Macchine
                return `<span style="display:block; width:100%; height:100%; background:${currentStato.bg}; 
                           color:${currentStato.color}; font-weight:bold; text-align:center; 
                           line-height:28px; font-size:inherit;">
                    ${currentStato.label}
                </span>`;
            },
            cellStyle: { padding: 0 },
            onCellClicked: async params => {
                // Gestisce il click sul pulsante Ripristina
                if (params.event.target.classList.contains('btn-ripristina')) {
                    const commessaId = params.data.id;
                    
                    if (!confirm('Ripristinare questa commessa a stato "Programmata"?')) return;
                    
                    const btn = params.event.target;
                    try {
                        btn.disabled = true;
                        btn.textContent = '...';
                        
                        const response = await fetch(`/api/Commesse/${commessaId}/stato-programma`, {
                            method: 'PATCH',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify({ statoProgramma: 'Programmata' })
                        });
                        
                        if (!response.ok) {
                            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
                        }
                        
                        console.log(`✓ Commessa ${commessaId} ripristinata a Programmata`);
                        
                        // Ricarica i dati
                        await refreshGridData();
                        
                    } catch (err) {
                        console.error(`Error restoring commessa:`, err);
                        alert(`Errore durante il ripristino: ${err.message}`);
                        btn.disabled = false;
                        btn.textContent = 'Ripristina';
                    }
                }
            }
        },
        { field: 'riferimentoOrdineCliente', headerName: 'Rif. Cliente', sortable: true, filter: true, width: 150 },
        { field: 'ourReference', headerName: 'Ns. Riferimento', sortable: true, filter: true, width: 150 },
        { 
            field: 'ultimaModifica', 
            headerName: 'Ultima Modifica', 
            sortable: true, 
            filter: true, 
            width: 160,
            valueFormatter: params => params.value ? new Date(params.value).toLocaleString('it-IT') : ''
        },
        { 
            field: 'timestampSync', 
            headerName: 'Sync', 
            sortable: true, 
            filter: true, 
            width: 160,
            valueFormatter: params => params.value ? new Date(params.value).toLocaleString('it-IT') : ''
        },
        // Anime columns - importate da file condiviso (anime-columns-shared.js)
        // Le colonne vengono aggiunte dinamicamente in getColumnDefs()
    ];

    // Funzione per ottenere tutte le colonne incluse quelle anime condivise
    function getColumnDefs() {
        // Prende le colonne anime dal file condiviso se disponibile
        if (window.animeColumnsShared && window.animeColumnsShared.getAnimeColumns) {
            const animeColumns = window.animeColumnsShared.getAnimeColumns();
            console.log('[commesse-aperte-grid v1.45.2] getColumnDefs: Loaded', animeColumns.length, 'anime columns from shared module');
            return [...columnDefs, ...animeColumns];
        }
        // Fallback: usa colonne inline se il file condiviso non è caricato
        console.warn('[commesse-aperte-grid] anime-columns-shared.js NON caricato, uso colonne fallback');
        return columnDefs;
    }

    // Funzione per ricaricare i dati della griglia
    async function refreshGridData() {
        try {
            console.log('[commesse-aperte-grid v1.45.2] refreshGridData: Starting...');
            const response = await fetch('/api/Commesse');
            if (!response.ok) throw new Error('Failed to fetch');
            const allData = await response.json();
            
            // DEBUG: Log primi 2 record per verificare struttura dati
            if (allData && allData.length > 0) {
                console.log('[commesse-aperte-grid] Ricevuti', allData.length, 'record. Primo record:', allData[0]);
                console.log('[commesse-aperte-grid] Check campo hasRicetta:', allData[0].hasRicetta, 'numeroParametri:', allData[0].numeroParametri);
            }
            
            // Filtra solo commesse aperte, rispettando il toggle showArchived
            const showArchived = window.commesseAperteShowArchived || false;
            const filteredData = showArchived 
                ? allData.filter(c => c.stato === 'Aperta')
                : allData.filter(c => c.stato === 'Aperta' && c.statoProgramma !== 'Archiviata');
            
            safeApiCall(() => {
                gridApi.setGridOption('rowData', filteredData);
                console.log(`Grid refreshed with ${filteredData.length} rows (showArchived: ${showArchived})`);
            });
        } catch (err) {
            console.error('Error refreshing grid data:', err);
        }
    }

    // Funzione per mostrare lo storico programmazione in un dialog
    async function showStoricoProgrammazione(commessaId, numeroCommessa) {
        try {
            const response = await fetch(`/api/Commesse/${commessaId}/storico-programmazione`);
            if (!response.ok) throw new Error('Errore nel recupero dello storico');
            
            const storico = await response.json();
            
            // Crea il dialog overlay
            const overlay = document.createElement('div');
            overlay.id = 'storico-dialog-overlay';
            overlay.style.cssText = 'position:fixed;top:0;left:0;width:100%;height:100%;background:rgba(0,0,0,0.5);z-index:9999;display:flex;justify-content:center;align-items:center;';
            
            // Contenuto del dialog
            let tableRows = '';
            if (storico.length === 0) {
                tableRows = '<tr><td colspan="5" style="text-align:center;padding:20px;color:#666;">Nessuna modifica registrata</td></tr>';
            } else {
                storico.forEach(s => {
                    const data = new Date(s.dataModifica).toLocaleString('it-IT');
                    tableRows += `<tr>
                        <td style="padding:8px;border-bottom:1px solid #e0e0e0;">${data}</td>
                        <td style="padding:8px;border-bottom:1px solid #e0e0e0;">${s.statoPrecedente}</td>
                        <td style="padding:8px;border-bottom:1px solid #e0e0e0;">→</td>
                        <td style="padding:8px;border-bottom:1px solid #e0e0e0;">${s.statoNuovo}</td>
                        <td style="padding:8px;border-bottom:1px solid #e0e0e0;">${s.note || '-'}</td>
                    </tr>`;
                });
            }
            
            overlay.innerHTML = `
                <div style="background:white;border-radius:8px;max-width:700px;width:90%;max-height:80vh;overflow:auto;box-shadow:0 4px 20px rgba(0,0,0,0.3);">
                    <div style="padding:16px 20px;background:#9c27b0;color:white;border-radius:8px 8px 0 0;display:flex;justify-content:space-between;align-items:center;">
                        <h3 style="margin:0;font-size:18px;">📋 Storico Programmazione - ${numeroCommessa}</h3>
                        <button id="close-storico-btn" style="border:none;background:transparent;color:white;font-size:24px;cursor:pointer;padding:0;line-height:1;">&times;</button>
                    </div>
                    <div style="padding:20px;">
                        <table style="width:100%;border-collapse:collapse;">
                            <thead>
                                <tr style="background:#f5f5f5;">
                                    <th style="padding:10px;text-align:left;border-bottom:2px solid #9c27b0;">Data</th>
                                    <th style="padding:10px;text-align:left;border-bottom:2px solid #9c27b0;">Da</th>
                                    <th style="padding:10px;text-align:center;border-bottom:2px solid #9c27b0;width:30px;"></th>
                                    <th style="padding:10px;text-align:left;border-bottom:2px solid #9c27b0;">A</th>
                                    <th style="padding:10px;text-align:left;border-bottom:2px solid #9c27b0;">Note</th>
                                </tr>
                            </thead>
                            <tbody>
                                ${tableRows}
                            </tbody>
                        </table>
                    </div>
                </div>
            `;
            
            document.body.appendChild(overlay);
            
            // Gestione chiusura
            const closeBtn = document.getElementById('close-storico-btn');
            closeBtn.addEventListener('click', () => overlay.remove());
            overlay.addEventListener('click', (e) => {
                if (e.target === overlay) overlay.remove();
            });
            
        } catch (err) {
            console.error('Error loading storico:', err);
            alert('Errore nel caricamento dello storico: ' + err.message);
        }
    }

    function init(gridId, data, savedColumnState) {
        const gridDiv = document.getElementById(gridId);
        if (!gridDiv) {
            console.error('Grid element not found:', gridId);
            return;
        }
        
        // Prima usa lo stato passato da Blazor, poi fallback a localStorage
        const storageKey = getStorageKey();
        let stateToRestore = savedColumnState;
        if (!stateToRestore) {
            stateToRestore = localStorage.getItem(storageKey);
            console.log('Loading column state from localStorage:', storageKey, stateToRestore ? 'found' : 'not found');
        } else {
            console.log('Using column state from Blazor');
        }
        
        // Destroy existing grid if it exists to prevent duplication
        if (gridApi) {
            console.log('Destroying existing grid before reinitializing...');
            gridApi.destroy();
            gridApi = null;
            // Clear the grid container
            gridDiv.innerHTML = '';
        }

        const initialRowCount = Array.isArray(data) ? data.length : 0;

        console.log('Initializing commesse aperte grid with data:', data);
        console.log('Data length:', data ? data.length : 'null');
        if (data && data.length > 0) {
            console.log('First row sample:', data[0]);
        }

        const gridOptions = {
            columnDefs: getColumnDefs(),
            rowData: data,
            defaultColDef: {
                resizable: true,
                sortable: true,
                filter: true,
                suppressMenu: true
            },
            getRowStyle: params => {
                if (params.data && params.data.numeroMacchina != null && params.data.numeroMacchina !== '') {
                    return { backgroundColor: '#e8f5e9' }; // Verde chiaro per commesse assegnate a macchina
                }
                return null;
            },
            sideBar: {
                toolPanels: [
                    {
                        id: 'columns',
                        labelDefault: 'Columns',
                        labelKey: 'columns',
                        iconKey: 'columns',
                        toolPanel: 'agColumnsToolPanel',
                        toolPanelParams: {
                            suppressRowGroups: true,
                            suppressValues: true,
                            suppressPivots: true,
                            suppressPivotMode: true,
                            suppressColumnFilter: false,
                            suppressColumnSelectAll: false,
                            suppressColumnExpandAll: false
                        }
                    }
                ],
                defaultToolPanel: ''
            },
            headerHeight: 24,
            rowHeight: 28,
            animateRows: true,
            rowSelection: 'single',
            getRowId: (params) => params.data.id,
            onGridReady: (params) => {
                gridApi = params.api;
                console.log('Commesse Aperte Grid ready, rowData count:', gridApi.getDisplayedRowCount());
                
                // Ripristina stato colonne se disponibile
                if (stateToRestore) {
                    try {
                        const state = typeof stateToRestore === 'string' ? JSON.parse(stateToRestore) : stateToRestore;
                        setTimeout(() => {
                            safeApiCall(() => {
                                gridApi.applyColumnState({
                                    state: state,
                                    applyOrder: true
                                });
                                console.log('✓ Column state restored');
                            });
                        }, 0);
                    } catch (e) {
                        console.warn('Failed to restore column state:', e);
                    }
                }

                if (initialRowCount > 0 && gridApi.getDisplayedRowCount() === 0) {
                    console.warn('Grid has data but shows 0 rows. Clearing filters/quick filter.');
                    gridApi.setFilterModel(null);
                    gridApi.setGridOption('quickFilterText', '');
                    gridApi.onFilterChanged();
                }
                
            },
            onColumnMoved: (params) => {
                if (params.finished) saveColumnState();
                notifyBlazorStateChanged();
            },
            onColumnResized: (params) => {
                if (params.finished) saveColumnState();
                notifyBlazorStateChanged();
            },
            onColumnVisible: () => {
                saveColumnState();
                notifyBlazorStateChanged();
            },
            onColumnPinned: () => {
                saveColumnState();
                notifyBlazorStateChanged();
            },
            onSortChanged: () => {
                saveColumnState();
                notifyBlazorStateChanged();
            },
            onSelectionChanged: () => {
                window.dispatchEvent(new CustomEvent('commesseAperteGridStatsChanged'));
            },
            onFilterChanged: () => {
                window.dispatchEvent(new CustomEvent('commesseAperteGridStatsChanged'));
            },
            onModelUpdated: () => {
                window.dispatchEvent(new CustomEvent('commesseAperteGridStatsChanged'));
            },
            onRowDoubleClicked: (event) => {
                // Skip navigation if double-clicking on machine select or special columns
                const colId = event.column?.getColId?.() || event.colDef?.field;
                if (colId === 'numeroMacchina' || colId === 'stampaEtichetta' || colId === 'storico') {
                    return;
                }
                // Doppio click: apri dialog modifica anima
                if (dotNetHelper && event.data && event.data.articoloCodice) {
                    console.log('Double click: opening anima edit for', event.data.articoloCodice);
                    dotNetHelper.invokeMethodAsync('OnRowDoubleClick', event.data.articoloCodice);
                }
            },
            onCellValueChanged: async (event) => {
                // Gestisci cambio macchina
                if (event.colDef.field === 'numeroMacchina') {
                    if (event.oldValue === event.newValue) return;
                    
                    const commessaId = event.data.id;
                    const numeroMacchina = event.newValue || null;
                    
                    console.log(`Assegnazione macchina: ${commessaId} -> ${numeroMacchina}`);
                    
                    try {
                        // Usa API pianificazione per calcolare date automaticamente
                        // Estrae solo i numeri: "01" -> 1, "M001" -> 1, "02" -> 2
                        const machineNumber = numeroMacchina ? parseInt(numeroMacchina.replace(/\D/g, '')) : null;
                        console.log('[v1.23] machineNumber estratto:', machineNumber, 'from:', numeroMacchina);
                        
                        const response = await fetch('/api/pianificazione/sposta-commessa', {
                            method: 'POST',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify({
                                commessaId: commessaId,
                                targetMacchina: machineNumber,
                                insertBeforeCommessaId: null,
                                targetDataInizio: null
                            })
                        });
                        
                        if (!response.ok) {
                            const errorText = await response.text();
                            throw new Error(errorText || `HTTP ${response.status}`);
                        }
                        
                        console.log('✓ Commessa pianificata con calcolo automatico date');
                        
                        // Ricarica i dati per mostrare le date calcolate
                        await refreshGridData();
                        
                    } catch (err) {
                        console.error('Errore pianificazione:', err);
                        alert(`Errore nella pianificazione: ${err.message}`);
                        // Ripristina valore precedente
                        event.node.setDataValue('numeroMacchina', event.oldValue);
                    }
                }
            }
        };

        agGrid.createGrid(gridDiv, gridOptions);
    }

    function saveColumnState() {
        if (gridApi) {
            const columnState = gridApi.getColumnState();
            const storageKey = getStorageKey();
            localStorage.setItem(storageKey, JSON.stringify(columnState));
            console.log('Column state saved to localStorage:', storageKey);
        }
    }

    // Debounced notification to Blazor for DB persistence (avoids flooding)
    let _blazorStateTimer = null;
    function notifyBlazorStateChanged() {
        window.dispatchEvent(new CustomEvent('commesseAperteGridStateChanged'));
        if (_blazorStateTimer) clearTimeout(_blazorStateTimer);
        _blazorStateTimer = setTimeout(() => {
            if (dotNetHelper) {
                dotNetHelper.invokeMethodAsync('SaveGridStateFromJs').catch(() => {});
            }
        }, 1000); // Debounce 1 second
    }

    function setDotNetHelper(helper) {
        dotNetHelper = helper;
    }

    function setQuickFilter(searchText) {
        safeApiCall(() => {
            gridApi.setGridOption('quickFilterText', searchText);
        });
    }

    function setColumnVisible(field, visible) {
        if (gridApi) {
            gridApi.setColumnsVisible([field], visible);
        }
    }

    function getAllColumns() {
        if (!gridApi) return [];
        
        const columns = gridApi.getColumns();
        return columns.map(col => ({
            Field: col.getColDef().field,
            HeaderName: col.getColDef().headerName,
            Visible: col.isVisible()
        }));
    }

    function getState() {
        if (!gridApi) return null;
        return JSON.stringify(gridApi.getColumnState());
    }

    function setState(stateJson) {
        if (!gridApi || !stateJson) return;
        try {
            const state = JSON.parse(stateJson);
            safeApiCall(() => {
                gridApi.applyColumnState({ state: state, applyOrder: true });
                console.log('setState: applied successfully');
            });
        } catch (e) {
            console.error('setState: error parsing state', e);
        }
    }

    function resetState() {
        if (gridApi) {
            gridApi.resetColumnState();
            gridApi.setFilterModel(null);
            const storageKey = getStorageKey();
            localStorage.removeItem(storageKey);
            console.log('Column state reset and cleared from localStorage:', storageKey);
        }
    }

    function setUiVars(fontSize, rowHeight, densityPadding, zebra, gridLines) {
        const gridDiv = document.querySelector('.ag-theme-alpine');
        if (gridDiv) {
            gridDiv.style.setProperty('--ag-font-size', fontSize + 'px');
            gridDiv.style.setProperty('--ag-row-height', rowHeight + 'px');
            gridDiv.style.setProperty('--ag-cell-horizontal-padding', densityPadding);
            
            if (zebra) {
                gridDiv.style.setProperty('--ag-odd-row-background-color', '#f9f9f9');
            } else {
                gridDiv.style.setProperty('--ag-odd-row-background-color', 'transparent');
            }
            
            if (gridLines) {
                gridDiv.style.setProperty('--ag-row-border-width', '1px');
                gridDiv.style.setProperty('--ag-row-border-color', '#ddd');
            } else {
                gridDiv.style.setProperty('--ag-row-border-width', '0px');
            }
        }
    }

    function exportCsv() {
        if (gridApi) {
            gridApi.exportDataAsCsv({
                fileName: 'commesse_aperte_export.csv',
                columnSeparator: ';'
            });
        }
    }

    function getStats() {
        if (!gridApi) return { total: 0, filtered: 0, selected: 0 };
        
        return {
            total: gridApi.getModel().getRowCount(),
            filtered: gridApi.getDisplayedRowCount(),
            selected: gridApi.getSelectedRows().length
        };
    }

    function getSelectedRows() {
        if (!gridApi) return [];
        return gridApi.getSelectedRows() || [];
    }

    function toggleColumnPanel() {
        if (gridApi) {
            gridApi.openToolPanel('columns');
        }
    }

    function openRicetta(codiceArticolo) {
        window.ricettaColumnShared.openRicettaDialog(
            codiceArticolo, 
            dotNetHelper, 
            'commesseAperteGrid'
        );
    }
    
    function reinit(jsonData) {
        console.log('Reinitializing grid with fresh data...');
        const data = JSON.parse(jsonData);
        const savedState = getState();
        init('commesseAperteGrid', data, savedState);
    }
    
    function updateRowData(jsonData) {
        console.log('Updating grid row data...');
        const data = JSON.parse(jsonData);
        safeApiCall(() => {
            gridApi.setRowData(data);
        });
    }

    return {
        init: init,
        reinit: reinit,
        updateRowData: updateRowData,
        setDotNetHelper: setDotNetHelper,
        setCurrentUser: setCurrentUser,
        setQuickFilter: setQuickFilter,
        setColumnVisible: setColumnVisible,
        getAllColumns: getAllColumns,
        getState: getState,
        setState: setState,
        resetState: resetState,
        setUiVars: setUiVars,
        exportCsv: exportCsv,
        getStats: getStats,
        getSelectedRows: getSelectedRows,
        toggleColumnPanel: toggleColumnPanel,
        openRicetta: openRicetta
    };
})();
