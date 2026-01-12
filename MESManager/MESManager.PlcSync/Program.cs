using MESManager.Infrastructure.Data;
using MESManager.PlcSync;
using MESManager.PlcSync.Services;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

// DbContext
builder.Services.AddDbContext<MesManagerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MESManagerDb")));

// Servizi PLC
builder.Services.AddSingleton<PlcConnectionService>();
builder.Services.AddScoped<PlcReaderService>();
builder.Services.AddScoped<PlcSyncService>();

// Worker
builder.Services.AddHostedService<PlcSyncWorker>();

var host = builder.Build();
host.Run();
