namespace MESManager.Application.DTOs;

/// <summary>
/// Request per salvare parametri runtime dal PLC come ricetta articolo.
/// SORGENTE DATI: DB56 offset 100-196 (parametri esecuzione macchina)
/// </summary>
public class SaveRecipeFromPlcRequest
{
    /// <summary>
    /// ID macchina da cui leggere i parametri DB56
    /// </summary>
    public Guid MacchinaId { get; set; }
    
    /// <summary>
    /// Codice articolo del catalogo Anime per cui salvare la ricetta
    /// </summary>
    public string CodiceArticolo { get; set; } = string.Empty;
    
    /// <summary>
    /// Parametri DB56 (offset 100-196) da salvare come ricetta.
    /// Se null/vuoto, verranno letti automaticamente dal PLC.
    /// </summary>
    public List<PlcDbEntryDto>? Entries { get; set; }
}
