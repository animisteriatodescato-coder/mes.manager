/**
 * Anime Grid - Catalogo Anime
 * ============================
 * columnDefs + callback unici (auto-save API, doppio click).
 * Tutta la logica condivisa è in ag-grid-factory.js
 */
(function () {
    const columnDefs = [
        // READONLY - da sync commesse
        { field: 'id', headerName: 'ID', sortable: true, filter: true, width: 80, editable: false },
        { field: 'codiceArticolo', headerName: 'Codice', sortable: true, filter: true, width: 150, editable: false, cellStyle: {backgroundColor: '#f5f5f5'} },
        
        // RICETTA - Badge con numero parametri (shared component)
        Object.assign(
            window.ricettaColumnShared.createColumnDef({
                fieldPrefix: 'camelCase',
                gridNamespace: 'animeGrid',
                codiceArticoloField: 'codiceArticolo'
            }),
            { editable: false }
        ),
        
        { field: 'descrizioneArticolo', headerName: 'Descrizione Articolo', sortable: true, filter: true, width: 250, editable: false, cellStyle: {backgroundColor: '#f5f5f5'} },
        { field: 'cliente', headerName: 'Cliente', sortable: true, filter: true, width: 150, editable: false, cellStyle: {backgroundColor: '#f5f5f5'} },
        
        // EDITABLE - campi modificabili
        { field: 'ubicazione', headerName: 'Ubicazione', sortable: true, filter: true, width: 150, editable: true },
        { field: 'codiceCassa', headerName: 'Codice Cassa', sortable: true, filter: true, width: 120, editable: true },
        { field: 'codiceAnime', headerName: 'Codice Anima', sortable: true, filter: true, width: 120, editable: true },
        { field: 'unitaMisura', headerName: 'Unità Misura', sortable: true, filter: true, width: 100, editable: true },
        
        // Imballo - mostra descrizione
        { field: 'imballoDescrizione', headerName: 'Imballo', sortable: true, filter: true, width: 150, editable: false },
        
        { field: 'note', headerName: 'Note', sortable: true, filter: true, width: 200, editable: true, cellEditor: 'agLargeTextCellEditor' },
        
        // Macchine - mostra descrizione (nomi macchine)
        { field: 'macchineSuDisponibiliDescrizione', headerName: 'Macchine', sortable: true, filter: true, width: 150, editable: false },
        
        // Sabbia - mostra descrizione
        { field: 'sabbiaDescrizione', headerName: 'Sabbia', sortable: true, filter: true, width: 120, editable: false },
        
        { field: 'togliereSparo', headerName: 'Tog.Sparo', sortable: true, filter: true, width: 100, editable: false, valueFormatter: params => params.value === '1' ? 'Sì' : (params.value === '0' ? 'No' : '') },
        
        // Vernice - mostra descrizione
        { field: 'verniceDescrizione', headerName: 'Vernice', sortable: true, filter: true, width: 150, editable: false },
        
        // Colla - mostra descrizione
        { field: 'collaDescrizione', headerName: 'Colla', sortable: true, filter: true, width: 120, editable: false },
        
        { field: 'quantitaPiano', headerName: 'Qta Piano', sortable: true, filter: true, width: 100, editable: true, cellEditor: 'agNumberCellEditor' },
        { field: 'numeroPiani', headerName: 'N.Piani', sortable: true, filter: true, width: 90, editable: true, cellEditor: 'agNumberCellEditor' },
        { field: 'ciclo', headerName: 'Ciclo', sortable: true, filter: true, width: 150, editable: true },
        { field: 'peso', headerName: 'Peso', sortable: true, filter: true, width: 100, editable: true },
        { field: 'figure', headerName: 'Figure', sortable: true, filter: true, width: 120, editable: true },
        { field: 'maschere', headerName: 'Maschere', sortable: true, filter: true, width: 120, editable: true },
        { field: 'assemblata', headerName: 'Assemblata', sortable: true, filter: true, width: 120, editable: true },
        { field: 'armataL', headerName: 'Armata L', sortable: true, filter: true, width: 120, editable: true },
        { field: 'larghezza', headerName: 'Larghezza', sortable: true, filter: true, width: 100, editable: true, cellEditor: 'agNumberCellEditor' },
        { field: 'altezza', headerName: 'Altezza', sortable: true, filter: true, width: 100, editable: true, cellEditor: 'agNumberCellEditor' },
        { field: 'profondita', headerName: 'Profondità', sortable: true, filter: true, width: 100, editable: true, cellEditor: 'agNumberCellEditor' },
        
        { field: 'idArticolo', headerName: 'ID Articolo', sortable: true, filter: true, width: 100, editable: false },
        { 
            field: 'trasmettiTutto', 
            headerName: 'Trasmetti Tutto', 
            sortable: true, 
            filter: true, 
            width: 130,
            valueFormatter: params => params.value ? 'Sì' : 'No',
            editable: false
        },
        { field: 'allegato', headerName: 'Allegato', sortable: true, filter: true, width: 150, editable: true },
        { 
            field: 'numeroFoto', 
            headerName: 'N.Foto', 
            sortable: true, 
            filter: true, 
            width: 90,
            editable: false,
            cellStyle: params => params.value > 0 ? { backgroundColor: '#e8f5e9', fontWeight: 'bold' } : null,
            valueFormatter: params => params.value || 0
        },
        { 
            field: 'numeroDocumenti', 
            headerName: 'N.Doc', 
            sortable: true, 
            filter: true, 
            width: 90,
            editable: false,
            cellStyle: params => params.value > 0 ? { backgroundColor: '#e3f2fd', fontWeight: 'bold' } : null,
            valueFormatter: params => params.value || 0
        },
        { 
            field: 'dataModificaRecord', 
            headerName: 'Data Modifica', 
            sortable: true, 
            filter: true, 
            width: 150,
            valueFormatter: params => params.value ? new Date(params.value).toLocaleDateString('it-IT') : '',
            editable: false
        },
        { 
            field: 'utenteModificaRecord', 
            headerName: 'Utente Modifica', 
            sortable: true, 
            filter: true, 
            width: 150,
            editable: false
        },
        { 
            field: 'dataImportazione', 
            headerName: 'Data Importazione', 
            sortable: true, 
            filter: true, 
            width: 180,
            valueFormatter: params => params.value ? new Date(params.value).toLocaleString('it-IT') : '',
            editable: false
        }
    ];

    window.agGridFactory.setup({
        namespace: 'animeGrid',
        columnDefs: columnDefs,
        exportFileName: 'anime_export.csv',
        storageKeyBase: 'anime-grid-columnState',
        eventName: 'animeGridStatsChanged',
        rowSelection: 'single',
        dotNetRefVar: 'animeGridDotNetRef',
        hasRicetta: true,
        hasUpdateData: true,

        // Auto-salva modifica su API non appena l'utente cambia un valore nella cella
        onCellValueChanged: async (event) => {
            console.log('[animeGrid] Cell changed:', event.colDef.field, '=', event.newValue);
            try {
                const response = await fetch(`/api/Anime/${event.data.id}`, {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(event.data)
                });
                if (response.ok) {
                    console.log('[animeGrid] Anime updated successfully');
                } else {
                    console.error('[animeGrid] Failed to update anime:', response.statusText);
                }
            } catch (err) {
                console.error('[animeGrid] Error updating anime:', err);
            }
        },

        // Doppio click su riga -> apre dialog di modifica Blazor
        onRowDoubleClicked: (event) => {
            if (window.animeGridDotNetRef) {
                window.animeGridDotNetRef.invokeMethodAsync('OnRowDoubleClicked', event.data);
            }
        }
    });
})();
