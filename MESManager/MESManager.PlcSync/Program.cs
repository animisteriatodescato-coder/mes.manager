using MESManager.Application.Interfaces;
using MESManager.Infrastructure.Data;
using MESManager.PlcSync;
using MESManager.PlcSync.Services;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

// Carica configurazione database condivisa dalla root del progetto
builder.Configuration.AddJsonFile(
    Path.Combine(Directory.GetParent(builder.Environment.ContentRootPath)!.FullName, "appsettings.Database.json"),
    optional: false,
    reloadOnChange: true);

// Bind PlcSync settings for options pattern
builder.Services.Configure<MESManager.PlcSync.Configuration.PlcSyncSettings>(
    builder.Configuration.GetSection("PlcSync"));

// DbContext con factory per supportare Singleton services
builder.Services.AddDbContextFactory<MesManagerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MESManagerDb"), sqlOptions => 
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)));

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
