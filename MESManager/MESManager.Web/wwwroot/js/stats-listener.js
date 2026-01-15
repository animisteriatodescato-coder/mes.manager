let dotNetRef = null;

export function subscribeToStatsChanges(dotNetReference) {
    dotNetRef = dotNetReference;
    
    window.addEventListener('commesseGridStatsChanged', async () => {
        if (dotNetRef) {
            await dotNetRef.invokeMethodAsync('UpdateStats');
        }
    });
}
