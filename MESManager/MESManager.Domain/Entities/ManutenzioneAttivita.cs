using MESManager.Domain.Enums;

namespace MESManager.Domain.Entities;

/// <summary>
/// Catalogo delle attività di manutenzione eseguibili, distinte per frequenza.
/// Fonte di verità per la generazione automatica delle righe nelle schede.
/// </summary>
public class ManutenzioneAttivita
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public TipoFrequenzaManutenzione TipoFrequenza { get; set; }
    public int Ordine { get; set; }
    public bool Attiva { get; set; } = true;

    /// <summary>
    /// Dimensione font etichetta in griglia (px). Default 11.
    /// </summary>
    public int FontSize { get; set; } = 11;

    /// <summary>
    /// Futuro: soglia cicli PLC per manutenzione preventiva.
    /// NULL = trigger temporale (Settimanale/Mensile). Non-null = trigger a cicli.
    /// </summary>
    public int? CicliSogliaPLC { get; set; }

    // Navigazioni
    public ICollection<ManutenzioneRiga> Righe { get; set; } = new List<ManutenzioneRiga>();
}
