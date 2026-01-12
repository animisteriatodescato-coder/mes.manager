using System.Text.Json;
using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;
using MESManager.PlcSync.Models;
using Microsoft.EntityFrameworkCore;

namespace MESManager.PlcSync.Services;

public class PlcSyncService
{
    private readonly ILogger<PlcSyncService> _logger;
    private readonly IDbContextFactory<MesManagerDbContext> _contextFactory;

    public PlcSyncService(
        ILogger<PlcSyncService> logger,
        IDbContextFactory<MesManagerDbContext> contextFactory)
    {
        _logger = logger;
        _contextFactory = contextFactory;
    }

    public async Task SyncSnapshotAsync(Guid macchinaId, PlcSnapshot snapshot, MachineState? state, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            // 1. UPSERT PLCRealtime (sempre)
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

            // Risolvi operatore
            realtime.OperatoreId = await ResolveOperatoreIdAsync(context, snapshot.NumeroOperatore, cancellationToken);

            await context.SaveChangesAsync(cancellationToken);

            // 2. INSERT PLCStorico (solo se cambio significativo)
            bool hasChange = state != null && 
                             (state.LastStato != snapshot.StatoMacchina || 
                              state.LastNumeroOperatore != snapshot.NumeroOperatore);

            if (hasChange)
            {
                var storico = new PLCStorico
                {
                    Id = Guid.NewGuid(),
                    MacchinaId = macchinaId,
                    DataOra = snapshot.Timestamp,
                    Dati = JsonSerializer.Serialize(snapshot),
                    StatoMacchina = snapshot.StatoMacchina,
                    OperatoreId = realtime.OperatoreId
                };

                context.PLCStorico.Add(storico);
                await context.SaveChangesAsync(cancellationToken);

                if (state != null)
                {
                    state.LastStato = snapshot.StatoMacchina;
                    state.LastNumeroOperatore = snapshot.NumeroOperatore;
                }

                _logger.LogInformation("Snapshot storico salvato per macchina {MacchinaId} - Stato: {Stato}", 
                    macchinaId, snapshot.StatoMacchina);
            }

            // 3. INSERT EventoPLC (se eventi 0→1)
            await ProcessEventsAsync(context, macchinaId, snapshot, cancellationToken);
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
}
