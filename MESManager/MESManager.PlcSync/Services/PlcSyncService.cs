using System.Collections.Concurrent;
using System.Text.Json;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;
using MESManager.PlcSync.Configuration;
using MESManager.PlcSync.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MESManager.PlcSync.Services;

public class PlcSyncService : IPlcSyncService
{
    private readonly ILogger<PlcSyncService> _logger;
    private readonly IDbContextFactory<MesManagerDbContext> _contextFactory;
    private readonly PlcSyncSettings _settings;
    
    // Tracking ultimo barcode per rilevare cambiamenti
    private readonly ConcurrentDictionary<Guid, string> _ultimoBarcodeLetto = new();
    
    /// <summary>
    /// Evento scatenato quando cambia il barcode su DB55 (indica cambio commessa)
    /// </summary>
    public event EventHandler<CommessaCambiataEventArgs>? CommessaCambiata;

    public PlcSyncService(
        ILogger<PlcSyncService> logger,
        IDbContextFactory<MesManagerDbContext> contextFactory,
        IOptions<PlcSyncSettings> settings)
    {
        _logger = logger;
        _contextFactory = contextFactory;
        _settings = settings.Value;
    }

    public async Task SyncSnapshotAsync(Guid macchinaId, PlcSnapshot snapshot, MachineState? state, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            Guid? operatoreId = null;

            // 1. UPSERT PLCRealtime (se abilitato)
            if (_settings.EnableRealtime)
            {
                var realtime = await context.PLCRealtime
                    .FirstOrDefaultAsync(r => r.MacchinaId == macchinaId, cancellationToken);

                if (realtime == null)
                {
                    realtime = new PLCRealtime
                    {
                        Id = Guid.NewGuid(),
                        MacchinaId = macchinaId
                    };
                    context.PLCRealtime.Add(realtime);
                }

                // Map snapshot → realtime
                realtime.DataUltimoAggiornamento = snapshot.Timestamp;
                realtime.CicliFatti = snapshot.CicliFatti;
                realtime.QuantitaDaProdurre = snapshot.QuantitaDaProdurre;
                realtime.CicliScarti = snapshot.CicliScarti;
                realtime.BarcodeLavorazione = snapshot.BarcodeLavorazione;
                realtime.TempoMedioRilevato = snapshot.TempoMedioRilevato;
                realtime.TempoMedio = snapshot.TempoMedio;
                realtime.Figure = snapshot.Figure;
                realtime.StatoMacchina = snapshot.StatoMacchina;
                realtime.QuantitaRaggiunta = snapshot.QuantitaRaggiunta;
                realtime.NumeroOperatore = snapshot.NumeroOperatore;

                // Risolvi operatore
                realtime.OperatoreId = await ResolveOperatoreIdAsync(context, snapshot.NumeroOperatore, cancellationToken);
                operatoreId = realtime.OperatoreId;

                await context.SaveChangesAsync(cancellationToken);
                
                // Rilevamento cambio barcode (trigger evento CommessaCambiata)
                DetectBarcodeChange(macchinaId, snapshot.BarcodeLavorazione.ToString());
            }
            else
            {
                // Risolvi operatore solo per uso storico/eventi
                operatoreId = await ResolveOperatoreIdAsync(context, snapshot.NumeroOperatore, cancellationToken);
            }

            // 2. INSERT PLCStorico (ogni 20 cicli o cambio significativo)
            bool hasChange = state != null && 
                             (state.LastStato != snapshot.StatoMacchina || 
                              state.LastNumeroOperatore != snapshot.NumeroOperatore);

            // Verifica se sono passati 20 cicli dall'ultimo salvataggio
            bool shouldSaveEvery20Cycles = false;
            if (state != null && snapshot.CicliFatti > 0)
            {
                int cyclesSinceLastSave = snapshot.CicliFatti - state.LastCicliFatti;
                shouldSaveEvery20Cycles = cyclesSinceLastSave >= 20;
            }

            if (_settings.EnableStorico && (hasChange || shouldSaveEvery20Cycles))
            {
                var storico = new PLCStorico
                {
                    Id = Guid.NewGuid(),
                    MacchinaId = macchinaId,
                    DataOra = snapshot.Timestamp,
                    Dati = JsonSerializer.Serialize(snapshot),
                    StatoMacchina = snapshot.StatoMacchina,
                    NumeroOperatore = snapshot.NumeroOperatore,
                    OperatoreId = operatoreId
                };

                context.PLCStorico.Add(storico);
                await context.SaveChangesAsync(cancellationToken);

                if (state != null)
                {
                    state.LastStato = snapshot.StatoMacchina;
                    state.LastNumeroOperatore = snapshot.NumeroOperatore;
                    state.LastCicliFatti = snapshot.CicliFatti;
                }

                string reason = hasChange ? "cambio stato/operatore" : "20 cicli";
                _logger.LogInformation("Snapshot storico salvato per macchina {MacchinaId} ({Reason}) - Stato: {Stato}, Cicli: {Cicli}", 
                    macchinaId, reason, snapshot.StatoMacchina, snapshot.CicliFatti);
            }

            // 3. INSERT EventoPLC (se eventi 0→1)
            if (_settings.EnableEvents)
            {
                await ProcessEventsAsync(context, macchinaId, snapshot, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante sincronizzazione snapshot per macchina {MacchinaId}", macchinaId);
        }
    }

    private async Task<Guid?> ResolveOperatoreIdAsync(MesManagerDbContext context, int numeroOperatore, CancellationToken cancellationToken)
    {
        if (numeroOperatore <= 0)
            return null;

        var operatore = await context.Operatori
            .FirstOrDefaultAsync(o => o.NumeroOperatore == numeroOperatore, cancellationToken);

        return operatore?.Id;
    }

    private async Task ProcessEventsAsync(MesManagerDbContext context, Guid macchinaId, PlcSnapshot snapshot, CancellationToken cancellationToken)
    {
        var events = new List<EventoPLC>();

        if (!string.IsNullOrEmpty(snapshot.NuovaProduzioneTs))
        {
            events.Add(new EventoPLC
            {
                Id = Guid.NewGuid(),
                MacchinaId = macchinaId,
                DataOra = snapshot.Timestamp,
                TipoEvento = PlcEventType.NuovaProduzione,
                Dettagli = $"Nuova produzione iniziata - Barcode: {snapshot.BarcodeLavorazione}"
            });
        }

        if (!string.IsNullOrEmpty(snapshot.InizioSetupTs))
        {
            events.Add(new EventoPLC
            {
                Id = Guid.NewGuid(),
                MacchinaId = macchinaId,
                DataOra = snapshot.Timestamp,
                TipoEvento = PlcEventType.InizioSetup
            });
        }

        if (!string.IsNullOrEmpty(snapshot.FineSetupTs))
        {
            events.Add(new EventoPLC
            {
                Id = Guid.NewGuid(),
                MacchinaId = macchinaId,
                DataOra = snapshot.Timestamp,
                TipoEvento = PlcEventType.FineSetup
            });
        }

        if (!string.IsNullOrEmpty(snapshot.InProduzioneTs))
        {
            events.Add(new EventoPLC
            {
                Id = Guid.NewGuid(),
                MacchinaId = macchinaId,
                DataOra = snapshot.Timestamp,
                TipoEvento = PlcEventType.InProduzione
            });
        }

        if (snapshot.QuantitaRaggiunta)
        {
            events.Add(new EventoPLC
            {
                Id = Guid.NewGuid(),
                MacchinaId = macchinaId,
                DataOra = snapshot.Timestamp,
                TipoEvento = PlcEventType.QuantitaRaggiunta,
                Dettagli = $"Quantità raggiunta: {snapshot.CicliFatti}/{snapshot.QuantitaDaProdurre}"
            });
        }

        if (events.Any())
        {
            context.EventiPLC.AddRange(events);
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Salvati {Count} eventi per macchina {MacchinaId}", events.Count, macchinaId);
        }
    }
    
    /// <summary>
    /// Rileva cambio barcode e triggera evento CommessaCambiata
    /// </summary>
    private void DetectBarcodeChange(Guid macchinaId, string? nuovoBarcode)
    {
        if (string.IsNullOrWhiteSpace(nuovoBarcode))
            return;
        
        // Recupera ultimo barcode registrato
        if (_ultimoBarcodeLetto.TryGetValue(macchinaId, out var vecchioBarcode))
        {
            // Barcode cambiato?
            if (vecchioBarcode != nuovoBarcode)
            {
                _logger.LogInformation("🔔 [BARCODE-CHANGE] Macchina {Id}: {Vecchio} → {Nuovo}", 
                    macchinaId, vecchioBarcode, nuovoBarcode);
                
                // Trigger evento
                var eventArgs = new CommessaCambiataEventArgs
                {
                    MacchinaId = macchinaId,
                    NuovoBarcode = nuovoBarcode,
                    VecchioBarcode = vecchioBarcode,
                    Timestamp = DateTime.UtcNow,
                    NumeroMacchina = null // TODO: potremmo recuperarlo se serve
                };
                
                CommessaCambiata?.Invoke(this, eventArgs);
                
                // Aggiorna cache
                _ultimoBarcodeLetto[macchinaId] = nuovoBarcode;
            }
        }
        else
        {
            // Prima volta che leggiamo barcode per questa macchina
            _logger.LogInformation("📌 [BARCODE-INIT] Macchina {Id}: primo barcode rilevato = {Barcode}", 
                macchinaId, nuovoBarcode);
            
            _ultimoBarcodeLetto[macchinaId] = nuovoBarcode;
        }
    }
}
