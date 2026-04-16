using MESManager.Domain.Enums;

namespace MESManager.Domain.Entities;

/// <summary>
/// Singola attività compilata all'interno di una scheda manutenzione cassa d'anima.
/// </summary>
public class ManutenzioneCassaRiga
{
    public Guid Id { get; set; }
    public Guid SchedaId { get; set; }
    public Guid AttivitaId { get; set; }
    public EsitoAttivitaManutenzione Esito { get; set; } = EsitoAttivitaManutenzione.NonEseguita;
    public string? Commento { get; set; }

    // Navigazioni
    public ManutenzioneCassaScheda Scheda { get; set; } = null!;
    public ManutenzioneCassaAttivita Attivita { get; set; } = null!;
}
