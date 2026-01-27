using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MESManager.Infrastructure.Services;

public interface IPlcSyncCoordinator
{
    Task<PlcSyncResult> SyncMacchinaAsync(Guid macchinaId);
    Task<List<PlcSyncResult>> SyncTutteMacchineAsync();
}

/// <summary>
/// Coordinatore per la sincronizzazione manuale dei dati PLC.
/// Gestisce le richieste di sync dalla UI e coordina con il worker PlcSync.
/// </summary>
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
        var result = new PlcSyncResult 
        { 
            MacchinaId = macchinaId,
            DataOra = DateTime.Now
        };

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

            result.MacchinaCodiceMacchina = macchina.Codice;

            // Verifica se la macchina ha un IP PLC configurato
            if (string.IsNullOrWhiteSpace(macchina.IndirizzoPLC))
            {
                result.Successo = false;
                result.MessaggioErrore = $"Macchina {macchina.Codice} non ha un indirizzo PLC configurato";
                return result;
            }

            // Verifica lo stato del servizio PlcSync
            var serviceStatus = await _context.PlcServiceStatus.FirstOrDefaultAsync();
            if (serviceStatus == null || !serviceStatus.IsRunning)
            {
                result.Successo = false;
                result.MessaggioErrore = "Il servizio PlcSync non è in esecuzione";
                return result;
            }

            // Verifica se la macchina è connessa (dati recenti)
            var plcRealtime = await _context.PLCRealtime
                .FirstOrDefaultAsync(p => p.MacchinaId == macchinaId);

            if (plcRealtime == null)
            {
                // Crea un record vuoto per la macchina
                plcRealtime = new PLCRealtime
                {
                    Id = Guid.NewGuid(),
                    MacchinaId = macchinaId,
                    DataUltimoAggiornamento = DateTime.MinValue,
                    StatoMacchina = "In attesa di connessione..."
                };
                _context.PLCRealtime.Add(plcRealtime);
                await _context.SaveChangesAsync();
                
                result.Successo = true;
                result.RecordAggiornati = 1;
                result.MessaggioErrore = "Record creato. Il worker PlcSync aggiornerà i dati al prossimo ciclo.";
                
                _logger.LogInformation("Creato record PLCRealtime per macchina {MacchinaId} ({Codice})", 
                    macchinaId, macchina.Codice);
                return result;
            }

            // Verifica quanto sono vecchi i dati
            var dataAge = DateTime.Now - plcRealtime.DataUltimoAggiornamento;
            
            if (dataAge.TotalMinutes > 2)
            {
                // Dati vecchi - la macchina potrebbe essere disconnessa
                result.Successo = true;
                result.RecordAggiornati = 0;
                result.MessaggioErrore = $"Macchina {macchina.Codice} non connessa. Ultimo aggiornamento: {plcRealtime.DataUltimoAggiornamento:HH:mm:ss}";
                
                _logger.LogWarning("Macchina {MacchinaId} ({Codice}) non connessa - ultimo update {LastUpdate}", 
                    macchinaId, macchina.Codice, plcRealtime.DataUltimoAggiornamento);
            }
            else
            {
                // Dati recenti - la macchina è connessa
                result.Successo = true;
                result.RecordAggiornati = 1;
                
                _logger.LogInformation("Macchina {MacchinaId} ({Codice}) sincronizzata - stato: {Stato}, cicli: {Cicli}", 
                    macchinaId, macchina.Codice, plcRealtime.StatoMacchina, plcRealtime.CicliFatti);
            }

            // Log dell'operazione di sync
            var syncLog = new PlcSyncLog
            {
                Timestamp = DateTime.UtcNow,
                Level = result.Successo ? "Info" : "Warning",
                Message = $"Sync manuale macchina {macchina.Codice}",
                MacchinaId = macchinaId,
                MacchinaNumero = macchina.Codice,
                Details = result.MessaggioErrore
            };
            _context.PlcSyncLogs.Add(syncLog);
            await _context.SaveChangesAsync();
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
            // Prendi solo le macchine con IP PLC configurato
            var macchine = await _context.Macchine
                .Where(m => m.IndirizzoPLC != null && m.IndirizzoPLC != "")
                .OrderBy(m => m.Codice)
                .ToListAsync();

            if (!macchine.Any())
            {
                _logger.LogWarning("Nessuna macchina con IP PLC configurato");
                results.Add(new PlcSyncResult
                {
                    Successo = false,
                    MessaggioErrore = "Nessuna macchina con IP PLC configurato",
                    DataOra = DateTime.Now
                });
                return results;
            }

            foreach (var macchina in macchine)
            {
                var result = await SyncMacchinaAsync(macchina.Id);
                results.Add(result);
            }

            var successCount = results.Count(r => r.Successo);
            var errorCount = results.Count(r => !r.Successo);

            _logger.LogInformation("Sincronizzazione manuale completata: {Success} successi, {Errors} errori su {Total} macchine", 
                successCount, errorCount, macchine.Count);
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
