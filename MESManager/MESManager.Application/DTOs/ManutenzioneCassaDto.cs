using MESManager.Domain.Enums;

namespace MESManager.Application.DTOs;

public class ManutenzioneCassaAttivitaDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int Ordine { get; set; }
    public bool Attiva { get; set; } = true;
    public int FontSize { get; set; } = 11;
}

public class ManutenzioneCassaRigaDto
{
    public Guid Id { get; set; }
    public Guid SchedaId { get; set; }
    public Guid AttivitaId { get; set; }
    public string NomeAttivita { get; set; } = string.Empty;
    public int OrdineAttivita { get; set; }
    public EsitoAttivitaManutenzione Esito { get; set; } = EsitoAttivitaManutenzione.NonEseguita;
    public string? Commento { get; set; }
}

public class ManutenzioneCassaSchedaDto
{
    public Guid Id { get; set; }
    public string CodiceCassa { get; set; } = string.Empty;
    public DateTime DataEsecuzione { get; set; }
    public string? OperatoreId { get; set; }
    public string? NomeOperatore { get; set; }
    public string? Note { get; set; }
    public StatoSchedaManutenzione Stato { get; set; } = StatoSchedaManutenzione.InCompilazione;
    public DateTime? DataChiusura { get; set; }
    public List<ManutenzioneCassaRigaDto> Righe { get; set; } = new();
}

public class NuovaSchedaCassaRequest
{
    public string CodiceCassa { get; set; } = string.Empty;
    public DateTime DataEsecuzione { get; set; }
    public string? OperatoreId { get; set; }
    public string? NomeOperatore { get; set; }
    public string? Note { get; set; }
}
