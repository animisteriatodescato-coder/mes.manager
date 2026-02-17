namespace MESManager.Application.DTOs;

/// <summary>
/// Risultato operazione scrittura ricetta su DB55 (offset 100+)
/// </summary>
public class RecipeWriteResult
{
    /// <summary>
    /// Indica se la scrittura è stata completata con successo
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Messaggio esplicativo (successo o errore)
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp scrittura (UTC)
    /// </summary>
    public DateTime WriteTimestamp { get; set; }
    
    /// <summary>
    /// Numero parametri scritti su DB55 (offset 100+)
    /// </summary>
    public int ParametersWritten { get; set; }
    
    /// <summary>
    /// Codice articolo della ricetta scritta
    /// </summary>
    public string? CodiceArticolo { get; set; }
    
    /// <summary>
    /// ID macchina target
    /// </summary>
    public Guid? MacchinaId { get; set; }
    
    /// <summary>
    /// Messaggio errore dettagliato (se Success = false)
    /// </summary>
    public string? ErrorMessage { get; set; }
}
