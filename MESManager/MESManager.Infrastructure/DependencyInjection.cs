using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MESManager.Application.Interfaces;
using MESManager.Infrastructure.Data;
using MESManager.Infrastructure.Services;
using MESManager.Application.Services;
using MESManager.Infrastructure.Repositories;

namespace MESManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        // DbContext
        services.AddDbContext<MesManagerDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions => 
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null)));

        // Repositories
        services.AddScoped<IAnimeRepository, AnimeRepository>();

        // App Services
        services.AddScoped<IArticoloAppService, ArticoloAppService>(); // ILogger<T> is injected by default in .NET DI
        services.AddScoped<IMacchinaAppService, MacchinaAppService>();
        services.AddScoped<ICommessaAppService, CommessaAppService>();
        services.AddScoped<IRicettaAppService, RicettaAppService>();
        services.AddScoped<IClienteAppService, ClienteAppService>();
        services.AddScoped<IAnimeService, AnimeService>();
        services.AddScoped<IPlcAppService, PlcAppService>();
        // Aggiungi altri servizi qui quando implementati

        return services;
    }
}

