using MESManager.Worker;
using MESManager.Infrastructure;
using MESManager.Sync;
using MESManager.Sync.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Configurazione Connection String
var connectionString = "Server=localhost\\SQLEXPRESS;Database=MESManager;Trusted_Connection=True;TrustServerCertificate=True;";

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
