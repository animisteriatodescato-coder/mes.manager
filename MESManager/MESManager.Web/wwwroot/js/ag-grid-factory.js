/**
 * AG Grid Factory - Centralizzazione COMPLETA logica grid
 * ========================================================
 * UN UNICO punto di modifica per tutti i catalog grid.
 * Qualsiasi modifica qui si propaga automaticamente a:
 *   - articoliGrid, clientiGrid, commesseGrid, animeGrid
 *
 * Utilizzo in ogni file grid:
 *   window.agGridFactory.setup({ namespace, columnDefs, ... });
 *
 * Config obbligatoria:
 *   namespace:       'articoliGrid'              -> window.articoliGrid
 *   columnDefs:      [...]                       -> colonne AG Grid
 *   exportFileName:  'articoli_export.csv'
 *   storageKeyBase:  'articoli-grid-columnState' -> localStorage key
 *   eventName:       'articoliGridStatsChanged'  -> CustomEvent per stats
 *
 * Config opzionale:
 *   rowSelection:        'single' | 'multiple'   (default: 'single')
 *   dotNetRefVar:        'animeGridDotNetRef'     -> window var per DotNetRef
 *   hasRicetta:          true                    -> aggiunge openRicetta()
 *   hasUpdateData:       true                    -> aggiunge updateData() (anime)
 *   onCellValueChanged:  async (event) => {}     -> callback auto-save
 *   onRowDoubleClicked:  (event) => {}           -> callback doppio click
 */
window.agGridFactory = (function () {
    // Registry gridApi: usato da refreshAllGrids() per forzare re-render su cambio tema
    var _registeredApis = [];

    function setup(config) {
        let gridApi = null;
        let isGridInitialized = false;
        let currentUserId = null;
        let lastGridId = null;

        // ── Storage ──────────────────────────────────────────────────────────
        function getStorageKey() {
            return currentUserId
                ? `${config.storageKeyBase}-${currentUserId}`
                : config.storageKeyBase;
        }

        function saveColumnState() {
            if (gridApi) {
                localStorage.setItem(
                    getStorageKey(),
                    JSON.stringify(gridApi.getColumnState())
                );
            }
        }

        // ── Init ─────────────────────────────────────────────────────────────
        function init(gridId, data, savedColumnState) {
            lastGridId = gridId;
            const gridDiv = document.getElementById(gridId);
            if (!gridDiv) {
                console.error('[' + config.namespace + '] Grid element not found:', gridId);
                return;
            }

            // Distruggi grid esistente se presente
            if (gridApi) {
                try { gridApi.destroy(); } catch (e) { }
                gridApi = null;
                isGridInitialized = false;
            }

            const stateToRestore = savedColumnState ||
                localStorage.getItem(getStorageKey());

            const gridOptions = {
                columnDefs: config.columnDefs,
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
                rowSelection: config.rowSelection || 'single',
                suppressRowClickSelection: config.rowSelection === 'multiple',
                enableCellTextSelection: true,
                onGridReady: (params) => {
                    gridApi = params.api;
                    isGridInitialized = true;
                    if (stateToRestore) {
                        try {
                            gridApi.applyColumnState({
                                state: JSON.parse(stateToRestore),
                                applyOrder: true
                            });
                        } catch (e) {
                            console.warn('[' + config.namespace + '] Failed to restore column state:', e);
                        }
                    }
                    console.log('[' + config.namespace + '] Grid ready, rows:', gridApi.getDisplayedRowCount());
                    // Registra gridApi nel registry centrale per refreshAllGrids()
                    _registeredApis.push({ ns: config.namespace, api: gridApi });
                },
                onColumnVisible: saveColumnState,
                onColumnResized: (p) => { if (p.finished) saveColumnState(); },
                onColumnMoved: (p) => { if (p.finished) saveColumnState(); },
                onColumnPinned: saveColumnState,
                onSortChanged: saveColumnState,
                onSelectionChanged: () => window.dispatchEvent(new CustomEvent(config.eventName)),
                onFilterChanged: () => window.dispatchEvent(new CustomEvent(config.eventName)),
                onModelUpdated: () => window.dispatchEvent(new CustomEvent(config.eventName))
            };

            if (config.onCellValueChanged) gridOptions.onCellValueChanged = config.onCellValueChanged;
            if (config.onRowDoubleClicked) gridOptions.onRowDoubleClicked = config.onRowDoubleClicked;

            agGrid.createGrid(gridDiv, gridOptions);
        }

        // ── State ─────────────────────────────────────────────────────────────
        function getState() {
            if (!gridApi) return null;
            return JSON.stringify(gridApi.getColumnState());
        }

        function setState(stateJson) {
            if (!gridApi || !stateJson) return;
            try {
                gridApi.applyColumnState({
                    state: JSON.parse(stateJson),
                    applyOrder: true
                });
            } catch (e) {
                console.error('[' + config.namespace + '] setState error:', e);
            }
        }

        function resetState() {
            if (!gridApi) return;
            gridApi.resetColumnState();
            gridApi.setFilterModel(null);
            localStorage.removeItem(getStorageKey());
        }

        // ── Filtro rapido ─────────────────────────────────────────────────────
        function setQuickFilter(text) {
            if (gridApi) gridApi.setGridOption('quickFilterText', text);
        }

        // ── Export ────────────────────────────────────────────────────────────
        function exportCsv() {
            if (gridApi) {
                gridApi.exportDataAsCsv({
                    fileName: config.exportFileName,
                    columnSeparator: ';'
                });
            }
        }

        // ── UI Vars ───────────────────────────────────────────────────────────
        // Usa lastGridId per targettare il div corretto (evita querySelector('.ag-theme-alpine')
        // che prenderebbe il primo div della pagina invece di quello specifico di questo grid)
        function setUiVars(fontSize, rowHeight, densityPadding, zebra, gridLines) {
            const gridDiv = lastGridId
                ? document.getElementById(lastGridId)
                : document.querySelector('.ag-theme-alpine');
            if (!gridDiv) return;

            gridDiv.style.setProperty('--ag-font-size', fontSize + 'px');
            gridDiv.style.setProperty('--ag-row-height', rowHeight + 'px');
            gridDiv.style.setProperty('--ag-cell-horizontal-padding', densityPadding);
            gridDiv.style.setProperty(
                '--ag-odd-row-background-color',
                zebra ? '#f9f9f9' : 'transparent'
            );
            if (gridLines) {
                gridDiv.style.setProperty('--ag-row-border-width', '1px');
                gridDiv.style.setProperty('--ag-row-border-color', '#ddd');
            } else {
                gridDiv.style.setProperty('--ag-row-border-width', '0px');
            }
        }

        // ── Toggle Column Panel ───────────────────────────────────────────────
        // Overlay JS unificato (con supporto dark mode)
        function toggleColumnPanel() {
            if (!gridApi || !isGridInitialized) {
                console.warn('[' + config.namespace + '] toggleColumnPanel: grid not ready');
                return;
            }

            // Toggle: rimuovi overlay se già presente
            const existing = document.getElementById('columnSelectorOverlay');
            if (existing) { existing.remove(); return; }

            const isDark = document.body.classList.contains('mud-theme-dark') ||
                document.documentElement.getAttribute('data-mud-theme') === 'dark';
            const bg = isDark ? '#1e1e1e' : 'white';
            const fg = isDark ? '#e0e0e0' : '#333';

            const overlay = document.createElement('div');
            overlay.id = 'columnSelectorOverlay';
            overlay.style.cssText = [
                'position:fixed', 'top:0', 'left:0',
                'width:100%', 'height:100%',
                'background:rgba(0,0,0,0.5)',
                'z-index:10000',
                'display:flex',
                'align-items:center',
                'justify-content:center'
            ].join(';');

            const panel = document.createElement('div');
            panel.style.cssText = [
                `background:${bg}`, `color:${fg}`,
                'border-radius:8px', 'padding:20px',
                'max-width:400px', 'max-height:80vh',
                'overflow-y:auto',
                'box-shadow:0 4px 20px rgba(0,0,0,0.3)'
            ].join(';');

            const title = document.createElement('h3');
            title.textContent = 'Gestione Colonne';
            title.style.cssText = `margin:0 0 15px 0;font-size:18px;color:${fg};`;
            panel.appendChild(title);

            const columnState = gridApi.getColumnState();
            columnState.forEach(col => {
                const row = document.createElement('div');
                row.style.cssText = 'margin-bottom:10px;display:flex;align-items:center;';

                const cb = document.createElement('input');
                cb.type = 'checkbox';
                cb.id = `col_${col.colId}`;
                cb.checked = !col.hide;
                cb.style.cssText = 'margin-right:10px;width:18px;height:18px;cursor:pointer;';
                cb.addEventListener('change', e =>
                    gridApi.setColumnsVisible([col.colId], e.target.checked)
                );

                const lbl = document.createElement('label');
                lbl.htmlFor = `col_${col.colId}`;
                lbl.textContent = col.colId;
                lbl.style.cssText = `cursor:pointer;font-size:14px;user-select:none;color:${fg};`;

                row.appendChild(cb);
                row.appendChild(lbl);
                panel.appendChild(row);
            });

            const btnRow = document.createElement('div');
            btnRow.style.cssText = 'margin-top:20px;display:flex;gap:10px;justify-content:flex-end;';
            const btnStyle = 'padding:8px 16px;border:1px solid #ccc;border-radius:4px;cursor:pointer;';

            const btnAll = document.createElement('button');
            btnAll.textContent = 'Seleziona Tutto';
            btnAll.style.cssText = btnStyle + 'background:#f5f5f5;';
            btnAll.addEventListener('click', () => {
                gridApi.setColumnsVisible(columnState.map(c => c.colId), true);
                columnState.forEach(c => {
                    const el = document.getElementById(`col_${c.colId}`);
                    if (el) el.checked = true;
                });
            });

            const btnNone = document.createElement('button');
            btnNone.textContent = 'Deseleziona Tutto';
            btnNone.style.cssText = btnStyle + 'background:#f5f5f5;';
            btnNone.addEventListener('click', () => {
                gridApi.setColumnsVisible(columnState.map(c => c.colId), false);
                columnState.forEach(c => {
                    const el = document.getElementById(`col_${c.colId}`);
                    if (el) el.checked = false;
                });
            });

            const btnClose = document.createElement('button');
            btnClose.textContent = 'Chiudi';
            btnClose.style.cssText = btnStyle + 'background:#2196f3;color:white;border-color:#2196f3;';
            btnClose.addEventListener('click', () => overlay.remove());

            btnRow.appendChild(btnAll);
            btnRow.appendChild(btnNone);
            btnRow.appendChild(btnClose);
            panel.appendChild(btnRow);
            overlay.appendChild(panel);
            document.body.appendChild(overlay);

            overlay.addEventListener('click', e => {
                if (e.target === overlay) overlay.remove();
            });
        }

        // ── Colonne e Stats ───────────────────────────────────────────────────
        function getAllColumns() {
            if (!gridApi) return [];
            return gridApi.getColumns().map(col => ({
                Field: col.getColDef().field,
                HeaderName: col.getColDef().headerName,
                Visible: col.isVisible()
            }));
        }

        function setColumnVisible(field, visible) {
            if (gridApi) gridApi.setColumnsVisible([field], visible);
        }

        function getStats() {
            if (!gridApi) return { total: 0, filtered: 0, selected: 0 };
            return {
                total: gridApi.getModel().getRowCount(),
                filtered: gridApi.getDisplayedRowCount(),
                selected: gridApi.getSelectedRows().length
            };
        }

        function setCurrentUser(userId) {
            currentUserId = userId;
        }

        // ── API pubblica standard ─────────────────────────────────────────────
        const api = {
            init,
            getState,
            setState,
            resetState,
            setQuickFilter,
            exportCsv,
            setUiVars,
            toggleColumnPanel,
            getAllColumns,
            setColumnVisible,
            getStats,
            setCurrentUser,
            isInitialized: () => isGridInitialized
        };

        // ── Opzione: DotNet reference ─────────────────────────────────────────
        if (config.dotNetRefVar) {
            api.registerDotNetRef = (ref) => {
                window[config.dotNetRefVar] = ref;
                console.log('[' + config.namespace + '] DotNetRef registrato');
            };
            // Alias per retrocompatibilità (commesseGrid usava setDotNetRef)
            api.setDotNetRef = api.registerDotNetRef;
        }

        // ── Opzione: Ricetta column ───────────────────────────────────────────
        if (config.dotNetRefVar && config.hasRicetta) {
            api.openRicetta = (codiceArticolo) => {
                window.ricettaColumnShared.openRicettaDialog(
                    codiceArticolo,
                    window[config.dotNetRefVar],
                    config.namespace
                );
            };            // Apre dialog ImportaRicettaMacchinaDialog per celle senza ricetta
            api.openImportaRicetta = (codiceArticolo) => {
                window.ricettaColumnShared.openImportaRicettaDialog(
                    codiceArticolo,
                    window[config.dotNetRefVar],
                    config.namespace
                );
            };        }

        // ── Opzione: updateData per aggiornamento riga in-place (anime) ───────
        if (config.hasUpdateData) {
            api.updateData = (data, selectedId) => {
                if (!gridApi) return;
                if (selectedId) {
                    let updated = false;
                    gridApi.forEachNode(node => {
                        if (node.data && node.data.id === selectedId) {
                            const newData = data.find(item => item.id === selectedId);
                            if (newData) {
                                node.setData(newData);
                                node.setSelected(true);
                                gridApi.ensureNodeVisible(node, 'middle');
                                gridApi.refreshCells({ rowNodes: [node], force: true });
                                gridApi.flashCells({ rowNodes: [node] });
                                updated = true;
                            }
                        }
                    });
                    if (!updated) gridApi.setGridOption('rowData', data);
                } else {
                    gridApi.setGridOption('rowData', data);
                }
            };

            // Refresha solo la cella fotoPreview di una riga (usato dopo upload foto da dialog)
            // Chiamare anche quando il dialog viene annullato, per mostrare foto appena caricate
            api.refreshFotoRow = (animeId) => {
                if (!gridApi) return;
                gridApi.forEachNode(node => {
                    if (node.data && node.data.id === animeId) {
                        gridApi.refreshCells({ rowNodes: [node], columns: ['fotoPreview'], force: true });
                    }
                });
            };
        }

        // ── Registra il namespace su window ──────────────────────────────────
        window[config.namespace] = api;
        console.log('[agGridFactory] Registered:', config.namespace);
    }

    // ── refreshAllGrids: chiamabile da Blazor (MainLayout.ToggleTheme) ──────────
    // Forza re-render di tutte le celle registrate. NON necessario per i colori
    // basati su cellClassRules + CSS (il cascade gestisce automaticamente il tema),
    // ma utile per invalidare altre logiche di rendering custom.
    function refreshAllGrids() {
        _registeredApis.forEach(function (entry) {
            try { entry.api.refreshCells({ force: true }); } catch (e) {}
        });
    }

    return { setup, refreshAllGrids };
})();
