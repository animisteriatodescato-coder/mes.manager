window.plcRealtimeGrid = (function () {
    let gridApi = null;

    function init(containerId, rowData, savedColumnState) {
        const container = document.getElementById(containerId);
        if (!container) {
            console.error('Container not found:', containerId);
            return;
        }

        const columnDefs = [
            { 
                field: 'macchinaNumero', 
                headerName: 'Macchina', 
                pinned: 'left',
                width: 100,
                sortable: true, 
                filter: true, 
                resizable: true
            },
            { 
                field: 'macchianNome', 
                headerName: 'Nome', 
                width: 180,
                sortable: true, 
                filter: true, 
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
                field: 'cicliFatti', 
                headerName: 'Cicli Fatti', 
                width: 120,
                sortable: true, 
                filter: 'agNumberColumnFilter', 
                resizable: true,
                type: 'numericColumn',
                valueFormatter: params => params.value ? params.value.toLocaleString() : '0'
            },
            { 
                field: 'quantitaDaProdurre', 
                headerName: 'Da Produrre', 
                width: 130,
                sortable: true, 
                filter: 'agNumberColumnFilter', 
                resizable: true,
                type: 'numericColumn',
                valueFormatter: params => params.value ? params.value.toLocaleString() : '0'
            },
            { 
                field: 'percentualeCompletamento', 
                headerName: '% Completamento', 
                width: 200,
                sortable: true, 
                filter: 'agNumberColumnFilter', 
                resizable: true,
                type: 'numericColumn',
                cellRenderer: params => {
                    const percentuale = params.value || 0;
                    const tempoRilevato = params.data.tempoMedioRilevato || 0;
                    const tempoMedio = params.data.tempoMedio || 0;
                    
                    // Determina il colore in base al tempo
                    let barColor;
                    if (tempoMedio === 0) {
                        barColor = '#9e9e9e'; // Grigio se non c'è tempo medio
                    } else {
                        const ratio = tempoRilevato / tempoMedio;
                        if (ratio <= 0.95) {
                            barColor = '#4caf50'; // Verde - in anticipo o in linea
                        } else if (ratio <= 1.05) {
                            barColor = '#8bc34a'; // Verde chiaro - leggermente sopra
                        } else if (ratio <= 1.15) {
                            barColor = '#ffc107'; // Giallo - in ritardo moderato
                        } else if (ratio <= 1.3) {
                            barColor = '#ff9800'; // Arancione - in ritardo
                        } else {
                            barColor = '#f44336'; // Rosso - molto in ritardo
                        }
                    }
                    
                    return `
                        <div style="width: 100%; height: 100%; display: flex; align-items: center; position: relative;">
                            <div style="position: absolute; width: 100%; height: 100%; background: #e0e0e0;"></div>
                            <div style="position: absolute; width: ${Math.min(percentuale, 100)}%; height: 100%; background: ${barColor}; transition: width 0.3s ease;"></div>
                            <span style="position: relative; z-index: 1; margin-left: 8px; font-weight: bold; color: ${percentuale > 50 ? 'white' : 'black'}; text-shadow: ${percentuale > 50 ? '0 0 2px rgba(0,0,0,0.3)' : 'none'};">
                                ${percentuale.toFixed(1)}%
                            </span>
                        </div>
                    `;
                }
            },
            { 
                field: 'cicliScarti', 
                headerName: 'Scarti', 
                width: 120,
                sortable: true, 
                filter: 'agNumberColumnFilter', 
                resizable: true,
                type: 'numericColumn',
                cellRenderer: params => {
                    const scarti = params.value || 0;
                    const cicliFatti = params.data.cicliFatti || 0;
                    
                    if (cicliFatti === 0) {
                        return `<span style="color: #9e9e9e;">${scarti}</span>`;
                    }
                    
                    const percentualeScarti = (scarti / cicliFatti) * 100;
                    
                    // Determina il colore in base alla percentuale di scarti (1-8%)
                    let color;
                    if (percentualeScarti <= 1) {
                        color = '#2196f3'; // Blu - ottimo
                    } else if (percentualeScarti <= 2) {
                        color = '#4caf50'; // Verde - buono
                    } else if (percentualeScarti <= 3) {
                        color = '#8bc34a'; // Verde chiaro - accettabile
                    } else if (percentualeScarti <= 4) {
                        color = '#ffc107'; // Giallo - attenzione
                    } else if (percentualeScarti <= 5) {
                        color = '#ff9800'; // Arancione - problematico
                    } else if (percentualeScarti <= 6) {
                        color = '#ff5722'; // Arancione scuro - critico
                    } else if (percentualeScarti <= 8) {
                        color = '#f44336'; // Rosso - grave
                    } else {
                        color = '#b71c1c'; // Rosso scuro - gravissimo
                    }
                    
                    return `
                        <div style="display: flex; align-items: center; justify-content: space-between; height: 100%; padding: 0 8px;">
                            <span style="font-weight: bold; color: ${color};">${scarti}</span>
                            <span style="font-size: 0.85em; color: ${color}; background: ${color}22; padding: 2px 6px; border-radius: 4px;">
                                ${percentualeScarti.toFixed(1)}%
                            </span>
                        </div>
                    `;
                }
            },
            { 
                field: 'barcodeLavorazione', 
                headerName: 'Barcode', 
                width: 130,
                sortable: true, 
                filter: 'agNumberColumnFilter', 
                resizable: true
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
                field: 'tempoMedioRilevato', 
                headerName: 'Tempo Rilevato', 
                width: 150,
                sortable: true, 
                filter: 'agNumberColumnFilter', 
                resizable: true,
                type: 'numericColumn'
            },
            { 
                field: 'tempoMedio', 
                headerName: 'Tempo Medio', 
                width: 140,
                sortable: true, 
                filter: 'agNumberColumnFilter', 
                resizable: true,
                type: 'numericColumn'
            },
            { 
                field: 'figure', 
                headerName: 'Figure', 
                width: 100,
                sortable: true, 
                filter: 'agNumberColumnFilter', 
                resizable: true,
                type: 'numericColumn'
            },
            { 
                field: 'quantitaRaggiunta', 
                headerName: 'Completato', 
                width: 130,
                sortable: true, 
                filter: true, 
                resizable: true,
                valueFormatter: params => params.value ? 'Sì' : 'No',
                cellStyle: params => {
                    if (params.value) return { backgroundColor: '#4caf50', color: 'white', fontWeight: 'bold' };
                    return null;
                }
            },
            { 
                field: 'ultimoAggiornamento', 
                headerName: 'Ultimo Aggiornamento', 
                width: 180,
                sortable: true, 
                filter: 'agDateColumnFilter', 
                resizable: true,
                valueFormatter: params => params.value ? new Date(params.value).toLocaleString('it-IT') : ''
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
                resizable: true
            },
            enableRangeSelection: true,
            enableCellTextSelection: true,
            animateRows: true,
            pagination: false,
            onGridReady: (params) => {
                gridApi = params.api;
                console.log('PLC Grid initialized successfully');
                
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

    function applySettings(settings) {
        if (!gridApi) return;

        const container = document.getElementById('plcRealtimeGrid');
        if (!container) return;

        container.style.fontSize = `${settings.fontSize}px`;
        
        gridApi.setGridOption('rowHeight', settings.rowHeight);
        
        if (settings.zebra) {
            container.classList.add('ag-theme-alpine-zebra');
        } else {
            container.classList.remove('ag-theme-alpine-zebra');
        }

        if (settings.gridLines) {
            container.style.setProperty('--ag-borders', '1px solid');
        } else {
            container.style.setProperty('--ag-borders', 'none');
        }
    }

    function getColumnState() {
        if (gridApi) {
            const state = gridApi.getColumnState();
            return JSON.stringify(state);
        }
        return null;
    }

    return {
        init,
        updateData,
        setQuickFilter,
        toggleColumnPanel,
        resetGrid,
        applySettings,
        getColumnState
    };
})();
