using System;
using System.Threading.Tasks;
using PlcMagoSync.SYNC_MAGO.Services;
using PlcMagoSync.SYNC_MAGO.Models;
using PlcMagoSync.SYNC_MAGO.Config;

namespace PlcMagoSync.SYNC_MAGO.Modules
{
    public class SyncArticoli
    {
        private readonly MagoDbService _db;
        private readonly GoogleSheetsService _sheets;
        private readonly ConfigMago _config;

        public SyncArticoli(MagoDbService db, GoogleSheetsService sheets, ConfigMago config)
        {
            _db = db;
            _sheets = sheets;
            _config = config;
        }

        public async Task RunAsync()
        {
            try
            {
                Console.WriteLine("== SYNC ARTICOLI (ARTICOLI_MAGO) ==");
                Console.WriteLine($"  Spreadsheet ID: {_config.GoogleSheetId}");

                Console.WriteLine("Esecuzione query...");
                var articoli = await _db.GetArticoliAsync();

                if (articoli.Count == 0)
                {
                    Console.WriteLine("⚠ Nessun articolo trovato nel database");
                    return;
                }

                Console.WriteLine($"✓ Articoli letti da Mago: {articoli.Count}");
                Console.WriteLine("  Primi 3 articoli:");
                for (int i = 0; i < Math.Min(3, articoli.Count); i++)
                {
                    var a = articoli[i];
                    Console.WriteLine($"    - {a.Codice}: {a.Descrizione} (€ {a.Prezzo}, Attivo: {a.Attivo})");
                }

                await _sheets.WriteArticoliAsync(_config.GoogleSheetId, "ARTICOLI_MAGO", articoli);

                Console.WriteLine("✓ SYNC ARTICOLI completata.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Errore durante SYNC ARTICOLI: {ex.Message}");
                throw;
            }
        }
    }
}
