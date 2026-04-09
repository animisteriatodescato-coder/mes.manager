namespace MESManager.Domain.Entities;

/// <summary>
/// Anomalie standard disponibili come selezione rapida nel dropdown
/// durante la compilazione della griglia manutenzione.
/// </summary>
public class AnomaliaStandardManutenzione
{
    public Guid Id { get; set; }
    public string Testo { get; set; } = string.Empty;
    public int Ordine { get; set; }
    public bool Attiva { get; set; } = true;
}
