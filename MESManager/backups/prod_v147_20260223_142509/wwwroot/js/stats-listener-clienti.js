// Stats listener for Catalogo Clienti grid
export function subscribeToStatsChanges(dotNetHelper) {
    console.log('[CLIENTI-STATS] Subscribing to stats changes');
    
    // Listen to grid stats changes
    window.addEventListener('clientiGridStatsChanged', async () => {
        try {
            await dotNetHelper.invokeMethodAsync('UpdateStats');
        } catch (err) {
            console.warn('[CLIENTI-STATS] Error updating stats:', err);
        }
    });
    
    console.log('[CLIENTI-STATS] Subscribed successfully');
}
