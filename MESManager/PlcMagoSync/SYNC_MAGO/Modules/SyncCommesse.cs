using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlcMagoSync.SYNC_MAGO.Services;
using PlcMagoSync.SYNC_MAGO.Config;
using PlcMagoSync.SYNC_MAGO.Models;

namespace PlcMagoSync.SYNC_MAGO.Modules
{
    public class SyncCommesse
    {
        private readonly MagoDbService _db;
        private readonly GoogleSheetsService _sheets;
        private readonly ConfigMago _config;

        public SyncCommesse(MagoDbService db, GoogleSheetsService sheets, ConfigMago config)
        {
            _db = db;
            _sheets = sheets;
            _config = config;
        }

        public async Task RunAsync()
        {
            Console.WriteLine("== SYNC COMMESSE (COMMESSE_MAGO) ==");
            Console.WriteLine($"  Spreadsheet ID: {_config.GoogleSheetId}");

            string sql = @"
SELECT 
    SO.Delivered,
    SO.Customer, 
    SO.SaleOrdId, 
    SOD.Line, 
    SOD.Description, 
    SOD.Item, 
    SOD.Qty, 
    SOD.UoM, 
    SO.InternalOrdNo, 
    SO.ExternalOrdNo, 
    SO.OurReference, 
    SO.YourReference,
    SO.ExpectedDeliveryDate,
    C.CompanyName,
    CONVERT(VARCHAR(19), SO.TBModified, 120) AS TBModified
FROM MA_SaleOrd SO
INNER JOIN MA_SaleOrdDetails SOD ON SO.SaleOrdId = SOD.SaleOrdId
LEFT JOIN MA_CustSupp C ON C.CustSupp = SO.Customer AND C.CustSuppType = 3211264
ORDER BY SO.InternalOrdNo, SOD.Line;
";

            List<CommessaMago>? commesse = null;

            try
            {
                Console.WriteLine("Esecuzione query...");
                commesse = await _db.QueryAsync(sql, reader => new CommessaMago
                {
                    Delivered = reader["Delivered"]?.ToString() ?? "",
                    Customer = reader["Customer"]?.ToString() ?? "",
                    SaleOrdId = reader["SaleOrdId"]?.ToString() ?? "",
                    Line = reader["Line"]?.ToString() ?? "",
                    Description = reader["Description"]?.ToString() ?? "",
                    Item = reader["Item"]?.ToString() ?? "",
                    Qty = reader["Qty"]?.ToString() ?? "",
                    UoM = reader["UoM"]?.ToString() ?? "",
                    InternalOrdNo = reader["InternalOrdNo"]?.ToString() ?? "",
                    ExternalOrdNo = reader["ExternalOrdNo"]?.ToString() ?? "",
                    OurReference = reader["OurReference"]?.ToString() ?? "",
                    YourReference = reader["YourReference"]?.ToString() ?? "",
                    ExpectedDeliveryDate = reader["ExpectedDeliveryDate"]?.ToString() ?? "",
                    CompanyName = reader["CompanyName"]?.ToString() ?? "",
                    TBModified = reader["TBModified"]?.ToString() ?? ""
                });

                Console.WriteLine($"✓ Commesse lette da Mago: {commesse.Count}");

                if (commesse.Count > 0)
                {
                    Console.WriteLine($"  Primi 3 articoli di commesse:");
                    foreach (var c in commesse.Take(3))
                    {
                        Console.WriteLine($"    - {c.InternalOrdNo} / {c.Item}: {c.Description} (Qty: {c.Qty})");
                    }
                }

                // Scrivi su Google Sheets
                await _sheets.WriteCommesseAsync(_config.GoogleSheetId, "COMMESSE_MAGO", commesse);

                Console.WriteLine("✓ SYNC COMMESSE completata.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Errore durante SYNC COMMESSE: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner: {ex.InnerException.Message}");
                }

                if (commesse != null && commesse.Count > 0)
                {
                    try
                    {
                        var json = System.Text.Json.JsonSerializer.Serialize(commesse, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                        var backupFile = $"commesse_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                        await System.IO.File.WriteAllTextAsync(backupFile, json);
                        Console.WriteLine($"   💾 Dati salvati in backup: {backupFile}");
                    }
                    catch { }
                }

                Console.WriteLine($"   → Continuando con i prossimi moduli...");
            }
        }
    }
}
