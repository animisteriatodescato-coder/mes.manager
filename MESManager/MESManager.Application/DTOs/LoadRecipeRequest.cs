namespace MESManager.Application.DTOs;

/// <summary>
/// Request per caricamento manuale ricetta su DB52
/// </summary>
public class LoadRecipeRequest
{
    /// <summary>
    /// ID macchina target
    /// </summary>
    public Guid MacchinaId { get; set; }
    
    /// <summary>
    /// Codice articolo della ricetta da caricare
    /// </summary>
    public string CodiceArticolo { get; set; } = string.Empty;
    
    /// <summary>
    /// Flag: se true, forza caricamento anche se articolo uguale a corrente
    /// </summary>
    public bool ForceReload { get; set; } = false;
    
    /// <summary>
    /// Utente che ha richiesto il caricamento manuale (per audit)
    /// </summary>
    public string? UtenteRichiesta { get; set; }
}
