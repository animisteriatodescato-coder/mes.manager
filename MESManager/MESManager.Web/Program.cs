using MESManager.Web.Components;
using MESManager.Infrastructure;
using MESManager.Infrastructure.Services;
using MESManager.Sync;
using MESManager.Sync.Configuration;
using MudBlazor.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MESManager.Infrastructure.Data;
using MESManager.Web.Hubs;
using MESManager.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Abilita i controller API
builder.Services.AddControllers();

// Configurazione Connection String
var connectionString = "Server=localhost\\SQLEXPRESS;Database=MESManager;Trusted_Connection=True;TrustServerCertificate=True;";

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// MudBlazor
builder.Services.AddMudServices();

// HttpClient per Blazor
builder.Services.AddHttpClient();

// Custom Services
builder.Services.AddScoped<PreferencesService>();
builder.Services.AddScoped<IPlcSyncCoordinator, PlcSyncCoordinator>();

// Infrastructure e DbContext
builder.Services.AddInfrastructure(connectionString);

// Configurazione Mago Sync
var magoOptions = new MagoOptions();
builder.Configuration.GetSection("Mago").Bind(magoOptions);

var backupPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "SyncBackups");
builder.Services.AddMagoSync(magoOptions, backupPath);

// Identity
builder.Services.AddDbContext<MesManagerDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
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
