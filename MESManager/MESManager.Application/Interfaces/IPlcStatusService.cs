using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces;

/// <summary>
/// Servizio per monitorare e configurare il PlcSync service
/// </summary>
public interface IPlcStatusService
{
    /// <summary>
    /// Ottiene lo stato corrente del servizio PlcSync
    /// </summary>
    Task<PlcServiceStatusDto?> GetServiceStatusAsync();
    
    /// <summary>
    /// Ottiene gli ultimi N log di sincronizzazione
    /// </summary>
    Task<List<PlcSyncLogDto>> GetRecentLogsAsync(int count = 50, string? levelFilter = null);
    
    /// <summary>
    /// Ottiene i log di una specifica macchina
    /// </summary>
    Task<List<PlcSyncLogDto>> GetMachineLogsAsync(Guid macchinaId, int count = 50);
    
    /// <summary>
    /// Aggiorna le impostazioni del servizio (polling, enable flags)
    /// </summary>
    Task<bool> UpdateSettingsAsync(int pollingIntervalSeconds, bool enableRealtime, bool enableStorico, bool enableEvents);
    
    /// <summary>
    /// Pulisce i log più vecchi di N giorni
    /// </summary>
    Task<int> CleanupOldLogsAsync(int daysToKeep = 7);
}
