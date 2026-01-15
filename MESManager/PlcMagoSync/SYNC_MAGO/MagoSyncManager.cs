using System;
using System.Threading.Tasks;
using PlcMagoSync.SYNC_MAGO.Config;
using PlcMagoSync.SYNC_MAGO.Services;
using PlcMagoSync.SYNC_MAGO.Modules;

namespace PlcMagoSync.SYNC_MAGO
{
    public class MagoSyncManager
    {
        private readonly ConfigMago _config;
        private readonly MagoDbService _db;
        private readonly GoogleSheetsService _sheets;

        public MagoSyncManager(ConfigMago config)
        {
            _config = config;
            _db = new MagoDbService(config);
            _sheets = new GoogleSheetsService(config);
        }

        public async Task RunAsync()
        {
            Console.WriteLine("=== AVVIO SYNC MAGO ===");

            try
            {
                var syncClienti = new SyncClienti(_db, _sheets, _config);
                await syncClienti.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Modulo SyncClienti fallito: {ex.Message}");
            }

            try
            {
                var syncArticoli = new SyncArticoli(_db, _sheets, _config);
                await syncArticoli.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Modulo SyncArticoli fallito: {ex.Message}");
            }

            try
            {
                var syncCommesse = new SyncCommesse(_db, _sheets, _config);
                await syncCommesse.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Modulo SyncCommesse fallito: {ex.Message}");
            }

            Console.WriteLine("=== SYNC COMPLETATA ===");
        }
    }
}
