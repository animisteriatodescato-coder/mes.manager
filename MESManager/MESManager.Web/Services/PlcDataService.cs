using MESManager.Application.DTOs;

namespace MESManager.Web.Services;

public class PlcDataService
{
    private readonly HttpClient _http;
    private readonly ILogger<PlcDataService> _logger;
    private System.Threading.Timer? _timer;
    private Func<Task>? _onDataUpdated;
    private bool _autoRefreshEnabled = true;
    private int _refreshIntervalSeconds = 4;

    public PlcDataService(HttpClient http, ILogger<PlcDataService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public List<PlcRealtimeDto> RealtimeData { get; private set; } = new();
    public List<PlcStoricoDto> StoricoData { get; private set; } = new();
    public bool IsLoading { get; private set; }
    public DateTime LastUpdate { get; private set; }

    public bool AutoRefreshEnabled
    {
        get => _autoRefreshEnabled;
        set
        {
            _autoRefreshEnabled = value;
            UpdateTimer();
        }
    }

    public int RefreshIntervalSeconds
    {
        get => _refreshIntervalSeconds;
        set
        {
            _refreshIntervalSeconds = value;
            UpdateTimer();
        }
    }

    public void Initialize(Func<Task> onDataUpdated, bool enableAutoRefresh = true, int intervalSeconds = 4)
    {
        _onDataUpdated = onDataUpdated;
        _autoRefreshEnabled = enableAutoRefresh;
        _refreshIntervalSeconds = intervalSeconds;
        
        _timer = new System.Threading.Timer(async _ =>
        {
            if (_autoRefreshEnabled)
            {
                await LoadRealtimeDataAsync();
            }
        }, null, TimeSpan.FromSeconds(_refreshIntervalSeconds), TimeSpan.FromSeconds(_refreshIntervalSeconds));
    }

    private void UpdateTimer()
    {
        if (_timer != null)
        {
            if (_autoRefreshEnabled)
            {
                _timer.Change(TimeSpan.FromSeconds(_refreshIntervalSeconds), TimeSpan.FromSeconds(_refreshIntervalSeconds));
            }
            else
            {
                _timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            }
        }
    }

    public async Task<List<PlcRealtimeDto>> LoadRealtimeDataAsync()
    {
        try
        {
            IsLoading = true;
            var url = "api/Plc/realtime";
            RealtimeData = await _http.GetFromJsonAsync<List<PlcRealtimeDto>>(url) ?? new();
            LastUpdate = DateTime.Now;
            
            if (_onDataUpdated != null)
            {
                await _onDataUpdated();
            }
            
            return RealtimeData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore caricamento dati realtime");
            throw;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<List<PlcStoricoDto>> LoadStoricoDataAsync(DateTime? from = null, DateTime? to = null)
    {
        try
        {
            IsLoading = true;
            var fromDate = from?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd");
            var toDate = to?.ToString("yyyy-MM-dd") ?? DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");
            var url = $"api/Plc/storico?from={fromDate}&to={toDate}";
            
            StoricoData = await _http.GetFromJsonAsync<List<PlcStoricoDto>>(url) ?? new();
            LastUpdate = DateTime.Now;
            
            if (_onDataUpdated != null)
            {
                await _onDataUpdated();
            }
            
            return StoricoData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore caricamento dati storico");
            throw;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<List<PlcSyncResultDto>> SyncAllMachinesAsync()
    {
        try
        {
            _logger.LogInformation("Avvio sincronizzazione macchine");
            var response = await _http.PostAsync("api/Plc/sync/all", null);
            response.EnsureSuccessStatusCode();
            
            var results = await response.Content.ReadFromJsonAsync<List<PlcSyncResultDto>>() ?? new();
            
            // Refresh automatico dopo sync
            await Task.Delay(500);
            await LoadRealtimeDataAsync();
            
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore sincronizzazione macchine");
            throw;
        }
    }

    /// <summary>
    /// Sincronizza una singola macchina PLC.
    /// </summary>
    public async Task<PlcSyncResultDto> SyncMachineAsync(Guid macchinaId)
    {
        try
        {
            _logger.LogInformation("Avvio sincronizzazione macchina {MacchinaId}", macchinaId);
            var response = await _http.PostAsync($"api/Plc/sync/{macchinaId}", null);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<PlcSyncResultDto>();
            
            // Refresh automatico dopo sync
            await Task.Delay(300);
            await LoadRealtimeDataAsync();
            
            return result ?? new PlcSyncResultDto 
            { 
                MacchinaId = macchinaId, 
                Successo = false, 
                MessaggioErrore = "Risposta vuota dal server" 
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore sincronizzazione macchina {MacchinaId}", macchinaId);
            return new PlcSyncResultDto 
            { 
                MacchinaId = macchinaId, 
                Successo = false, 
                MessaggioErrore = ex.Message 
            };
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
