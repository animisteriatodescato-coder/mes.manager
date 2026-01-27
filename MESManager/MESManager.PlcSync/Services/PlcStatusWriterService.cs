using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MESManager.PlcSync.Services;

/// <summary>
/// Servizio per aggiornare lo stato del PlcSync nel database
/// e scrivere i log di sincronizzazione
/// </summary>
public class PlcStatusWriterService
{
    private readonly ILogger<PlcStatusWriterService> _logger;
    private readonly IDbContextFactory<MesManagerDbContext> _contextFactory;
    private readonly string _serviceVersion;

    public PlcStatusWriterService(
        ILogger<PlcStatusWriterService> logger,
        IDbContextFactory<MesManagerDbContext> contextFactory)
    {
        _logger = logger;
        _contextFactory = contextFactory;
        
        // Ottieni versione assembly
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        _serviceVersion = version != null ? version.ToString() : "1.0.0";
    }

    /// <summary>
    /// Inizializza o recupera lo stato del servizio dal database
    /// </summary>
    public async Task<PlcServiceStatus> InitializeStatusAsync(int pollingIntervalSeconds, CancellationToken ct = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);
            
            var status = await context.PlcServiceStatus.FirstOrDefaultAsync(ct);
            
            if (status == null)
            {
                status = new PlcServiceStatus
                {
                    IsRunning = true,
                    LastHeartbeat = DateTime.UtcNow,
                    ServiceStartTime = DateTime.UtcNow,
                    PollingIntervalSeconds = pollingIntervalSeconds,
                    EnableRealtime = true,
                    EnableStorico = true,
                    EnableEvents = true,
                    ServiceVersion = _serviceVersion
                };
                context.PlcServiceStatus.Add(status);
            }
            else
            {
                status.IsRunning = true;
                status.LastHeartbeat = DateTime.UtcNow;
                status.ServiceStartTime = DateTime.UtcNow;
                status.ServiceVersion = _serviceVersion;
                // Leggi le impostazioni dal DB (potrebbero essere state cambiate dalla UI)
            }
            
            await context.SaveChangesAsync(ct);
            
            _logger.LogInformation("Stato servizio inizializzato: Polling={Polling}s, Realtime={Realtime}, Storico={Storico}, Events={Events}",
                status.PollingIntervalSeconds, status.EnableRealtime, status.EnableStorico, status.EnableEvents);
                
            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore inizializzazione stato servizio");
            // Ritorna stato di default
            return new PlcServiceStatus
            {
                PollingIntervalSeconds = pollingIntervalSeconds,
                EnableRealtime = true,
                EnableStorico = true,
                EnableEvents = true
            };
        }
    }

    /// <summary>
    /// Aggiorna l'heartbeat e le statistiche del servizio
    /// </summary>
    public async Task UpdateHeartbeatAsync(
        int machinesConfigured, 
        int machinesConnected, 
        bool incrementSyncCount = false,
        CancellationToken ct = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);
            
            var status = await context.PlcServiceStatus.FirstOrDefaultAsync(ct);
            if (status == null) return;
            
            status.LastHeartbeat = DateTime.UtcNow;
            status.MachinesConfigured = machinesConfigured;
            status.MachinesConnected = machinesConnected;
            status.LastSyncTime = DateTime.UtcNow;
            
            if (incrementSyncCount)
                status.TotalSyncCount++;
            
            await context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Errore aggiornamento heartbeat");
        }
    }

    /// <summary>
    /// Ricarica le impostazioni dal database (per hot-reload)
    /// </summary>
    public async Task<PlcServiceStatus?> ReloadSettingsAsync(CancellationToken ct = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);
            return await context.PlcServiceStatus.AsNoTracking().FirstOrDefaultAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Errore ricarica impostazioni");
            return null;
        }
    }

    /// <summary>
    /// Segna il servizio come fermato
    /// </summary>
    public async Task MarkStoppedAsync(CancellationToken ct = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);
            
            var status = await context.PlcServiceStatus.FirstOrDefaultAsync(ct);
            if (status != null)
            {
                status.IsRunning = false;
                await context.SaveChangesAsync(ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Errore marcatura servizio come fermato");
        }
    }

    /// <summary>
    /// Registra un errore e incrementa il contatore
    /// </summary>
    public async Task RecordErrorAsync(string errorMessage, CancellationToken ct = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);
            
            var status = await context.PlcServiceStatus.FirstOrDefaultAsync(ct);
            if (status != null)
            {
                status.TotalErrorCount++;
                status.LastErrorMessage = errorMessage.Length > 500 
                    ? errorMessage.Substring(0, 500) 
                    : errorMessage;
                await context.SaveChangesAsync(ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Errore registrazione errore nel DB");
        }
    }

    /// <summary>
    /// Scrive un log di sincronizzazione
    /// </summary>
    public async Task LogAsync(
        string level, 
        string message, 
        Guid? macchinaId = null, 
        string? macchinaNumero = null, 
        string? details = null,
        CancellationToken ct = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);
            
            var log = new PlcSyncLog
            {
                Timestamp = DateTime.UtcNow,
                Level = level,
                Message = message,
                MacchinaId = macchinaId,
                MacchinaNumero = macchinaNumero,
                Details = details
            };
            
            context.PlcSyncLogs.Add(log);
            await context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Errore scrittura log nel DB");
        }
    }

    /// <summary>
    /// Log di info
    /// </summary>
    public Task LogInfoAsync(string message, Guid? macchinaId = null, string? macchinaNumero = null, CancellationToken ct = default)
        => LogAsync("Info", message, macchinaId, macchinaNumero, ct: ct);

    /// <summary>
    /// Log di successo
    /// </summary>
    public Task LogSuccessAsync(string message, Guid? macchinaId = null, string? macchinaNumero = null, CancellationToken ct = default)
        => LogAsync("Success", message, macchinaId, macchinaNumero, ct: ct);

    /// <summary>
    /// Log di warning
    /// </summary>
    public Task LogWarningAsync(string message, Guid? macchinaId = null, string? macchinaNumero = null, string? details = null, CancellationToken ct = default)
        => LogAsync("Warning", message, macchinaId, macchinaNumero, details, ct);

    /// <summary>
    /// Log di errore
    /// </summary>
    public Task LogErrorAsync(string message, Guid? macchinaId = null, string? macchinaNumero = null, string? details = null, CancellationToken ct = default)
        => LogAsync("Error", message, macchinaId, macchinaNumero, details, ct);
}
