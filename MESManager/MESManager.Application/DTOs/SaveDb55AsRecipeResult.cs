namespace MESManager.Application.DTOs;

/// <summary>
/// Risultato del salvataggio DB55 come ricetta articolo
/// </summary>
public class SaveDb55AsRecipeResult
{
    /// <summary>
    /// Indica se l'operazione è riuscita
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Messaggio di errore (se Success = false)
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// ID della ricetta creata/aggiornata
    /// </summary>
    public Guid? RicettaId { get; set; }
    
    /// <summary>
    /// Numero di parametri salvati
    /// </summary>
    public int NumeroParametriSalvati { get; set; }
    
    /// <summary>
    /// Timestamp dell'operazione
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
