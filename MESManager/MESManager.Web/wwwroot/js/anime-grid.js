window.animeGrid = (function() {
    let gridApi = null;
    let isGridInitialized = false;
    let currentUserId = null;

    function getStorageKey() {
        return currentUserId 
            ? `anime-grid-columnState-${currentUserId}`
            : 'anime-grid-columnState';
    }

    function setCurrentUser(userId) {
        currentUserId = userId;
        console.log('Anime grid user set to:', userId);
    }

    const columnDefs = [
        // READONLY - da sync commesse
        { field: 'id', headerName: 'ID', sortable: true, filter: true, width: 80, editable: false },
        { field: 'codiceArticolo', headerName: 'Codice', sortable: true, filter: true, width: 150, editable: false, cellStyle: {backgroundColor: '#f5f5f5'} },
        
        // RICETTA - Badge con numero parametri
        { 
            field: 'hasRicetta', 
            headerName: 'Ricetta', 
            sortable: true, 
            filter: true, 
            width: 100,
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
                    
                    return `<div style="display: flex; align-items: center; height: 100%; cursor: pointer;" 
                                 onclick="window.animeGrid.openRicetta('${params.data.codiceArticolo}')" 
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
        
        { field: 'descrizioneArticolo', headerName: 'Descrizione Articolo', sortable: true, filter: true, width: 250, editable: false, cellStyle: {backgroundColor: '#f5f5f5'} },
        { field: 'cliente', headerName: 'Cliente', sortable: true, filter: true, width: 150, editable: false, cellStyle: {backgroundColor: '#f5f5f5'} },
        
        // EDITABLE - campi modificabili
        { field: 'ubicazione', headerName: 'Ubicazione', sortable: true, filter: true, width: 150, editable: true },
        { field: 'codiceCassa', headerName: 'Codice Cassa', sortable: true, filter: true, width: 120, editable: true },
        { field: 'codiceAnime', headerName: 'Codice Anima', sortable: true, filter: true, width: 120, editable: true },
        { field: 'unitaMisura', headerName: 'Unità Misura', sortable: true, filter: true, width: 100, editable: true },
        
        // Imballo - mostra descrizione
        { field: 'imballoDescrizione', headerName: 'Imballo', sortable: true, filter: true, width: 150, editable: false },
        
        { field: 'note', headerName: 'Note', sortable: true, filter: true, width: 200, editable: true, cellEditor: 'agLargeTextCellEditor' },
        
        // Macchine - mostra descrizione (nomi macchine)
        { field: 'macchineSuDisponibiliDescrizione', headerName: 'Macchine', sortable: true, filter: true, width: 150, editable: false },
        
        // Sabbia - mostra descrizione
        { field: 'sabbiaDescrizione', headerName: 'Sabbia', sortable: true, filter: true, width: 120, editable: false },
        
        { field: 'togliereSparo', headerName: 'Tog.Sparo', sortable: true, filter: true, width: 100, editable: false, valueFormatter: params => params.value === '1' ? 'Sì' : (params.value === '0' ? 'No' : '') },
        
        // Vernice - mostra descrizione
        { field: 'verniceDescrizione', headerName: 'Vernice', sortable: true, filter: true, width: 150, editable: false },
        
        // Colla - mostra descrizione
        { field: 'collaDescrizione', headerName: 'Colla', sortable: true, filter: true, width: 120, editable: false },
        
        { field: 'quantitaPiano', headerName: 'Qta Piano', sortable: true, filter: true, width: 100, editable: true, cellEditor: 'agNumberCellEditor' },
        { field: 'numeroPiani', headerName: 'N.Piani', sortable: true, filter: true, width: 90, editable: true, cellEditor: 'agNumberCellEditor' },
        { field: 'ciclo', headerName: 'Ciclo', sortable: true, filter: true, width: 150, editable: true },
        { field: 'peso', headerName: 'Peso', sortable: true, filter: true, width: 100, editable: true },
        { field: 'figure', headerName: 'Figure', sortable: true, filter: true, width: 120, editable: true },
        { field: 'maschere', headerName: 'Maschere', sortable: true, filter: true, width: 120, editable: true },
        { field: 'assemblata', headerName: 'Assemblata', sortable: true, filter: true, width: 120, editable: true },
        { field: 'armataL', headerName: 'Armata L', sortable: true, filter: true, width: 120, editable: true },
        { field: 'larghezza', headerName: 'Larghezza', sortable: true, filter: true, width: 100, editable: true, cellEditor: 'agNumberCellEditor' },
        { field: 'altezza', headerName: 'Altezza', sortable: true, filter: true, width: 100, editable: true, cellEditor: 'agNumberCellEditor' },
        { field: 'profondita', headerName: 'Profondità', sortable: true, filter: true, width: 100, editable: true, cellEditor: 'agNumberCellEditor' },
        
        { field: 'idArticolo', headerName: 'ID Articolo', sortable: true, filter: true, width: 100, editable: false },
        { 
            field: 'trasmettiTutto', 
            headerName: 'Trasmetti Tutto', 
            sortable: true, 
            filter: true, 
            width: 130,
            valueFormatter: params => params.value ? 'Sì' : 'No',
            editable: false
        },
        { field: 'allegato', headerName: 'Allegato', sortable: true, filter: true, width: 150, editable: true },
        { 
            field: 'numeroFoto', 
            headerName: 'N.Foto', 
            sortable: true, 
            filter: true, 
            width: 90,
            editable: false,
            cellStyle: params => params.value > 0 ? { backgroundColor: '#e8f5e9', fontWeight: 'bold' } : null,
            valueFormatter: params => params.value || 0
        },
        { 
            field: 'numeroDocumenti', 
            headerName: 'N.Doc', 
            sortable: true, 
            filter: true, 
            width: 90,
            editable: false,
            cellStyle: params => params.value > 0 ? { backgroundColor: '#e3f2fd', fontWeight: 'bold' } : null,
            valueFormatter: params => params.value || 0
        },
        { 
            field: 'dataModificaRecord', 
            headerName: 'Data Modifica', 
            sortable: true, 
            filter: true, 
            width: 150,
            valueFormatter: params => params.value ? new Date(params.value).toLocaleDateString('it-IT') : '',
            editable: false
        },
        { 
            field: 'utenteModificaRecord', 
            headerName: 'Utente Modifica', 
            sortable: true, 
            filter: true, 
            width: 150,
            editable: false
        },
        { 
            field: 'dataImportazione', 
            headerName: 'Data Importazione', 
            sortable: true, 
            filter: true, 
            width: 180,
            valueFormatter: params => params.value ? new Date(params.value).toLocaleString('it-IT') : '',
            editable: false
        }
    ];

    async function init(gridId, data, savedColumnState) {
        const gridDiv = document.getElementById(gridId);
        if (!gridDiv) {
            console.error('Grid div not found:', gridId);
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

        // Distruggi la griglia esistente se presente
        if (gridApi) {
            try {
                gridApi.destroy();
            } catch (e) {
                console.warn('Error destroying grid:', e);
            }
            gridApi = null;
            isGridInitialized = false;
        }

        console.log('Initializing anime grid with data:', data);
        console.log('Data length:', data ? data.length : 'null');
        console.log('[DEBUG v1.38] ColumnDefs count:', columnDefs.length);
        console.log('[DEBUG v1.38] ColumnDefs fields:', columnDefs.map(c => c.field));

        const gridOptions = {
            columnDefs: columnDefs,
            rowData: data,
            defaultColDef: {
                resizable: true,
                sortable: true,
                filter: true,
                suppressMenu: true
            },
            headerHeight: 24,
            rowHeight: 28,
            animateRows: true,
            rowSelection: 'single',
            onGridReady: (params) => {
                gridApi = params.api;
                isGridInitialized = true;
                console.log('Grid ready, rowData count:', gridApi.getDisplayedRowCount());
                
                // Ripristina stato colonne se disponibile
                if (stateToRestore) {
                    try {
                        const state = typeof stateToRestore === 'string' ? JSON.parse(stateToRestore) : stateToRestore;
                        gridApi.applyColumnState({
                            state: state,
                            applyOrder: true
                        });
                        console.log('✓ Column state restored');
                    } catch (e) {
                        console.warn('Failed to restore column state:', e);
                    }
                }
            },
            onColumnVisible: () => saveColumnState(),
            onColumnResized: (params) => {
                if (params.finished) saveColumnState();
            },
            onColumnMoved: (params) => {
                if (params.finished) saveColumnState();
            },
            onColumnPinned: () => saveColumnState(),
            onSortChanged: () => saveColumnState(),
            onSelectionChanged: () => {
                window.dispatchEvent(new CustomEvent('animeGridStatsChanged'));
            },
            onFilterChanged: () => {
                window.dispatchEvent(new CustomEvent('animeGridStatsChanged'));
            },
            onModelUpdated: () => {
                window.dispatchEvent(new CustomEvent('animeGridStatsChanged'));
            },
            onCellValueChanged: async (event) => {
                // Salva automaticamente modifica su API
                console.log('Cell value changed:', event.colDef.field, '=', event.newValue);
                
                try {
                    const response = await fetch(`/api/Anime/${event.data.id}`, {
                        method: 'PUT',
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        body: JSON.stringify(event.data)
                    });
                    
                    if (response.ok) {
                        console.log('✓ Anime updated successfully');
                    } else {
                        console.error('Failed to update anime:', response.statusText);
                    }
                } catch (err) {
                    console.error('Error updating anime:', err);
                }
            },
            onRowDoubleClicked: (event) => {
                console.log('Row double-clicked:', event.data);
                // Invoca il callback Blazor per aprire il dialog di modifica
                if (window.animeGridDotNetRef) {
                    window.animeGridDotNetRef.invokeMethodAsync('OnRowDoubleClicked', event.data);
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

    function setQuickFilter(searchText) {
        if (gridApi) {
            gridApi.setGridOption('quickFilterText', searchText);
        }
    }

    function isInitialized() {
        return isGridInitialized;
    }

    function updateData(data, selectedId) {
        if (gridApi) {
            console.log('Updating grid data, selectedId:', selectedId);
            
            // Se è specificato un ID, prova ad aggiornare solo quella riga
            if (selectedId) {
                let rowUpdated = false;
                gridApi.forEachNode(node => {
                    if (node.data && node.data.id === selectedId) {
                        // Trova i dati aggiornati nell'array
                        const updatedData = data.find(item => item.id === selectedId);
                        if (updatedData) {
                            console.log('Updating single row with data:', updatedData);
                            // Aggiorna i dati del nodo
                            node.setData(updatedData);
                            // Seleziona e rendi visibile
                            node.setSelected(true);
                            gridApi.ensureNodeVisible(node, 'middle');
                            // IMPORTANTE: Forza il refresh completo della riga
                            gridApi.refreshCells({ 
                                rowNodes: [node], 
                                force: true,
                                suppressFlash: false 
                            });
                            // Trigger anche il flash per evidenziare la modifica
                            gridApi.flashCells({ rowNodes: [node] });
                            rowUpdated = true;
                            console.log('✓ Row updated successfully');
                        }
                    }
                });
                
                // Se non è riuscito ad aggiornare la singola riga, aggiorna tutta la griglia
                if (!rowUpdated) {
                    console.log('Row not found, updating all data');
                    gridApi.setGridOption('rowData', data);
                }
            } else {
                // Nessun ID specificato, aggiorna tutta la griglia
                console.log('No selectedId, updating all grid data');
                gridApi.setGridOption('rowData', data);
            }
        }
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
            gridApi.applyColumnState({ state: state, applyOrder: true });
            console.log('setState: applied successfully');
        } catch (e) {
            console.error('setState: error parsing state', e);
        }
    }

    function resetState() {
        if (gridApi) {
            // Mostra tutte le colonne
            const allColumns = gridApi.getColumns();
            const allColIds = allColumns.map(col => col.getColId());
            gridApi.setColumnsVisible(allColIds, true);
            
            // Reset dello stato
            gridApi.resetColumnState();
            gridApi.setFilterModel(null);
            
            // Rimuovi stato salvato da localStorage
            const storageKey = getStorageKey();
            localStorage.removeItem(storageKey);
            console.log('Column state reset and cleared from localStorage:', storageKey);
        }
    }

    function toggleColumnPanel() {
        console.log('toggleColumnPanel called');
        if (!gridApi || !isGridInitialized) {
            console.error('toggleColumnPanel: gridApi is null or not initialized!');
            return;
        }
        
        try {
            gridApi.getColumnState();
        } catch (e) {
            console.error('toggleColumnPanel: gridApi is destroyed or invalid', e);
            return;
        }
        
        console.log('toggleColumnPanel: gridApi is valid, proceeding...');

        // Rimuovi overlay esistente se presente
        let existingOverlay = document.getElementById('columnSelectorOverlay');
        if (existingOverlay) {
            console.log('toggleColumnPanel: removing existing overlay');
            existingOverlay.remove();
            return;
        }

        // Crea overlay
        const overlay = document.createElement('div');
        overlay.id = 'columnSelectorOverlay';
        overlay.style.cssText = `
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background: rgba(0,0,0,0.5);
            z-index: 10000;
            display: flex;
            align-items: center;
            justify-content: center;
        `;

        // Crea pannello
        const isDarkMode = document.body.classList.contains('mud-theme-dark') || 
                          document.documentElement.getAttribute('data-mud-theme') === 'dark';
        const panel = document.createElement('div');
        panel.style.cssText = `
            background: ${isDarkMode ? '#1e1e1e' : 'white'};
            color: ${isDarkMode ? '#e0e0e0' : '#333'};
            border-radius: 8px;
            padding: 20px;
            max-width: 400px;
            max-height: 80vh;
            overflow-y: auto;
            box-shadow: 0 4px 20px rgba(0,0,0,0.3);
        `;

        // Titolo
        const title = document.createElement('h3');
        title.textContent = 'Gestione Colonne';
        title.style.cssText = `margin: 0 0 15px 0; font-size: 18px; color: ${isDarkMode ? '#e0e0e0' : '#333'};`;
        panel.appendChild(title);

        // Ottieni lo stato delle colonne
        const columnState = gridApi.getColumnState();

        // Crea checkbox per ogni colonna
        columnState.forEach(col => {
            const div = document.createElement('div');
            div.style.cssText = 'margin-bottom: 10px; display: flex; align-items: center;';

            const checkbox = document.createElement('input');
            checkbox.type = 'checkbox';
            checkbox.id = `col_${col.colId}`;
            checkbox.checked = !col.hide;
            checkbox.style.cssText = 'margin-right: 10px; width: 18px; height: 18px; cursor: pointer;';

            const label = document.createElement('label');
            label.htmlFor = `col_${col.colId}`;
            label.textContent = col.colId;
            label.style.cssText = `cursor: pointer; font-size: 14px; user-select: none; color: ${isDarkMode ? '#e0e0e0' : '#333'};`;

            checkbox.addEventListener('change', (e) => {
                gridApi.setColumnsVisible([col.colId], e.target.checked);
            });

            div.appendChild(checkbox);
            div.appendChild(label);
            panel.appendChild(div);
        });

        // Pulsanti
        const buttonContainer = document.createElement('div');
        buttonContainer.style.cssText = 'margin-top: 20px; display: flex; gap: 10px; justify-content: flex-end;';

        const selectAllBtn = document.createElement('button');
        selectAllBtn.textContent = 'Seleziona Tutto';
        selectAllBtn.style.cssText = 'padding: 8px 16px; border: 1px solid #ccc; background: #f5f5f5; border-radius: 4px; cursor: pointer;';
        selectAllBtn.addEventListener('click', () => {
            const allColIds = columnState.map(c => c.colId);
            gridApi.setColumnsVisible(allColIds, true);
            columnState.forEach(col => {
                const cb = document.getElementById(`col_${col.colId}`);
                if (cb) cb.checked = true;
            });
        });

        const deselectAllBtn = document.createElement('button');
        deselectAllBtn.textContent = 'Deseleziona Tutto';
        deselectAllBtn.style.cssText = 'padding: 8px 16px; border: 1px solid #ccc; background: #f5f5f5; border-radius: 4px; cursor: pointer;';
        deselectAllBtn.addEventListener('click', () => {
            const allColIds = columnState.map(c => c.colId);
            gridApi.setColumnsVisible(allColIds, false);
            columnState.forEach(col => {
                const cb = document.getElementById(`col_${col.colId}`);
                if (cb) cb.checked = false;
            });
        });

        const closeBtn = document.createElement('button');
        closeBtn.textContent = 'Chiudi';
        closeBtn.style.cssText = 'padding: 8px 16px; border: 1px solid #ccc; background: #2196f3; color: white; border-radius: 4px; cursor: pointer;';
        closeBtn.addEventListener('click', () => {
            overlay.remove();
        });

        buttonContainer.appendChild(selectAllBtn);
        buttonContainer.appendChild(deselectAllBtn);
        buttonContainer.appendChild(closeBtn);
        panel.appendChild(buttonContainer);

        overlay.appendChild(panel);
        document.body.appendChild(overlay);

        overlay.addEventListener('click', (e) => {
            if (e.target === overlay) {
                overlay.remove();
            }
        });
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
                fileName: 'anime_export.csv',
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

    function registerDotNetRef(dotNetRef) {
        window.animeGridDotNetRef = dotNetRef;
        console.log('DotNet reference registered for anime grid');
    }

    function getSelectedRow() {
        if (gridApi) {
            const selectedRows = gridApi.getSelectedRows();
            return selectedRows.length > 0 ? selectedRows[0] : null;
        }
        return null;
    }

    function openRicetta(codiceArticolo) {
        if (window.animeGridDotNetRef) {
            window.animeGridDotNetRef.invokeMethodAsync('ViewRicetta', codiceArticolo);
        } else {
            console.error('DotNet reference not registered for anime grid');
        }
    }

    return {
        init: init,
        setQuickFilter: setQuickFilter,
        isInitialized: isInitialized,
        updateData: updateData,
        setColumnVisible: setColumnVisible,
        getAllColumns: getAllColumns,
        getState: getState,
        setState: setState,
        resetState: resetState,
        toggleColumnPanel: toggleColumnPanel,
        setUiVars: setUiVars,
        exportCsv: exportCsv,
        getStats: getStats,
        registerDotNetRef: registerDotNetRef,
        getSelectedRow: getSelectedRow,
        setCurrentUser: setCurrentUser,
        openRicetta: openRicetta
    };
})();
