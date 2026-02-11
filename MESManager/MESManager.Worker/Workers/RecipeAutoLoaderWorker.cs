using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using MESManager.Application.Interfaces;
using MESManager.Application.DTOs;

namespace MESManager.Worker.Workers;

/// <summary>
/// Background service che ascolta eventi di cambio commessa e triggera auto-load ricette
/// </summary>
public class RecipeAutoLoaderWorker : BackgroundService
{
    private readonly ILogger<RecipeAutoLoaderWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    
    public RecipeAutoLoaderWorker(
        ILogger<RecipeAutoLoaderWorker> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 [RECIPE-AUTO-LOADER] Worker avviato - in ascolto eventi commessa");
        
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var plcSyncService = scope.ServiceProvider.GetService<IPlcSyncService>();
            var autoLoaderService = scope.ServiceProvider.GetRequiredService<IRecipeAutoLoaderService>();
            
            if (plcSyncService == null)
            {
                _logger.LogWarning("⚠️ [RECIPE-AUTO-LOADER] PlcSyncService non disponibile - worker disabilitato");
                return;
            }
            
            // Sottoscrizione evento
            plcSyncService.CommessaCambiata += async (sender, args) =>
            {
                try
                {
                    _logger.LogInformation("📢 [RECIPE-AUTO-LOADER] Evento ricevuto: Macchina {Num} | Barcode {Barcode}", 
                        args.NumeroMacchina, args.NuovoBarcode);
                    
                    await autoLoaderService.OnCommessaCambiataAsync(args.MacchinaId, args.NuovoBarcode, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ [RECIPE-AUTO-LOADER] Errore gestione evento commessa cambiata");
                }
            };
            
            _logger.LogInformation("✅ [RECIPE-AUTO-LOADER] Sottoscrizione evento completata - worker in attesa");
            
            // Keep alive
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("⏹️ [RECIPE-AUTO-LOADER] Worker arrestato");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [RECIPE-AUTO-LOADER] Errore critico nel worker");
        }
    }
    
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("⏹️ [RECIPE-AUTO-LOADER] Arresto worker in corso...");
        return base.StopAsync(cancellationToken);
    }
}
