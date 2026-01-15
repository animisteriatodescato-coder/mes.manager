let dotNetRef = null;

export function subscribeToStatsChanges(dotNetReference) {
    dotNetRef = dotNetReference;
    
    window.addEventListener('commesseGridStatsChanged', async () => {
        if (dotNetRef) {
            await dotNetRef.invokeMethodAsync('UpdateStats');
        }
    });

    // Auto-salva stato quando cambia struttura grid
    let saveTimeout;
    window.addEventListener('commesseGridStateChanged', async () => {
        if (dotNetRef) {
            // Debounce per evitare troppi salvataggi durante resize
            clearTimeout(saveTimeout);
            saveTimeout = setTimeout(async () => {
                await dotNetRef.invokeMethodAsync('SaveGridStateFromJs');
            }, 500);
        }
    });
}
