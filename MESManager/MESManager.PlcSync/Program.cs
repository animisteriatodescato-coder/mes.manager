using MESManager.Infrastructure.Data;
using MESManager.PlcSync;
using MESManager.PlcSync.Services;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

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

// Worker
builder.Services.AddHostedService<PlcSyncWorker>();

var host = builder.Build();
host.Run();
