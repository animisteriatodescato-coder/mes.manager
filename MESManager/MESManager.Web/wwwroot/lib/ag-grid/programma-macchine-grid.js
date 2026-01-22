window.programmaMacchineGrid = (function() {
    let gridApi = null;

    // Colori alternati per le macchine (azzurro e verde pallido)
    const machineColors = {
        even: '#e3f2fd', // azzurro pallido
        odd: '#e8f5e9'   // verde pallido
    };

    const columnDefs = [
        {
            field: 'storico',
            headerName: '',
            width: 50,
            pinned: 'left',
            sortable: false,
            filter: false,
            suppressMenu: true,
            cellRenderer: params => {
                return `<button class="storico-btn" style="border:none;background:transparent;cursor:pointer;font-size:16px;color:#9c27b0" title="Visualizza Storico Programmazione">📋</button>`;
            },
            onCellClicked: params => {
                showStoricoProgrammazione(params.data.id, params.data.codice || params.data.numeroCommessa);
            }
        },
        { 
            field: 'numeroMacchina', 
            headerName: 'MA', 
            sortable: true, 
            filter: true, 
            width: 70, 
            pinned: 'left',
            sort: 'asc',
            cellStyle: { fontWeight: 'bold', textAlign: 'center' },
            valueFormatter: params => {
                if (!params.value) return '';
                // Convert M001 to 01, M002 to 02, etc.
                const match = params.value.match(/^M0*(\d+)$/);
                if (match) {
                    return match[1].padStart(2, '0');
                }
                return params.value;
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
            headerName: 'Stato Mago', 
            sortable: true, 
            filter: true, 
            width: 120
        },
        { 
            field: 'statoProgramma', 
            headerName: 'Stato Programma', 
            sortable: true, 
            filter: true, 
            width: 160,
            cellRenderer: params => {
                const stati = [
                    { value: 'NonProgrammata', label: 'Non Programmata', color: '#9e9e9e', bg: '#f5f5f5' },
                    { value: 'Programmata', label: 'Programmata', color: '#1976d2', bg: '#e3f2fd' },
                    { value: 'InProduzione', label: 'In Produzione', color: '#ff9800', bg: '#fff3e0' },
                    { value: 'Completata', label: 'Completata', color: '#4caf50', bg: '#e8f5e9' },
                    { value: 'Archiviata', label: 'Archiviata', color: '#616161', bg: '#eeeeee' }
                ];
                
                const currentValue = params.value || 'NonProgrammata';
                const selectId = `stato-programma-pm-${params.data.id}`;
                
                let options = '';
                stati.forEach(s => {
                    const selected = currentValue === s.value ? 'selected' : '';
                    options += `<option value="${s.value}" ${selected} style="color:${s.color}">${s.label}</option>`;
                });
                
                const currentStato = stati.find(s => s.value === currentValue) || stati[0];
                
                return `<select id="${selectId}" 
                    style="width:100%; height:100%; border:none; background:${currentStato.bg}; 
                           color:${currentStato.color}; font-weight:bold; text-align:center; cursor:pointer; 
                           font-size:inherit; padding:2px;"
                    data-commessa-id="${params.data.id}"
                    data-field="statoProgramma">
                    ${options}
                </select>`;
            },
            onCellClicked: (event) => {
                event.event.stopPropagation();
            },
            cellStyle: { padding: 0 }
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
        }
    ];

    // Estrae il numero dalla macchina (M001 -> 1, M002 -> 2)
    function getMachineNumber(numeroMacchina) {
        if (!numeroMacchina) return 0;
        const match = numeroMacchina.toString().match(/^M?(\d+)$/);
        return match ? parseInt(match[1], 10) : 0;
    }

    // Calcola il colore basato sul numero macchina (alternando tra azzurro e verde)
    function getMachineColor(numeroMacchina) {
        const num = getMachineNumber(numeroMacchina);
        if (num === 0) return null;
        return num % 2 === 0 ? machineColors.even : machineColors.odd;
    }

    // Funzione per mostrare lo storico programmazione in un dialog
    async function showStoricoProgrammazione(commessaId, codiceCommessa) {
        try {
            const response = await fetch(`/api/Commesse/${commessaId}/storico-programmazione`);
            if (!response.ok) throw new Error('Errore nel recupero dello storico');
            
            const storico = await response.json();
            
            // Crea il dialog overlay
            const overlay = document.createElement('div');
            overlay.id = 'storico-dialog-overlay';
            overlay.style.cssText = 'position:fixed;top:0;left:0;width:100%;height:100%;background:rgba(0,0,0,0.5);z-index:9999;display:flex;justify-content:center;align-items:center;';
            
            // Contenuto del dialog
            let tableRows = '';
            if (storico.length === 0) {
                tableRows = '<tr><td colspan="5" style="text-align:center;padding:20px;color:#666;">Nessuna modifica registrata</td></tr>';
            } else {
                storico.forEach(s => {
                    const data = new Date(s.dataModifica).toLocaleString('it-IT');
                    tableRows += `<tr>
                        <td style="padding:8px;border-bottom:1px solid #e0e0e0;">${data}</td>
                        <td style="padding:8px;border-bottom:1px solid #e0e0e0;">${s.statoPrecedente}</td>
                        <td style="padding:8px;border-bottom:1px solid #e0e0e0;">→</td>
                        <td style="padding:8px;border-bottom:1px solid #e0e0e0;">${s.statoNuovo}</td>
                        <td style="padding:8px;border-bottom:1px solid #e0e0e0;">${s.note || '-'}</td>
                    </tr>`;
                });
            }
            
            overlay.innerHTML = `
                <div style="background:white;border-radius:8px;max-width:700px;width:90%;max-height:80vh;overflow:auto;box-shadow:0 4px 20px rgba(0,0,0,0.3);">
                    <div style="padding:16px 20px;background:#9c27b0;color:white;border-radius:8px 8px 0 0;display:flex;justify-content:space-between;align-items:center;">
                        <h3 style="margin:0;font-size:18px;">📋 Storico Programmazione - ${codiceCommessa}</h3>
                        <button id="close-storico-btn" style="border:none;background:transparent;color:white;font-size:24px;cursor:pointer;padding:0;line-height:1;">&times;</button>
                    </div>
                    <div style="padding:20px;">
                        <table style="width:100%;border-collapse:collapse;">
                            <thead>
                                <tr style="background:#f5f5f5;">
                                    <th style="padding:10px;text-align:left;border-bottom:2px solid #9c27b0;">Data</th>
                                    <th style="padding:10px;text-align:left;border-bottom:2px solid #9c27b0;">Da</th>
                                    <th style="padding:10px;text-align:center;border-bottom:2px solid #9c27b0;width:30px;"></th>
                                    <th style="padding:10px;text-align:left;border-bottom:2px solid #9c27b0;">A</th>
                                    <th style="padding:10px;text-align:left;border-bottom:2px solid #9c27b0;">Note</th>
                                </tr>
                            </thead>
                            <tbody>
                                ${tableRows}
                            </tbody>
                        </table>
                    </div>
                </div>
            `;
            
            document.body.appendChild(overlay);
            
            // Gestione chiusura
            const closeBtn = document.getElementById('close-storico-btn');
            closeBtn.addEventListener('click', () => overlay.remove());
            overlay.addEventListener('click', (e) => {
                if (e.target === overlay) overlay.remove();
            });
            
        } catch (err) {
            console.error('Error loading storico:', err);
            alert('Errore nel caricamento dello storico: ' + err.message);
        }
    }

    function init(gridId, data, savedColumnState) {
        const gridDiv = document.getElementById(gridId);
        if (!gridDiv) {
            console.error('Grid element not found:', gridId);
            return;
        }

        // Filter only rows with numeroMacchina assigned (numeroMacchina is a string)
        const filteredData = data.filter(row => row.numeroMacchina != null && row.numeroMacchina !== '');
        console.log('init: received', data.length, 'rows, filtered to', filteredData.length, 'with machine');

        const gridOptions = {
            columnDefs: columnDefs,
            rowData: filteredData,
            defaultColDef: {
                resizable: true,
                sortable: true,
                filter: true,
                suppressMenu: true
            },
            getRowStyle: params => {
                const style = {};
                
                if (params.data && params.data.numeroMacchina != null && params.data.numeroMacchina !== '') {
                    const color = getMachineColor(params.data.numeroMacchina);
                    if (color) {
                        style.backgroundColor = color;
                    }
                    
                    // Aggiungi bordo superiore nero se la macchina è diversa dalla riga precedente
                    const rowIndex = params.node.rowIndex;
                    if (rowIndex > 0) {
                        const prevNode = params.api.getDisplayedRowAtIndex(rowIndex - 1);
                        if (prevNode && prevNode.data && prevNode.data.numeroMacchina !== params.data.numeroMacchina) {
                            style.borderTop = '3px solid #000';
                        }
                    }
                }
                
                return style;
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
                console.log('onGridReady: gridApi set');
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
                window.dispatchEvent(new CustomEvent('programmaMacchineGridStateChanged'));
            },
            onColumnResized: () => {
                window.dispatchEvent(new CustomEvent('programmaMacchineGridStateChanged'));
            },
            onColumnVisible: () => {
                window.dispatchEvent(new CustomEvent('programmaMacchineGridStateChanged'));
            },
            onSortChanged: () => {
                window.dispatchEvent(new CustomEvent('programmaMacchineGridStateChanged'));
            },
            onSelectionChanged: () => {
                window.dispatchEvent(new CustomEvent('programmaMacchineGridStatsChanged'));
            },
            onFilterChanged: () => {
                window.dispatchEvent(new CustomEvent('programmaMacchineGridStatsChanged'));
            },
            onModelUpdated: () => {
                window.dispatchEvent(new CustomEvent('programmaMacchineGridStatsChanged'));
            },
            onRowDataUpdated: () => {
                // Attach handlers for statoProgramma selects after row data is updated
                setTimeout(() => attachStatoProgrammaHandlers(), 100);
            },
            onFirstDataRendered: () => {
                // Attach handlers for statoProgramma selects after first render
                setTimeout(() => attachStatoProgrammaHandlers(), 100);
            }
        };

        // agGrid.createGrid returns the API in AG Grid 32+
        const api = agGrid.createGrid(gridDiv, gridOptions);
        if (api) {
            gridApi = api;
            console.log('Grid created, gridApi set from createGrid return value');
        } else {
            console.log('Grid created, waiting for onGridReady for gridApi');
        }
    }

    function updateData(data) {
        console.log('updateData called with', data?.length, 'rows, gridApi exists:', !!gridApi);
        if (gridApi) {
            // numeroMacchina is a string, filter by non-empty value
            const filteredData = data.filter(row => row.numeroMacchina != null && row.numeroMacchina !== '');
            console.log('updateData: filtered to', filteredData.length, 'with machine');
            gridApi.setGridOption('rowData', filteredData);
        } else {
            console.error('updateData: gridApi is null, cannot update data');
        }
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

    function getStats() {
        if (!gridApi) return { total: 0, filtered: 0, selected: 0 };
        
        return {
            total: gridApi.getModel().getRowCount(),
            filtered: gridApi.getDisplayedRowCount(),
            selected: gridApi.getSelectedRows().length
        };
    }

    function exportCsv() {
        if (gridApi) {
            gridApi.exportDataAsCsv({
                fileName: 'programma_macchine_export.csv',
                columnSeparator: ';'
            });
        }
    }

    // Funzione per gestire i cambi di stato programma
    function attachStatoProgrammaHandlers() {
        document.querySelectorAll('select[data-commessa-id][data-field="statoProgramma"]').forEach(select => {
            if (select.hasAttribute('data-listener-attached')) return;
            select.setAttribute('data-listener-attached', 'true');
            
            select.addEventListener('change', async (e) => {
                const commessaId = e.target.getAttribute('data-commessa-id');
                const newValue = e.target.value;
                const selectEl = e.target;
                
                console.log(`Updating statoProgramma for commessa ${commessaId} to ${newValue}`);
                
                try {
                    const response = await fetch(`/api/Commesse/${commessaId}/stato-programma`, {
                        method: 'PATCH',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ statoProgramma: newValue })
                    });
                    
                    if (!response.ok) {
                        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
                    }
                    
                    console.log(`✓ statoProgramma updated successfully for ${commessaId}`);
                    
                    // Dispatch event for other components
                    window.dispatchEvent(new CustomEvent('commessaStatoProgrammaChanged', { 
                        detail: { id: commessaId, statoProgramma: newValue } 
                    }));
                    
                    // Ricarica tutti i dati per riflettere cambiamenti automatici
                    await refreshGridData();
                    
                } catch (err) {
                    console.error('Error updating statoProgramma:', err);
                    alert(`Errore durante il salvataggio: ${err.message}`);
                    selectEl.value = selectEl.dataset.previousValue || 'NonProgrammata';
                }
            });
            
            // Store current value for revert on error
            select.addEventListener('focus', (e) => {
                e.target.dataset.previousValue = e.target.value;
            });
        });
    }

    // Funzione per ricaricare i dati della griglia
    async function refreshGridData() {
        try {
            const response = await fetch('/api/Commesse');
            if (!response.ok) throw new Error('Failed to fetch');
            const allData = await response.json();
            // Filtra solo commesse con macchina assegnata e stato != Archiviata
            const filteredData = allData.filter(c => 
                c.numeroMacchina != null && 
                c.numeroMacchina !== '' && 
                c.statoProgramma !== 'Archiviata'
            );
            if (gridApi) {
                gridApi.setGridOption('rowData', filteredData);
                console.log(`Grid refreshed with ${filteredData.length} rows`);
                setTimeout(() => attachStatoProgrammaHandlers(), 100);
            }
        } catch (err) {
            console.error('Error refreshing grid data:', err);
        }
    }

    function resetState() {
        console.log('resetState called, gridApi exists:', !!gridApi);
        if (gridApi) {
            gridApi.resetColumnState();
            gridApi.setFilterModel(null);
            console.log('resetState: done');
        } else {
            console.error('resetState: gridApi is null');
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
            gridApi.applyColumnState({ state: state, applyOrder: true });
            console.log('setState: applied successfully');
        } catch (e) {
            console.error('setState: error parsing state', e);
        }
    }

    function toggleColumnPanel() {
        if (gridApi) {
            gridApi.openToolPanel('columns');
        }
    }

    function setUiVars(fontSize, rowHeight, densityPadding, zebra, gridLines) {
        const gridDiv = document.querySelector('#programmaMacchineGrid');
        if (gridDiv) {
            gridDiv.style.setProperty('--ag-font-size', fontSize + 'px');
            gridDiv.style.setProperty('--ag-row-height', rowHeight + 'px');
            gridDiv.style.setProperty('--ag-cell-horizontal-padding', densityPadding);
            
            if (gridLines) {
                gridDiv.style.setProperty('--ag-row-border-width', '1px');
                gridDiv.style.setProperty('--ag-row-border-color', '#ddd');
            } else {
                gridDiv.style.setProperty('--ag-row-border-width', '0px');
            }
        }
    }

    function generatePrintTable() {
        if (!gridApi) return;

        const printDiv = document.getElementById('printableCommesse');
        if (!printDiv) return;

        // Ottieni le colonne visibili
        const visibleColumns = gridApi.getColumns()
            .filter(col => col.isVisible() && col.getColId() !== 'ag-Grid-AutoColumn');

        // Ottieni tutte le righe visualizzate (filtrate e ordinate)
        const rowData = [];
        gridApi.forEachNodeAfterFilterAndSort(node => {
            if (node.data) {
                rowData.push(node.data);
            }
        });

        // Data e ora di stampa
        const now = new Date();
        const dateStr = now.toLocaleDateString('it-IT');
        const timeStr = now.toLocaleTimeString('it-IT', { hour: '2-digit', minute: '2-digit' });

        // Genera HTML per la tabella con stile esplicito per stampa
        let html = '<div style="background: white; color: black; padding: 10px; font-family: Arial, sans-serif;">';
        html += `<div style="text-align: center; margin-bottom: 15px;">`;
        html += `<h2 style="margin: 0 0 5px 0; color: black;">Programma Macchine</h2>`;
        html += `<p style="margin: 0; font-size: 12px; color: #666;">Data stampa: ${dateStr} ${timeStr}</p>`;
        html += `</div>`;
        html += '<table style="width: 100%; border-collapse: collapse; font-size: 10px; background: white;">';
        
        // Header
        html += '<thead><tr style="background-color: #f0f0f0; -webkit-print-color-adjust: exact; print-color-adjust: exact;">';
        visibleColumns.forEach(col => {
            const colDef = col.getColDef();
            const align = colDef.type === 'numericColumn' ? 'right' : 
                         colDef.field === 'numeroMacchina' ? 'center' : 'left';
            const style = `border: 1px solid #ddd; padding: 4px; text-align: ${align}; font-weight: bold; background-color: #f0f0f0; -webkit-print-color-adjust: exact; print-color-adjust: exact;`;
            html += `<th style="${style}">${colDef.headerName}</th>`;
        });
        html += '</tr></thead>';

        // Body
        html += '<tbody>';
        let previousMachine = null;
        rowData.forEach(row => {
            const machineNum = getMachineNumber(row.numeroMacchina);
            const bgColor = machineNum % 2 === 0 ? '#e3f2fd' : '#e8f5e9';
            const borderTop = previousMachine !== null && previousMachine !== row.numeroMacchina 
                ? 'border-top: 3px solid #000;' : '';
            previousMachine = row.numeroMacchina;
            
            html += `<tr style="background-color: ${bgColor}; ${borderTop} -webkit-print-color-adjust: exact; print-color-adjust: exact;">`;
            visibleColumns.forEach(col => {
                const colDef = col.getColDef();
                const field = colDef.field;
                let value = row[field];
                
                // Formatta il valore usando il valueFormatter se presente
                if (colDef.valueFormatter && typeof colDef.valueFormatter === 'function') {
                    value = colDef.valueFormatter({ value: value, data: row });
                } else if (value === null || value === undefined) {
                    value = '';
                }

                const align = colDef.type === 'numericColumn' ? 'right' : 
                             field === 'numeroMacchina' ? 'center' : 'left';
                const fontWeight = field === 'numeroMacchina' ? 'font-weight: bold;' : '';
                const style = `border: 1px solid #ddd; padding: 3px; text-align: ${align}; ${fontWeight} background-color: ${bgColor}; -webkit-print-color-adjust: exact; print-color-adjust: exact;`;
                html += `<td style="${style}">${value}</td>`;
            });
            html += '</tr>';
        });
        html += '</tbody>';
        html += '</table>';

        // Footer - già incluso data e ora nell'header, aggiungiamo solo il totale
        html += `<div style="margin-top: 10px; font-size: 9px; color: #666; text-align: right;">`;
        html += `<p style="margin: 2px 0;">Totale commesse: ${rowData.length}</p>`;
        html += `</div>`;
        html += '</div>'; // Chiude il div wrapper principale

        printDiv.innerHTML = html;
    }

    function printInNewWindow() {
        if (!gridApi) return;

        // Ottieni le colonne visibili
        const visibleColumns = gridApi.getColumns()
            .filter(col => col.isVisible() && col.getColId() !== 'ag-Grid-AutoColumn');

        // Ottieni tutte le righe visualizzate (filtrate e ordinate)
        const rowData = [];
        gridApi.forEachNodeAfterFilterAndSort(node => {
            if (node.data) {
                rowData.push(node.data);
            }
        });

        // Data e ora di stampa
        const now = new Date();
        const dateStr = now.toLocaleDateString('it-IT');
        const timeStr = now.toLocaleTimeString('it-IT', { hour: '2-digit', minute: '2-digit' });

        // Costruisci HTML completo per la finestra di stampa
        let html = `<!DOCTYPE html>
<html>
<head>
    <title>Programma Macchine - Stampa</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        html, body { 
            background-color: #ffffff !important; 
            background: #ffffff !important;
            color: #000000 !important; 
            font-family: Arial, sans-serif;
            -webkit-print-color-adjust: exact !important;
            print-color-adjust: exact !important;
        }
        @page { 
            size: landscape; 
            margin: 10mm; 
        }
        @media print {
            html, body { 
                background-color: #ffffff !important; 
                background: #ffffff !important;
            }
        }
        h2 { text-align: center; margin-bottom: 5px; color: #000; }
        .print-date { text-align: center; font-size: 12px; color: #666; margin-bottom: 15px; }
        table { width: 100%; border-collapse: collapse; font-size: 10px; background-color: #ffffff; }
        th { 
            border: 1px solid #ddd; 
            padding: 4px; 
            font-weight: bold; 
            background-color: #f0f0f0 !important; 
            -webkit-print-color-adjust: exact !important;
            print-color-adjust: exact !important;
        }
        td { border: 1px solid #ddd; padding: 3px; }
        .footer { margin-top: 10px; font-size: 9px; color: #666; text-align: right; }
        .machine-even { background-color: #e3f2fd !important; -webkit-print-color-adjust: exact !important; print-color-adjust: exact !important; }
        .machine-odd { background-color: #e8f5e9 !important; -webkit-print-color-adjust: exact !important; print-color-adjust: exact !important; }
        .machine-separator { border-top: 3px solid #000 !important; }
        .center { text-align: center; }
        .right { text-align: right; }
        .bold { font-weight: bold; }
    </style>
</head>
<body>
    <h2>Programma Macchine</h2>
    <p class="print-date">Data stampa: ${dateStr} ${timeStr}</p>
    <table>
        <thead>
            <tr>`;

        // Header columns
        visibleColumns.forEach(col => {
            const colDef = col.getColDef();
            const alignClass = colDef.type === 'numericColumn' ? 'right' : 
                              colDef.field === 'numeroMacchina' ? 'center' : '';
            html += `<th class="${alignClass}">${colDef.headerName}</th>`;
        });

        html += `</tr>
        </thead>
        <tbody>`;

        // Body rows
        let previousMachine = null;
        rowData.forEach(row => {
            const machineNum = getMachineNumber(row.numeroMacchina);
            const colorClass = machineNum % 2 === 0 ? 'machine-even' : 'machine-odd';
            const separatorClass = previousMachine !== null && previousMachine !== row.numeroMacchina ? 'machine-separator' : '';
            previousMachine = row.numeroMacchina;
            
            html += `<tr class="${colorClass} ${separatorClass}">`;
            visibleColumns.forEach(col => {
                const colDef = col.getColDef();
                const field = colDef.field;
                let value = row[field];
                
                // Formatta il valore usando il valueFormatter se presente
                if (colDef.valueFormatter && typeof colDef.valueFormatter === 'function') {
                    value = colDef.valueFormatter({ value: value, data: row });
                } else if (value === null || value === undefined) {
                    value = '';
                }

                const alignClass = colDef.type === 'numericColumn' ? 'right' : 
                                  field === 'numeroMacchina' ? 'center bold' : '';
                html += `<td class="${alignClass}">${value}</td>`;
            });
            html += '</tr>';
        });

        html += `</tbody>
    </table>
    <div class="footer">
        <p>Totale commesse: ${rowData.length}</p>
    </div>
    <script>
        window.onload = function() {
            setTimeout(function() {
                window.print();
            }, 300);
        };
    </script>
</body>
</html>`;

        // Apri nuova finestra e stampa
        const printWindow = window.open('', '_blank', 'width=1200,height=800');
        if (printWindow) {
            printWindow.document.write(html);
            printWindow.document.close();
        }
    }

    function printViaIframe(printColumnFields) {
        if (!gridApi) return;

        // Ottieni le colonne visibili
        let visibleColumns = gridApi.getColumns()
            .filter(col => col.isVisible() && col.getColId() !== 'ag-Grid-AutoColumn');

        // Se sono specificate colonne per la stampa, filtra solo quelle
        if (printColumnFields && Array.isArray(printColumnFields) && printColumnFields.length > 0) {
            visibleColumns = visibleColumns.filter(col => {
                const field = col.getColDef().field;
                return printColumnFields.includes(field);
            });
        }

        // Ottieni tutte le righe visualizzate (filtrate e ordinate)
        const rowData = [];
        gridApi.forEachNodeAfterFilterAndSort(node => {
            if (node.data) {
                rowData.push(node.data);
            }
        });

        // Data e ora di stampa
        const now = new Date();
        const dateStr = now.toLocaleDateString('it-IT');
        const timeStr = now.toLocaleTimeString('it-IT', { hour: '2-digit', minute: '2-digit' });

        // Costruisci HTML per l'iframe
        let html = `<!DOCTYPE html>
<html>
<head>
    <title>Programma Macchine - Stampa</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        html, body { 
            background-color: #ffffff !important; 
            background: #ffffff !important;
            color: #000000 !important; 
            font-family: Arial, sans-serif;
            -webkit-print-color-adjust: exact !important;
            print-color-adjust: exact !important;
        }
        @page { 
            size: landscape; 
            margin: 10mm; 
        }
        h2 { text-align: center; margin-bottom: 5px; color: #000; }
        .print-date { text-align: center; font-size: 12px; color: #666; margin-bottom: 15px; }
        table { width: 100%; border-collapse: collapse; font-size: 10px; background-color: #ffffff; }
        th { 
            border: 1px solid #ddd; 
            padding: 4px; 
            font-weight: bold; 
            background-color: #f0f0f0 !important; 
            -webkit-print-color-adjust: exact !important;
            print-color-adjust: exact !important;
        }
        td { border: 1px solid #ddd; padding: 3px; }
        .footer { margin-top: 10px; font-size: 9px; color: #666; text-align: right; }
        .machine-even { background-color: #e3f2fd !important; -webkit-print-color-adjust: exact !important; print-color-adjust: exact !important; }
        .machine-odd { background-color: #e8f5e9 !important; -webkit-print-color-adjust: exact !important; print-color-adjust: exact !important; }
        .machine-separator { border-top: 3px solid #000 !important; }
        .center { text-align: center; }
        .right { text-align: right; }
        .bold { font-weight: bold; }
    </style>
</head>
<body>
    <h2>Programma Macchine</h2>
    <p class="print-date">Data stampa: ${dateStr} ${timeStr}</p>
    <table>
        <thead>
            <tr>`;

        // Header columns
        visibleColumns.forEach(col => {
            const colDef = col.getColDef();
            const alignClass = colDef.type === 'numericColumn' ? 'right' : 
                              colDef.field === 'numeroMacchina' ? 'center' : '';
            html += `<th class="${alignClass}">${colDef.headerName}</th>`;
        });

        html += `</tr>
        </thead>
        <tbody>`;

        // Body rows
        let previousMachine = null;
        rowData.forEach(row => {
            const machineNum = getMachineNumber(row.numeroMacchina);
            const colorClass = machineNum % 2 === 0 ? 'machine-even' : 'machine-odd';
            const separatorClass = previousMachine !== null && previousMachine !== row.numeroMacchina ? 'machine-separator' : '';
            previousMachine = row.numeroMacchina;
            
            html += `<tr class="${colorClass} ${separatorClass}">`;
            visibleColumns.forEach(col => {
                const colDef = col.getColDef();
                const field = colDef.field;
                let value = row[field];
                
                if (colDef.valueFormatter && typeof colDef.valueFormatter === 'function') {
                    value = colDef.valueFormatter({ value: value, data: row });
                } else if (value === null || value === undefined) {
                    value = '';
                }

                const alignClass = colDef.type === 'numericColumn' ? 'right' : 
                                  field === 'numeroMacchina' ? 'center bold' : '';
                html += `<td class="${alignClass}">${value}</td>`;
            });
            html += '</tr>';
        });

        html += `</tbody>
    </table>
    <div class="footer">
        <p>Totale commesse: ${rowData.length}</p>
    </div>
</body>
</html>`;

        // Crea un iframe nascosto, inserisce il contenuto e stampa
        let iframe = document.getElementById('printIframe');
        if (!iframe) {
            iframe = document.createElement('iframe');
            iframe.id = 'printIframe';
            iframe.style.position = 'absolute';
            iframe.style.width = '0';
            iframe.style.height = '0';
            iframe.style.border = 'none';
            iframe.style.left = '-9999px';
            document.body.appendChild(iframe);
        }

        const iframeDoc = iframe.contentWindow || iframe.contentDocument;
        const doc = iframeDoc.document || iframeDoc;
        
        doc.open();
        doc.write(html);
        doc.close();

        // Attendi che il contenuto sia caricato, poi stampa
        setTimeout(() => {
            iframe.contentWindow.focus();
            iframe.contentWindow.print();
        }, 250);
    }

    return {
        init: init,
        updateData: updateData,
        setQuickFilter: setQuickFilter,
        setColumnVisible: setColumnVisible,
        getAllColumns: getAllColumns,
        getStats: getStats,
        exportCsv: exportCsv,
        resetState: resetState,
        getState: getState,
        setState: setState,
        toggleColumnPanel: toggleColumnPanel,
        setUiVars: setUiVars,
        generatePrintTable: generatePrintTable,
        printInNewWindow: printInNewWindow,
        printViaIframe: printViaIframe
    };
})();
