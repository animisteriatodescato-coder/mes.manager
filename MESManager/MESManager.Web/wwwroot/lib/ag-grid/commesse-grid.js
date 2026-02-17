window.commesseGrid = (function() {
    let gridApi = null;

    const columnDefs = [
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
        // RICETTA - Badge con numero parametri
        {
            field: 'hasRicetta',
            headerName: 'Ricetta',
            sortable: true,
            filter: true,
            width: 100,
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
                                 onclick="window.commesseGrid.openRicetta('${params.data.articoloCodice}')" 
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
        { 
            field: 'description', 
            headerName: 'Descrizione', 
            sortable: true, 
            filter: true, 
            width: 300 
        },
        { 
            field: 'clienteRagioneSociale', 
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
        }
    ];

    function init(gridId, data, savedColumnState) {
        const gridDiv = document.getElementById(gridId);
        if (!gridDiv) {
            console.error('Grid element not found:', gridId);
            return;
        }

        console.log('Initializing grid with data:', data);
        console.log('Data length:', data ? data.length : 'null');
        if (data && data.length > 0) {
            console.log('First row sample:', data[0]);
            console.log('internalOrdNo:', data[0].internalOrdNo);
            console.log('line:', data[0].line);
            console.log('description:', data[0].description);
        }

        const gridOptions = {
            columnDefs: columnDefs,
            rowData: data,
            defaultColDef: {
                resizable: true,
                sortable: true,
                filter: true,
                suppressMenu: true
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
            onGridReady: (params) => {
                gridApi = params.api;
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
            onColumnMoved: () => {
                window.dispatchEvent(new CustomEvent('commesseGridStateChanged'));
            },
            onColumnResized: () => {
                window.dispatchEvent(new CustomEvent('commesseGridStateChanged'));
            },
            onColumnVisible: () => {
                window.dispatchEvent(new CustomEvent('commesseGridStateChanged'));
            },
            onSortChanged: () => {
                window.dispatchEvent(new CustomEvent('commesseGridStateChanged'));
            },
            onSelectionChanged: () => {
                window.dispatchEvent(new CustomEvent('commesseGridStatsChanged'));
            },
            onFilterChanged: () => {
                window.dispatchEvent(new CustomEvent('commesseGridStatsChanged'));
            },
            onModelUpdated: () => {
                window.dispatchEvent(new CustomEvent('commesseGridStatsChanged'));
            }
        };

        agGrid.createGrid(gridDiv, gridOptions);
    }

    function setQuickFilter(searchText) {
        if (gridApi) {
            gridApi.setGridOption('quickFilterText', searchText);
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
            gridApi.resetColumnState();
            gridApi.setFilterModel(null);
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
                fileName: 'commesse_export.csv',
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

    function toggleColumnPanel() {
        if (gridApi) {
            gridApi.openToolPanel('columns');
        }
    }

    function openRicetta(codiceArticolo) {
        if (!codiceArticolo) {
            console.warn('[commesseGrid] openRicetta chiamato senza codiceArticolo');
            return;
        }
        
        console.log('[commesseGrid] Apertura ricetta per:', codiceArticolo);
        
        // Invoca il metodo Blazor lato server
        if (window.commesseGridDotNetRef) {
            window.commesseGridDotNetRef.invokeMethodAsync('ViewRicetta', codiceArticolo);
        } else {
            console.error('[commesseGrid] DotNetRef non registrato');
        }
    }

    function setDotNetRef(dotNetRef) {
        window.commesseGridDotNetRef = dotNetRef;
        console.log('[commesseGrid] DotNetRef registrato');
    }

    return {
        init: init,
        setQuickFilter: setQuickFilter,
        setColumnVisible: setColumnVisible,
        getAllColumns: getAllColumns,
        getState: getState,
        setState: setState,
        resetState: resetState,
        setUiVars: setUiVars,
        exportCsv: exportCsv,
        getStats: getStats,
        toggleColumnPanel: toggleColumnPanel,
        openRicetta: openRicetta,
        setDotNetRef: setDotNetRef
    };
})();
