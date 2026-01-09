using System.Collections.Generic;
using System.Threading.Tasks;
using PlcMagoSync.SYNC_MAGO.Config;
using PlcMagoSync.SYNC_MAGO.Models;
using PlcShared.Services;

namespace PlcMagoSync.SYNC_MAGO.Services
{
    /// <summary>
    /// Wrapper per GoogleSheetsService di PlcShared
    /// Mantiene compatibilità con codice esistente PlcMagoSync
    /// </summary>
    public class GoogleSheetsService
    {
        private readonly PlcShared.Services.GoogleSheetsService _sharedService;
        private readonly ConfigMago _config;

        public GoogleSheetsService(ConfigMago config)
        {
            _config = config;
            _sharedService = new PlcShared.Services.GoogleSheetsService(
                config.ServiceAccountJsonPath,
                "PlcMagoSync"
            );
        }

        public async Task WriteClientiAsync(string spreadsheetId, string sheetName, List<ClienteMago> clienti)
        {
            if (clienti.Count == 0)
            {
                System.Console.WriteLine($"  ⓘ Nessun cliente da scrivere, operazione skippata.");
                return;
            }

            // Assicura che il foglio esista
            await _sharedService.EnsureSheetExistsAsync(spreadsheetId, sheetName);

            // Pulisci il foglio
            await _sharedService.ClearRangeAsync(spreadsheetId, $"{sheetName}!A1:E");

            // Prepara i dati con intestazioni
            var allValues = new List<IList<object>>
            {
                new List<object> { "Codice", "Nome", "Email", "Note", "UltimaModifica" }
            };

            foreach (var c in clienti)
            {
                allValues.Add(new List<object>
                {
                    c.Codice,
                    c.Nome,
                    c.Email,
                    c.Note,
                    c.UltimaModifica
                });
            }

            // Scrivi tutto
            await _sharedService.UpdateRangeAsync(spreadsheetId, $"{sheetName}!A1", allValues);
            System.Console.WriteLine($"  ✓ {allValues.Count - 1} righe scritte con successo!");
        }

        public async Task WriteArticoliAsync(string spreadsheetId, string sheetName, List<ArticoloMago> articoli)
        {
            if (articoli.Count == 0)
            {
                System.Console.WriteLine($"  ⓘ Nessun articolo da scrivere, operazione skippata.");
                return;
            }

            await _sharedService.EnsureSheetExistsAsync(spreadsheetId, sheetName);
            await _sharedService.ClearRangeAsync(spreadsheetId, $"{sheetName}!A1:G");

            var allValues = new List<IList<object>>
            {
                new List<object> { "Codice", "Descrizione", "Prezzo", "Attivo", "UltimaModifica", "StatoCancellato", "TimestampSync" }
            };

            foreach (var a in articoli)
            {
                allValues.Add(new List<object>
                {
                    a.Codice,
                    a.Descrizione,
                    a.Prezzo,
                    a.Attivo,
                    a.UltimaModifica,
                    a.StatoCancellato,
                    a.TimestampSync
                });
            }

            await _sharedService.UpdateRangeAsync(spreadsheetId, $"{sheetName}!A1", allValues);
            System.Console.WriteLine($"  ✓ {allValues.Count - 1} righe scritte con successo!");
        }

        public async Task WriteCommesseAsync(string spreadsheetId, string sheetName, List<CommessaMago> commesse)
        {
            if (commesse.Count == 0)
            {
                System.Console.WriteLine($"  ⓘ Nessuna commessa da scrivere, operazione skippata.");
                return;
            }

            await _sharedService.EnsureSheetExistsAsync(spreadsheetId, sheetName);
            await _sharedService.ClearRangeAsync(spreadsheetId, $"{sheetName}!A1:O");

            var allValues = new List<IList<object>>
            {
                new List<object> { "SaleOrdId", "InternalOrdNo", "ExternalOrdNo", "Customer", "CompanyName", "Delivered", "Item", "Line", "Description", "Qty", "UoM", "OurReference", "YourReference", "ExpectedDeliveryDate", "TBModified" }
            };

            foreach (var c in commesse)
            {
                allValues.Add(new List<object>
                {
                    c.SaleOrdId,
                    c.InternalOrdNo,
                    c.ExternalOrdNo,
                    c.Customer,
                    c.CompanyName,
                    c.Delivered,
                    c.Item,
                    c.Line,
                    c.Description,
                    c.Qty,
                    c.UoM,
                    c.OurReference,
                    c.YourReference,
                    c.ExpectedDeliveryDate,
                    c.TBModified
                });
            }

            await _sharedService.UpdateRangeAsync(spreadsheetId, $"{sheetName}!A1", allValues);
            System.Console.WriteLine($"  ✓ {allValues.Count - 1} righe scritte con successo!");
        }
    }
}
