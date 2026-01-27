namespace MESManager.Domain.Entities;

/// <summary>
/// Log delle sincronizzazioni PLC - memorizza eventi di sync
/// </summary>
public class PlcSyncLog
{
    public long Id { get; set; }
    
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Null = evento di servizio (start, stop, etc.)
    /// Valorizzato = evento specifico macchina
    /// </summary>
    public Guid? MacchinaId { get; set; }
    
    public string? MacchinaNumero { get; set; }
    
    /// <summary>
    /// Info, Warning, Error, Success
    /// </summary>
    public string Level { get; set; } = "Info";
    
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Dettagli aggiuntivi (es. stack trace per errori)
    /// </summary>
    public string? Details { get; set; }
    
    // Navigazione
    public Macchina? Macchina { get; set; }
}
