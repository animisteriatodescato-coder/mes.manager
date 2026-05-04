window.plcRealtimeGrid = (function () {
    let gridApi = null;
    let currentUserId = null;

    function getStorageKey() {
        return currentUserId 
            ? `plc-realtime-grid-columns-${currentUserId}`
            : 'plc-realtime-grid-columns';
    }

    function setCurrentUser(userId) {
        currentUserId = userId;
        console.log('PLC Realtime grid user set to:', userId);
    }

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
                field: 'macchianaNome', 
                headerName: 'Nome', 
                width: 180,
                sortable: true, 
                filter: true, 
                resizable: true 
            },
            {
                field: 'isConnessa',
                headerName: 'Connessione',
                width: 120,
                sortable: true,
                filter: true,
                resizable: true,
                cellRenderer: params => {
                    if (params.value === true) {
                        return `<span style="color: #4caf50; font-weight: bold;">● Online</span>`;
                    } else {
                        return `<span style="color: #bdbdbd; font-weight: bold;">○ Offline</span>`;
                    }
                }
            },
            {
                field: 'indirizzoPLC',
                headerName: 'IP PLC',
                width: 130,
                sortable: true,
                filter: true,
                resizable: true,
                hide: true,
                cellRenderer: params => {
                    if (params.value) {
                        return `<span style="font-family: monospace;">${params.value}</span>`;
                    }
                    return `<span style="color: #bdbdbd; font-style: italic;">N/A</span>`;
                }
            },
            { 
                field: 'statoMacchina', 
                headerName: 'Stato', 
                width: 180,
                sortable: true, 
                filter: true, 
                resizable: true,
                cellStyle: params => {
                    if (params.value === 'NON CONNESSA') return { backgroundColor: '#e0e0e0', color: '#757575', fontWeight: 'bold', fontStyle: 'italic' };
                    if (params.value === 'EMERGENZA') return { backgroundColor: '#f44336', color: 'white', fontWeight: 'bold' };
                    if (params.value === 'ALLARME') return { backgroundColor: '#ff9800', color: 'white', fontWeight: 'bold' };
                    if (params.value === 'MANUALE') return { backgroundColor: '#616161', color: 'white', fontWeight: 'bold' };
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
                field: 'ultimaNuovaProduzione',
                headerName: 'Ultima Nuova Prod.',
                width: 170,
                sortable: true,
                filter: 'agDateColumnFilter',
                resizable: true,
                valueFormatter: params => params.value ? new Date(params.value).toLocaleString('it-IT') : '—'
            },
            {
                field: 'ultimoInizioSetup',
                headerName: 'Ultimo Inizio Setup',
                width: 170,
                sortable: true,
                filter: 'agDateColumnFilter',
                resizable: true,
                valueFormatter: params => params.value ? new Date(params.value).toLocaleString('it-IT') : '—'
            },
            {
                field: 'ultimoFineSetup',
                headerName: 'Ultimo Fine Setup',
                width: 170,
                sortable: true,
                filter: 'agDateColumnFilter',
                resizable: true,
                cellRenderer: params => {
                    const inizio = params.data?.ultimoInizioSetup;
                    const fine   = params.value;
                    if (!fine) return '<span style="color:#9e9e9e">—</span>';
                    const fineDate  = new Date(fine);
                    const str = fineDate.toLocaleString('it-IT');
                    if (!inizio) return str;
                    const inizioDate = new Date(inizio);
                    if (fineDate > inizioDate) {
                        const minuti = Math.round((fineDate - inizioDate) / 60000);
                        return `${str} <span style="color:#9e9e9e;font-size:0.85em">(${minuti}min)</span>`;
                    }
                    return str;
                }
            },
            {
                field: 'inSetupOra',
                headerName: 'In Setup',
                width: 90,
                sortable: true,
                filter: true,
                resizable: true,
                cellRenderer: params => params.value
                    ? '<span style="background:#ff9800;color:white;padding:2px 8px;border-radius:4px;font-weight:bold">SETUP</span>'
                    : ''
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
                suppressMenu: true
            },
            getRowId: (params) => params.data.macchinaId, // Usa macchinaId come chiave univoca
            enableRangeSelection: true,
            enableCellTextSelection: true,
            animateRows: true,
            pagination: false,
            onGridReady: (params) => {
                gridApi = params.api;
                console.log('PLC Grid initialized successfully');
                
                // Prova prima lo stato passato da Blazor, poi quello dal localStorage
                let stateToRestore = savedColumnState;
                if (!stateToRestore) {
                    try {
                        const storageKey = getStorageKey();
                        stateToRestore = localStorage.getItem(storageKey);
                    } catch (e) {
                        console.error('Error reading from localStorage:', e);
                    }
                }
                
                if (stateToRestore) {
                    try {
                        const state = JSON.parse(stateToRestore);
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
            console.log('Updating grid with', rowData.length, 'rows');
            console.log('Sample data:', rowData[0]); // Mostra il primo record
            // Usa setGridOption per aggiornare i dati - più semplice e affidabile
            gridApi.setGridOption('rowData', rowData);
            // Forza il refresh delle celle per aggiornare i renderer
            setTimeout(() => {
                gridApi.refreshCells({ force: true, suppressFlash: false });
                console.log('Cells refreshed');
            }, 100);
        }
    }

    function refreshCells() {
        if (gridApi) {
            gridApi.refreshCells({ force: true });
        }
    }

    function saveState() {
        if (gridApi) {
            try {
                const state = gridApi.getColumnState();
                const storageKey = getStorageKey();
                localStorage.setItem(storageKey, JSON.stringify(state));
            } catch (e) {
                console.error('Error saving grid state:', e);
            }
        }
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

        const isDark = document.body.classList.contains('mud-theme-dark');
        const panelBg    = isDark ? '#1e1e2e' : '#ffffff';
        const panelColor = isDark ? '#e6e6f0' : '#212121';
        const borderCol  = isDark ? '#444460' : '#e0e0e0';
        const btnBg      = isDark ? '#37374f' : '#f5f5f5';
        const btnColor   = isDark ? '#e6e6f0' : '#212121';
        const dividerCol = isDark ? '#333348' : '#f0f0f0';

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
            background: ${panelBg};
            color: ${panelColor};
            border-radius: 8px;
            padding: 20px;
            min-width: 280px;
            max-width: 400px;
            max-height: 80vh;
            overflow-y: auto;
            box-shadow: 0 4px 20px rgba(0,0,0,0.5);
            border: 1px solid ${borderCol};
        `;

        const title = document.createElement('h3');
        title.textContent = 'Gestione Colonne';
        title.style.cssText = `margin-top: 0; color: ${panelColor}; font-size: 16px;`;
        panel.appendChild(title);

        const columns = gridApi.getColumns();
        columns.forEach((col, idx) => {
            const colDef = col.getColDef();
            const div = document.createElement('div');
            div.style.cssText = `
                padding: 8px 4px;
                border-bottom: 1px solid ${dividerCol};
                display: flex;
                align-items: center;
            `;

            const checkbox = document.createElement('input');
            checkbox.type = 'checkbox';
            checkbox.checked = col.isVisible();
            checkbox.style.cssText = 'margin-right: 10px; width: 16px; height: 16px; cursor: pointer; flex-shrink: 0;';
            checkbox.onchange = () => {
                gridApi.setColumnsVisible([col.getColId()], checkbox.checked);
            };

            const label = document.createElement('label');
            label.textContent = colDef.headerName || colDef.field;
            label.style.cssText = `cursor: pointer; color: ${panelColor}; font-size: 14px; user-select: none;`;
            label.onclick = () => {
                checkbox.checked = !checkbox.checked;
                checkbox.onchange();
            };

            div.appendChild(checkbox);
            div.appendChild(label);
            panel.appendChild(div);
        });

        const closeBtn = document.createElement('button');
        closeBtn.textContent = 'Chiudi';
        closeBtn.style.cssText = `
            margin-top: 15px;
            padding: 8px 20px;
            cursor: pointer;
            background: ${btnBg};
            color: ${btnColor};
            border: 1px solid ${borderCol};
            border-radius: 4px;
            font-size: 14px;
            width: 100%;
        `;
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
            try {
                const storageKey = getStorageKey();
                localStorage.removeItem(storageKey);
            } catch (e) {
                console.error('Error clearing localStorage:', e);
            }
        }
    }

    function resetState() {
        if (gridApi) {
            gridApi.resetColumnState();
            gridApi.setFilterModel(null);
        }
    }

    function getState() {
        if (!gridApi) return null;
        return JSON.stringify(gridApi.getColumnState());
    }

    function setState(stateJson) {
        if (!gridApi || !stateJson) return;
        try {
            const state = JSON.parse(stateJson);
            setTimeout(() => {
                try {
                    gridApi.applyColumnState({ state: state, applyOrder: true });
                } catch (err) {
                    console.warn('setState failed:', err);
                }
            }, 0);
        } catch (e) {
            console.error('setState: error parsing state', e);
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
        refreshCells,
        setQuickFilter,
        toggleColumnPanel,
        resetGrid,
        resetState,
        getState,
        setState,
        applySettings,
        getColumnState,
        setCurrentUser
    };
})();
