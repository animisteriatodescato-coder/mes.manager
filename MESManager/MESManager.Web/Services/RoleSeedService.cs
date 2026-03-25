using Microsoft.AspNetCore.Identity;
using MESManager.Infrastructure.Entities;

namespace MESManager.Web.Services;

/// <summary>
/// Seed automatico dei ruoli Identity e dell'utente Admin al primo avvio.
/// Chiamato da Program.cs dopo app.Build().
/// </summary>
public static class RoleSeedService
{
    /// <summary>
    /// I 5 ruoli del sistema MESManager.
    /// Usato anche da GestioneAccessi.razor per elencare i ruoli disponibili.
    /// </summary>
    public static readonly string[] Ruoli =
        ["Admin", "Produzione", "Ufficio", "Manutenzione", "Visualizzazione"];

    public static async Task SeedAsync(IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var config = services.GetRequiredService<IConfiguration>();

        // Crea i ruoli se non esistono
        foreach (var ruolo in Ruoli)
        {
            if (!await roleManager.RoleExistsAsync(ruolo))
            {
                await roleManager.CreateAsync(new IdentityRole(ruolo));
                logger.LogInformation("Ruolo creato: {Ruolo}", ruolo);
            }
        }

        // Crea l'utente admin di default (solo se non esiste)
        var adminUsername = config["Identity:AdminUsername"] ?? "admin";
        var adminPassword = config["Identity:AdminPassword"] ?? "Admin@123!";
        var adminEmail    = config["Identity:AdminEmail"]    ?? "admin@mesmanager.local";

        if (await userManager.FindByNameAsync(adminUsername) == null)
        {
            var admin = new ApplicationUser
            {
                UserName       = adminUsername,
                Nome           = "Admin",
                Email          = adminEmail,
                EmailConfirmed = true,
                Attivo         = true
            };

            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
                logger.LogInformation("Utente Admin creato: {Username}", adminUsername);
            }
            else
            {
                var err = string.Join(", ", result.Errors.Select(e => e.Description));
                logger.LogError("Errore creazione Admin: {Errors}", err);
            }
        }
    }
}
