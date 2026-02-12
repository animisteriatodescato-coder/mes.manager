namespace MESManager.Application.DTOs;

/// <summary>
/// Request per salvare DB55 corrente come ricetta articolo nel database
/// </summary>
public class SaveDb55AsRecipeRequest
{
    /// <summary>
    /// ID macchina da cui leggere DB55
    /// </summary>
    public Guid MacchinaId { get; set; }
    
    /// <summary>
    /// Codice articolo del catalogo Anime per cui salvare la ricetta
    /// </summary>
    public string CodiceArticolo { get; set; } = string.Empty;
    
    /// <summary>
    /// Entries DB55 da salvare come parametri ricetta (opzionale, se non fornite vengono lette dal PLC)
    /// </summary>
    public List<PlcDbEntryDto>? Entries { get; set; }
    
    /// <summary>
    /// Utente che ha richiesto il salvataggio (per audit)
    /// </summary>
    public string? UtenteRichiesta { get; set; }
}
