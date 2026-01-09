using Microsoft.Extensions.DependencyInjection;
using MESManager.Sync.Backup;
using MESManager.Sync.Configuration;
using MESManager.Sync.Repositories;
using MESManager.Sync.Services;

namespace MESManager.Sync;

public static class DependencyInjection
{
    public static IServiceCollection AddMagoSync(
        this IServiceCollection services,
        MagoOptions magoOptions,
        string backupPath)
    {
        // Configurazione
        services.AddSingleton(magoOptions);
        services.AddSingleton(new JsonBackupService(backupPath));

        // Repository
        services.AddScoped<MagoRepository>();

        // Services
        services.AddScoped<SyncClientiService>();
        services.AddScoped<SyncArticoliService>();
        services.AddScoped<SyncCommesseService>();
        services.AddScoped<ISyncCoordinator, SyncCoordinator>();

        return services;
    }
}
