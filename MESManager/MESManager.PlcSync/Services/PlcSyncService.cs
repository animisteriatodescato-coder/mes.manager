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

                // Aggiorna timestamp eventi — usa il più recente disponibile (non sovrascrivere con null)
                if (!string.IsNullOrEmpty(snapshot.NuovaProduzioneTs))
                    realtime.UltimaNuovaProduzione = snapshot.Timestamp;
                if (!string.IsNullOrEmpty(snapshot.InizioSetupTs))
                    realtime.UltimoInizioSetup = snapshot.Timestamp;
                if (!string.IsNullOrEmpty(snapshot.FineSetupTs))
                    realtime.UltimoFineSetup = snapshot.Timestamp;

                // Risolvi operatore
                realtime.OperatoreId = await ResolveOperatoreIdAsync(context, snapshot.NumeroOperatore, cancellationToken);
                operatoreId = realtime.OperatoreId;

                await context.SaveChangesAsync(cancellationToken);

                // Rilevamento cambio barcode:
                // 1) Imposta NuovaProduzioneTs PRIMA del save storico (segnale affidabile vs pulse PLC)
                string newBarcodeStr = snapshot.BarcodeLavorazione.ToString();
                if (snapshot.BarcodeLavorazione > 0 &&
                    _ultimoBarcodeLetto.TryGetValue(macchinaId, out var lastTrackedBarcode) &&
                    lastTrackedBarcode != newBarcodeStr &&
                    string.IsNullOrEmpty(snapshot.NuovaProduzioneTs))
                {
                    snapshot.NuovaProduzioneTs = snapshot.Timestamp.ToString("dd.MM.yy HH:mm:ss");
                    _logger.LogInformation("Barcode cambiato ({Old} → {New}) → NuovaProduzione via barcode rilevata su macchina {Id}",
                        lastTrackedBarcode, newBarcodeStr, macchinaId);
                }

                // 2) Trigger evento CommessaCambiata e aggiorna cache barcode
                DetectBarcodeChange(macchinaId, newBarcodeStr);
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

            // Fix S1: salva anche quando il main loop ha catturato un evento (flag rimasto alto abbastanza a lungo)
            bool hasEventInSnapshot = !string.IsNullOrEmpty(snapshot.NuovaProduzioneTs) ||
                                      !string.IsNullOrEmpty(snapshot.InizioSetupTs)     ||
                                      !string.IsNullOrEmpty(snapshot.FineSetupTs)       ||
                                      !string.IsNullOrEmpty(snapshot.InProduzioneTs);

            if (_settings.EnableStorico && (hasChange || shouldSaveEvery20Cycles || hasEventInSnapshot))
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

    /// <summary>
    /// Salva un record PLCStorico con StatoMacchina = "NON CONNESSA" quando la macchina diventa
    /// irraggiungibile. Scrive il record una sola volta (non in loop): se l'ultimo stato salvato
    /// era già "NON CONNESSA", non fa nulla.
    /// Nota: il caso string.IsNullOrEmpty(LastStato) è intentenzionalmente incluso per gestire
    /// il boot di PlcSync quando la macchina è già offline (LastStato non ancora popolato).
    /// </summary>
    public async Task SaveDisconnessaAsync(Guid macchinaId, MachineState? state, CancellationToken cancellationToken = default)
    {
        // Guard: non spammare — scrivi solo al momento della disconnessione (transizione → NON CONNESSA)
        if (!_settings.EnableStorico)
            return;
        // Scrivi se: stato non nullo E stato attuale NON è già NON CONNESSA.
        // Rimosso il blocco su string.IsNullOrEmpty: al boot di PlcSync LastStato="" 
        // e la macchina può essere già offline → dobbiamo registrare la disconnessione.
        if (state == null || state.LastStato == "NON CONNESSA")
            return;

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var now = DateTime.Now;

            var storico = new PLCStorico
            {
                Id             = Guid.NewGuid(),
                MacchinaId     = macchinaId,
                DataOra        = now,          // Local time (coerente con snapshot.Timestamp)
                Dati           = "{}",
                StatoMacchina  = "NON CONNESSA",
                NumeroOperatore = 0,
                OperatoreId    = null
            };

            context.PLCStorico.Add(storico);

            // Aggiorna PLCRealtime immediatamente per riflettere la disconnessione
            var realtime = await context.PLCRealtime
                .FirstOrDefaultAsync(r => r.MacchinaId == macchinaId, cancellationToken);
            if (realtime != null)
            {
                realtime.StatoMacchina             = "NON CONNESSA";
                realtime.DataUltimoAggiornamento   = now;
            }

            await context.SaveChangesAsync(cancellationToken);

            // Aggiorna stato in memoria per evitare doppi inserimenti
            state.LastStato             = "NON CONNESSA";
            state.LastNumeroOperatore   = 0;

            _logger.LogInformation("Record NON CONNESSA salvato su PLCStorico per macchina {MacchinaId}", macchinaId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Errore salvataggio record NON CONNESSA per macchina {MacchinaId}", macchinaId);
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
    /// Fast event polling: rileva rising edge dei 4 flag evento dal mini-snapshot
    /// (letto ogni 500ms) e salva EventoPLC + PLCStorico immediatamente.
    /// I Prev* su MachineState vengono aggiornati qui, così il main loop
    /// non genera doppi eventi sugli stessi flag.
    /// </summary>
    public async Task ProcessEventFlagsAsync(Guid macchinaId, PlcEventFlagsSnapshot flags, MachineState state, CancellationToken ct = default)
    {
        string ts = flags.Timestamp.ToString("dd.MM.yy HH:mm:ss");

        var risingEdges = new List<(string tipo, string dettagli)>();

        if (!state.PrevNuovaProduzione && flags.NuovaProduzione)
            risingEdges.Add((PlcEventType.NuovaProduzione, $"Nuova produzione - {ts}"));
        if (!state.PrevInizioSetup && flags.InizioSetup)
            risingEdges.Add((PlcEventType.InizioSetup,     $"Inizio setup - {ts}"));
        if (!state.PrevFineSetup && flags.FineSetup)
            risingEdges.Add((PlcEventType.FineSetup,       $"Fine setup - {ts}"));
        if (!state.PrevInProduzione && flags.FineProduzione)
            risingEdges.Add((PlcEventType.InProduzione,    $"Fine produzione - {ts}"));

        // Aggiorna sempre i Prev* per tenere traccia dello stato corrente
        state.PrevNuovaProduzione = flags.NuovaProduzione;
        state.PrevInizioSetup     = flags.InizioSetup;
        state.PrevFineSetup       = flags.FineSetup;
        state.PrevInProduzione    = flags.FineProduzione;

        if (!risingEdges.Any()) return;

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            if (_settings.EnableEvents)
            {
                var eventi = risingEdges.Select(e => new EventoPLC
                {
                    Id         = Guid.NewGuid(),
                    MacchinaId = macchinaId,
                    DataOra    = flags.Timestamp,
                    TipoEvento = e.tipo,
                    Dettagli   = e.dettagli
                }).ToList();
                context.EventiPLC.AddRange(eventi);
            }

            // Aggiorna timestamp eventi in PLCRealtime anche dal fast polling
            if (_settings.EnableRealtime)
            {
                var rt = await context.PLCRealtime.FirstOrDefaultAsync(r => r.MacchinaId == macchinaId, ct);
                if (rt != null)
                {
                    if (risingEdges.Any(e => e.tipo == PlcEventType.NuovaProduzione))
                        rt.UltimaNuovaProduzione = flags.Timestamp;
                    if (risingEdges.Any(e => e.tipo == PlcEventType.InizioSetup))
                        rt.UltimoInizioSetup = flags.Timestamp;
                    if (risingEdges.Any(e => e.tipo == PlcEventType.FineSetup))
                        rt.UltimoFineSetup = flags.Timestamp;
                }
            }

            if (_settings.EnableStorico)
            {
                // Snapshot minimale con solo i Ts valorizzati per gli eventi rilevati
                var mini = new PlcSnapshot
                {
                    MacchinaId        = macchinaId,
                    Timestamp         = flags.Timestamp,
                    NuovaProduzioneTs = risingEdges.Any(e => e.tipo == PlcEventType.NuovaProduzione) ? ts : string.Empty,
                    InizioSetupTs     = risingEdges.Any(e => e.tipo == PlcEventType.InizioSetup)     ? ts : string.Empty,
                    FineSetupTs       = risingEdges.Any(e => e.tipo == PlcEventType.FineSetup)       ? ts : string.Empty,
                    InProduzioneTs    = risingEdges.Any(e => e.tipo == PlcEventType.InProduzione)    ? ts : string.Empty,
                };
                context.PLCStorico.Add(new PLCStorico
                {
                    Id              = Guid.NewGuid(),
                    MacchinaId      = macchinaId,
                    DataOra         = flags.Timestamp,
                    Dati            = JsonSerializer.Serialize(mini),
                    StatoMacchina   = string.Join("+", risingEdges.Select(e => e.tipo)),
                    NumeroOperatore = 0,
                    OperatoreId     = null
                });
            }

            await context.SaveChangesAsync(ct);

            _logger.LogInformation("⚡ [FAST-EVENT] Macchina {Id}: {Events}",
                macchinaId, string.Join(", ", risingEdges.Select(e => e.tipo)));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Errore salvataggio fast event per macchina {Id}", macchinaId);
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
