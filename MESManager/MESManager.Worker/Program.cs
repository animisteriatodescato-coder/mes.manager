using MESManager.Worker;
using MESManager.Infrastructure;
using MESManager.Sync;
using MESManager.Sync.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Carica configurazione database condivisa dalla root del progetto
builder.Configuration.AddJsonFile(
    Path.Combine(Directory.GetParent(builder.Environment.ContentRootPath)!.FullName, "appsettings.Database.json"),
    optional: false,
    reloadOnChange: true);

// Configurazione Connection String dal file condiviso
var connectionString = builder.Configuration.GetConnectionString("MESManagerDb")
    ?? throw new InvalidOperationException("Connection string 'MESManagerDb' not found in appsettings.Database.json");

// Infrastructure e DbContext
builder.Services.AddInfrastructure(connectionString);

// Configurazione Mago Sync
var magoOptions = new MagoOptions();
builder.Configuration.GetSection("Mago").Bind(magoOptions);

var backupPath = Path.Combine(AppContext.BaseDirectory, "SyncBackups");
builder.Services.AddMagoSync(magoOptions, backupPath);

// Worker Services
builder.Services.AddHostedService<SimulatorePLCWorker>();
builder.Services.AddHostedService<SyncMagoWorker>();

// Support Windows Service
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "MESManager Worker";
});

var host = builder.Build();
host.Run();
