using MESManager.Domain.Enums;

namespace MESManager.Application.DTOs;

public class ControlloQualitaAttivitaDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Dettaglio { get; set; }
    public int Ordine { get; set; }
    public bool Attiva { get; set; } = true;
    public int FontSize { get; set; } = 11;
    public bool QuandoNecessario { get; set; }
    public bool RichiedeNotaSeProblema { get; set; } = true;
    public string? MacchinaCodiceFiltro { get; set; }
}

public class ControlloQualitaRigaDto
{
    public Guid Id { get; set; }
    public Guid SchedaId { get; set; }
    public Guid AttivitaId { get; set; }
    public string NomeAttivita { get; set; } = string.Empty;
    public string? DettaglioAttivita { get; set; }
    public int OrdineAttivita { get; set; }
    public bool QuandoNecessario { get; set; }
    public bool RichiedeNotaSeProblema { get; set; }
    public EsitoControlloQualita Esito { get; set; } = EsitoControlloQualita.NonEseguito;
    public string? Commento { get; set; }
    public DateTime? DataUltimaModifica { get; set; }
}

public class ControlloQualitaSchedaDto
{
    public Guid Id { get; set; }
    public Guid MacchinaId { get; set; }
    public string NomeMacchina { get; set; } = string.Empty;
    public string CodiceMacchina { get; set; } = string.Empty;
    public DateTime DataEsecuzione { get; set; }
    public string? OperatoreId { get; set; }
    public string? NomeOperatore { get; set; }
    public string? Note { get; set; }
    public StatoSchedaControlloQualita Stato { get; set; } = StatoSchedaControlloQualita.InCompilazione;
    public DateTime? DataChiusura { get; set; }
    public List<ControlloQualitaRigaDto> Righe { get; set; } = new();
}

public class NuovaSchedaControlloQualitaRequest
{
    public Guid MacchinaId { get; set; }
    public DateTime DataEsecuzione { get; set; } = DateTime.Today;
    public string? OperatoreId { get; set; }
    public string? NomeOperatore { get; set; }
    public string? Note { get; set; }
}
