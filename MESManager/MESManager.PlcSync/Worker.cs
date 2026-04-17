using System.Text.Json;
using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;
using MESManager.PlcSync.Configuration;
using MESManager.PlcSync.Services;
using Microsoft.EntityFrameworkCore;

namespace MESManager.PlcSync;

public class PlcSyncWorker : BackgroundService
{
    private readonly ILogger<PlcSyncWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IDbContextFactory<MesManagerDbContext> _contextFactory;
    private readonly PlcConnectionService _connectionService;
    private readonly PlcReaderService _readerService;
    private readonly PlcSyncService _syncService;
    private readonly PlcStatusWriterService _statusWriter;
    private readonly IHostApplicationLifetime _appLifetime;
    private PlcSyncSettings _settings = null!;
    private PlcServiceStatus? _dbSettings;
    private bool _isShuttingDown = false;

    public PlcSyncWorker(
        ILogger<PlcSyncWorker> logger,
        IConfiguration configuration,
        IDbContextFactory<MesManagerDbContext> contextFactory,
        PlcConnectionService connectionService,
        PlcReaderService readerService,
        PlcSyncService syncService,
        PlcStatusWriterService statusWriter,
        IHostApplicationLifetime appLifetime)
    {
        _logger = logger;
        _contextFactory = contextFactory;
        _configuration = configuration;
        _connectionService = connectionService;
        _readerService = readerService;
        _syncService = syncService;
        _statusWriter = statusWriter;
        _appLifetime = appLifetime;
        
        // Registra handler per shutdown graceful
        _appLifetime.ApplicationStopping.Register(OnApplicationStopping);
    }

    private void OnApplicationStopping()
    {
        _isShuttingDown = true;
        _logger.LogWarning("⚠️ Ricevuto segnale di shutdown - Chiusura connessioni PLC in corso...");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PlcSyncWorker avviato");

        // Carica settings da file config
        _settings = _configuration.GetSection("PlcSync").Get<PlcSyncSettings>() ?? new PlcSyncSettings();

        // Inizializza stato nel database e leggi impostazioni salvate
        _dbSettings = await _statusWriter.InitializeStatusAsync(_settings.PollingIntervalSeconds, stoppingToken);
        
        // Usa le impostazioni dal database se disponibili
        var pollingInterval = _dbSettings?.PollingIntervalSeconds ?? _settings.PollingIntervalSeconds;
        var enableRealtime = _dbSettings?.EnableRealtime ?? _settings.EnableRealtime;
        var enableStorico = _dbSettings?.EnableStorico ?? _settings.EnableStorico;
        var enableEvents = _dbSettings?.EnableEvents ?? _settings.EnableEvents;

        _logger.LogInformation("Impostazioni PlcSync: Polling={Polling}s, Realtime={Realtime}, Storico={Storico}, Events={Events}, ConfigPath={ConfigPath}",
            pollingInterval,
            enableRealtime,
            enableStorico,
            enableEvents,
            _settings.MachineConfigPath);

        await _statusWriter.LogInfoAsync($"Servizio avviato - Polling: {pollingInterval}s");

        // Carica configurazioni macchine
        var machines = await LoadMachineConfigsAsync();

        if (!machines.Any())
        {
            _logger.LogWarning("Nessuna macchina configurata. Servizio in attesa.");
            await _statusWriter.LogWarningAsync("Nessuna macchina configurata");
            await Task.Delay(Timeout.Infinite, stoppingToken);
            return;
        }

        var enabledCount = machines.Count(m => m.Enabled);
        var disabledCount = machines.Count - enabledCount;
        _logger.LogInformation("Macchine caricate: {Total}, abilitate: {Enabled}, disabilitate: {Disabled}", machines.Count, enabledCount, disabledCount);
        await _statusWriter.LogInfoAsync($"Macchine caricate: {machines.Count} ({enabledCount} abilitate)");

        // Connetti a tutte le macchine
        foreach (var machine in machines.Where(m => m.Enabled))
        {
            var connected = await _connectionService.ConnectAsync(machine, stoppingToken);
            var macchinaNumero = $"M{machine.Numero:D3}";
            if (connected)
            {
                await _statusWriter.LogSuccessAsync($"Connesso a {macchinaNumero}", machine.MacchinaId, macchinaNumero);
            }
            else
            {
                await _statusWriter.LogWarningAsync($"Connessione fallita a {macchinaNumero}", machine.MacchinaId, macchinaNumero);
            }
        }

        // Ripristina LastStato dal DB per ogni macchina: evita di scrivere record NON CONNESSA
        // spurii ad ogni restart del servizio quando la macchina era già offline.
        await RestoreLastStatiFromDbAsync(machines.Where(m => m.Enabled).ToList(), stoppingToken);

        int syncCycleCount = 0;
        int settingsReloadInterval = 10; // Ricarica impostazioni ogni 10 cicli

        // Loop principale
        while (!stoppingToken.IsCancellationRequested && !_isShuttingDown)
        {
            try
            {
                // Aggiorna heartbeat all'inizio del ciclo (anche se le connessioni falliscono)
                await _statusWriter.UpdateHeartbeatAsync(enabledCount, 0, incrementSyncCount: false, stoppingToken);
                
                var connectedMachines = 0;
                
                foreach (var machine in machines.Where(m => m.Enabled))
                {
                    try
                    {
                        // Leggi snapshot PLC
                        var snapshot = await _readerService.ReadSnapshotAsync(machine, stoppingToken);
                        
                        if (snapshot == null)
                        {
                            // Registra NON CONNESSA su PLCStorico (solo al momento della disconnessione)
                            var stateForDisconnect = _connectionService.GetMachineState(machine.MacchinaId);
                            await _syncService.SaveDisconnessaAsync(machine.MacchinaId, stateForDisconnect, stoppingToken);
                            // Tentativo riconnessione
                            await _connectionService.ReconnectAsync(machine, _settings.PlcDefaults.ReconnectDelayMs, stoppingToken);
                            continue;
                        }

                        connectedMachines++;

                        // Sync verso database
                        var state = _connectionService.GetMachineState(machine.MacchinaId);
                        await _syncService.SyncSnapshotAsync(machine.MacchinaId, snapshot, state, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        var macchinaNumero = $"M{machine.Numero:D3}";
                        _logger.LogError(ex, "Errore processamento macchina {MacchinaNumero}", macchinaNumero);
                        await _statusWriter.LogErrorAsync($"Errore sync {macchinaNumero}", machine.MacchinaId, macchinaNumero, ex.Message);
                    }
                }

                syncCycleCount++;

                // Aggiorna heartbeat finale con conteggio connessioni e incrementa sync count
                await _statusWriter.UpdateHeartbeatAsync(enabledCount, connectedMachines, incrementSyncCount: true, stoppingToken);

                // Ogni N cicli, ricarica le impostazioni dal database per hot-reload
                if (syncCycleCount % settingsReloadInterval == 0)
                {
                    var newSettings = await _statusWriter.ReloadSettingsAsync(stoppingToken);
                    if (newSettings != null && newSettings.PollingIntervalSeconds != pollingInterval)
                    {
                        pollingInterval = newSettings.PollingIntervalSeconds;
                        _logger.LogInformation("Polling interval aggiornato a {Polling}s", pollingInterval);
                        await _statusWriter.LogInfoAsync($"Polling interval cambiato a {pollingInterval}s");
                    }
                    
                    // Hot-reload IP macchine dal database
                    await ReloadMachineIpsAsync(machines, stoppingToken);
                }

                // Attesa inter-ciclo con fast event polling ogni 500ms
                await FastEventPollingDuringDelayAsync(
                    machines.Where(m => m.Enabled).ToList(),
                    pollingInterval,
                    stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel ciclo principale PlcSyncWorker");
                await _statusWriter.RecordErrorAsync(ex.Message, stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        // Servizio in arresto - Graceful shutdown
        _logger.LogWarning("🛑 PlcSyncWorker in fase di shutdown...");
        
        try
        {
            // Chiudi tutte le connessioni PLC in modo ordinato
            _connectionService.DisconnectAll();
            _logger.LogInformation("✅ Connessioni PLC chiuse correttamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante chiusura connessioni PLC");
        }
        
        await _statusWriter.LogInfoAsync("Servizio arrestato correttamente (graceful shutdown)");
        await _statusWriter.MarkStoppedAsync(CancellationToken.None);
        _logger.LogInformation("✅ PlcSyncWorker arrestato correttamente");
    }

    /// <summary>
    /// Durante l'attesa inter-ciclo (es. 4s) esegue un fast polling dei soli
    /// 4 flag evento ogni 500ms su tutte le macchine abilitate e connesse.
    /// Quando rileva un rising edge 0→1, salva subito EventoPLC + PLCStorico
    /// tramite ProcessEventFlagsAsync, aggiornando i Prev* in MachineState.
    /// Questo garantisce cattura ~90% degli eventi indipendentemente dal polling principale.
    /// </summary>
    private async Task FastEventPollingDuringDelayAsync(
        List<PlcMachineConfig> machines,
        int totalSeconds,
        CancellationToken ct)
    {
        const int fastIntervalMs = 500;
        int totalMs = totalSeconds * 1000;
        int elapsed  = 0;

        while (elapsed < totalMs && !ct.IsCancellationRequested && !_isShuttingDown)
        {
            await Task.Delay(fastIntervalMs, ct);
            elapsed += fastIntervalMs;

            foreach (var machine in machines)
            {
                try
                {
                    var flags = _readerService.ReadEventFlagsOnly(machine);
                    if (flags == null) continue;

                    var state = _connectionService.GetMachineState(machine.MacchinaId);
                    if (state == null) continue;

                    await _syncService.ProcessEventFlagsAsync(machine.MacchinaId, flags, state, ct);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Errore fast event polling macchina {Id}", machine.MacchinaId);
                }
            }
        }
    }

    private async Task<List<PlcMachineConfig>> LoadMachineConfigsAsync()
    {
        var configs = new List<PlcMachineConfig>();
        var configPath = Path.Combine(AppContext.BaseDirectory, _settings.MachineConfigPath);

        // Carica gli IP dal database
        Dictionary<Guid, string?> macchineDbIps = new();
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var macchineDb = await context.Macchine.ToListAsync();
            macchineDbIps = macchineDb.ToDictionary(m => m.Id, m => m.IndirizzoPLC);
            _logger.LogInformation("Caricate {Count} macchine dal database per sincronizzazione IP", macchineDbIps.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Impossibile caricare macchine dal database, uso IP dai file JSON");
        }

        if (!Directory.Exists(configPath))
        {
            _logger.LogWarning("Cartella configurazioni macchine non trovata: {Path}", configPath);
            return configs;
        }

        foreach (var file in Directory.GetFiles(configPath, "macchina_*.json"))
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var config = JsonSerializer.Deserialize<PlcMachineConfig>(json);
                
                if (config != null)
                {
                    // Sovrascrivi IP con quello dal database se disponibile
                    if (macchineDbIps.TryGetValue(config.MacchinaId, out var dbIp) && !string.IsNullOrWhiteSpace(dbIp))
                    {
                        var oldIp = config.PlcIp;
                        config.PlcIp = dbIp;
                        if (oldIp != dbIp)
                        {
                            _logger.LogInformation("Macchina {Numero}: IP aggiornato da database ({OldIp} -> {NewIp})", 
                                config.Numero, oldIp, dbIp);
                        }
                    }
                    
                    configs.Add(config);
                    _logger.LogInformation("Caricata configurazione macchina {Numero} ({Nome}) - IP: {Ip}", 
                        config.Numero, config.Nome, config.PlcIp);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore caricamento config da {File}", file);
            }
        }

        return configs;
    }

    /// <summary>
    /// Ricarica gli IP delle macchine dal database e aggiorna le configurazioni in memoria.
    /// Se l'IP cambia, forza la riconnessione al PLC.
    /// </summary>
    private async Task ReloadMachineIpsAsync(List<PlcMachineConfig> machines, CancellationToken ct)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);
            var macchineDb = await context.Macchine.ToListAsync(ct);
            var dbIps = macchineDb.ToDictionary(m => m.Id, m => m.IndirizzoPLC);

            foreach (var machine in machines)
            {
                if (dbIps.TryGetValue(machine.MacchinaId, out var newIp) && !string.IsNullOrWhiteSpace(newIp))
                {
                    if (machine.PlcIp != newIp)
                    {
                        _logger.LogInformation("🔄 IP macchina {Numero} cambiato: {OldIp} → {NewIp}. Riconnessione...",
                            machine.Numero, machine.PlcIp, newIp);
                        
                        machine.PlcIp = newIp;
                        
                        // Forza riconnessione con il nuovo IP
                        await _connectionService.ReconnectAsync(machine, 0, ct);
                        
                        await _statusWriter.LogInfoAsync($"IP macchina {machine.Numero} cambiato in {newIp}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Errore durante hot-reload IP macchine");
        }
    }

    /// <summary>
    /// Al boot, legge l'ultimo StatoMacchina da PLCStorico per ogni macchina e
    /// inizializza MachineState.LastStato in memoria. In questo modo, se la macchina
    /// era già NON CONNESSA prima del restart, SaveDisconnessaAsync non scrive un
    /// record spurio al primo ciclo di polling.
    /// </summary>
    private async Task RestoreLastStatiFromDbAsync(List<PlcMachineConfig> machines, CancellationToken ct)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);
            var ids = machines.Select(m => m.MacchinaId).ToList();

            // Una query che restituisce l'ultimo record PLCStorico per ogni macchina
            var lastRecords = await context.PLCStorico
                .Where(s => ids.Contains(s.MacchinaId))
                .GroupBy(s => s.MacchinaId)
                .Select(g => new { MacchinaId = g.Key, StatoMacchina = g.OrderByDescending(s => s.DataOra).First().StatoMacchina })
                .ToListAsync(ct);

            foreach (var record in lastRecords)
            {
                var state = _connectionService.GetMachineState(record.MacchinaId);
                if (state != null && !string.IsNullOrEmpty(record.StatoMacchina))
                {
                    state.LastStato = record.StatoMacchina;
                    _logger.LogInformation("Ripristinato LastStato={Stato} per macchina {MacchinaId} dal DB", 
                        record.StatoMacchina, record.MacchinaId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Errore durante ripristino LastStati da DB — i record NON CONNESSA al boot potrebbero essere spurii");
        }
    }

    public override void Dispose()
    {
        _connectionService.Dispose();
        base.Dispose();
    }
}

