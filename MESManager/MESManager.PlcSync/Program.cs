using MESManager.Application.Interfaces;
using MESManager.Infrastructure;
using MESManager.Infrastructure.Configuration;
using MESManager.PlcSync;
using MESManager.PlcSync.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddMesManagerSharedConfiguration(builder.Environment);

// Bind PlcSync settings for options pattern
builder.Services.Configure<MESManager.PlcSync.Configuration.PlcSyncSettings>(
    builder.Configuration.GetSection("PlcSync"));

// DbContext con factory per supportare Singleton services
var connectionString = builder.Configuration.GetRequiredMesManagerConnectionString();
builder.Services.AddMesManagerDbContextFactory(connectionString);

// Servizi PLC - Singleton per Worker Service
builder.Services.AddSingleton<PlcConnectionService>();
builder.Services.AddSingleton<PlcReaderService>();
builder.Services.AddSingleton<PlcSyncService>();
builder.Services.AddSingleton<IPlcSyncService>(sp => sp.GetRequiredService<PlcSyncService>()); // Interfaccia per eventi
builder.Services.AddSingleton<PlcStatusWriterService>();

// Worker
builder.Services.AddHostedService<PlcSyncWorker>();

// Supporto Windows Service per graceful shutdown
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "MESManager.PlcSync";
});

// Configura timeout di shutdown più lungo per chiudere connessioni PLC
builder.Services.Configure<HostOptions>(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(30);
});

var host = builder.Build();
host.Run();
