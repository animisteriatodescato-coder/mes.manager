namespace MESManager.Application.DTOs;

/// <summary>
/// Risultato del salvataggio parametri DB56 come ricetta articolo
/// </summary>
public class SaveRecipeFromPlcResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public Guid RicettaId { get; set; }
    public int NumeroParametriSalvati { get; set; }
}
