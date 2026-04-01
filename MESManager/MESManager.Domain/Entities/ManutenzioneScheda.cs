using MESManager.Domain.Enums;

namespace MESManager.Domain.Entities;

/// <summary>
/// Una sessione di manutenzione (settimanale o mensile) per una macchina specifica.
/// </summary>
public class ManutenzioneScheda
{
    public Guid Id { get; set; }
    public Guid MacchinaId { get; set; }
    public TipoFrequenzaManutenzione TipoFrequenza { get; set; }
    public DateTime DataEsecuzione { get; set; }

    /// <summary>ID utente AspNetUsers (stringa Identity)</summary>
    public string? OperatoreId { get; set; }
    public string? NomeOperatore { get; set; }

    public string? Note { get; set; }
    public StatoSchedaManutenzione Stato { get; set; } = StatoSchedaManutenzione.InCompilazione;
    public DateTime? DataChiusura { get; set; }

    // Navigazioni
    public Macchina Macchina { get; set; } = null!;
    public ICollection<ManutenzioneRiga> Righe { get; set; } = new List<ManutenzioneRiga>();
}
