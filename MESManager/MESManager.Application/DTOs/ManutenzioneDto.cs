using MESManager.Domain.Enums;

namespace MESManager.Application.DTOs;

public class ManutenzioneAttivitaDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public TipoFrequenzaManutenzione TipoFrequenza { get; set; }
    public int Ordine { get; set; }
    public bool Attiva { get; set; } = true;
    public int FontSize { get; set; } = 11;
    public int? CicliSogliaPLC { get; set; }
}

public class AnomaliaStandardDto
{
    public Guid Id { get; set; }
    public string Testo { get; set; } = string.Empty;
    public int Ordine { get; set; }
    public bool Attiva { get; set; } = true;
}

public class ManutenzioneRigaDto
{
    public Guid Id { get; set; }
    public Guid SchedaId { get; set; }
    public Guid AttivitaId { get; set; }
    public string NomeAttivita { get; set; } = string.Empty;
    public int OrdineAttivita { get; set; }
    public EsitoAttivitaManutenzione Esito { get; set; } = EsitoAttivitaManutenzione.NonEseguita;
    public string? Commento { get; set; }
    public string? FotoPath { get; set; }
    public int? CicloMacchinaAlEsecuzione { get; set; }
}

public class ManutenzioneSchedaDto
{
    public Guid Id { get; set; }
    public Guid MacchinaId { get; set; }
    public string NomeMacchina { get; set; } = string.Empty;
    public string CodiceMacchina { get; set; } = string.Empty;
    public TipoFrequenzaManutenzione TipoFrequenza { get; set; }
    public DateTime DataEsecuzione { get; set; }
    public string? OperatoreId { get; set; }
    public string? NomeOperatore { get; set; }
    public string? Note { get; set; }
    public StatoSchedaManutenzione Stato { get; set; } = StatoSchedaManutenzione.InCompilazione;
    public DateTime? DataChiusura { get; set; }
    public List<ManutenzioneRigaDto> Righe { get; set; } = new();
}

public class NuovaSchedaRequest
{
    public Guid MacchinaId { get; set; }
    public TipoFrequenzaManutenzione TipoFrequenza { get; set; }
    public DateTime DataEsecuzione { get; set; } = DateTime.Today;
    public string? OperatoreId { get; set; }
    public string? NomeOperatore { get; set; }
    public string? Note { get; set; }
}
