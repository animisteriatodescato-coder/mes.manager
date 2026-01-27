using MESManager.PlcSync.Configuration;
using MESManager.PlcSync.Models;
using Sharp7;

namespace MESManager.PlcSync.Services;

public class PlcConnectionService
{
    private readonly ILogger<PlcConnectionService> _logger;
    private readonly Dictionary<Guid, MachineState> _machineStates = new();

    public PlcConnectionService(ILogger<PlcConnectionService> logger)
    {
        _logger = logger;
    }

    public Task<bool> ConnectAsync(PlcMachineConfig config, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_machineStates.ContainsKey(config.MacchinaId))
            {
                _machineStates[config.MacchinaId] = new MachineState();
            }

            var state = _machineStates[config.MacchinaId];
            
            if (state.Client.Connected)
                return Task.FromResult(true);

            // Imposta timeout più brevi per evitare blocchi lunghi
            state.Client.SetConnectionParams(
                config.PlcIp,
                (ushort)config.PlcSettings.Rack,
                (ushort)config.PlcSettings.Slot);
            state.Client.ConnTimeout = 2000; // 2 secondi timeout connessione
            state.Client.RecvTimeout = 2000; // 2 secondi timeout ricezione

            var result = state.Client.Connect();

            if (result == 0)
            {
                _logger.LogInformation("Connesso al PLC {MacchinaNumero} ({PlcIp})", 
                    config.Numero, config.PlcIp);
                return Task.FromResult(true);
            }
            else
            {
                _logger.LogError("Errore connessione PLC {MacchinaNumero} ({PlcIp}): Codice {ErrorCode}", 
                    config.Numero, config.PlcIp, result);
                return Task.FromResult(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Eccezione durante connessione PLC {MacchinaNumero}", config.Numero);
            return Task.FromResult(false);
        }
    }

    public async Task ReconnectAsync(PlcMachineConfig config, int delayMs, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_machineStates.TryGetValue(config.MacchinaId, out var state))
            {
                try { state.Client.Disconnect(); } catch { }
            }

            await Task.Delay(delayMs, cancellationToken);
            await ConnectAsync(config, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante riconnessione PLC {MacchinaNumero}", config.Numero);
        }
    }

    public MachineState? GetMachineState(Guid macchinaId)
    {
        return _machineStates.TryGetValue(macchinaId, out var state) ? state : null;
    }

    /// <summary>
    /// Disconnette tutte le macchine PLC in modo ordinato (graceful shutdown).
    /// </summary>
    public void DisconnectAll()
    {
        _logger.LogInformation("🔌 Disconnessione di {Count} macchine PLC...", _machineStates.Count);
        
        foreach (var (macchinaId, state) in _machineStates)
        {
            try
            {
                if (state.Client.Connected)
                {
                    state.Client.Disconnect();
                    _logger.LogDebug("Disconnesso PLC {MacchinaId}", macchinaId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Errore disconnessione PLC {MacchinaId}", macchinaId);
            }
        }
        
        _machineStates.Clear();
        _logger.LogInformation("✅ Tutte le connessioni PLC sono state chiuse");
    }

    public void Dispose()
    {
        DisconnectAll();
    }
}
