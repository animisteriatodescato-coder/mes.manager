/**
 * Commesse Grid - Catalogo Commesse
 * ==================================
 * SOLO columnDefs + config. Tutta la logica condivisa è in ag-grid-factory.js
 * Per modificare comportamento comune a tutti i grid: modifica ag-grid-factory.js
 */
(function () {
    function hasDatiEtichettaCompleti(data) {
        return data && data.codiceAnime && data.clienteDisplay;
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
                if (window.commesseGridDotNetRef) {
                    window.commesseGridDotNetRef.invokeMethodAsync('OnPrintLabelClick', params.data);
                }
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
        // FOTO - Anteprima prima immagine anima per priorità (n=1)
        window.fotoPreviewShared.createColumnDef({
            codiceArticoloField: 'articoloCodice',
            photoIndex: 2
        }),
        // RICETTA - Badge con numero parametri (shared component)
        window.ricettaColumnShared.createColumnDef({
            fieldPrefix: 'camelCase',
            gridNamespace: 'commesseGrid',
            codiceArticoloField: 'articoloCodice'
        }),
        { 
            field: 'description', 
            headerName: 'Descrizione', 
            sortable: true, 
            filter: true, 
            width: 300 
        },
        // Prezzo articolo - definizione centralizzata in anime-columns-shared.js
        window.animeColumnsShared.getPrezzoArticoloColumn(),
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
            // cellClassRules: AG Grid aggiunge la classe CSS alla cella.
            // Il colore è definito in app.css (.mes-stato-*), che risponde
            // automaticamente a .mud-theme-dark senza nessun JS refresh.
            cellClassRules: {
                'mes-stato-aperta': params => params.value === 'Aperta',
                'mes-stato-chiusa':  params => params.value === 'Chiusa'
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

    window.agGridFactory.setup({
        namespace: 'commesseGrid',
        columnDefs: columnDefs,
        exportFileName: 'commesse_export.csv',
        storageKeyBase: 'commesse-grid-columnState',
        eventName: 'commesseGridStatsChanged',
        rowSelection: 'single',
        dotNetRefVar: 'commesseGridDotNetRef',
        hasRicetta: true,
        onRowDoubleClicked: (event) => {
            if (window.commesseGridDotNetRef && event.data && event.data.articoloCodice) {
                window.commesseGridDotNetRef.invokeMethodAsync('OnRowDoubleClick', event.data.articoloCodice);
            }
        }
    });
})();
