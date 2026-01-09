using MESManager.Infrastructure.Data;
using MESManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MESManager.Worker;

public class SimulatorePLCWorker : BackgroundService
{
    private readonly ILogger<SimulatorePLCWorker> _logger;
    private readonly IServiceProvider _serviceProvider;

    public SimulatorePLCWorker(ILogger<SimulatorePLCWorker> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Simulatore PLC Worker avviato");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<MesManagerDbContext>();
                    
                    // Crea un log di test
                    var logEvento = new LogEvento
                    {
                        Id = Guid.NewGuid(),
                        DataOra = DateTime.Now,
                        Utente = "SimulatorePLC",
                        Azione = "Update",
                        Entita = "PLCRealtime",
                        IdEntita = null,
                        ValorePrecedenteJson = null,
                        ValoreSuccessivoJson = "Simulatore PLC - aggiornamento ogni 4 secondi"
                    };

                    context.LogEventi.Add(logEvento);
                    await context.SaveChangesAsync(stoppingToken);
                    
                    _logger.LogInformation("Simulatore PLC - Update effettuato alle {time}", DateTimeOffset.Now);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel Simulatore PLC");
            }

            await Task.Delay(4000, stoppingToken); // 4 secondi
        }
    }
}
