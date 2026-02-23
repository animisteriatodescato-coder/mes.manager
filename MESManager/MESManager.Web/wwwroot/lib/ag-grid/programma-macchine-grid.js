window.programmaMacchineGrid = (function() {
    let gridApi = null;
        function safeApiCall(action) {
            if (!gridApi) return;
            setTimeout(() => {
                try {
                    action();
                } catch (err) {
                    console.warn('safeApiCall failed:', err);
                }
            }, 0);
        }

    let dotNetHelper = null;
    
    // Lista macchine disponibili - caricata dinamicamente dal database
    let allMachines = [];
    
    // Funzione per impostare le macchine dal backend
    function setMachines(machines) {
        // machines è un array di oggetti MacchinaDto con Codice (es: "M001", "M011")
        allMachines = machines.map(m => m.codice).sort();
        console.log('Macchine configurate:', allMachines);
    }

    // Colori alternati per le macchine (verde pallido alternato)
    const machineColors = {
        light: '#e8f5e9',  // Verde pallido
        dark: '#c8e6c9'    // Verde leggermente più scuro
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
                if (params.data.isPlaceholder) return '';
                return `<button class="storico-btn" style="border:none;background:transparent;cursor:pointer;font-size:16px;color:#9c27b0" title="Visualizza Storico Programmazione">📋</button>`;
            },
            onCellClicked: params => {
                if (params.data.isPlaceholder) return;
                showStoricoProgrammazione(params.data.id, params.data.codice || params.data.numeroCommessa);
            }
        },
        {
            field: 'stampaEtichetta',
            headerName: '',
            width: 50,
            pinned: 'left',
            sortable: false,
            filter: false,
            suppressMenu: true,
            cellRenderer: params => {
                if (params.data.isPlaceholder) return '';
                const hasData = params.data && params.data.codiceAnime && params.data.clienteDisplay;
                const icon = hasData ? '🖨️' : '⚠️';
                const title = hasData ? 'Stampa Etichetta' : 'Dati incompleti - Clicca per dettagli';
                const color = hasData ? '#1976d2' : '#ff9800';
                return `<button class="print-label-btn" style="border:none;background:transparent;cursor:pointer;font-size:18px;color:${color}" title="${title}">${icon}</button>`;
            },
            onCellClicked: params => {
                if (params.data.isPlaceholder) return;
                if (dotNetHelper) {
                    dotNetHelper.invokeMethodAsync('OnPrintLabelClick', params.data);
                }
            }
        },
        {
            field: 'hasRicetta',
            headerName: '',
            width: 50,
            pinned: 'left',
            sortable: true,
            filter: false,
            suppressMenu: true,
            cellRenderer: params => {
                if (params.data.isPlaceholder) return '';
                const hasRicetta = params.data.hasRicetta === true;
                const icon = hasRicetta ? '✅' : '⚠️';
                const title = hasRicetta ? 'Ricetta configurata' : 'Ricetta mancante - Configurare prima di produrre';
                const color = hasRicetta ? '#4caf50' : '#ff5722';
                return `<div style="text-align:center;font-size:18px;color:${color}" title="${title}">${icon}</div>`;
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
            sortIndex: 0,
            editable: params => !params.data.isPlaceholder,
            cellEditor: 'agSelectCellEditor',
            cellEditorParams: {
                values: () => allMachines
            },
            cellStyle: { fontWeight: 'bold', textAlign: 'center' },
            valueFormatter: params => {
                if (!params.value) return '';
                // Convert M001 to 01, M002 to 02, etc.
                const match = params.value.match(/^M0*(\d+)$/);
                if (match) {
                    return match[1].padStart(2, '0');
                }
                return params.value;
            },
            onCellValueChanged: async (params) => {
                console.log('onCellValueChanged triggered:', params.oldValue, '->', params.newValue);
                if (params.oldValue === params.newValue) {
                    console.log('Valore non cambiato, skip');
                    return;
                }
                console.log('Chiamata assignCommessaToMachine per commessa:', params.data.id);
                await assignCommessaToMachine(params.data.id, params.newValue);
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
            field: 'clienteDisplay', 
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
            field: 'ordineSequenza', 
            headerName: 'Ord.', 
            sortable: true, 
            filter: true, 
            width: 70,
            sort: 'asc',
            sortIndex: 1,
            type: 'numericColumn',
            cellStyle: { textAlign: 'center', fontWeight: 'bold', color: '#1976d2' }
        },
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
        // Anime columns - importate da file condiviso (anime-columns-shared.js)
        // Le colonne vengono aggiunte dinamicamente in getColumnDefs()
    ];

    // Funzione per ottenere tutte le colonne incluse quelle anime condivise
    function getColumnDefs() {
        // Prende le colonne anime dal file condiviso se disponibile
        if (window.animeColumnsShared && window.animeColumnsShared.getAnimeColumns) {
            return [...columnDefs, ...window.animeColumnsShared.getAnimeColumns()];
        }
        // Fallback: usa colonne inline se il file condiviso non è caricato
        console.warn('anime-columns-shared.js non caricato, uso colonne fallback');
        return columnDefs;
    }

    // Estrae il numero dalla macchina (M001 -> 1, M002 -> 2)
    function getMachineNumber(numeroMacchina) {
        if (!numeroMacchina) return 0;
        const match = numeroMacchina.toString().match(/^M?(\d+)$/);
        return match ? parseInt(match[1], 10) : 0;
    }

    // Assegna una commessa a una macchina (calcolo automatico date)
    async function assignCommessaToMachine(commessaId, targetMacchina) {
        try {
            console.log('=== assignCommessaToMachine START ===');
            console.log('commessaId:', commessaId);
            console.log('targetMacchina:', targetMacchina);
            
            if (!targetMacchina) {
                console.log('Nessuna macchina selezionata - skip');
                return;
            }

            // Estrai il numero della macchina (M001 -> 1)
            const machineNumber = getMachineNumber(targetMacchina);
            console.log('machineNumber estratto:', machineNumber);
            
            // Chiama l'API di pianificazione che calcola automaticamente le date
            const requestBody = {
                commessaId: commessaId,
                targetMacchina: machineNumber,
                // Accoda in fondo - le date saranno calcolate automaticamente
                insertBeforeCommessaId: null,
                targetDataInizio: null
            };
            console.log('Request body:', JSON.stringify(requestBody));
            
            const response = await fetch('/api/pianificazione/sposta-commessa', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(requestBody)
            });
            
            console.log('Response status:', response.status);
            
            if (!response.ok) {
                const errorText = await response.text();
                console.error('Response error:', errorText);
                throw new Error(errorText || `HTTP ${response.status}`);
            }
            
            const result = await response.json();
            console.log('Response result:', result);
            
            if (result.success) {
                console.log('✓ Commessa assegnata con successo. Date calcolate automaticamente.');
                
                // Ricarica i dati per mostrare le nuove date
                await refreshGridData();
            } else {
                throw new Error(result.errorMessage || 'Errore sconosciuto');
            }
            
        } catch (err) {
            console.error('Errore durante l\'assegnazione:', err);
            alert(`Errore nell'assegnazione: ${err.message}`);
            
            // Ricarica comunque per annullare la modifica locale
            await refreshGridData();
        }
    }

    // ==================== END MOVE FUNCTIONS ====================

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
        console.log('[JS-PROGRAMMA] 🔍 init() START - gridId:', gridId, 'data.length:', data?.length);
        
        const gridDiv = document.getElementById(gridId);
        if (!gridDiv) {
            console.error('[JS-PROGRAMMA] ❌ Grid element not found:', gridId);
            return;
        }
        console.log('[JS-PROGRAMMA] ✅ Grid element found');

        // Aggiungi CSS per impedire la selezione del testo al doppio click
        if (!document.getElementById('pm-grid-noselect-style')) {
            const style = document.createElement('style');
            style.id = 'pm-grid-noselect-style';
            style.textContent = `
                #${gridId} .ag-cell {
                    user-select: none;
                    -webkit-user-select: none;
                    -moz-user-select: none;
                    -ms-user-select: none;
                }
                .placeholder-row {
                    color: #bbb !important;
                    font-style: italic;
                }
            `;
            document.head.appendChild(style);
        }

        // Prepara i dati con placeholder per macchine vuote
        const preparedData = prepareDataWithPlaceholders(data);
        console.log('[JS-PROGRAMMA] 📊 init: received', data?.length, 'rows, prepared to', preparedData.length, 'with placeholders');
        if (data?.length > 0) {
            console.log('[JS-PROGRAMMA] Sample data[0]:', data[0]);
        }

        const gridOptions = {
            columnDefs: getColumnDefs(),
            rowData: preparedData,
            defaultColDef: {
                resizable: true,
                sortable: true,
                filter: true,
                suppressMenu: true
            },
            getRowStyle: params => {
                const style = {};
                
                if (params.data) {
                    // Calcola il colore alternato in base al cambio macchina
                    const rowIndex = params.node.rowIndex;
                    let colorToggle = false;
                    
                    // Conta quante volte cambia la macchina prima di questa riga
                    if (rowIndex > 0) {
                        let prevMachine = null;
                        for (let i = 0; i <= rowIndex; i++) {
                            const node = params.api.getDisplayedRowAtIndex(i);
                            if (node && node.data && node.data.numeroMacchina) {
                                if (prevMachine !== null && prevMachine !== node.data.numeroMacchina) {
                                    colorToggle = !colorToggle;
                                }
                                prevMachine = node.data.numeroMacchina;
                            }
                        }
                    }
                    
                    // Applica il colore in base a quanti cambi ci sono stati
                    style.backgroundColor = colorToggle ? machineColors.dark : machineColors.light;
                    
                    // Aggiungi bordo superiore nero se la macchina è diversa dalla riga precedente
                    if (rowIndex > 0) {
                        const prevNode = params.api.getDisplayedRowAtIndex(rowIndex - 1);
                        if (prevNode && prevNode.data && prevNode.data.numeroMacchina !== params.data.numeroMacchina) {
                            style.borderTop = '3px solid #000';
                        }
                    }
                    
                    // Righe placeholder più sbiadite
                    if (params.data.isPlaceholder) {
                        style.opacity = '0.6';
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
            rowSelection: null, // Disabilita completamente la selezione
            suppressRowClickSelection: true, // Evita selezione riga al click
            suppressCellFocus: true, // Disabilita focus delle celle
            onGridReady: (params) => {
                gridApi = params.api;
                console.log('onGridReady: gridApi set');
                if (savedColumnState) {
                    try {
                        setTimeout(() => {
                            safeApiCall(() => {
                                gridApi.applyColumnState({
                                    state: JSON.parse(savedColumnState),
                                    applyOrder: true
                                });
                            });
                        }, 0);
                    } catch (e) {
                        console.warn('Failed to restore column state:', e);
                    }
                }
            },
            onRowDoubleClicked: (event) => {
                // Non fare nulla per le righe placeholder
                if (event.data && event.data.isPlaceholder) return;
                
                // Chiama il metodo .NET per aprire il dialog di modifica anima
                if (dotNetHelper && event.data && event.data.articoloCodice) {
                    dotNetHelper.invokeMethodAsync('OnRowDoubleClick', event.data.articoloCodice);
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

    // Prepara i dati aggiungendo placeholder per macchine vuote
    function prepareDataWithPlaceholders(data) {
        // Filtra solo commesse con macchina assegnata e non archiviate
        const commesse = data.filter(row => 
            row.numeroMacchina != null && 
            row.numeroMacchina !== '' &&
            row.statoProgramma !== 'Archiviata'
        );
        
        // 🔍 DEBUG: Log per verificare formato dati
        if (commesse.length > 0) {
            console.log('[JS-PROGRAMMA] prepareData: commesse[0].numeroMacchina =', commesse[0].numeroMacchina, 'tipo:', typeof commesse[0].numeroMacchina);
            console.log('[JS-PROGRAMMA] prepareData: allMachines =', allMachines);
        }
        
        // Helper: converte numeroMacchina (int o string) in codice macchina "M001"
        function toMachineCode(num) {
            if (typeof num === 'string' && num.startsWith('M')) return num; // già in formato M00X
            const n = parseInt(num);
            if (isNaN(n)) return null;
            return 'M' + String(n).padStart(3, '0'); // 5 → "M005", 11 → "M011"
        }
        
        // Raggruppa per macchina
        const byMachine = {};
        allMachines.forEach(m => byMachine[m] = []);
        
        let matched = 0, unmatched = 0;
        commesse.forEach(c => {
            const machineCode = toMachineCode(c.numeroMacchina);
            if (machineCode && byMachine[machineCode]) {
                byMachine[machineCode].push(c);
                matched++;
            } else {
                console.warn('[JS-PROGRAMMA] ⚠️ Commessa senza match macchina:', c.codice, 'numeroMacchina:', c.numeroMacchina, '→ machineCode:', machineCode);
                unmatched++;
            }
        });
        console.log('[JS-PROGRAMMA] prepareData: matched =', matched, 'unmatched =', unmatched);
        
        // Ordina per ordineSequenza dentro ogni macchina
        allMachines.forEach(m => {
            byMachine[m].sort((a, b) => (a.ordineSequenza || 0) - (b.ordineSequenza || 0));
        });
        
        // Costruisci l'array finale con placeholder per macchine vuote
        const result = [];
        allMachines.forEach(machine => {
            if (byMachine[machine].length === 0) {
                // Macchina vuota: aggiungi placeholder
                result.push({
                    id: `placeholder-${machine}`,
                    numeroMacchina: machine,
                    codice: '',
                    isPlaceholder: true
                });
            } else {
                // Macchina con commesse: aggiungile tutte
                // ⚠️ IMPORTANTE: Converti numeroMacchina nel formato M00X per coerenza con la griglia
                byMachine[machine].forEach(c => {
                    c.numeroMacchina = machine; // normalizza al formato M00X
                    result.push(c);
                });
            }
        });
        
        console.log('[JS-PROGRAMMA] prepareData: result =', result.length, 'righe (commesse + placeholder)');
        return result;
    }

    function updateData(data) {
        console.log('updateData called with', data?.length, 'rows, gridApi exists:', !!gridApi);
        if (!gridApi) {
            console.error('updateData: gridApi is null, cannot update data');
            return;
        }

        const preparedData = prepareDataWithPlaceholders(data);
        console.log('updateData: prepared to', preparedData.length, 'with placeholders');
        safeApiCall(() => {
            gridApi.setGridOption('rowData', preparedData);
        });
    }

    function setDotNetHelper(helper) {
        dotNetHelper = helper;
    }

    function setQuickFilter(searchText) {
        safeApiCall(() => {
            gridApi.setGridOption('quickFilterText', searchText);
        });
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
            
            // Filtra solo commesse:
            // 1. Con macchina assegnata
            // 2. Stato Mago = "Aperta" (esclude le chiuse da Mago)
            // 3. StatoProgramma != "Archiviata"
            const filteredData = allData.filter(c => 
                c.numeroMacchina != null && 
                c.numeroMacchina !== '' && 
                c.stato === 'Aperta' &&
                c.statoProgramma !== 'Archiviata'
            );
            
            if (gridApi) {
                // Prepara i dati con placeholder per macchine vuote
                const preparedData = prepareDataWithPlaceholders(filteredData);
                gridApi.setGridOption('rowData', preparedData);
                console.log(`Grid refreshed with ${preparedData.length} rows (${filteredData.length} commesse + placeholder)`);
                
                // Forza il re-render per aggiornare i colori alternati
                gridApi.redrawRows();
                
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
            safeApiCall(() => {
                gridApi.applyColumnState({ state: state, applyOrder: true });
                console.log('setState: applied successfully');
            });
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

        // Ottieni le colonne nell'ordine attuale della griglia (come visualizzato dall'utente)
        const visibleColumns = gridApi.getAllDisplayedColumns()
            .filter(col => col.getColId() !== 'ag-Grid-AutoColumn');

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
        let colorToggle = false;
        rowData.forEach((row, index) => {
            // Salta i placeholder nella stampa
            if (row.isPlaceholder) return;
            
            // Alterna colore quando cambia macchina
            if (previousMachine !== null && previousMachine !== row.numeroMacchina) {
                colorToggle = !colorToggle;
            }
            previousMachine = row.numeroMacchina;
            
            const bgColor = colorToggle ? '#c8e6c9' : '#e8f5e9';
            const borderTop = index > 0 && rowData[index-1] && rowData[index-1].numeroMacchina !== row.numeroMacchina 
                ? 'border-top: 3px solid #000;' : '';
            
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

        // Conta le righe effettive (senza placeholder)
        const actualRowCount1 = rowData.filter(r => !r.isPlaceholder).length;

        // Footer - già incluso data e ora nell'header, aggiungiamo solo il totale
        html += `<div style="margin-top: 10px; font-size: 9px; color: #666; text-align: right;">`;
        html += `<p style="margin: 2px 0;">Totale commesse: ${actualRowCount1}</p>`;
        html += `</div>`;
        html += '</div>'; // Chiude il div wrapper principale

        printDiv.innerHTML = html;
    }

    function printInNewWindow() {
        if (!gridApi) return;

        // Ottieni le colonne nell'ordine attuale della griglia (come visualizzato dall'utente)
        const visibleColumns = gridApi.getAllDisplayedColumns()
            .filter(col => col.getColId() !== 'ag-Grid-AutoColumn');

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
        .machine-light { background-color: #e8f5e9 !important; -webkit-print-color-adjust: exact !important; print-color-adjust: exact !important; }
        .machine-dark { background-color: #c8e6c9 !important; -webkit-print-color-adjust: exact !important; print-color-adjust: exact !important; }
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

        // Body rows - alterna colori in base al cambio macchina
        let previousMachine2 = null;
        let colorToggle2 = false;
        rowData.forEach(row => {
            // Salta i placeholder
            if (row.isPlaceholder) return;
            
            // Alterna colore quando cambia macchina
            if (previousMachine2 !== null && previousMachine2 !== row.numeroMacchina) {
                colorToggle2 = !colorToggle2;
            }
            const separatorClass = previousMachine2 !== null && previousMachine2 !== row.numeroMacchina ? 'machine-separator' : '';
            previousMachine2 = row.numeroMacchina;
            
            const colorClass = colorToggle2 ? 'machine-dark' : 'machine-light';
            
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

        // Conta le righe effettive (senza placeholder)
        const actualRowCount = rowData.filter(r => !r.isPlaceholder).length;
        
        html += `</tbody>
    </table>
    <div class="footer">
        <p>Totale commesse: ${actualRowCount}</p>
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

        // Ottieni le colonne nell'ordine attuale della griglia (come visualizzato dall'utente)
        // getAllDisplayedColumns() rispetta l'ordine delle colonne dopo drag & drop
        let visibleColumns = gridApi.getAllDisplayedColumns()
            .filter(col => col.getColId() !== 'ag-Grid-AutoColumn');

        // Se sono specificate colonne per la stampa, filtra solo quelle
        // IMPORTANTE: manteniamo l'ordine delle colonne dalla griglia, non dall'array printColumnFields
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
            background-color: white !important; 
            background: white !important;
            color: black !important; 
            font-family: Arial, sans-serif;
            -webkit-print-color-adjust: exact !important;
            print-color-adjust: exact !important;
            color-adjust: exact !important;
        }
        @page { 
            size: landscape; 
            margin: 10mm;
            background: white !important;
        }
        @media print {
            html, body { background: white !important; background-color: white !important; }
            * { background-color: inherit; }
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
        .machine-light { background-color: #e8f5e9 !important; -webkit-print-color-adjust: exact !important; print-color-adjust: exact !important; }
        .machine-dark { background-color: #c8e6c9 !important; -webkit-print-color-adjust: exact !important; print-color-adjust: exact !important; }
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

        // Body rows - alterna colori in base al cambio macchina
        let previousMachine3 = null;
        let colorToggle3 = false;
        rowData.forEach(row => {
            // Salta i placeholder
            if (row.isPlaceholder) return;
            
            // Alterna colore quando cambia macchina
            if (previousMachine3 !== null && previousMachine3 !== row.numeroMacchina) {
                colorToggle3 = !colorToggle3;
            }
            const separatorClass = previousMachine3 !== null && previousMachine3 !== row.numeroMacchina ? 'machine-separator' : '';
            previousMachine3 = row.numeroMacchina;
            
            const colorClass = colorToggle3 ? 'machine-dark' : 'machine-light';
            
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

        // Conta le righe effettive (senza placeholder)
        const actualRowCount3 = rowData.filter(r => !r.isPlaceholder).length;

        html += `</tbody>
    </table>
    <div class="footer">
        <p>Totale commesse: ${actualRowCount3}</p>
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
        setMachines: setMachines,
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
        printViaIframe: printViaIframe,
        setDotNetHelper: setDotNetHelper
    };
})();
