/**
 * Commesse Grid - Catalogo Commesse
 * ==================================
 * SOLO columnDefs + config. Tutta la logica condivisa è in ag-grid-factory.js
 * Per modificare comportamento comune a tutti i grid: modifica ag-grid-factory.js
 */
(function () {
    const columnDefs = [
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
        // FOTO - Anteprima seconda immagine anima (shared component)
        window.fotoPreviewShared.createColumnDef({
            codiceArticoloField: 'articoloCodice'
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
        hasRicetta: true
    });
})();
