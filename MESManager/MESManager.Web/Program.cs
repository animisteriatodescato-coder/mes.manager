using MESManager.Web.Components;
using MESManager.Infrastructure;
using MESManager.Infrastructure.Entities;
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
var dbConfigEnvPath = Path.Combine(solutionRoot, $"appsettings.Database.{builder.Environment.EnvironmentName}.json");

if (File.Exists(encryptedSecretsPath))
{
    // Decripta e carica in memoria (nessun file temporaneo su disco)
    // CA1416 ignorato: l'app è Windows-only, usa DPAPI per crittografia
#pragma warning disable CA1416
    builder.Configuration.AddEncryptedSecrets(encryptedSecretsPath);
#pragma warning restore CA1416
}

// Carica sempre anche il JSON in chiaro (opzionale) — sovrascrive/integra l'encrypted.
// Permette di aggiungere chiavi (es. OpenAI) senza ricreare il file criptato.
if (File.Exists(secretsPath))
{
    builder.Configuration.AddJsonFile(secretsPath, optional: true, reloadOnChange: true);
}

if (!File.Exists(encryptedSecretsPath) && !File.Exists(secretsPath) && File.Exists(dbConfigPath))
{
    // Legacy fallback
    builder.Configuration.AddJsonFile(dbConfigPath, optional: false, reloadOnChange: true);
}

// Override locale per ambiente (solo se presente)
if (!builder.Environment.IsProduction() && File.Exists(dbConfigEnvPath))
{
    builder.Configuration.AddJsonFile(dbConfigEnvPath, optional: false, reloadOnChange: true);
}

// Configura DatabaseConfiguration per la DI
builder.Services.Configure<DatabaseConfiguration>(options =>
{
    options.MESManagerDb = builder.Configuration.GetConnectionString("MESManagerDb") ?? "";
    options.MagoDb = builder.Configuration.GetConnectionString("MagoDb") 
                     ?? builder.Configuration["Mago:ConnectionString"] ?? "";
    options.AllegatiDb = builder.Configuration.GetConnectionString("AllegatiDb"); // Null se non configurato (fallback a MESManagerDb)
    // GanttDb non più usato - dati migrati in MESManagerDb
});

// Configura FileConfiguration per i percorsi allegati (con valori di default)
builder.Services.Configure<FileConfiguration>(options =>
{
    var section = builder.Configuration.GetSection("Files");
    options.AllegatiBasePath = section["AllegatiBasePath"] ?? @"C:\Dati\Documenti\AA SCHEDE PRODUZIONE\foto cel";
    var mappings = section.GetSection("PathMappings").Get<List<string>>();
    options.PathMappings = mappings ?? new List<string> { @"P:\Documenti->C:\Dati\Documenti" };
});

// Abilita i controller API con JSON camelCase
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Razor Pages (usato per le pagine Login/Logout di Identity)
builder.Services.AddRazorPages();

// Legge la connection string dal file condiviso
var connectionString = builder.Configuration.GetConnectionString("MESManagerDb")
    ?? throw new InvalidOperationException("Connection string 'MESManagerDb' not found. Please create appsettings.Secrets.json from template.");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        // Abilita errori dettagliati per Blazor Server (solo in Development)
        if (builder.Environment.IsDevelopment())
        {
            options.DetailedErrors = true;
        }
    });

// MudBlazor
builder.Services.AddMudServices();

// Syncfusion Blazor
builder.Services.AddSyncfusionBlazor();

// ============================================
// HTTPCLIENT CON COOKIE FORWARDING (sicurezza)
// ============================================
// In Blazor Server, le chiamate HttpClient server-side non portano
// automaticamente i cookie. CookieForwardingHandler li trasferisce
// dalla sessione SignalR alle chiamate interne a localhost.
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<CookieForwardingHandler>();

// Client con nome "blazor-internal": usato da tutti i componenti Blazor
builder.Services.AddHttpClient("blazor-internal", client =>
{
    client.BaseAddress = new Uri("http://localhost:5156/");
}).AddHttpMessageHandler<CookieForwardingHandler>();

// Registrazione scoped di HttpClient: inietta il named client per @inject HttpClient Http
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("blazor-internal"));

// Custom Services
builder.Services.AddScoped<PreferencesService>();
builder.Services.AddScoped<UserThemeService>();
builder.Services.AddScoped<AnimeImportService>();
builder.Services.AddScoped<AnimeExcelImportService>();
builder.Services.AddScoped<AllegatiAnimaService>();
builder.Services.AddScoped<RicettaGanttService>();
builder.Services.AddScoped<IAllegatoArticoloRepository, AllegatoArticoloRepository>();
builder.Services.AddScoped<IAllegatoArticoloService, AllegatoArticoloService>();
builder.Services.AddScoped<IPianificazioneService, PianificazioneService>();
builder.Services.AddScoped<PianificazioneNotificationService>();
// Modulo Manutenzioni
builder.Services.AddScoped<IManutenzioneService, ManutenzioneService>();
builder.Services.AddScoped<IManutenzioneCassaService, ManutenzioneCassaService>();
builder.Services.AddScoped<IManutenzioneCassaAllegatoService, ManutenzioneCassaAllegatoService>();
builder.Services.AddHttpClient<PlcDataService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5156/");
}).AddHttpMessageHandler<CookieForwardingHandler>();
builder.Services.AddScoped<IPlcSyncCoordinator, PlcSyncCoordinator>();
builder.Services.AddScoped<IPlcStatusService, PlcStatusService>();
builder.Services.AddSingleton<IPageToolbarService, PageToolbarService>();
builder.Services.AddScoped<AppBarContentService>();
builder.Services.AddSingleton<ColorExtractionService>();
builder.Services.AddSingleton<AppSettingsService>();
// Generazione PDF preventivi via headless Chrome/Edge (v1.65.56)
builder.Services.AddSingleton<ChromiumPdfService>();

// Bridge AI: legge la config provider (OpenAI/Ollama) da AppSettingsService per Infrastructure (v1.65.12)
builder.Services.AddSingleton<IAiSettingsReader, WebAiSettingsReader>();

// Tabelle di lookup con persistenza su file JSON (tabelle-config.json)
builder.Services.AddSingleton<ITabelleService, TabelleService>();

// Tema dark/light — Scoped (una istanza per circuito Blazor Server = per sessione utente)
// I componenti che dipendono da IsDarkMode iniettano IThemeModeService anziché accedere a MainLayout
builder.Services.AddScoped<IThemeModeService, ThemeModeService>();
builder.Services.AddScoped<ThemeCssService>();

// Servizio Singleton per gestione stato real-time PLC (avviato automaticamente)
builder.Services.AddSingleton<RealtimeStateService>();

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

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
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

// Cookie di autenticazione: percorsi, scadenza e sicurezza
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath        = "/Account/Login";
    options.LogoutPath       = "/Account/Logout";
    options.AccessDeniedPath = "/Account/Login";
    options.ExpireTimeSpan   = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly  = true;
    options.Cookie.SecurePolicy = builder.Environment.IsProduction()
        ? Microsoft.AspNetCore.Http.CookieSecurePolicy.Always
        : Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite  = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

// Policy per accesso pagine (claim-based per ruolo Visualizzazione).
// Ogni gruppo definisce i ruoli con accesso completo (BaseRoles).
// Visualizzazione: solo se ha claim "pagina=<valore>".
builder.Services.AddAuthorization(options =>
{
    foreach (var gruppo in MESManager.Web.Constants.PaginaPolicy.Gruppi)
    {
        var baseRoles = gruppo.BaseRoles;
        foreach (var pagina in gruppo.Pagine)
        {
            var claimValue = pagina.ClaimValue; // closure
            options.AddPolicy(pagina.PolicyName, p =>
                p.RequireAssertion(ctx =>
                    baseRoles.Any(r => ctx.User.IsInRole(r)) ||
                    ctx.User.HasClaim(MESManager.Web.Constants.PaginaPolicy.ClaimType, claimValue)));
        }
    }
});

// SignalR Hub
builder.Services.AddSignalR();

var app = builder.Build();

// Seed automatico ruoli e utente Admin
using (var seedScope = app.Services.CreateScope())
{
    await MESManager.Web.Services.RoleSeedService.SeedAsync(seedScope.ServiceProvider);
    // Seed attività manutenzione di default (idempotente)
    var manService = seedScope.ServiceProvider.GetRequiredService<IManutenzioneService>();
    await manService.SeedAttivitaDefaultAsync();
    // Seed attività manutenzione casse d'anima (idempotente)
    var manCassaService = seedScope.ServiceProvider.GetRequiredService<IManutenzioneCassaService>();
    await manCassaService.SeedAttivitaDefaultAsync();
}
var enableE2ESeed = (Environment.GetEnvironmentVariable("E2E_SEED") ?? "")
    .Equals("1", StringComparison.OrdinalIgnoreCase)
    || (Environment.GetEnvironmentVariable("E2E_SEED") ?? "")
        .Equals("true", StringComparison.OrdinalIgnoreCase);

if (enableE2ESeed)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<MesManagerDbContext>();
    var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("E2ESeed");
    await E2ETestDataSeeder.SeedAsync(db, logger);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// ── Security Headers ─────────────────────────────────────────────────────────
// Aggiunge header di sicurezza HTTP a tutte le risposte
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    // CSP: permette risorse solo da stessa origine + CDN noti usati dall'app
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://unpkg.com; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; " +
        "font-src 'self' https://fonts.gstatic.com; " +
        "img-src 'self' data: blob:; " +
        "connect-src 'self' ws: wss:; " +
        "frame-ancestors 'none';";
    await next();
});

app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

// Razor Pages (Login/Logout Identity)
app.MapRazorPages();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// SignalR Hub per dati PLC real-time
app.MapHub<RealtimeHub>("/hubs/realtime");

// SignalR Hub per pianificazione Gantt
app.MapHub<PianificazioneHub>("/hubs/pianificazione");

// API Controllers
app.MapControllers();

// Avvia il servizio di polling PLC real-time
var realtimeService = app.Services.GetRequiredService<RealtimeStateService>();
realtimeService.Start();

// ── Preventivo PDF: genera PDF via headless Chrome/Edge e lo restituisce come file download (v1.65.56) ──
app.MapPost("/api/preventivo/pdf", async (
    HttpContext ctx,
    ChromiumPdfService pdfSvc) =>
{
    // Legge manualmente il body JSON per evitare problemi con antiforgery
    using var reader = new System.IO.StreamReader(ctx.Request.Body);
    var body = await reader.ReadToEndAsync();
    var req = System.Text.Json.JsonSerializer.Deserialize<PreventivoRenderPdfRequest>(
        body,
        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    if (req == null || string.IsNullOrWhiteSpace(req.Html))
        return Results.BadRequest("HTML mancante.");

    if (!pdfSvc.IsAvailable())
        return Results.Problem(
            "Chrome/Edge non trovato dal servizio server-side. Installare Chrome/Edge o configurare PATH.",
            statusCode: 503);

    var (bytes, pdfError) = await pdfSvc.GeneratePdfAsync(req.Html);
    if (bytes == null)
        return Results.Problem($"Generazione PDF fallita: {pdfError}", statusCode: 500);

    var fileName = string.IsNullOrWhiteSpace(req.FileName) ? "preventivo.pdf" : req.FileName;
    return Results.File(bytes, "application/pdf", fileName);
})
.DisableAntiforgery()
.RequireAuthorization();

app.Run();

// Rendi Program visibile ai test E2E
public partial class Program { }

/// <summary>Payload per l'endpoint /api/preventivo/pdf (v1.65.56)</summary>
internal record PreventivoRenderPdfRequest(string Html, string FileName);
