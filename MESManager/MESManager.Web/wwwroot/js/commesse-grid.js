window.commesseGrid = (function() {
    let gridApi = null;

    const columnDefs = [
        { field: 'Codice', headerName: 'Codice', sortable: true, filter: true, width: 150 },
        { field: 'ArticoloId', headerName: 'Articolo ID', sortable: true, filter: true, width: 280 },
        { field: 'ClienteId', headerName: 'Cliente ID', sortable: true, filter: true, width: 280 },
        { field: 'QuantitaRichiesta', headerName: 'Quantità', sortable: true, filter: true, width: 120 },
        { 
            field: 'DataConsegna', 
            headerName: 'Data Consegna', 
            sortable: true, 
            filter: true, 
            width: 150,
            valueFormatter: params => params.value ? new Date(params.value).toLocaleDateString('it-IT') : ''
        },
        { field: 'Stato', headerName: 'Stato', sortable: true, filter: true, width: 150 },
        { field: 'RiferimentoOrdineCliente', headerName: 'Rif. Ordine Cliente', sortable: true, filter: true, width: 200 },
        { 
            field: 'UltimaModifica', 
            headerName: 'Ultima Modifica', 
            sortable: true, 
            filter: true, 
            width: 180,
            valueFormatter: params => params.value ? new Date(params.value).toLocaleString('it-IT') : ''
        },
        { 
            field: 'TimestampSync', 
            headerName: 'Timestamp Sync', 
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

        console.log('Initializing grid with data:', data);
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

    return {
        init: init,
        setQuickFilter: setQuickFilter,
        setColumnVisible: setColumnVisible,
        getAllColumns: getAllColumns,
        getState: getState,
        resetState: resetState,
        setUiVars: setUiVars,
        exportCsv: exportCsv
    };
})();
