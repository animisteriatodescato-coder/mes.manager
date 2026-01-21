window.commesseGrid = (function() {
    let gridApi = null;

    const columnDefs = [
        { field: 'Codice', headerName: 'Codice', sortable: true, filter: true, width: 180, pinned: 'left' },
        { field: 'InternalOrdNo', headerName: 'Num. Ordine', sortable: true, filter: true, width: 130 },
        { field: 'ExternalOrdNo', headerName: 'Ordine Esterno', sortable: true, filter: true, width: 150 },
        { field: 'Line', headerName: 'Linea', sortable: true, filter: true, width: 80 },
        { 
            field: 'ArticoloCodice', 
            headerName: 'Cod. Articolo', 
            sortable: true, 
            filter: true, 
            width: 150 
        },
        { 
            field: 'Description', 
            headerName: 'Descrizione', 
            sortable: true, 
            filter: true, 
            width: 300 
        },
        { 
            field: 'ClienteRagioneSociale', 
            headerName: 'Cliente', 
            sortable: true, 
            filter: true, 
            width: 250 
        },
        { 
            field: 'QuantitaRichiesta', 
            headerName: 'Quantità', 
            sortable: true, 
            filter: true, 
            width: 100,
            type: 'numericColumn'
        },
        { field: 'UoM', headerName: 'U.M.', sortable: true, filter: true, width: 80 },
        { 
            field: 'DataConsegna', 
            headerName: 'Data Consegna', 
            sortable: true, 
            filter: true, 
            width: 120,
            valueFormatter: params => params.value ? new Date(params.value).toLocaleDateString('it-IT') : ''
        },
        { 
            field: 'Stato', 
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
        { field: 'RiferimentoOrdineCliente', headerName: 'Rif. Cliente', sortable: true, filter: true, width: 150 },
        { field: 'OurReference', headerName: 'Ns. Riferimento', sortable: true, filter: true, width: 150 },
        { 
            field: 'UltimaModifica', 
            headerName: 'Ultima Modifica', 
            sortable: true, 
            filter: true, 
            width: 160,
            valueFormatter: params => params.value ? new Date(params.value).toLocaleString('it-IT') : ''
        },
        { 
            field: 'TimestampSync', 
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

    return {
        init: init,
        setQuickFilter: setQuickFilter,
        setColumnVisible: setColumnVisible,
        getAllColumns: getAllColumns,
        getState: getState,
        resetState: resetState,
        setUiVars: setUiVars,
        exportCsv: exportCsv,
        getStats: getStats
    };
})();
