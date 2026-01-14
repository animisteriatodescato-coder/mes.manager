window.commesseGrid = (function () {
    let gridApi = null;

    function init(containerId, rowData, savedColumnState) {
        const container = document.getElementById(containerId);
        if (!container) {
            console.error('Container not found:', containerId);
            return;
        }

        const columnDefs = [
            { 
                field: 'codice', 
                headerName: 'Codice', 
                pinned: 'left',
                width: 150,
                sortable: true, 
                filter: true, 
                resizable: true,
                checkboxSelection: true
            },
            { 
                field: 'clienteRagioneSociale', 
                headerName: 'Ragione Sociale', 
                width: 250,
                sortable: true, 
                filter: true, 
                resizable: true 
            },
            { 
                field: 'articoloCodice', 
                headerName: 'Cod. Articolo', 
                width: 150,
                sortable: true, 
                filter: true, 
                resizable: true 
            },
            { 
                field: 'articoloDescrizione', 
                headerName: 'Descrizione Articolo', 
                width: 300,
                sortable: true, 
                filter: true, 
                resizable: true 
            },
            { 
                field: 'articoloId', 
                headerName: 'ArticoloId', 
                width: 280,
                sortable: true, 
                filter: true, 
                resizable: true,
                hide: true // Nascosto per default
            },
            { 
                field: 'clienteId', 
                headerName: 'ClienteId', 
                width: 280,
                sortable: true, 
                filter: true, 
                resizable: true,
                hide: true // Nascosto per default
            },
            { 
                field: 'quantitaRichiesta', 
                headerName: 'Quantità', 
                width: 120,
                sortable: true, 
                filter: 'agNumberColumnFilter', 
                resizable: true,
                type: 'numericColumn',
                valueFormatter: params => params.value ? params.value.toFixed(0) : ''
            },
            { 
                field: 'dataConsegna', 
                headerName: 'Data Consegna', 
                width: 150,
                sortable: true, 
                filter: 'agDateColumnFilter', 
                resizable: true,
                valueFormatter: params => params.value ? new Date(params.value).toLocaleDateString('it-IT') : ''
            },
            { 
                field: 'stato', 
                headerName: 'Stato', 
                width: 150,
                sortable: true, 
                filter: true, 
                resizable: true,
                editable: true,
                cellEditor: 'agSelectCellEditor',
                cellEditorParams: {
                    values: ['Aperta', 'InLavorazione', 'Completata', 'Chiusa']
                },
                valueFormatter: params => {
                    if (!params.value) return '';
                    if (params.value === 'InLavorazione') return 'In Lavorazione';
                    return params.value;
                },
                cellStyle: params => {
                    if (params.value === 'Completata') return { backgroundColor: '#4caf50', color: 'white', fontWeight: 'bold' };
                    if (params.value === 'InLavorazione') return { backgroundColor: '#ff9800', color: 'white', fontWeight: 'bold' };
                    if (params.value === 'Aperta') return { backgroundColor: '#2196f3', color: 'white', fontWeight: 'bold' };
                    if (params.value === 'Chiusa') return { backgroundColor: '#757575', color: 'white', fontWeight: 'bold' };
                    return null;
                }
            },
            { 
                field: 'riferimentoOrdineCliente', 
                headerName: 'Rif. Ordine Cliente', 
                width: 200,
                sortable: true, 
                filter: true, 
                resizable: true 
            },
            { 
                field: 'ultimaModifica', 
                headerName: 'Ultima Modifica', 
                width: 180,
                sortable: true, 
                filter: 'agDateColumnFilter', 
                resizable: true,
                valueFormatter: params => params.value ? new Date(params.value).toLocaleString('it-IT') : ''
            },
            { 
                field: 'timestampSync', 
                headerName: 'Sync', 
                width: 180,
                sortable: true, 
                filter: 'agDateColumnFilter', 
                resizable: true,
                valueFormatter: params => params.value ? new Date(params.value).toLocaleString('it-IT') : ''
            },
            { 
                field: 'id', 
                headerName: 'ID', 
                width: 280,
                sortable: true, 
                filter: true, 
                resizable: true,
                hide: true // Nascosto per default
            }
        ];

        const gridOptions = {
            columnDefs: columnDefs,
            rowData: rowData,
            defaultColDef: {
                sortable: true,
                filter: true,
                resizable: true
            },
            enableRangeSelection: true,
            enableCellTextSelection: true,
            rowSelection: 'multiple',
            suppressRowClickSelection: true,
            animateRows: true,
            pagination: false,
            onGridReady: (params) => {
                gridApi = params.api;
                console.log('Grid initialized successfully');
                
                if (savedColumnState) {
                    try {
                        const state = JSON.parse(savedColumnState);
                        params.api.applyColumnState({ state: state, applyOrder: true });
                    } catch (e) {
                        console.error('Error restoring column state:', e);
                    }
                }
            },
            onColumnMoved: saveState,
            onColumnResized: saveState,
            onColumnVisible: saveState,
            onSortChanged: saveState,
            onFilterChanged: saveState,
            onCellValueChanged: (event) => {
                if (event.column.colId === 'stato') {
                    updateStatoCommessa(event.data.id, event.newValue);
                }
            }
        };

        const gridDiv = new agGrid.Grid(container, gridOptions);
        console.log('AG Grid created:', gridDiv);
    }

    function saveState() {
        // State saving is handled by Blazor component
    }

    function updateStatoCommessa(id, nuovoStato) {
        fetch(`/api/Commesse/${id}/stato`, {
            method: 'PATCH',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ stato: nuovoStato })
        })
        .then(response => {
            if (!response.ok) {
                throw new Error('Errore aggiornamento stato');
            }
            console.log('Stato aggiornato con successo');
        })
        .catch(error => {
            console.error('Errore:', error);
            alert('Errore durante l\'aggiornamento dello stato');
        });
    }

    function setQuickFilter(text) {
        console.log('setQuickFilter called with:', text);
        if (gridApi) {
            gridApi.setGridOption('quickFilterText', text);
            console.log('Quick filter applied');
        } else {
            console.error('gridApi is null');
        }
    }

    function toggleColumnPanel() {
        console.log('toggleColumnPanel called');
        if (!gridApi) return;

        // Rimuovi overlay esistente se presente
        let existingOverlay = document.getElementById('columnSelectorOverlay');
        if (existingOverlay) {
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
        const panel = document.createElement('div');
        panel.style.cssText = `
            background: white;
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
        title.style.cssText = 'margin: 0 0 15px 0; font-size: 18px; color: #333;';
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
            label.style.cssText = 'cursor: pointer; font-size: 14px; user-select: none;';

            // Gestisci il cambio di visibilità
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
        closeBtn.style.cssText = 'padding: 8px 16px; border: 1px solid #1976d2; background: #1976d2; color: white; border-radius: 4px; cursor: pointer;';
        closeBtn.addEventListener('click', () => {
            overlay.remove();
        });

        buttonContainer.appendChild(selectAllBtn);
        buttonContainer.appendChild(deselectAllBtn);
        buttonContainer.appendChild(closeBtn);
        panel.appendChild(buttonContainer);

        overlay.appendChild(panel);

        // Chiudi cliccando fuori dal pannello
        overlay.addEventListener('click', (e) => {
            if (e.target === overlay) {
                overlay.remove();
            }
        });

        document.body.appendChild(overlay);
    }

    function resetState() {
        if (gridApi) {
            gridApi.setFilterModel(null);
            gridApi.setSortModel(null);
            gridApi.setQuickFilter('');
            gridApi.resetColumnState();
        }
    }

    function getState() {
        if (gridApi) {
            const state = gridApi.getColumnState();
            return JSON.stringify(state);
        }
        return null;
    }

    function setState(stateJson) {
        if (gridApi && stateJson) {
            try {
                const state = JSON.parse(stateJson);
                gridApi.applyColumnState({ state: state, applyOrder: true });
            } catch (e) {
                console.error('Error applying state:', e);
            }
        }
    }

    function setUiVars(fontSize, rowHeight, densityPadding, zebra, gridLines) {
        const container = document.getElementById('commesseGrid');
        if (container) {
            container.style.setProperty('--ag-font-size', fontSize + 'px');
            container.style.setProperty('--ag-row-height', rowHeight + 'px');
            container.style.setProperty('--ag-cell-horizontal-padding', densityPadding);
            
            if (zebra) {
                container.style.setProperty('--ag-odd-row-background-color', '#f9f9f9');
            } else {
                container.style.setProperty('--ag-odd-row-background-color', 'transparent');
            }
            
            if (gridLines) {
                container.style.setProperty('--ag-borders', 'solid 1px');
                container.style.setProperty('--ag-border-color', '#dde2eb');
            } else {
                container.style.setProperty('--ag-borders', 'none');
            }
            
            if (gridApi) {
                gridApi.redrawRows();
            }
        }
    }

    function exportCsv() {
        if (gridApi) {
            gridApi.exportDataAsCsv({
                fileName: 'commesse_export.csv',
                columnSeparator: ';'
            });
        }
    }

    return {
        init: init,
        setQuickFilter: setQuickFilter,
        toggleColumnPanel: toggleColumnPanel,
        resetState: resetState,
        getState: getState,
        setState: setState,
        setUiVars: setUiVars,
        exportCsv: exportCsv
    };
})();
