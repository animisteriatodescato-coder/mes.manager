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
    public int CodiceRiferimento { get; set; }
    public string CodiceCassa { get; set; } = string.Empty;
    public DateTime DataEsecuzione { get; set; }
    public string? OperatoreId { get; set; }
    public string? NomeOperatore { get; set; }
    public string? Note { get; set; }
    public StatoSchedaManutenzione Stato { get; set; } = StatoSchedaManutenzione.InCompilazione;
    public DateTime? DataChiusura { get; set; }
    public List<ManutenzioneCassaRigaDto> Righe { get; set; } = new();
    /// <summary>Dati cliente ricavati dall'entity Anime (via CodiceCassa)</summary>
    public string? Cliente { get; set; }
    public string? ArticoloDescrizione { get; set; }
    public string? CodiceArticolo { get; set; }
    public List<string> Problematiche { get; set; } = new();
}

public class NuovaSchedaCassaRequest
{
    public string CodiceCassa { get; set; } = string.Empty;
    public DateTime DataEsecuzione { get; set; }
    public string? OperatoreId { get; set; }
    public string? NomeOperatore { get; set; }
    public string? Note { get; set; }
}

// ── Allegati ──────────────────────────────────────────────────────────────────

public class ManutenzioneCassaAllegatoDto
{
    public int Id { get; set; }
    public Guid SchedaId { get; set; }
    public string NomeFile { get; set; } = string.Empty;
    public string TipoFile { get; set; } = "Documento";
    public string Estensione { get; set; } = string.Empty;
    public string? Descrizione { get; set; }
    public long DimensioneBytes { get; set; }
    public DateTime DataCaricamento { get; set; }
    public bool IsFoto => TipoFile == "Foto";
    /// <summary>URL proxy per scaricare/visualizzare il file</summary>
    public string UrlProxy => $"/api/allegati-manutenzione-casse/{Id}/file";
}

public class AllegatiManutenzioneCassaResponse
{
    public List<ManutenzioneCassaAllegatoDto> Foto { get; set; } = new();
    public List<ManutenzioneCassaAllegatoDto> Documenti { get; set; } = new();
    public int Totale => Foto.Count + Documenti.Count;
}
