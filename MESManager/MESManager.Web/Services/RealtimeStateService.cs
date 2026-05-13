using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;
using MESManager.Web.Hubs;

namespace MESManager.Web.Services;

/// <summary>
/// Servizio singleton che gestisce lo stato real-time dei dati PLC.
/// Fornisce un punto centralizzato per l'aggiornamento e la distribuzione dei dati.
/// </summary>
public class RealtimeStateService : IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<RealtimeHub> _hubContext;
    private readonly ILogger<RealtimeStateService> _logger;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private readonly object _stateLock = new();
    
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;
    private int _intervalSeconds = 4;
    private bool _isRunning = false;
    private List<PlcRealtimeDto>? _currentData;
    
    /// <summary>
    /// Evento scatenato quando i dati vengono aggiornati.
    /// </summary>
    public event Action<List<PlcRealtimeDto>>? OnDataUpdated;
    
    /// <summary>
    /// Dati correnti delle macchine PLC.
    /// </summary>
    public List<PlcRealtimeDto>? CurrentData
    {
        get
        {
            lock (_stateLock)
            {
                return _currentData?.ToList();
            }
        }
    }
    
    /// <summary>
    /// Timestamp dell'ultimo aggiornamento.
    /// </summary>
    public DateTime LastUpdate { get; private set; }
    
    /// <summary>
    /// Indica se il servizio sta effettuando polling.
    /// </summary>
    public bool IsRunning => _isRunning;
    
    /// <summary>
    /// Intervallo di polling in secondi.
    /// </summary>
    public int IntervalSeconds 
    { 
        get => _intervalSeconds;
        set 
        {
            if (value >= 1 && value <= 300)
            {
                _intervalSeconds = value;
                if (_isRunning)
                {
                    // Riavvia il timer con il nuovo intervallo
                    Stop();
                    Start();
                }
            }
        }
    }

    public RealtimeStateService(
        IServiceScopeFactory scopeFactory,
        IHubContext<RealtimeHub> hubContext,
        ILogger<RealtimeStateService> logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Avvia il polling periodico dei dati PLC.
    /// </summary>
    public void Start()
    {
        if (_isRunning) return;
        
        _cts = new CancellationTokenSource();
        _isRunning = true;
        
        _ = RunPollingLoopAsync(_cts.Token);
        
        _logger.LogInformation("RealtimeStateService avviato con intervallo {Interval}s", _intervalSeconds);
    }

    /// <summary>
    /// Ferma il polling periodico.
    /// </summary>
    public void Stop()
    {
        if (!_isRunning) return;
        
        _cts?.Cancel();
        _timer?.Dispose();
        _timer = null;
        _isRunning = false;
        
        _logger.LogInformation("RealtimeStateService fermato");
    }

    /// <summary>
    /// Forza un aggiornamento immediato dei dati.
    /// </summary>
    public async Task RefreshNowAsync()
    {
        await LoadDataAsync();
    }

    private async Task RunPollingLoopAsync(CancellationToken ct)
    {
        // Carica i dati immediatamente all'avvio
        await LoadDataAsync();
        
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(_intervalSeconds));
        
        try
        {
            while (await _timer.WaitForNextTickAsync(ct))
            {
                await LoadDataAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // Normale quando viene fermato
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nel polling loop RealtimeStateService");
        }
        finally
        {
            _isRunning = false;
        }
    }

    private async Task LoadDataAsync()
    {
        if (!await _loadLock.WaitAsync(0))
        {
            _logger.LogTrace("RealtimeStateService: aggiornamento PLC saltato per caricamento gia in corso");
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var plcService = scope.ServiceProvider.GetRequiredService<IPlcAppService>();
            
            var data = (await plcService.GetRealtimeDataAsync()).ToList();

            lock (_stateLock)
            {
                _currentData = data;
                LastUpdate = DateTime.Now;
            }
            
            // Notifica i subscriber locali (componenti Blazor)
            NotifyLocalSubscribers(data);
            
            // Notifica i client SignalR
            await _hubContext.Clients.All.SendAsync("PlcDataUpdated", data);
            
            _logger.LogTrace("RealtimeStateService: caricati {Count} record PLC", data.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore caricamento dati PLC in RealtimeStateService");
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private void NotifyLocalSubscribers(List<PlcRealtimeDto> data)
    {
        var handlers = OnDataUpdated;
        if (handlers == null)
        {
            return;
        }

        foreach (Action<List<PlcRealtimeDto>> handler in handlers.GetInvocationList())
        {
            try
            {
                handler(data);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Subscriber realtime PLC non notificato correttamente");
            }
        }
    }

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
    }
}
