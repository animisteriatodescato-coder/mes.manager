using System.Security.Cryptography;
using System.Text;
using MESManager.Application.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MESManager.Infrastructure.Configuration;

/// <summary>
/// Bootstrap condiviso per configurazione e connection string dei processi MESManager.
/// Mantiene Web, Worker e PlcSync allineati sullo stesso ordine di precedenza.
/// </summary>
public static class SharedConfigurationExtensions
{
    public static IConfigurationBuilder AddMesManagerSharedConfiguration(
        this IConfigurationBuilder configuration,
        IHostEnvironment environment)
    {
        var root = FindSharedConfigurationRoot(environment);
        var encryptedSecretsPath = Path.Combine(root, "appsettings.Secrets.encrypted");
        var secretsPath = Path.Combine(root, "appsettings.Secrets.json");
        var dbConfigPath = Path.Combine(root, "appsettings.Database.json");
        var dbConfigEnvPath = Path.Combine(root, $"appsettings.Database.{environment.EnvironmentName}.json");

        var encryptedSecretsLoaded = false;

        if (File.Exists(encryptedSecretsPath))
        {
            try
            {
                configuration.AddEncryptedSecrets(encryptedSecretsPath);
                encryptedSecretsLoaded = true;
            }
            catch (InvalidOperationException) when (!environment.IsProduction()
                && (File.Exists(secretsPath) || File.Exists(dbConfigPath) || File.Exists(dbConfigEnvPath)))
            {
                // In sviluppo un file DPAPI creato da un altro utente non deve bloccare
                // il fallback ai JSON locali. In produzione l'errore resta bloccante.
            }
        }

        if (File.Exists(secretsPath))
        {
            configuration.AddJsonFile(secretsPath, optional: true, reloadOnChange: true);
        }

        if (!encryptedSecretsLoaded && !File.Exists(secretsPath) && File.Exists(dbConfigPath))
        {
            configuration.AddJsonFile(dbConfigPath, optional: false, reloadOnChange: true);
        }

        if (!environment.IsProduction() && File.Exists(dbConfigEnvPath))
        {
            configuration.AddJsonFile(dbConfigEnvPath, optional: true, reloadOnChange: true);
        }

        return configuration;
    }

    public static IServiceCollection ConfigureMesManagerDatabaseConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DatabaseConfiguration>(options =>
        {
            options.MESManagerDb = configuration.GetConnectionString("MESManagerDb") ?? "";
            options.MagoDb = configuration.GetConnectionString("MagoDb")
                             ?? configuration["Mago:ConnectionString"]
                             ?? "";
            options.GanttDb = configuration.GetConnectionString("GanttDb") ?? "";
            options.AllegatiDb = configuration.GetConnectionString("AllegatiDb");
        });

        return services;
    }

    public static string GetRequiredMesManagerConnectionString(this IConfiguration configuration)
    {
        return configuration.GetConnectionString("MESManagerDb")
            ?? throw new InvalidOperationException("Connection string 'MESManagerDb' not found in MESManager shared configuration.");
    }

    private static string FindSharedConfigurationRoot(IHostEnvironment environment)
    {
        var contentRoot = new DirectoryInfo(environment.ContentRootPath);
        var current = contentRoot.Parent;
        while (current != null)
        {
            if (ContainsSharedConfigurationFile(current.FullName, environment.EnvironmentName))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        if (ContainsSharedConfigurationFile(contentRoot.FullName, environment.EnvironmentName))
        {
            return contentRoot.FullName;
        }

        return environment.ContentRootPath;
    }

    private static bool ContainsSharedConfigurationFile(string path, string environmentName)
    {
        return File.Exists(Path.Combine(path, "appsettings.Secrets.encrypted"))
            || File.Exists(Path.Combine(path, "appsettings.Secrets.json"))
            || File.Exists(Path.Combine(path, "appsettings.Database.json"))
            || File.Exists(Path.Combine(path, $"appsettings.Database.{environmentName}.json"));
    }

    private static void AddEncryptedSecrets(this IConfigurationBuilder configuration, string encryptedFilePath)
    {
        var json = DecryptSecretsFile(encryptedFilePath);
        if (json == null)
        {
            return;
        }

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        configuration.AddJsonStream(stream);
    }

    private static string? DecryptSecretsFile(string encryptedFilePath)
    {
        if (!File.Exists(encryptedFilePath))
        {
            return null;
        }

        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("appsettings.Secrets.encrypted usa DPAPI ed è supportato solo su Windows.");
        }

        try
        {
            var encryptedBytes = File.ReadAllBytes(encryptedFilePath);
            var decryptedBytes = ProtectedData.Unprotect(
                encryptedBytes,
                null,
                DataProtectionScope.CurrentUser);

            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (CryptographicException)
        {
            throw new InvalidOperationException(
                $"Impossibile decriptare {encryptedFilePath}. " +
                "Il file può essere decriptato solo dall'utente Windows che lo ha criptato. " +
                "Esegui 'protect-secrets.ps1 -Decrypt' per rigenerare il file in chiaro.");
        }
    }
}
