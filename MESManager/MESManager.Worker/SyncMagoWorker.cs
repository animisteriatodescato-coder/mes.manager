using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;
using MESManager.Sync.Services;
using Microsoft.Extensions.Hosting;

namespace MESManager.Worker;

public class SyncMagoWorker : BackgroundService
{
    private readonly ILogger<SyncMagoWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly TimeSpan _intervallo = TimeSpan.FromHours(1);
    private bool _isSyncing = false;

    public SyncMagoWorker(
        ILogger<SyncMagoWorker> logger,
        IServiceProvider serviceProvider,
        IHostApplicationLifetime appLifetime)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _appLifetime = appLifetime;
        
        // Registra handler per shutdown graceful
        _appLifetime.ApplicationStopping.Register(OnApplicationStopping);
    }

    private void OnApplicationStopping()
    {
        if (_isSyncing)
        {
            _logger.LogWarning("⚠️ Shutdown richiesto durante sync Mago in corso - attendere completamento...");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SyncMagoWorker avviato");

        // Attendi 10 secondi all'avvio
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        using var timer = new PeriodicTimer(_intervallo);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _isSyncing = true;
                _logger.LogInformation("Inizio sincronizzazione Mago alle {time}", DateTimeOffset.Now);

                using var scope = _serviceProvider.CreateScope();
                var syncCoordinator = scope.ServiceProvider.GetRequiredService<ISyncCoordinator>();
                var context = scope.ServiceProvider.GetRequiredService<MesManagerDbContext>();

                var logs = await syncCoordinator.SyncTuttoAsync(stoppingToken);

                foreach (var log in logs)
                {
                    _logger.LogInformation(
                        "Sync {modulo}: Nuovi={nuovi}, Aggiornati={aggiornati}, Ignorati={ignorati}, Errori={errori}",
                        log.Modulo, log.Nuovi, log.Aggiornati, log.Ignorati, log.Errori);

                    if (log.Errori > 0)
                    {
                        _logger.LogError("Errore durante sync {modulo}: {errore}",
                            log.Modulo, log.MessaggioErrore);
                    }

                }

                // I log sono già salvati dal SyncCoordinator

                _logger.LogInformation("Sincronizzazione Mago completata alle {time}", DateTimeOffset.Now);
                _isSyncing = false;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("⏹️ Sync Mago interrotta per shutdown");
                _isSyncing = false;
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la sincronizzazione Mago");
                _isSyncing = false;
            }

            // Attendi il prossimo intervallo
            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("✅ SyncMagoWorker arrestato correttamente");
    }
}
