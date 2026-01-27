using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Infrastructure.Data;

namespace MESManager.Infrastructure.Services;

/// <summary>
/// Implementazione del servizio per monitorare lo stato del PlcSync
/// </summary>
public class PlcStatusService : IPlcStatusService
{
    private readonly MesManagerDbContext _context;
    private readonly ILogger<PlcStatusService> _logger;

    public PlcStatusService(MesManagerDbContext context, ILogger<PlcStatusService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PlcServiceStatusDto?> GetServiceStatusAsync()
    {
        try
        {
            var status = await _context.PlcServiceStatus.FirstOrDefaultAsync();
            if (status == null)
                return null;

            return new PlcServiceStatusDto
            {
                IsRunning = status.IsRunning,
                LastHeartbeat = status.LastHeartbeat,
                PollingIntervalSeconds = status.PollingIntervalSeconds,
                EnableRealtime = status.EnableRealtime,
                EnableStorico = status.EnableStorico,
                EnableEvents = status.EnableEvents,
                MachinesConfigured = status.MachinesConfigured,
                MachinesConnected = status.MachinesConnected,
                TotalSyncCount = status.TotalSyncCount,
                TotalErrorCount = status.TotalErrorCount,
                LastSyncTime = status.LastSyncTime,
                LastErrorMessage = status.LastErrorMessage,
                ServiceStartTime = status.ServiceStartTime,
                ServiceVersion = status.ServiceVersion
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nel recupero dello stato del servizio PlcSync");
            return null;
        }
    }

    public async Task<List<PlcSyncLogDto>> GetRecentLogsAsync(int count = 50, string? levelFilter = null)
    {
        try
        {
            var query = _context.PlcSyncLogs.AsQueryable();

            if (!string.IsNullOrEmpty(levelFilter))
            {
                query = query.Where(l => l.Level == levelFilter);
            }

            return await query
                .OrderByDescending(l => l.Timestamp)
                .Take(count)
                .Select(l => new PlcSyncLogDto
                {
                    Id = l.Id,
                    Timestamp = l.Timestamp,
                    MacchinaId = l.MacchinaId,
                    MacchinaNumero = l.MacchinaNumero,
                    Level = l.Level,
                    Message = l.Message,
                    Details = l.Details
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nel recupero dei log PlcSync");
            return new List<PlcSyncLogDto>();
        }
    }

    public async Task<List<PlcSyncLogDto>> GetMachineLogsAsync(Guid macchinaId, int count = 50)
    {
        try
        {
            return await _context.PlcSyncLogs
                .Where(l => l.MacchinaId == macchinaId)
                .OrderByDescending(l => l.Timestamp)
                .Take(count)
                .Select(l => new PlcSyncLogDto
                {
                    Id = l.Id,
                    Timestamp = l.Timestamp,
                    MacchinaId = l.MacchinaId,
                    MacchinaNumero = l.MacchinaNumero,
                    Level = l.Level,
                    Message = l.Message,
                    Details = l.Details
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nel recupero dei log per macchina {MacchinaId}", macchinaId);
            return new List<PlcSyncLogDto>();
        }
    }

    public async Task<bool> UpdateSettingsAsync(int pollingIntervalSeconds, bool enableRealtime, bool enableStorico, bool enableEvents)
    {
        try
        {
            var status = await _context.PlcServiceStatus.FirstOrDefaultAsync();
            if (status == null)
            {
                // Crea un nuovo record se non esiste
                status = new Domain.Entities.PlcServiceStatus
                {
                    PollingIntervalSeconds = pollingIntervalSeconds,
                    EnableRealtime = enableRealtime,
                    EnableStorico = enableStorico,
                    EnableEvents = enableEvents,
                    IsRunning = false,
                    LastHeartbeat = DateTime.UtcNow
                };
                _context.PlcServiceStatus.Add(status);
            }
            else
            {
                status.PollingIntervalSeconds = pollingIntervalSeconds;
                status.EnableRealtime = enableRealtime;
                status.EnableStorico = enableStorico;
                status.EnableEvents = enableEvents;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Impostazioni PlcSync aggiornate: Polling={Polling}s, Realtime={Realtime}, Storico={Storico}, Events={Events}",
                pollingIntervalSeconds, enableRealtime, enableStorico, enableEvents);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nell'aggiornamento delle impostazioni PlcSync");
            return false;
        }
    }

    public async Task<int> CleanupOldLogsAsync(int daysToKeep = 7)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
            var oldLogs = await _context.PlcSyncLogs
                .Where(l => l.Timestamp < cutoffDate)
                .ToListAsync();

            var count = oldLogs.Count;
            if (count > 0)
            {
                _context.PlcSyncLogs.RemoveRange(oldLogs);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Rimossi {Count} log PlcSync più vecchi di {Days} giorni", count, daysToKeep);
            }

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nella pulizia dei log PlcSync");
            return 0;
        }
    }
}
