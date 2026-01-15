using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using PlcMagoSync.SYNC_MAGO.Config;

namespace PlcMagoSync.SYNC_MAGO
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("== PlcMagoSync SYNC_MAGO ==");

            // Carica le variabili d'ambiente da .env.local
            LoadEnvironmentVariablesFromFile();

            var configPath = "config_mago.json";
            if (!File.Exists(configPath))
            {
                Console.WriteLine($"Config non trovata: {configPath}");
                return;
            }

            var json = await File.ReadAllTextAsync(configPath);
            var cfg = JsonSerializer.Deserialize<ConfigMago>(json);

            if (cfg == null)
            {
                Console.WriteLine("Errore nella lettura di config_mago.json");
                return;
            }

            // Sostituisci i placeholder con variabili d'ambiente
            cfg.GoogleSheetId = ReplaceWithEnvironmentVariable(cfg.GoogleSheetId, "GOOGLE_SHEET_ID");
            cfg.ServiceAccountJsonPath = ReplaceWithEnvironmentVariable(cfg.ServiceAccountJsonPath, "SERVICE_ACCOUNT_JSON_PATH");
            cfg.MagoConnectionString = ReplaceWithEnvironmentVariable(cfg.MagoConnectionString, "MAGO_CONNECTION_STRING");

            // Valida la configurazione
            if (!ValidateConfig(cfg))
            {
                return;
            }

            var manager = new MagoSyncManager(cfg);
            await manager.RunAsync();
        }

        static void LoadEnvironmentVariablesFromFile()
        {
            var envLocalPath = ".env.local";

            // Se il file non esiste, crealo con il template
            if (!File.Exists(envLocalPath))
            {
                Console.WriteLine($"File {envLocalPath} non trovato. Creazione in corso...");
                var template = @"# Configurazione PlcMagoSync - File locale (NON committare!)
# Copia da .env.local.example e compila con i tuoi valori

GOOGLE_SHEET_ID=
SERVICE_ACCOUNT_JSON_PATH=
MAGO_CONNECTION_STRING=
";
                File.WriteAllText(envLocalPath, template);
                Console.WriteLine($"✓ File {envLocalPath} creato. Compila i valori e riavvia l'applicazione.");
                return;
            }

            // Carica le variabili dal file .env.local
            try
            {
                var lines = File.ReadAllLines(envLocalPath);
                foreach (var line in lines)
                {
                    // Ignora linee vuote e commenti
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                        continue;

                    var parts = line.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim();

                        // Imposta la variabile d'ambiente solo se il valore non è vuoto
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            Environment.SetEnvironmentVariable(key, value);
                        }
                    }
                }

                Console.WriteLine($"✓ Variabili d'ambiente caricate da {envLocalPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante la lettura di {envLocalPath}: {ex.Message}");
            }
        }


        static string ReplaceWithEnvironmentVariable(string value, string envVarName)
        {
            if (string.IsNullOrEmpty(value) || value.StartsWith("${"))
            {
                var envValue = Environment.GetEnvironmentVariable(envVarName);
                if (string.IsNullOrEmpty(envValue))
                {
                    throw new InvalidOperationException($"Errore: La variabile d'ambiente '{envVarName}' non è configurata. " +
                        $"Verifica che sia presente in .env.local o nelle variabili di sistema.");
                }
                return envValue;
            }
            return value;
        }

        static bool ValidateConfig(ConfigMago cfg)
        {
            if (string.IsNullOrWhiteSpace(cfg.GoogleSheetId))
            {
                Console.WriteLine("Errore: GoogleSheetId non configurato");
                return false;
            }

            if (string.IsNullOrWhiteSpace(cfg.ServiceAccountJsonPath))
            {
                Console.WriteLine("Errore: ServiceAccountJsonPath non configurato");
                return false;
            }

            if (!File.Exists(cfg.ServiceAccountJsonPath))
            {
                Console.WriteLine($"Errore: File service account non trovato: {cfg.ServiceAccountJsonPath}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(cfg.MagoConnectionString))
            {
                Console.WriteLine("Errore: MagoConnectionString non configurato");
                return false;
            }

            return true;
        }
    }
}
