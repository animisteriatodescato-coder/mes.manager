namespace MESManager.Application.DTOs;

/// <summary>
/// DTO per rappresentare un singolo entry (offset + valore) di un DB PLC
/// Usato per visualizzazione DB55 e DB52 nel popup DB Viewer
/// </summary>
public class PlcDbEntryDto
{
    /// <summary>
    /// Offset all'interno del Data Block
    /// </summary>
    public int Offset { get; set; }
    
    /// <summary>
    /// Nome parametro (es: "CicliFatti", "QuantitaDaProdurre", etc.)
    /// </summary>
    public string Nome { get; set; } = string.Empty;
    
    /// <summary>
    /// Valore corrente (convertito in stringa per visualizzazione)
    /// </summary>
    public string Valore { get; set; } = string.Empty;
    
    /// <summary>
    /// Tipo dato PLC (INT, REAL, STRING, BOOL, etc.)
    /// </summary>
    public string Tipo { get; set; } = string.Empty;
    
    /// <summary>
    /// Unità di misura (se applicabile)
    /// </summary>
    public string? UnitaMisura { get; set; }
}
