using MESManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MESManager.Infrastructure.Services;

public interface IPlcSyncCoordinator
{
    Task<PlcSyncResult> SyncMacchinaAsync(Guid macchinaId);
    Task<List<PlcSyncResult>> SyncTutteMacchineAsync();
}

public class PlcSyncCoordinator : IPlcSyncCoordinator
{
    private readonly MesManagerDbContext _context;
    private readonly ILogger<PlcSyncCoordinator> _logger;

    public PlcSyncCoordinator(MesManagerDbContext context, ILogger<PlcSyncCoordinator> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PlcSyncResult> SyncMacchinaAsync(Guid macchinaId)
    {
        var result = new PlcSyncResult { MacchinaId = macchinaId };

        try
        {
            var macchina = await _context.Macchine
                .FirstOrDefaultAsync(m => m.Id == macchinaId);

            if (macchina == null)
            {
                result.Successo = false;
                result.MessaggioErrore = $"Macchina con ID {macchinaId} non trovata";
                return result;
            }

            // TODO: Implementare logica sync manuale con PlcConnectionService/PlcReaderService
            // Per ora simuliamo un aggiornamento timestamp
            var plcRealtime = await _context.PLCRealtime
                .FirstOrDefaultAsync(p => p.MacchinaId == macchinaId);

            if (plcRealtime != null)
            {
                plcRealtime.DataUltimoAggiornamento = DateTime.Now;
                await _context.SaveChangesAsync();
                result.RecordAggiornati = 1;
            }

            result.Successo = true;
            result.MacchinaCodiceMacchina = macchina.Codice;
            result.DataOra = DateTime.Now;

            _logger.LogInformation("Sincronizzazione manuale macchina {MacchinaId} completata", macchinaId);
        }
        catch (Exception ex)
        {
            result.Successo = false;
            result.MessaggioErrore = ex.Message;
            _logger.LogError(ex, "Errore durante sincronizzazione macchina {MacchinaId}", macchinaId);
        }

        return result;
    }

    public async Task<List<PlcSyncResult>> SyncTutteMacchineAsync()
    {
        var results = new List<PlcSyncResult>();

        try
        {
            var macchine = await _context.Macchine.ToListAsync();

            foreach (var macchina in macchine)
            {
                var result = await SyncMacchinaAsync(macchina.Id);
                results.Add(result);
            }

            _logger.LogInformation("Sincronizzazione manuale di {Count} macchine completata", macchine.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante sincronizzazione di tutte le macchine");
            
            results.Add(new PlcSyncResult
            {
                Successo = false,
                MessaggioErrore = $"Errore generale: {ex.Message}",
                DataOra = DateTime.Now
            });
        }

        return results;
    }
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
