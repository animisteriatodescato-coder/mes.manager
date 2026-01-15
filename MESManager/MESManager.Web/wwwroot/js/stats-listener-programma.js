let dotNetRef = null;

export function subscribeToStatsChanges(dotNetReference) {
    dotNetRef = dotNetReference;
    
    window.addEventListener('programmaMacchineGridStatsChanged', async () => {
        if (dotNetRef) {
            await dotNetRef.invokeMethodAsync('UpdateStats');
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
