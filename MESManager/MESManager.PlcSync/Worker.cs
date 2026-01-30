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
                }

                await Task.Delay(TimeSpan.FromSeconds(pollingInterval), stoppingToken);
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

    public override void Dispose()
    {
        _connectionService.Dispose();
        base.Dispose();
    }
}

