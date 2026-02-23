let dotNetRef = null;

export function subscribeToStatsChanges(dotNetReference) {
    dotNetRef = dotNetReference;
    
    window.addEventListener('programmaMacchineGridStatsChanged', async () => {
        if (dotNetRef) {
            await dotNetRef.invokeMethodAsync('UpdateStats');
        }
    });

    // Ascolta cambi di selezione macchina dalle Commesse Aperte
    window.addEventListener('commessaNumeroMacchinaChanged', async (e) => {
        console.log('Received commessaNumeroMacchinaChanged event:', e.detail);
        if (dotNetRef) {
            // Notifica il componente Blazor per ricaricare i dati
            await dotNetRef.invokeMethodAsync('OnMachineSelectionChanged', e.detail.id, e.detail.numeroMacchina);
        }
    });

    // Auto-salva stato quando cambia struttura grid
    let saveTimeout;
    window.addEventListener('programmaMacchineGridStateChanged', async () => {
        if (dotNetRef) {
            // Debounce per evitare troppi salvataggi durante resize
            clearTimeout(saveTimeout);
            saveTimeout = setTimeout(async () => {
                await dotNetRef.invokeMethodAsync('SaveGridStateFromJs');
            }, 500);
        }
    });
}
