window.plcStoricoGrid = (function () {
    let gridApi = null;

    function init(containerId, rowData, savedColumnState) {
        const container = document.getElementById(containerId);
        if (!container) {
            console.error('Container not found:', containerId);
            return;
        }

        const columnDefs = [
            { 
                field: 'timestamp', 
                headerName: 'Data/Ora', 
                pinned: 'left',
                width: 180,
                sortable: true, 
                filter: 'agDateColumnFilter', 
                resizable: true,
                valueFormatter: params => params.value ? new Date(params.value).toLocaleString('it-IT') : ''
            },
            { 
                field: 'macchinaNumero', 
                headerName: 'Macchina', 
                width: 100,
                sortable: true, 
                filter: true, 
                resizable: true
            },
            { 
                field: 'macchianaNome', 
                headerName: 'Nome', 
                width: 180,
                sortable: true, 
                filter: true, 
                resizable: true 
            },
            { 
                field: 'barcodeLavorazione', 
                headerName: 'Commessa', 
                width: 140,
                sortable: true, 
                filter: 'agNumberColumnFilter', 
                resizable: true 
            },
            { 
                field: 'cicliFatti', 
                headerName: 'Qta prodotta', 
                width: 140,
                sortable: true, 
                filter: 'agNumberColumnFilter', 
                resizable: true 
            },
            { 
                field: 'quantitaDaProdurre', 
                headerName: 'Qta da produrre', 
                width: 160,
                sortable: true, 
                filter: 'agNumberColumnFilter', 
                resizable: true 
            },
            { 
                field: 'cicliScarti', 
                headerName: 'Scarti', 
                width: 120,
                sortable: true, 
                filter: 'agNumberColumnFilter', 
                resizable: true 
            },
            { 
                field: 'tempoMedioRilevato', 
                headerName: 'T. rilevato', 
                width: 130,
                sortable: true, 
                filter: 'agNumberColumnFilter', 
                resizable: true 
            },
            { 
                field: 'tempoMedio', 
                headerName: 'T. medio', 
                width: 120,
                sortable: true, 
                filter: 'agNumberColumnFilter', 
                resizable: true 
            },
            { 
                field: 'figure', 
                headerName: 'Figure', 
                width: 110,
                sortable: true, 
                filter: 'agNumberColumnFilter', 
                resizable: true 
            },
            { 
                field: 'statoMacchina', 
                headerName: 'Stato', 
                width: 180,
                sortable: true, 
                filter: true, 
                resizable: true,
                cellStyle: params => {
                    if (params.value === 'EMERGENZA') return { backgroundColor: '#f44336', color: 'white', fontWeight: 'bold' };
                    if (params.value === 'ALLARME') return { backgroundColor: '#ff9800', color: 'white', fontWeight: 'bold' };
                    if (params.value === 'MANUALE') return { backgroundColor: '#9e9e9e', color: 'white', fontWeight: 'bold' };
                    if (params.value && params.value.includes('AUTOMATICO')) return { backgroundColor: '#4caf50', color: 'white', fontWeight: 'bold' };
                    if (params.value && params.value.includes('CICLO')) return { backgroundColor: '#2196f3', color: 'white', fontWeight: 'bold' };
                    return null;
                }
            },
            { 
                field: 'nomeOperatore', 
                headerName: 'Operatore', 
                width: 180,
                sortable: true, 
                filter: true, 
                resizable: true 
            },
            { 
                field: 'numeroOperatore', 
                headerName: 'N. Operatore', 
                width: 130,
                sortable: true, 
                filter: 'agNumberColumnFilter', 
                resizable: true,
                hide: true
            },
            { 
                field: 'nuovaProduzioneTs', 
                headerName: 'Nuova Produzione', 
                width: 160,
                sortable: true, 
                filter: true, 
                resizable: true 
            },
            { 
                field: 'inizioSetupTs', 
                headerName: 'Inizio setup', 
                width: 140,
                sortable: true, 
                filter: true, 
                resizable: true 
            },
            { 
                field: 'fineSetupTs', 
                headerName: 'Fine setup', 
                width: 140,
                sortable: true, 
                filter: true, 
                resizable: true 
            },
            { 
                field: 'inProduzioneTs', 
                headerName: 'In produzione', 
                width: 140,
                sortable: true, 
                filter: true, 
                resizable: true 
            },
            { 
                field: 'dati', 
                headerName: 'Dati JSON', 
                width: 400,
                sortable: false, 
                filter: true, 
                resizable: true,
                hide: true,
                cellRenderer: params => {
                    if (!params.value) return '';
                    try {
                        const json = JSON.parse(params.value);
                        return JSON.stringify(json, null, 2);
                    } catch {
                        return params.value;
                    }
                }
            },
            { 
                field: 'id', 
                headerName: 'ID', 
                width: 280,
                sortable: true, 
                filter: true, 
                resizable: true,
                hide: true
            },
            { 
                field: 'macchinaId', 
                headerName: 'Macchina ID', 
                width: 280,
                sortable: true, 
                filter: true, 
                resizable: true,
                hide: true
            }
        ];

        const gridOptions = {
            columnDefs: columnDefs,
            rowData: rowData,
            defaultColDef: {
                sortable: true,
                filter: true,
                resizable: true,
                floatingFilter: false,
                suppressMenu: true
            },
            suppressMenuHide: true,
            enableRangeSelection: true,
            enableCellTextSelection: true,
            animateRows: true,
            pagination: true,
            paginationPageSize: 100,
            onGridReady: (params) => {
                gridApi = params.api;
                console.log('PLC Storico Grid initialized successfully');
                
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
            onFilterChanged: saveState
        };

        new agGrid.Grid(container, gridOptions);
    }

    function updateData(rowData) {
        if (gridApi) {
            gridApi.setGridOption('rowData', rowData);
        }
    }

    function saveState() {
        // State saving handled by Blazor
    }

    function setQuickFilter(text) {
        if (gridApi) {
            gridApi.setGridOption('quickFilterText', text);
        }
    }

    function toggleColumnPanel() {
        if (!gridApi) return;

        let existingOverlay = document.getElementById('columnSelectorOverlay');
        if (existingOverlay) {
            existingOverlay.remove();
            return;
        }

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

        const title = document.createElement('h3');
        title.textContent = 'Gestione Colonne';
        title.style.marginTop = '0';
        panel.appendChild(title);

        const columns = gridApi.getColumns();
        columns.forEach(col => {
            const colDef = col.getColDef();
            const div = document.createElement('div');
            div.style.padding = '8px 0';

            const checkbox = document.createElement('input');
            checkbox.type = 'checkbox';
            checkbox.checked = col.isVisible();
            checkbox.style.marginRight = '8px';
            checkbox.onchange = () => {
                gridApi.setColumnsVisible([col.getColId()], checkbox.checked);
            };

            const label = document.createElement('label');
            label.textContent = colDef.headerName || colDef.field;
            label.style.cursor = 'pointer';
            label.prepend(checkbox);

            div.appendChild(label);
            panel.appendChild(div);
        });

        const closeBtn = document.createElement('button');
        closeBtn.textContent = 'Chiudi';
        closeBtn.style.cssText = 'margin-top: 15px; padding: 8px 16px; cursor: pointer;';
        closeBtn.onclick = () => overlay.remove();
        panel.appendChild(closeBtn);

        overlay.appendChild(panel);
        overlay.onclick = (e) => {
            if (e.target === overlay) overlay.remove();
        };

        document.body.appendChild(overlay);
    }

    function resetGrid() {
        if (gridApi) {
            gridApi.resetColumnState();
        }
    }

    function getColumnState() {
        if (gridApi) {
            const state = gridApi.getColumnState();
            return JSON.stringify(state);
        }
        return null;
    }

    function setColumnState(stateJson) {
        if (!gridApi || !stateJson) return;
        try {
            const state = JSON.parse(stateJson);
            gridApi.applyColumnState({ state: state, applyOrder: true });
            console.log('setColumnState: applied successfully');
        } catch (e) {
            console.error('setColumnState: error parsing state', e);
        }
    }

    return {
        init,
        updateData,
        setQuickFilter,
        toggleColumnPanel,
        resetGrid,
        getColumnState,
        setColumnState
    };
})();
