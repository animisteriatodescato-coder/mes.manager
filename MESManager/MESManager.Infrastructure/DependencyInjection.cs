using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MESManager.Application.Interfaces;
using MESManager.Application.Services;
using MESManager.Infrastructure.Data;
using MESManager.Infrastructure.Services;
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

        // DbContextFactory per operazioni thread-safe
        services.AddDbContextFactory<MesManagerDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions => 
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null)), ServiceLifetime.Scoped);

        // Repositories
        services.AddScoped<IAnimeRepository, AnimeRepository>();
        services.AddScoped<IRicettaRepository, RicettaRepository>();

        // App Services
        services.AddScoped<IArticoloAppService, ArticoloAppService>(); // ILogger<T> is injected by default in .NET DI
        services.AddScoped<IMacchinaAppService, MacchinaAppService>();
        services.AddScoped<ICommessaAppService, CommessaAppService>();
        services.AddScoped<IRicettaAppService, RicettaAppService>();
        services.AddScoped<IClienteAppService, ClienteAppService>();
        services.AddScoped<IAnimeService, AnimeService>();
        services.AddScoped<IPlcAppService, PlcAppService>();
        services.AddScoped<IOperatoreAppService, OperatoreAppService>();
        services.AddScoped<ICalendarioLavoroAppService, CalendarioLavoroAppService>();
        services.AddScoped<IImpostazioniGanttAppService, ImpostazioniGanttAppService>();
        services.AddScoped<IFestiviAppService, FestiviAppService>();
        services.AddScoped<IPreferenzeUtenteService, PreferenzeUtenteService>();
        services.AddScoped<CurrentUserService>();
        services.AddScoped<ITechnicalIssueService, TechnicalIssueService>();
        services.AddScoped<IPianificazioneEngineService, PianificazioneEngineService>();
        
        // Preventivi Services
        services.AddScoped<IQuotePricingEngine, QuotePricingEngine>();
        services.AddScoped<IPriceListService, PriceListService>();
        services.AddScoped<IExcelImportService, ExcelImportService>();
        services.AddScoped<IQuoteService, QuoteService>();
        services.AddScoped<IQuoteAttachmentService, QuoteAttachmentService>();
        services.AddScoped<IQuotePdfGenerator, QuotePdfGenerator>();
        
        // Lavorazioni Anime Services (v1.40.0 - Preventivi lavorazioni)
        services.AddScoped<IWorkProcessingService, WorkProcessingService>();
        
        // PLC Recipe Services (v1.33.0 - Trasmissione ricette a PLC)
        services.AddScoped<IRicettaGanttService, RicettaGanttService>();
        services.AddScoped<IPlcRecipeWriterService, PlcRecipeWriterService>();
        services.AddScoped<IRecipeAutoLoaderService, RecipeAutoLoaderService>();
        // Analisi Operatori (v1.54.2 - Performance/premi produttivi)
        services.AddScoped<IOperatoreAnalisiService, OperatoreAnalisiService>();

        // Aggiungi altri servizi qui quando implementati

        return services;
    }
}

