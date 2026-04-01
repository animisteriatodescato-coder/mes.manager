using MESManager.Domain.Enums;

namespace MESManager.Domain.Entities;

/// <summary>
/// Singola attività compilata all'interno di una scheda manutenzione.
/// </summary>
public class ManutenzioneRiga
{
    public Guid Id { get; set; }
    public Guid SchedaId { get; set; }
    public Guid AttivitaId { get; set; }
    public EsitoAttivitaManutenzione Esito { get; set; } = EsitoAttivitaManutenzione.NonEseguita;
    public string? Commento { get; set; }
    public string? FotoPath { get; set; }

    /// <summary>
    /// Snapshot del contatore cicli PLC al momento della compilazione.
    /// NULL finché non verrà implementata l'integrazione PLC.
    /// </summary>
    public int? CicloMacchinaAlEsecuzione { get; set; }

    // Navigazioni
    public ManutenzioneScheda Scheda { get; set; } = null!;
    public ManutenzioneAttivita Attivita { get; set; } = null!;
}
