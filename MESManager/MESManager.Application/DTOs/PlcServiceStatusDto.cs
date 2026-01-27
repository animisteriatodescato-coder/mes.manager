using MESManager.Domain.Entities;

namespace MESManager.Application.DTOs;

public class PlcServiceStatusDto
{
    public bool IsRunning { get; set; }
    public DateTime? ServiceStartTime { get; set; }
    public DateTime? LastHeartbeat { get; set; }
    public string ServiceVersion { get; set; } = string.Empty;
    
    public int PollingIntervalSeconds { get; set; }
    public bool EnableRealtime { get; set; }
    public bool EnableStorico { get; set; }
    public bool EnableEvents { get; set; }
    
    public int TotalSyncCount { get; set; }
    public int TotalErrorCount { get; set; }
    public DateTime? LastSyncTime { get; set; }
    public DateTime? LastErrorTime { get; set; }
    public string? LastErrorMessage { get; set; }
    
    public int MachinesConfigured { get; set; }
    public int MachinesConnected { get; set; }
    
    /// <summary>
    /// True se LastHeartbeat è negli ultimi 30 secondi
    /// </summary>
    public bool IsAlive => LastHeartbeat.HasValue && 
                           (DateTime.Now - LastHeartbeat.Value).TotalSeconds < 30;
    
    /// <summary>
    /// Tempo dall'ultimo heartbeat
    /// </summary>
    public TimeSpan? TimeSinceHeartbeat => LastHeartbeat.HasValue 
        ? DateTime.Now - LastHeartbeat.Value 
        : null;
}

public class PlcSyncLogDto
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid? MacchinaId { get; set; }
    public string? MacchinaNumero { get; set; }
    public string Level { get; set; } = "Info";
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
}
