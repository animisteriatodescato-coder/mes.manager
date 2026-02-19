using MESManager.Worker;
using MESManager.Worker.Workers;
using MESManager.Infrastructure;
using MESManager.Sync;
using MESManager.Sync.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Carica configurazione database condivisa dalla root del progetto
// Usa stessa logica del Web: Secrets.json > Database.json (unificazione configurazione)
var solutionRoot = Directory.GetParent(builder.Environment.ContentRootPath)!.FullName;
var secretsPath = Path.Combine(solutionRoot, "appsettings.Secrets.json");
var dbConfigPath = Path.Combine(solutionRoot, "appsettings.Database.json");
var dbConfigEnvPath = Path.Combine(solutionRoot, $"appsettings.Database.{builder.Environment.EnvironmentName}.json");

if (File.Exists(secretsPath))
{
    // Preferito: usa secrets condiviso con Web
    builder.Configuration.AddJsonFile(secretsPath, optional: false, reloadOnChange: true);
}
else if (File.Exists(dbConfigPath))
{
    // Fallback legacy
    builder.Configuration.AddJsonFile(dbConfigPath, optional: false, reloadOnChange: true);
}

// Override locale per ambiente (solo se presente)
if (!builder.Environment.IsProduction() && File.Exists(dbConfigEnvPath))
{
    builder.Configuration.AddJsonFile(dbConfigEnvPath, optional: true, reloadOnChange: true);
}

// Configurazione Connection String dal file condiviso
var connectionString = builder.Configuration.GetConnectionString("MESManagerDb")
    ?? throw new InvalidOperationException("Connection string 'MESManagerDb' not found in configuration");

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
