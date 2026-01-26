using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace MESManager.Infrastructure.Services;

/// <summary>
/// Servizio hosted per la sincronizzazione automatica delle macchine PLC.
/// Gira in background sul server indipendentemente dalla sessione utente.
/// </summary>
public class PlcAutoSyncService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PlcAutoSyncService> _logger;
    
    private bool _isEnabled = false;
    private int _intervalSeconds = 30;
    private DateTime? _lastSync;
    private DateTime? _nextSync;
    private int _syncCount = 0;
    private int _errorCount = 0;
    private string? _lastError;
    private List<PlcSyncResult> _lastResults = new();

    public PlcAutoSyncService(
        IServiceProvider serviceProvider,
        ILogger<PlcAutoSyncService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    // Proprietà pubbliche per lo stato
    public bool IsEnabled => _isEnabled;
    public int IntervalSeconds => _intervalSeconds;
    public DateTime? LastSync => _lastSync;
    public DateTime? NextSync => _nextSync;
    public int SyncCount => _syncCount;
    public int ErrorCount => _errorCount;
    public string? LastError => _lastError;
    public List<PlcSyncResult> LastResults => _lastResults;
    public bool IsSyncing { get; private set; }

    /// <summary>
    /// Avvia la sincronizzazione automatica
    /// </summary>
    public void Start(int intervalSeconds = 30)
    {
        _intervalSeconds = Math.Clamp(intervalSeconds, 5, 300);
        _isEnabled = true;
        _nextSync = DateTime.Now.AddSeconds(_intervalSeconds);
        _logger.LogInformation("PlcAutoSync avviato con intervallo di {Interval} secondi", _intervalSeconds);
    }

    /// <summary>
    /// Ferma la sincronizzazione automatica
    /// </summary>
    public void Stop()
    {
        _isEnabled = false;
        _nextSync = null;
        _logger.LogInformation("PlcAutoSync fermato");
    }

    /// <summary>
    /// Aggiorna l'intervallo di sincronizzazione
    /// </summary>
    public void SetInterval(int intervalSeconds)
    {
        _intervalSeconds = Math.Clamp(intervalSeconds, 5, 300);
        if (_isEnabled)
        {
            _nextSync = DateTime.Now.AddSeconds(_intervalSeconds);
        }
        _logger.LogInformation("PlcAutoSync intervallo aggiornato a {Interval} secondi", _intervalSeconds);
    }

    /// <summary>
    /// Esegue una sincronizzazione manuale immediata
    /// </summary>
    public async Task<List<PlcSyncResult>> SyncNowAsync()
    {
        return await ExecuteSyncAsync();
    }

    /// <summary>
    /// Restituisce lo stato corrente del servizio
    /// </summary>
    public PlcAutoSyncStatus GetStatus()
    {
        return new PlcAutoSyncStatus
        {
            IsEnabled = _isEnabled,
            IsSyncing = IsSyncing,
            IntervalSeconds = _intervalSeconds,
            LastSync = _lastSync,
            NextSync = _nextSync,
            SyncCount = _syncCount,
            ErrorCount = _errorCount,
            LastError = _lastError,
            SecondsToNextSync = _nextSync.HasValue ? (int)Math.Max(0, (_nextSync.Value - DateTime.Now).TotalSeconds) : 0
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PlcAutoSyncService avviato");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_isEnabled && _nextSync.HasValue && DateTime.Now >= _nextSync.Value)
                {
                    await ExecuteSyncAsync();
                    _nextSync = DateTime.Now.AddSeconds(_intervalSeconds);
                }

                // Controlla ogni secondo
                await Task.Delay(1000, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Shutdown normale
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel loop PlcAutoSyncService");
                await Task.Delay(5000, stoppingToken); // Attendi prima di riprovare
            }
        }

        _logger.LogInformation("PlcAutoSyncService terminato");
    }

    private async Task<List<PlcSyncResult>> ExecuteSyncAsync()
    {
        if (IsSyncing)
        {
            _logger.LogWarning("Sincronizzazione già in corso, skip");
            return _lastResults;
        }

        IsSyncing = true;
        var results = new List<PlcSyncResult>();

        try
        {
            _logger.LogInformation("PlcAutoSync: avvio sincronizzazione automatica");

            using var scope = _serviceProvider.CreateScope();
            var syncCoordinator = scope.ServiceProvider.GetRequiredService<IPlcSyncCoordinator>();

            results = await syncCoordinator.SyncTutteMacchineAsync();

            _lastSync = DateTime.Now;
            _syncCount++;
            _lastResults = results;

            var errors = results.Count(r => !r.Successo);
            if (errors > 0)
            {
                _errorCount += errors;
                _lastError = string.Join("; ", results.Where(r => !r.Successo).Select(r => $"{r.MacchinaCodiceMacchina}: {r.MessaggioErrore}"));
                _logger.LogWarning("PlcAutoSync: sincronizzazione completata con {Errors} errori", errors);
            }
            else
            {
                _lastError = null;
                _logger.LogInformation("PlcAutoSync: sincronizzazione completata con successo ({Count} macchine)", results.Count);
            }
        }
        catch (Exception ex)
        {
            _errorCount++;
            _lastError = ex.Message;
            _logger.LogError(ex, "PlcAutoSync: errore durante sincronizzazione");
        }
        finally
        {
            IsSyncing = false;
        }

        return results;
    }
}

/// <summary>
/// DTO per lo stato del servizio di sincronizzazione automatica
/// </summary>
public class PlcAutoSyncStatus
{
    public bool IsEnabled { get; set; }
    public bool IsSyncing { get; set; }
    public int IntervalSeconds { get; set; }
    public DateTime? LastSync { get; set; }
    public DateTime? NextSync { get; set; }
    public int SyncCount { get; set; }
    public int ErrorCount { get; set; }
    public string? LastError { get; set; }
    public int SecondsToNextSync { get; set; }
}
