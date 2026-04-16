using MESManager.Domain.Enums;

namespace MESManager.Domain.Entities;

/// <summary>
/// Una sessione di manutenzione per una cassa d'anima specifica (identificata da CodiceCassa).
/// </summary>
public class ManutenzioneCassaScheda
{
    public Guid Id { get; set; }

    /// <summary>
    /// Codice cassa (da Anime.CodiceCassa). Stringa, non FK verso entità dedicata.
    /// </summary>
    public string CodiceCassa { get; set; } = string.Empty;

    public DateTime DataEsecuzione { get; set; }

    /// <summary>ID utente AspNetUsers (stringa Identity)</summary>
    public string? OperatoreId { get; set; }
    public string? NomeOperatore { get; set; }

    public string? Note { get; set; }
    public StatoSchedaManutenzione Stato { get; set; } = StatoSchedaManutenzione.InCompilazione;
    public DateTime? DataChiusura { get; set; }

    // Navigazioni
    public ICollection<ManutenzioneCassaRiga> Righe { get; set; } = new List<ManutenzioneCassaRiga>();
}
