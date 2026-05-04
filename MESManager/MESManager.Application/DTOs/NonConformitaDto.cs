namespace MESManager.Application.DTOs;

public class NonConformitaDto
{
    public Guid Id { get; set; }
    public string CodiceArticolo { get; set; } = string.Empty;
    public string? DescrizioneArticolo { get; set; }
    public string? Cliente { get; set; }
    public DateTime DataSegnalazione { get; set; } = DateTime.Today;
    /// <summary>NonConformita | Segnalazione</summary>
    public string Tipo { get; set; } = "NonConformita";
    /// <summary>Bassa | Media | Alta | Critica</summary>
    public string Gravita { get; set; } = "Media";
    public string Descrizione { get; set; } = string.Empty;
    public string? AzioneCorrettiva { get; set; }
    /// <summary>Aperta | InGestione | Chiusa</summary>
    public string Stato { get; set; } = "Aperta";
    public string? CreatoDa { get; set; }
    public DateTime CreatoIl { get; set; }
    public DateTime? DataChiusura { get; set; }
}

/// <summary>
/// Alert unificato per produzione (S4): aggrega NC + future sorgenti (Manutenzioni, Note).
/// Consumato da Dashboard e PlcRealtime via IAlertProduzioneService.
/// </summary>
public class AlertProduzioneDto
{
    public Guid SourceId { get; set; }
    public string CodiceArticolo { get; set; } = string.Empty;
    /// <summary>NonConformita | Segnalazione | Manutenzione</summary>
    public string Tipo { get; set; } = string.Empty;
    /// <summary>Bassa | Media | Alta | Critica</summary>
    public string Gravita { get; set; } = string.Empty;
    /// <summary>Testo breve per il banner in produzione (cliente + inizio descrizione).</summary>
    public string Messaggio { get; set; } = string.Empty;
    public DateTime Data { get; set; }
    /// <summary>Fonte del dato: NonConformita | Manutenzione (futuro)</summary>
    public string Fonte { get; set; } = "NonConformita";
}
