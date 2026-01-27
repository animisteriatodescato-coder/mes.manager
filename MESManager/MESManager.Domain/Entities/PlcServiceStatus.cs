namespace MESManager.Domain.Entities;

/// <summary>
/// Stato del servizio PlcSync - una sola riga nel DB
/// </summary>
public class PlcServiceStatus
{
    public int Id { get; set; } = 1; // Sempre 1, riga unica
    
    // === STATO SERVIZIO ===
    public bool IsRunning { get; set; }
    public DateTime? ServiceStartTime { get; set; }
    public DateTime? LastHeartbeat { get; set; }
    public string ServiceVersion { get; set; } = string.Empty;
    
    // === IMPOSTAZIONI ===
    public int PollingIntervalSeconds { get; set; } = 4;
    public bool EnableRealtime { get; set; } = true;
    public bool EnableStorico { get; set; } = true;
    public bool EnableEvents { get; set; } = true;
    
    // === STATISTICHE ===
    public int TotalSyncCount { get; set; }
    public int TotalErrorCount { get; set; }
    public DateTime? LastSyncTime { get; set; }
    public DateTime? LastErrorTime { get; set; }
    public string? LastErrorMessage { get; set; }
    
    // === MACCHINE ===
    public int MachinesConfigured { get; set; }
    public int MachinesConnected { get; set; }
}
