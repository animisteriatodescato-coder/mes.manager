namespace MESManager.Application.DTOs;

public class AnalisiCommessaApertaReportDto
{
    public int Ordine { get; set; }
    public string CodiceCommessa { get; set; } = "";
    public string Stato { get; set; } = "";
    public DateTime? DataConsegna { get; set; }
    public int? NumeroMacchina { get; set; }
    public string? CodiceArticolo { get; set; }
    public string Cliente { get; set; } = "";
    public string Esito { get; set; } = "";
    public string Dettaglio { get; set; } = "";
    public bool AnalisiCompleta { get; set; }
    public AnalisiPrezziRigaDto? Analisi { get; set; }
}
