using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;
using MESManager.Sync.Services;

namespace MESManager.Worker;

public class SyncMagoWorker : BackgroundService
{
    private readonly ILogger<SyncMagoWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _intervallo = TimeSpan.FromHours(1);

    public SyncMagoWorker(
        ILogger<SyncMagoWorker> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la sincronizzazione Mago");
            }

            // Attendi il prossimo intervallo
            await timer.WaitForNextTickAsync(stoppingToken);
        }

        _logger.LogInformation("SyncMagoWorker arrestato");
    }
}
