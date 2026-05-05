using MESManager.Domain.Enums;

namespace MESManager.Domain.Entities;

/// <summary>
/// Log dei cambi di stato manuale delle schede manutenzione (sia Catalogo che Casse).
/// Unica tabella con discriminatore TipoScheda per evitare duplicazione.
/// </summary>
public class SchedaStatoLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>ID logico della scheda (ManutenzioneCassaScheda.Id o ManutenzioneScheda.Id)</summary>
    public Guid SchedaId { get; set; }

    /// <summary>Indica a quale modulo appartiene la scheda</summary>
    public TipoSchedaManutenzione TipoScheda { get; set; }

    public StatoSchedaManutenzione StatoPrecedente { get; set; }
    public StatoSchedaManutenzione StatoNuovo { get; set; }

    public DateTime DataCambio { get; set; } = DateTime.UtcNow;

    public string? OperatoreId { get; set; }
    public string? NomeOperatore { get; set; }
}
