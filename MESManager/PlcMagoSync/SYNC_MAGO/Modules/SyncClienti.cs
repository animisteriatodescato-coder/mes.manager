using System;
using System.Threading.Tasks;
using PlcMagoSync.SYNC_MAGO.Services;
using PlcMagoSync.SYNC_MAGO.Config;
using PlcMagoSync.SYNC_MAGO.Models;

namespace PlcMagoSync.SYNC_MAGO.Modules
{
    public class SyncClienti
    {
        private readonly MagoDbService _db;
        private readonly GoogleSheetsService _sheets;
        private readonly ConfigMago _config;

        public SyncClienti(MagoDbService db, GoogleSheetsService sheets, ConfigMago config)
        {
            _db = db;
            _sheets = sheets;
            _config = config;
        }

        public async Task RunAsync()
        {
            Console.WriteLine("== SYNC CLIENTI (CLIENTI_MAGO) ==");
            Console.WriteLine($"  Spreadsheet ID: {_config.GoogleSheetId}");

            string sql = @"
    SELECT 
        CAST(CustSupp AS VARCHAR(50)) AS Codice,
        CompanyName AS Nome,
        EMail AS Email,
        Notes AS Note,
        CONVERT(VARCHAR(19), TBModified, 120) AS UltimaModifica
    FROM MA_CustSupp
    ORDER BY CAST(CustSupp AS VARCHAR(50));
";

            List<ClienteMago>? clienti = null;

            try
            {
                Console.WriteLine($"Esecuzione query...");
                clienti = await _db.QueryAsync(sql, reader => new ClienteMago
                {
                    Codice = reader["Codice"]?.ToString() ?? "",
                    Nome = reader["Nome"]?.ToString() ?? "",
                    Email = reader["Email"]?.ToString() ?? "",
                    Note = reader["Note"]?.ToString() ?? "",
                    UltimaModifica = reader["UltimaModifica"]?.ToString() ?? ""
                });

                Console.WriteLine($"✓ Clienti letti da Mago: {clienti.Count}");
                
                if (clienti.Count > 0)
                {
                    Console.WriteLine($"  Primi 3 clienti:");
                    foreach (var c in clienti.Take(3))
                    {
                        Console.WriteLine($"    - {c.Codice}: {c.Nome}");
                    }
                }

                await _sheets.WriteClientiAsync(_config.GoogleSheetId, "CLIENTI_MAGO", clienti);

                Console.WriteLine("✓ SYNC CLIENTI completata.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Errore durante SYNC CLIENTI: {ex.Message}");
                Console.WriteLine($"   Type: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner: {ex.InnerException.Message}");
                }
                
                // Salva i clienti in un file locale come fallback
                if (clienti != null && clienti.Count > 0)
                {
                    try
                    {
                        var json = System.Text.Json.JsonSerializer.Serialize(clienti, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                        var backupDir = "backup";
                        if (!System.IO.Directory.Exists(backupDir))
                            System.IO.Directory.CreateDirectory(backupDir);
                        var backupFile = System.IO.Path.Combine(backupDir, $"clienti_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                        await System.IO.File.WriteAllTextAsync(backupFile, json);
                        Console.WriteLine($"   💾 Dati salvati in backup: {backupFile}");
                    }
                    catch { }
                }
                
                // Continua con i prossimi sync anche se questo fallisce
                Console.WriteLine($"   → Continuando con i prossimi moduli...");
            }
        }
    }
}
