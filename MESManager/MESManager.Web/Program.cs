using MESManager.Web.Components;
using MESManager.Infrastructure;
using MESManager.Infrastructure.Services;
using MESManager.Infrastructure.Repositories;
using MESManager.Sync;
using MESManager.Sync.Configuration;
using MudBlazor.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MESManager.Infrastructure.Data;
using MESManager.Web.Hubs;
using MESManager.Web.Services;
using MESManager.Web.Security;
using MESManager.Application.Services;
using MESManager.Application.Interfaces;
using MESManager.Application.Configuration;
using Syncfusion.Blazor;
using OfficeOpenXml;

// Configura licenza EPPlus per uso non commerciale
ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// CONFIGURAZIONE SICURA DELLE CREDENZIALI
// ============================================
// Le credenziali sono criptate con DPAPI (Windows Data Protection)
// e possono essere decriptate solo da questo utente su questa macchina.
// Ordine di caricamento (ultimo vince):
// 1. appsettings.json (base)
// 2. appsettings.{Environment}.json
// 3. appsettings.Secrets.encrypted (CRIPTATO - preferito)
// 4. appsettings.Secrets.json (in chiaro - fallback)
// 5. appsettings.Database.json (legacy)
// 6. Variabili d'ambiente (per produzione/container)

var solutionRoot = builder.Environment.IsProduction()
    ? builder.Environment.ContentRootPath
    : Directory.GetParent(builder.Environment.ContentRootPath)!.FullName;

// Prima prova con file criptato (più sicuro)
var encryptedSecretsPath = Path.Combine(solutionRoot, "appsettings.Secrets.encrypted");
var secretsPath = Path.Combine(solutionRoot, "appsettings.Secrets.json");
var dbConfigPath = Path.Combine(solutionRoot, "appsettings.Database.json");

if (File.Exists(encryptedSecretsPath))
{
    // Decripta e carica in memoria (nessun file temporaneo su disco)
    builder.Configuration.AddEncryptedSecrets(encryptedSecretsPath);
}
else if (File.Exists(secretsPath))
{
    // Fallback a file in chiaro (sviluppo iniziale)
    builder.Configuration.AddJsonFile(secretsPath, optional: false, reloadOnChange: true);
}
else if (File.Exists(dbConfigPath))
{
    // Legacy fallback
    builder.Configuration.AddJsonFile(dbConfigPath, optional: false, reloadOnChange: true);
}

// Configura DatabaseConfiguration per la DI
builder.Services.Configure<DatabaseConfiguration>(options =>
{
    options.MESManagerDb = builder.Configuration.GetConnectionString("MESManagerDb") ?? "";
    options.MagoDb = builder.Configuration.GetConnectionString("MagoDb") 
                     ?? builder.Configuration["Mago:ConnectionString"] ?? "";
    options.GanttDb = builder.Configuration.GetConnectionString("GanttDb") ?? "";
});

// Abilita i controller API con JSON camelCase
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Legge la connection string dal file condiviso
var connectionString = builder.Configuration.GetConnectionString("MESManagerDb")
    ?? throw new InvalidOperationException("Connection string 'MESManagerDb' not found. Please create appsettings.Secrets.json from template.");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// MudBlazor
builder.Services.AddMudServices();

// Syncfusion Blazor
builder.Services.AddSyncfusionBlazor();

// HttpClient per Blazor
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("http://localhost:5156/")
});

// Custom Services
builder.Services.AddScoped<PreferencesService>();
builder.Services.AddScoped<AnimeImportService>();
builder.Services.AddScoped<AnimeExcelImportService>();
builder.Services.AddScoped<AllegatiAnimaService>();
builder.Services.AddScoped<IAllegatoArticoloRepository, AllegatoArticoloRepository>();
builder.Services.AddScoped<IAllegatoArticoloService, AllegatoArticoloService>();
builder.Services.AddScoped<IPianificazioneService, PianificazioneService>();
builder.Services.AddHttpClient<PlcDataService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5156/");
});
builder.Services.AddScoped<IPlcSyncCoordinator, PlcSyncCoordinator>();
builder.Services.AddSingleton<IPageToolbarService, PageToolbarService>();
builder.Services.AddScoped<AppBarContentService>();

// Infrastructure e DbContext
builder.Services.AddInfrastructure(connectionString);

// Configurazione Mago Sync
var magoOptions = new MagoOptions();
builder.Configuration.GetSection("Mago").Bind(magoOptions);

var backupPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "SyncBackups");
builder.Services.AddMagoSync(magoOptions, backupPath);

// Identity
builder.Services.AddDbContext<MesManagerDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions => 
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    // Policy password più sicura
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false; // Opzionale per usabilità
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 4;
    
    // Lockout per prevenire brute force
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
    .AddEntityFrameworkStores<MesManagerDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

// SignalR Hub
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// SignalR Hub
app.MapHub<RealtimeHub>("/hubs/realtime");

// API Controllers
app.MapControllers();

app.Run();

// Rendi Program visibile ai test E2E
public partial class Program { }
