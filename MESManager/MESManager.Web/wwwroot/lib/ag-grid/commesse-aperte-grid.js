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
            editable: false,
            cellRenderer: params => {
                const raw = params.data?.macchineSuDisponibili || '';
                // Split per ottenere i codici disponibili (M001, M002, etc.)
                const macchine = raw.split(';').map(s => s.trim()).filter(s => s.length > 0);
                
                if (macchine.length === 0) {
                    return '<span style="color:#999">-</span>';
                }
                
                let currentValue = params.value || '';
                
                // Normalizza il valore corrente: se è un numero (1,2,3...) convertilo in codice (M001, M002, M003...)
                if (currentValue && /^\d+$/.test(currentValue)) {
                    const num = parseInt(currentValue);
                    currentValue = 'M' + num.toString().padStart(3, '0');
                }
                
                const selectId = `ma-select-${params.data.id}`;
                
                let options = '<option value="">-</option>';
                macchine.forEach(codice => {
                    // Estrai il numero dal codice (M001 -> 001 -> 1 -> 01)
                    const numero = codice.replace(/^M0*/, ''); // Rimuove M e zeri iniziali
                    const nome = numero.padStart(2, '0'); // 01, 02, 03, etc.
                    const selected = currentValue === codice ? 'selected' : '';
                    options += `<option value="${codice}" ${selected}>${nome}</option>`;
                });
                
                const bgColor = currentValue ? '#e3f2fd' : 'transparent';
                const fontWeight = currentValue ? 'bold' : 'normal';
                
                return `<select id="${selectId}" 
                    style="width:100%; height:100%; border:none; background:${bgColor}; 
                           font-weight:${fontWeight}; text-align:center; cursor:pointer; 
                           font-size:inherit; padding:2px;"
                    size="1"
                    data-commessa-id="${params.data.id}">
                    ${options}
                </select>`;
            },
            onCellClicked: (event) => {
                event.event.stopPropagation();
            },
            cellStyle: { padding: 0 }
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

    // Funzione per gestire i cambi di selezione macchina
    function attachMachineSelectHandlers() {
        document.querySelectorAll('select[data-commessa-id]').forEach(select => {
            if (select.hasAttribute('data-listener-attached')) return;
            select.setAttribute('data-listener-attached', 'true');
            
            select.addEventListener('change', async (e) => {
                const commessaId = e.target.getAttribute('data-commessa-id');
                const newValue = e.target.value;
                const selectEl = e.target;
                
                console.log(`Updating machine for commessa ${commessaId} to ${newValue}`);
                
                try {
                    const response = await fetch(`/api/Commesse/${commessaId}/numero-macchina`, {
                        method: 'PATCH',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ numeroMacchina: newValue })
                    });
                    
                    if (!response.ok) {
                        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
                    }
                    
                    console.log(`✓ Machine updated successfully for ${commessaId}`);
                    
                    // Update row data directly
                    const rowNode = gridApi.getRowNode(commessaId);
                    if (rowNode) {
                        rowNode.data.numeroMacchina = newValue;
                        
                        // Refresh the specific row
                        gridApi.refreshCells({
                            rowNodes: [rowNode],
                            force: true
                        });
                        
                        // Re-attach listener after cell refresh (the select was recreated)
                        setTimeout(() => {
                            const newSelect = document.getElementById(`ma-select-${commessaId}`);
                            if (newSelect) {
                                newSelect.removeAttribute('data-listener-attached');
                                attachMachineSelectHandlers();
                            }
                        }, 50);
                        
                        // Flash the row to indicate success
                        gridApi.flashCells({
                            rowNodes: [rowNode],
                            flashDuration: 500
                        });
                    } else {
                        console.warn('Row node not found for:', commessaId);
                        // Fallback: redraw all rows
                        gridApi.redrawRows();
                    }
                    
                    // Dispatch event for other components
                    window.dispatchEvent(new CustomEvent('commessaNumeroMacchinaChanged', { 
                        detail: { id: commessaId, numeroMacchina: newValue } 
                    }));
                    
                } catch (err) {
                    console.error('Error updating numero macchina:', err);
                    alert('Errore durante il salvataggio della macchina: ' + err.message);
                    // Revert the select to previous value
                    selectEl.value = selectEl.dataset.previousValue || '';
                }
            });
            
            // Store current value for revert on error
            select.addEventListener('focus', (e) => {
                e.target.dataset.previousValue = e.target.value;
            });
        });
    }

    function init(gridId, data, savedColumnState) {
        const gridDiv = document.getElementById(gridId);
        if (!gridDiv) {
            console.error('Grid element not found:', gridId);
            return;
        }
        
        // Carica stato colonne da localStorage
        const savedState = localStorage.getItem('commesse-aperte-grid-columnState');
        console.log('Loading column state from localStorage:', savedState ? 'found' : 'not found');
        
        // Destroy existing grid if it exists to prevent duplication
        if (gridApi) {
            console.log('Destroying existing grid before reinitializing...');
            gridApi.destroy();
            gridApi = null;
            // Clear the grid container
            gridDiv.innerHTML = '';
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
            getRowId: (params) => params.data.id,
            onGridReady: (params) => {
                gridApi = params.api;
                console.log('Commesse Aperte Grid ready, rowData count:', gridApi.getDisplayedRowCount());
                
                // Ripristina stato colonne se disponibile
                if (savedState) {
                    try {
                        gridApi.applyColumnState({
                            state: JSON.parse(savedState),
                            applyOrder: true
                        });
                        console.log('✓ Column state restored');
                    } catch (e) {
                        console.warn('Failed to restore column state:', e);
                    }
                }
                
                // Attach change handlers to machine selects
                setTimeout(() => {
                    attachMachineSelectHandlers();
                }, 100);
            },
            onColumnMoved: (params) => {
                if (params.finished) saveColumnState();
                window.dispatchEvent(new CustomEvent('commesseAperteGridStateChanged'));
            },
            onColumnResized: (params) => {
                if (params.finished) saveColumnState();
                window.dispatchEvent(new CustomEvent('commesseAperteGridStateChanged'));
            },
            onColumnVisible: () => {
                saveColumnState();
                window.dispatchEvent(new CustomEvent('commesseAperteGridStateChanged'));
            },
            onColumnPinned: () => saveColumnState(),
            onSortChanged: () => {
                saveColumnState();
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
                // Skip navigation if double-clicking on machine select
                if (event.column && event.column.getColId() === 'numeroMacchina') {
                    return;
                }
                // Doppio click: naviga a Catalogo Anime per modificare i dati
                if (dotNetHelper && event.data && event.data.articoloCodice) {
                    dotNetHelper.invokeMethodAsync('OnRowDoubleClick', event.data.articoloCodice);
                }
            },
            onCellValueChanged: async (event) => {
                // Machine select changes are handled in onGridReady
                if (event.colDef.field === 'numeroMacchina') {
                    return;
                }
            },
            onModelUpdated: () => {
                // Re-attach select handlers after grid updates
                setTimeout(() => {
                    attachMachineSelectHandlers();
                }, 50);
            }
        };

        agGrid.createGrid(gridDiv, gridOptions);
    }

    function saveColumnState() {
        if (gridApi) {
            const columnState = gridApi.getColumnState();
            localStorage.setItem('commesse-aperte-grid-columnState', JSON.stringify(columnState));
            console.log('Column state saved to localStorage');
        }
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
            localStorage.removeItem('commesse-aperte-grid-columnState');
            console.log('Column state reset and cleared from localStorage');
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
    
    function reinit(jsonData) {
        console.log('Reinitializing grid with fresh data...');
        const data = JSON.parse(jsonData);
        const savedState = getState();
        init('commesseAperteGrid', data, savedState);
    }
    
    function updateRowData(jsonData) {
        console.log('Updating grid row data...');
        const data = JSON.parse(jsonData);
        if (gridApi) {
            gridApi.setRowData(data);
        }
    }

    return {
        init: init,
        reinit: reinit,
        updateRowData: updateRowData,
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
