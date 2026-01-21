window.commesseAperteGrid = (function() {
    let gridApi = null;
    let dotNetHelper = null;

    // Funzione per verificare se i dati etichetta sono completi
    function hasDatiEtichettaCompleti(data) {
        return data && 
               data.codiceAnime && 
               data.clienteRagioneSociale;
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
            field: 'numeroMacchina', 
            headerName: 'MA', 
            sortable: true, 
            filter: true, 
            width: 120, 
            pinned: 'left',
            editable: true,
            cellEditor: 'agSelectCellEditor',
            cellEditorParams: params => {
                const raw = params.data?.macchineSuDisponibili || '';
                const macchine = raw.split(',').map(s => s.trim()).filter(s => s.length > 0);
                return { values: macchine };
            },
            cellStyle: params => {
                if (params.value != null && params.value !== '') {
                    return { backgroundColor: '#e3f2fd', fontWeight: 'bold', textAlign: 'center' };
                }
                return { textAlign: 'center' };
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
        },
        // Anime columns
        { field: 'unitaMisura', headerName: 'U.M. Anime', sortable: true, filter: true, width: 100, hide: true },
        { field: 'larghezza', headerName: 'Larghezza', sortable: true, filter: 'agNumberColumnFilter', width: 100, hide: true },
        { field: 'altezza', headerName: 'Altezza', sortable: true, filter: 'agNumberColumnFilter', width: 100, hide: true },
        { field: 'profondita', headerName: 'Profondità', sortable: true, filter: 'agNumberColumnFilter', width: 100, hide: true },
        { field: 'imballo', headerName: 'Imballo', sortable: true, filter: 'agNumberColumnFilter', width: 100, hide: true },
        { field: 'noteAnime', headerName: 'Note Anime', sortable: true, filter: true, width: 200, hide: true },
        { field: 'allegato', headerName: 'Allegato', sortable: true, filter: true, width: 150, hide: true },
        { field: 'peso', headerName: 'Peso', sortable: true, filter: true, width: 100, hide: true },
        { field: 'ubicazione', headerName: 'Ubicazione', sortable: true, filter: true, width: 150, hide: true },
        { field: 'ciclo', headerName: 'Ciclo', sortable: true, filter: true, width: 150, hide: true },
        { field: 'codiceCassa', headerName: 'Codice Cassa', sortable: true, filter: true, width: 120, hide: true },
        { field: 'codiceAnime', headerName: 'Codice Anime', sortable: true, filter: true, width: 120, hide: true },
        { field: 'macchineSuDisponibili', headerName: 'Macchine Disponibili', sortable: true, filter: true, width: 180, hide: true },
        { 
            field: 'trasmettiTutto', 
            headerName: 'Trasmetti Tutto', 
            sortable: true, 
            filter: true, 
            width: 120,
            hide: true,
            valueFormatter: params => params.value === true ? 'Sì' : params.value === false ? 'No' : ''
        },
        // Campi etichetta
        { field: 'sabbiaDescrizione', headerName: 'Sabbia', sortable: true, filter: true, width: 120, hide: true },
        { field: 'verniceDescrizione', headerName: 'Vernice', sortable: true, filter: true, width: 150, hide: true },
        { field: 'quantitaPiano', headerName: 'Qtà Piano', sortable: true, filter: 'agNumberColumnFilter', width: 100, hide: true },
        { field: 'numeroPiani', headerName: 'N. Piani', sortable: true, filter: 'agNumberColumnFilter', width: 100, hide: true },
        { 
            field: 'quantitaEtichetta', 
            headerName: 'Qtà Etichetta', 
            sortable: true, 
            filter: 'agNumberColumnFilter', 
            width: 120, 
            hide: true,
            valueGetter: params => {
                const qp = params.data?.quantitaPiano || 0;
                const np = params.data?.numeroPiani || 0;
                return qp * np;
            }
        }
    ];

    function init(gridId, data, savedColumnState) {
        const gridDiv = document.getElementById(gridId);
        if (!gridDiv) {
            console.error('Grid element not found:', gridId);
            return;
        }

        console.log('Initializing commesse aperte grid with data:', data);
        console.log('Data length:', data ? data.length : 'null');
        if (data && data.length > 0) {
            console.log('First row sample:', data[0]);
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
            getRowStyle: params => {
                if (params.data && params.data.numeroMacchina != null && params.data.numeroMacchina !== '') {
                    return { backgroundColor: '#e3f2fd' };
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
            onGridReady: (params) => {
                gridApi = params.api;
                console.log('Commesse Aperte Grid ready, rowData count:', gridApi.getDisplayedRowCount());
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
                window.dispatchEvent(new CustomEvent('commesseAperteGridStateChanged'));
            },
            onColumnResized: () => {
                window.dispatchEvent(new CustomEvent('commesseAperteGridStateChanged'));
            },
            onColumnVisible: () => {
                window.dispatchEvent(new CustomEvent('commesseAperteGridStateChanged'));
            },
            onSortChanged: () => {
                window.dispatchEvent(new CustomEvent('commesseAperteGridStateChanged'));
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
                // Doppio click: naviga a Catalogo Anime per modificare i dati
                if (dotNetHelper && event.data && event.data.articoloCodice) {
                    dotNetHelper.invokeMethodAsync('OnRowDoubleClick', event.data.articoloCodice);
                }
            },
            onCellValueChanged: async (event) => {
                if (event.colDef.field === 'numeroMacchina') {
                    const id = event.data.id;
                    const value = event.newValue;
                    try {
                        await fetch(`/api/Commesse/${id}/numero-macchina`, {
                            method: 'PATCH',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify({ numeroMacchina: value })
                        });
                        // Refresh row style
                        gridApi.redrawRows({ rowNodes: [event.node] });
                        // Dispatch event for Programma Macchine update
                        window.dispatchEvent(new CustomEvent('commessaNumeroMacchinaChanged', { 
                            detail: { id: id, numeroMacchina: value } 
                        }));
                    } catch (err) {
                        console.error('Error updating numero macchina:', err);
                    }
                }
            }
        };

        agGrid.createGrid(gridDiv, gridOptions);
    }

    function setDotNetHelper(helper) {
        dotNetHelper = helper;
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

    function toggleColumnPanel() {
        if (gridApi) {
            gridApi.openToolPanel('columns');
        }
    }

    return {
        init: init,
        setDotNetHelper: setDotNetHelper,
        setQuickFilter: setQuickFilter,
        setColumnVisible: setColumnVisible,
        getAllColumns: getAllColumns,
        getState: getState,
        resetState: resetState,
        setUiVars: setUiVars,
        exportCsv: exportCsv,
        getStats: getStats,
        toggleColumnPanel: toggleColumnPanel
    };
})();
