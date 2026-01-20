window.animeGrid = (function() {
    let gridApi = null;
    let isGridInitialized = false;

    const columnDefs = [
        { field: 'id', headerName: 'ID', sortable: true, filter: true, width: 80 },
        { field: 'ubicazione', headerName: 'Ubicazione', sortable: true, filter: true, width: 150 },
        { field: 'cliente', headerName: 'Cliente', sortable: true, filter: true, width: 150 },
        { field: 'codiceArticolo', headerName: 'Codice', sortable: true, filter: true, width: 150 },
        { field: 'descrizioneArticolo', headerName: 'Descrizione Articolo', sortable: true, filter: true, width: 250 },
        { field: 'codiceCassa', headerName: 'Codice Cassa', sortable: true, filter: true, width: 120 },
        { field: 'codiceAnime', headerName: 'Codice Anima', sortable: true, filter: true, width: 120 },
        { field: 'unitaMisura', headerName: 'Unità Misura', sortable: true, filter: true, width: 100 },
        { field: 'imballo', headerName: 'Imballo', sortable: true, filter: true, width: 100 },
        { field: 'note', headerName: 'Note', sortable: true, filter: true, width: 200 },
        { field: 'macchineSuDisponibili', headerName: 'Macchine Disponibili', sortable: true, filter: true, width: 200 },
        { field: 'sabbia', headerName: 'Sabbia', sortable: true, filter: true, width: 150 },
        { field: 'togliereSparo', headerName: 'Togliere Sparo', sortable: true, filter: true, width: 130 },
        { field: 'vernice', headerName: 'Vernice', sortable: true, filter: true, width: 150 },
        { field: 'colla', headerName: 'Colla', sortable: true, filter: true, width: 150 },
        { field: 'quantitaPiano', headerName: 'Quantità Piano', sortable: true, filter: true, width: 120 },
        { field: 'numeroPiani', headerName: 'Numero Piani', sortable: true, filter: true, width: 120 },
        { field: 'ciclo', headerName: 'Ciclo', sortable: true, filter: true, width: 150 },
        { field: 'peso', headerName: 'Peso', sortable: true, filter: true, width: 100 },
        { field: 'figure', headerName: 'Figure', sortable: true, filter: true, width: 120 },
        { field: 'piastra', headerName: 'Piastra', sortable: true, filter: true, width: 120 },
        { field: 'maschere', headerName: 'Maschere', sortable: true, filter: true, width: 120 },
        { field: 'incollata', headerName: 'Incollata', sortable: true, filter: true, width: 120 },
        { field: 'assemblata', headerName: 'Assemblata', sortable: true, filter: true, width: 120 },
        { field: 'armataL', headerName: 'Armata L', sortable: true, filter: true, width: 120 },
        { field: 'larghezza', headerName: 'Larghezza', sortable: true, filter: true, width: 100 },
        { field: 'altezza', headerName: 'Altezza', sortable: true, filter: true, width: 100 },
        { field: 'profondita', headerName: 'Profondità', sortable: true, filter: true, width: 100 },
        { field: 'idArticolo', headerName: 'ID Articolo', sortable: true, filter: true, width: 100 },
        { 
            field: 'trasmettiTutto', 
            headerName: 'Trasmetti Tutto', 
            sortable: true, 
            filter: true, 
            width: 130,
            valueFormatter: params => params.value ? 'Sì' : 'No'
        },
        { field: 'allegato', headerName: 'Allegato', sortable: true, filter: true, width: 150 },
        { 
            field: 'dataModificaRecord', 
            headerName: 'Data Modifica', 
            sortable: true, 
            filter: true, 
            width: 150,
            valueFormatter: params => params.value ? new Date(params.value).toLocaleDateString('it-IT') : ''
        },
        { 
            field: 'utenteModificaRecord', 
            headerName: 'Utente Modifica', 
            sortable: true, 
            filter: true, 
            width: 150
        },
        { 
            field: 'dataImportazione', 
            headerName: 'Data Importazione', 
            sortable: true, 
            filter: true, 
            width: 180,
            valueFormatter: params => params.value ? new Date(params.value).toLocaleString('it-IT') : ''
        }
    ];

    function init(gridId, data, savedColumnState) {
        const gridDiv = document.getElementById(gridId);
        if (!gridDiv) {
            console.error('Grid element not found:', gridId);
            return;
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

        const gridOptions = {
            columnDefs: columnDefs,
            rowData: data,
            defaultColDef: {
                resizable: true,
                sortable: true,
                filter: true
            },
            headerHeight: 24,
            rowHeight: 28,
            animateRows: true,
            rowSelection: 'single',
            onGridReady: (params) => {
                gridApi = params.api;
                isGridInitialized = true;
                console.log('Grid ready, rowData count:', gridApi.getDisplayedRowCount());
                if (savedColumnState) {
                    try {
                        gridApi.applyColumnState({
                            state: JSON.parse(savedColumnState),
                            applyOrder: true
                        });
                    } catch (e) {
                        console.warn('Failed to restore column state:', e);
                    }
                }
            },
            onSelectionChanged: () => {
                window.dispatchEvent(new CustomEvent('animeGridStatsChanged'));
            },
            onFilterChanged: () => {
                window.dispatchEvent(new CustomEvent('animeGridStatsChanged'));
            },
            onModelUpdated: () => {
                window.dispatchEvent(new CustomEvent('animeGridStatsChanged'));
            }
        };

        agGrid.createGrid(gridDiv, gridOptions);
    }

    function setQuickFilter(searchText) {
        if (gridApi) {
            gridApi.setGridOption('quickFilterText', searchText);
        }
    }

    function isInitialized() {
        return isGridInitialized;
    }

    function updateData(data) {
        if (gridApi) {
            console.log('Updating grid data with:', data);
            gridApi.setGridOption('rowData', data);
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

    function resetState() {
        if (gridApi) {
            gridApi.resetColumnState();
            gridApi.setFilterModel(null);
        }
    }

    function toggleColumnPanel() {
        console.log('toggleColumnPanel called');
        if (!gridApi || !isGridInitialized) {
            console.error('toggleColumnPanel: gridApi is null or not initialized!');
            return;
        }
        
        try {
            // Verifica che gridApi sia ancora valido
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

        // Chiudi cliccando sull'overlay
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

    return {
        init: init,
        setQuickFilter: setQuickFilter,
        isInitialized: isInitialized,
        updateData: updateData,
        setColumnVisible: setColumnVisible,
        getAllColumns: getAllColumns,
        getState: getState,
        resetState: resetState,
        toggleColumnPanel: toggleColumnPanel,
        setUiVars: setUiVars,
        exportCsv: exportCsv
    };
})();
