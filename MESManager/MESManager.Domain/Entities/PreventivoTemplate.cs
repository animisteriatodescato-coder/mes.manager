namespace MESManager.Domain.Entities;

/// <summary>
/// Template riutilizzabile per preventivi (v1.65.7).
/// Salva i parametri tecnici e commerciali di un preventivo tipo per recuperarlo rapidamente.
/// </summary>
public class PreventivoTemplate
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descrizione { get; set; }
    public DateTime DataCreazione { get; set; } = DateTime.UtcNow;
    /// <summary>Serializzazione JSON dei parametri (PreventivoDto zonder Id/Cliente/DataCreazione/Stato)</summary>
    public string ParametriJson { get; set; } = string.Empty;
}
