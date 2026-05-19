using MESManager.Domain.Enums;

namespace MESManager.Domain.Entities;

/// <summary>
/// Sessione giornaliera di controlli qualita in-process per una macchina.
/// </summary>
public class ControlloQualitaScheda
{
    public Guid Id { get; set; }
    public Guid MacchinaId { get; set; }
    public DateTime DataEsecuzione { get; set; }

    public string? OperatoreId { get; set; }
    public string? NomeOperatore { get; set; }

    public string? Note { get; set; }
    public StatoSchedaControlloQualita Stato { get; set; } = StatoSchedaControlloQualita.InCompilazione;
    public DateTime? DataChiusura { get; set; }

    public Macchina Macchina { get; set; } = null!;
    public ICollection<ControlloQualitaRiga> Righe { get; set; } = new List<ControlloQualitaRiga>();
}
