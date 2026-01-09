using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MESManager.Application.Interfaces;
using MESManager.Infrastructure.Data;
using MESManager.Infrastructure.Services;

namespace MESManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        // DbContext
        services.AddDbContext<MesManagerDbContext>(options =>
            options.UseSqlServer(connectionString));

        // App Services
        services.AddScoped<IArticoloAppService, ArticoloAppService>(); // ILogger<T> is injected by default in .NET DI
        services.AddScoped<IMacchinaAppService, MacchinaAppService>();
        services.AddScoped<ICommessaAppService, CommessaAppService>();
        services.AddScoped<IRicettaAppService, RicettaAppService>();
        services.AddScoped<IClienteAppService, ClienteAppService>();
        services.AddScoped<IAnimeAppService, AnimeAppService>();
        // Aggiungi altri servizi qui quando implementati

        return services;
    }
}

