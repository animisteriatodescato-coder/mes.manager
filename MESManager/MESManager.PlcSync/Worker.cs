using System.Text.Json;
using MESManager.PlcSync.Configuration;
using MESManager.PlcSync.Services;

namespace MESManager.PlcSync;

public class PlcSyncWorker : BackgroundService
{
    private readonly ILogger<PlcSyncWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly PlcConnectionService _connectionService;
    private readonly PlcReaderService _readerService;
    private readonly PlcSyncService _syncService;
    private PlcSyncSettings _settings = null!;

    public PlcSyncWorker(
        ILogger<PlcSyncWorker> logger,
        IConfiguration configuration,
        PlcConnectionService connectionService,
        PlcReaderService readerService,
        PlcSyncService syncService)
    {
        _logger = logger;
        _configuration = configuration;
        _connectionService = connectionService;
        _readerService = readerService;
        _syncService = syncService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PlcSyncWorker avviato");

        // Carica settings
        _settings = _configuration.GetSection("PlcSync").Get<PlcSyncSettings>() ?? new PlcSyncSettings();

        // Carica configurazioni macchine
        var machines = await LoadMachineConfigsAsync();

        if (!machines.Any())
        {
            _logger.LogWarning("Nessuna macchina configurata. Servizio in attesa.");
            await Task.Delay(Timeout.Infinite, stoppingToken);
            return;
        }

        // Connetti a tutte le macchine
        foreach (var machine in machines.Where(m => m.Enabled))
        {
            await _connectionService.ConnectAsync(machine, stoppingToken);
        }

        // Loop principale
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
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

                        // Sync verso database
                        var state = _connectionService.GetMachineState(machine.MacchinaId);
                        await _syncService.SyncSnapshotAsync(machine.MacchinaId, snapshot, state, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Errore processamento macchina {MacchinaNumero}", machine.Numero);
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(_settings.PollingIntervalSeconds), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel ciclo principale PlcSyncWorker");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        _logger.LogInformation("PlcSyncWorker arrestato");
    }

    private async Task<List<PlcMachineConfig>> LoadMachineConfigsAsync()
    {
        var configs = new List<PlcMachineConfig>();
        var configPath = Path.Combine(AppContext.BaseDirectory, _settings.MachineConfigPath);

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
                    configs.Add(config);
                    _logger.LogInformation("Caricata configurazione macchina {Numero} ({Nome})", 
                        config.Numero, config.Nome);
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

