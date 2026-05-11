namespace MESManager.Application.Interfaces;

public interface IPlcSyncCoordinator
{
    Task<PlcSyncResult> SyncMacchinaAsync(Guid macchinaId);
    Task<List<PlcSyncResult>> SyncTutteMacchineAsync();
}

public class PlcSyncResult
{
    public Guid MacchinaId { get; set; }
    public string MacchinaCodiceMacchina { get; set; } = string.Empty;
    public bool Successo { get; set; }
    public int RecordAggiornati { get; set; }
    public string? MessaggioErrore { get; set; }
    public DateTime DataOra { get; set; }
}
