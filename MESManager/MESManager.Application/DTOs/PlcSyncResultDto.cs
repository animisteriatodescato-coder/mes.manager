namespace MESManager.Application.DTOs;

public class PlcSyncResultDto
{
    public Guid MacchinaId { get; set; }
    public string MacchinaCodiceMacchina { get; set; } = string.Empty;
    public bool Successo { get; set; }
    public int RecordAggiornati { get; set; }
    public string? MessaggioErrore { get; set; }
    public DateTime DataOra { get; set; }
}
