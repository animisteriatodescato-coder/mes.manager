/**
 * Clienti Grid - Catalogo Clienti
 * ==================================
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
            field: 'ragioneSociale', 
            headerName: 'Ragione Sociale', 
            sortable: true, 
            filter: true, 
            width: 250 
        },
        { 
            field: 'email', 
            headerName: 'Email', 
            sortable: true, 
            filter: true, 
            width: 200 
        },
        { 
            field: 'note', 
            headerName: 'Note', 
            sortable: true, 
            filter: true, 
            width: 200 
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
        namespace: 'clientiGrid',
        columnDefs: columnDefs,
        exportFileName: 'clienti_export.csv',
        storageKeyBase: 'clienti-grid-columnState',
        eventName: 'clientiGridStatsChanged',
        rowSelection: 'multiple'
    });
})();
