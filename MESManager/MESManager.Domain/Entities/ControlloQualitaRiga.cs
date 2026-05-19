using MESManager.Domain.Enums;

namespace MESManager.Domain.Entities;

/// <summary>
/// Esito di un singolo controllo qualita dentro una scheda macchina.
/// </summary>
public class ControlloQualitaRiga
{
    public Guid Id { get; set; }
    public Guid SchedaId { get; set; }
    public Guid AttivitaId { get; set; }
    public EsitoControlloQualita Esito { get; set; } = EsitoControlloQualita.NonEseguito;
    public string? Commento { get; set; }
    public DateTime? DataUltimaModifica { get; set; }

    public ControlloQualitaScheda Scheda { get; set; } = null!;
    public ControlloQualitaAttivita Attivita { get; set; } = null!;
}
