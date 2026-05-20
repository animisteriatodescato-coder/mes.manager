using MESManager.Worker;
using MESManager.Worker.Workers;
using MESManager.Infrastructure;
using MESManager.Infrastructure.Configuration;
using MESManager.Sync;
using MESManager.Sync.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddMesManagerSharedConfiguration(builder.Environment);
builder.Services.ConfigureMesManagerDatabaseConfiguration(builder.Configuration);

// Configurazione Connection String dal file condiviso
var connectionString = builder.Configuration.GetRequiredMesManagerConnectionString();

// Infrastructure e DbContext
builder.Services.AddInfrastructure(connectionString);

// Servizi Application mancanti richiesti da Infrastructure
builder.Services.AddScoped<MESManager.Application.Interfaces.IPianificazioneService, MESManager.Application.Services.PianificazioneService>();

// Configurazione Mago Sync
var magoOptions = new MagoOptions();
builder.Configuration.GetSection("Mago").Bind(magoOptions);

var backupPath = Path.Combine(AppContext.BaseDirectory, "SyncBackups");
builder.Services.AddMagoSync(magoOptions, backupPath);

// Worker Services
builder.Services.AddHostedService<SimulatorePLCWorker>();
builder.Services.AddHostedService<SyncMagoWorker>();
builder.Services.AddHostedService<MESManager.Worker.Workers.FotovoltaicoWorker>();
// TODO: RecipeAutoLoaderWorker richiede IPianificazioneService - da sistemare DI
// builder.Services.AddHostedService<RecipeAutoLoaderWorker>();  // v1.33.0 - Auto-load ricette su cambio commessa

// Support Windows Service
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "MESManager.Worker";
});

// Configura timeout di shutdown più lungo per completare sync in corso
builder.Services.Configure<HostOptions>(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(30);
});

var host = builder.Build();
host.Run();
