namespace MESManager.Domain.Entities;

/// <summary>
/// Catalogo delle attività di manutenzione per le casse d'anima.
/// Fonte di verità per la generazione automatica delle righe nelle schede.
/// </summary>
public class ManutenzioneCassaAttivita
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int Ordine { get; set; }
    public bool Attiva { get; set; } = true;

    /// <summary>
    /// Dimensione font etichetta in griglia (px). Default 11.
    /// </summary>
    public int FontSize { get; set; } = 11;

    // Navigazioni
    public ICollection<ManutenzioneCassaRiga> Righe { get; set; } = new List<ManutenzioneCassaRiga>();
}
