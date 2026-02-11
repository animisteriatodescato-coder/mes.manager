namespace MESManager.Application.DTOs;

/// <summary>
/// EventArgs per notificare cambio commessa rilevato su DB55
/// Trigger per auto-load ricetta prossima
/// </summary>
public class CommessaCambiataEventArgs : EventArgs
{
    /// <summary>
    /// ID macchina dove è avvenuto il cambio
    /// </summary>
    public Guid MacchinaId { get; set; }
    
    /// <summary>
    /// Nuovo barcode/codice commessa letto da DB55
    /// </summary>
    public string NuovoBarcode { get; set; } = string.Empty;
    
    /// <summary>
    /// Vecchio barcode/codice commessa precedente
    /// </summary>
    public string VecchioBarcode { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp rilevamento cambio (UTC)
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Numero macchina (per logging)
    /// </summary>
    public int? NumeroMacchina { get; set; }
}
