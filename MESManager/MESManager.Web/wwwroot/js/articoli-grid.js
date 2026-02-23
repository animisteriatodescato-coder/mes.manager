/**
 * Articoli Grid - Catalogo Articoli
 * ===================================
 * SOLO columnDefs + config. Tutta la logica condivisa è in ag-grid-factory.js
 * Per modificare comportamento comune a tutti i grid: modifica ag-grid-factory.js
 */
(function () {
    const columnDefs = [
        { 
            field: 'codice', 
            headerName: 'Codice', 
            sortable: true, 
            filter: true, 
            width: 150,
            pinned: 'left'
        },
        { 
            field: 'descrizione', 
            headerName: 'Descrizione', 
            sortable: true, 
            filter: true, 
            width: 300 
        },
        { 
            field: 'prezzo', 
            headerName: 'Prezzo', 
            sortable: true, 
            filter: 'agNumberColumnFilter', 
            width: 120,
            type: 'numericColumn',
            valueFormatter: params => params.value ? '€ ' + params.value.toFixed(2) : ''
        },
        { 
            field: 'attivo', 
            headerName: 'Attivo', 
            sortable: true, 
            filter: true, 
            width: 100,
            valueFormatter: params => params.value ? 'Sì' : 'No'
        },
        { 
            field: 'ultimaModifica', 
            headerName: 'Ultima Modifica', 
            sortable: true, 
            filter: 'agDateColumnFilter', 
            width: 180,
            valueFormatter: params => params.value ? new Date(params.value).toLocaleString('it-IT') : ''
        },
        { 
            field: 'timestampSync', 
            headerName: 'Timestamp Sync', 
            sortable: true, 
            filter: 'agDateColumnFilter', 
            width: 180,
            valueFormatter: params => params.value ? new Date(params.value).toLocaleString('it-IT') : ''
        },
        { 
            field: 'id', 
            headerName: 'ID', 
            width: 280,
            sortable: true, 
            filter: true, 
            resizable: true,
            hide: true
        }
    ];

    window.agGridFactory.setup({
        namespace: 'articoliGrid',
        columnDefs: columnDefs,
        exportFileName: 'articoli_export.csv',
        storageKeyBase: 'articoli-grid-columnState',
        eventName: 'articoliGridStatsChanged',
        rowSelection: 'multiple'
    });
})();
