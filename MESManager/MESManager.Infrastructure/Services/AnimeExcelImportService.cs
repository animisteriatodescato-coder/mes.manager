using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;

namespace MESManager.Infrastructure.Services;

public class AnimeExcelImportService
{
    private readonly MesManagerDbContext _context;
    private readonly ILogger<AnimeExcelImportService> _logger;

    public AnimeExcelImportService(MesManagerDbContext context, ILogger<AnimeExcelImportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> ImportFromExcelAsync(Stream fileStream)
    {
        try
        {
            _logger.LogInformation("=== START EXCEL IMPORT ===");
            _logger.LogInformation("DataSource: Excel Stream, Database: {Database}", _context.Database.GetDbConnection().Database);
            
            var animeList = new List<Anime>();

            using (var package = new ExcelPackage(fileStream))
            {
                var worksheet = package.Workbook.Worksheets[0];
                var rowCount = worksheet.Dimension.Rows;

                _logger.LogInformation("Inizio importazione di {rowCount} righe da Excel", rowCount - 1);

                // Mappa delle colonne Excel -> Indice
                var headers = new Dictionary<string, int>();
                for (int col = 1; col <= worksheet.Dimension.Columns; col++)
                {
                    var header = worksheet.Cells[1, col].Value?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(header))
                    {
                        headers[header] = col;
                    }
                }

                // Leggi tutte le righe (skip header)
                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        var anime = new Anime
                        {
                            IdArticolo = ParseInt32(worksheet, row, headers, "Id Articolo"),
                            CodiceArticolo = ParseString(worksheet, row, headers, "Codice Articolo") ?? string.Empty,
                            DescrizioneArticolo = ParseString(worksheet, row, headers, "Descrizione Articolo") ?? string.Empty,
                            CodiceCassa = ParseString(worksheet, row, headers, "Codice Cassa"),
                            CodiceAnime = ParseString(worksheet, row, headers, "Codice Anima"),
                            Ubicazione = ParseString(worksheet, row, headers, "Ubicazione"),
                            Cliente = ParseString(worksheet, row, headers, "Cliente"),
                            UnitaMisura = ParseString(worksheet, row, headers, "Unita Misura"),
                            Imballo = ParseInt32(worksheet, row, headers, "Imballo"),
                            Note = ParseString(worksheet, row, headers, "Note"),
                            Sabbia = ParseString(worksheet, row, headers, "Sabbia"),
                            TogliereSparo = ParseString(worksheet, row, headers, "Togliere Sparo"),
                            Vernice = ParseString(worksheet, row, headers, "Descrizione Vernice") ?? ParseString(worksheet, row, headers, "Vernice"),
                            Colla = ParseString(worksheet, row, headers, "Colla"),
                            QuantitaPiano = ParseInt32(worksheet, row, headers, "Quantita Piano"),
                            NumeroPiani = ParseInt32(worksheet, row, headers, "Numero Piani"),
                            Ciclo = ParseString(worksheet, row, headers, "Ciclo"),
                            Peso = ParseString(worksheet, row, headers, "Peso"),
                            Figure = ParseString(worksheet, row, headers, "Figure"),
                            Maschere = ParseString(worksheet, row, headers, "Maschere"),
                            Incollata = ParseString(worksheet, row, headers, "Incollata"),
                            Assemblata = ParseString(worksheet, row, headers, "Assemblata"),
                            ArmataL = ParseString(worksheet, row, headers, "Armata L"),
                            MacchineSuDisponibili = ParseString(worksheet, row, headers, "Macchine Disponibili Descrizione"),
                            Altezza = ParseInt32(worksheet, row, headers, "Altezza"),
                            Larghezza = ParseInt32(worksheet, row, headers, "Larghezza"),
                            Profondita = ParseInt32(worksheet, row, headers, "Profondita"),
                            Allegato = ParseString(worksheet, row, headers, "Allegato"),
                            DataModificaRecord = ParseDateTimeOrNull(worksheet, row, headers, "Data Modifica Record"),
                            UtenteModificaRecord = ParseString(worksheet, row, headers, "Utente Modifica Record"),
                            DataImportazione = DateTime.Now,
                            TrasmettiTutto = false
                        };

                        // Skip se non ha almeno il codice articolo
                        if (!string.IsNullOrEmpty(anime.CodiceArticolo))
                        {
                            animeList.Add(anime);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Errore durante il parsing della riga {row}", row);
                    }
                }
            }

            _logger.LogInformation("Elaborati {count} articoli validi", animeList.Count);

            int insertedCount = 0;
            int updatedCount = 0;

            // Merge intelligente: aggiorna campi mancanti o inserisci nuovi
            foreach (var excelAnime in animeList)
            {
                var existing = await _context.Anime
                    .FirstOrDefaultAsync(a => a.CodiceArticolo == excelAnime.CodiceArticolo);

                if (existing != null)
                {
                    // Aggiorna solo i campi che sono null o vuoti nel database
                    if (string.IsNullOrWhiteSpace(existing.DescrizioneArticolo) && !string.IsNullOrWhiteSpace(excelAnime.DescrizioneArticolo))
                        existing.DescrizioneArticolo = excelAnime.DescrizioneArticolo;
                    
                    if (string.IsNullOrWhiteSpace(existing.CodiceCassa) && !string.IsNullOrWhiteSpace(excelAnime.CodiceCassa))
                        existing.CodiceCassa = excelAnime.CodiceCassa;
                    
                    if (string.IsNullOrWhiteSpace(existing.CodiceAnime) && !string.IsNullOrWhiteSpace(excelAnime.CodiceAnime))
                        existing.CodiceAnime = excelAnime.CodiceAnime;
                    
                    if (string.IsNullOrWhiteSpace(existing.Ubicazione) && !string.IsNullOrWhiteSpace(excelAnime.Ubicazione))
                        existing.Ubicazione = excelAnime.Ubicazione;
                    
                    if (string.IsNullOrWhiteSpace(existing.Cliente) && !string.IsNullOrWhiteSpace(excelAnime.Cliente))
                        existing.Cliente = excelAnime.Cliente;
                    
                    if (string.IsNullOrWhiteSpace(existing.UnitaMisura) && !string.IsNullOrWhiteSpace(excelAnime.UnitaMisura))
                        existing.UnitaMisura = excelAnime.UnitaMisura;
                    
                    if (!existing.Imballo.HasValue && excelAnime.Imballo.HasValue)
                        existing.Imballo = excelAnime.Imballo;
                    
                    if (string.IsNullOrWhiteSpace(existing.Note) && !string.IsNullOrWhiteSpace(excelAnime.Note))
                        existing.Note = excelAnime.Note;
                    
                    if (string.IsNullOrWhiteSpace(existing.Sabbia) && !string.IsNullOrWhiteSpace(excelAnime.Sabbia))
                        existing.Sabbia = excelAnime.Sabbia;
                    
                    if (string.IsNullOrWhiteSpace(existing.TogliereSparo) && !string.IsNullOrWhiteSpace(excelAnime.TogliereSparo))
                        existing.TogliereSparo = excelAnime.TogliereSparo;
                    
                    if (string.IsNullOrWhiteSpace(existing.Vernice) && !string.IsNullOrWhiteSpace(excelAnime.Vernice))
                        existing.Vernice = excelAnime.Vernice;
                    
                    if (string.IsNullOrWhiteSpace(existing.Colla) && !string.IsNullOrWhiteSpace(excelAnime.Colla))
                        existing.Colla = excelAnime.Colla;
                    
                    if (!existing.QuantitaPiano.HasValue && excelAnime.QuantitaPiano.HasValue)
                        existing.QuantitaPiano = excelAnime.QuantitaPiano;
                    
                    if (!existing.NumeroPiani.HasValue && excelAnime.NumeroPiani.HasValue)
                        existing.NumeroPiani = excelAnime.NumeroPiani;
                    
                    if (string.IsNullOrWhiteSpace(existing.Ciclo) && !string.IsNullOrWhiteSpace(excelAnime.Ciclo))
                        existing.Ciclo = excelAnime.Ciclo;
                    
                    if (string.IsNullOrWhiteSpace(existing.Peso) && !string.IsNullOrWhiteSpace(excelAnime.Peso))
                        existing.Peso = excelAnime.Peso;
                    
                    if (string.IsNullOrWhiteSpace(existing.Figure) && !string.IsNullOrWhiteSpace(excelAnime.Figure))
                        existing.Figure = excelAnime.Figure;
                    
                    if (string.IsNullOrWhiteSpace(existing.Maschere) && !string.IsNullOrWhiteSpace(excelAnime.Maschere))
                        existing.Maschere = excelAnime.Maschere;
                    
                    if (string.IsNullOrWhiteSpace(existing.Incollata) && !string.IsNullOrWhiteSpace(excelAnime.Incollata))
                        existing.Incollata = excelAnime.Incollata;
                    
                    if (string.IsNullOrWhiteSpace(existing.Assemblata) && !string.IsNullOrWhiteSpace(excelAnime.Assemblata))
                        existing.Assemblata = excelAnime.Assemblata;
                    
                    if (string.IsNullOrWhiteSpace(existing.ArmataL) && !string.IsNullOrWhiteSpace(excelAnime.ArmataL))
                        existing.ArmataL = excelAnime.ArmataL;
                    
                    if (string.IsNullOrWhiteSpace(existing.MacchineSuDisponibili) && !string.IsNullOrWhiteSpace(excelAnime.MacchineSuDisponibili))
                        existing.MacchineSuDisponibili = excelAnime.MacchineSuDisponibili;
                    
                    if (!existing.Altezza.HasValue && excelAnime.Altezza.HasValue)
                        existing.Altezza = excelAnime.Altezza;
                    
                    if (!existing.Larghezza.HasValue && excelAnime.Larghezza.HasValue)
                        existing.Larghezza = excelAnime.Larghezza;
                    
                    if (!existing.Profondita.HasValue && excelAnime.Profondita.HasValue)
                        existing.Profondita = excelAnime.Profondita;
                    
                    if (string.IsNullOrWhiteSpace(existing.Allegato) && !string.IsNullOrWhiteSpace(excelAnime.Allegato))
                        existing.Allegato = excelAnime.Allegato;
                    
                    if (!existing.DataModificaRecord.HasValue && excelAnime.DataModificaRecord.HasValue)
                        existing.DataModificaRecord = excelAnime.DataModificaRecord;
                    
                    if (string.IsNullOrWhiteSpace(existing.UtenteModificaRecord) && !string.IsNullOrWhiteSpace(excelAnime.UtenteModificaRecord))
                        existing.UtenteModificaRecord = excelAnime.UtenteModificaRecord;
                    
                    if (!existing.IdArticolo.HasValue && excelAnime.IdArticolo.HasValue)
                        existing.IdArticolo = excelAnime.IdArticolo;
                    
                    existing.DataImportazione = DateTime.Now;
                    updatedCount++;
                }
                else
                {
                    // Inserisci nuovo record non presente nel database
                    await _context.Anime.AddAsync(excelAnime);
                    insertedCount++;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Importazione completata: {inserted} inseriti, {updated} aggiornati", 
                insertedCount, updatedCount);
            return insertedCount + updatedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'importazione da Excel");
            throw;
        }
    }

    private static string? ParseString(ExcelWorksheet worksheet, int row, Dictionary<string, int> headers, string columnName)
    {
        if (!headers.ContainsKey(columnName))
            return null;

        var value = worksheet.Cells[row, headers[columnName]].Value?.ToString()?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static int? ParseInt32(ExcelWorksheet worksheet, int row, Dictionary<string, int> headers, string columnName)
    {
        if (!headers.ContainsKey(columnName))
            return null;

        var value = worksheet.Cells[row, headers[columnName]].Value;
        
        if (value == null)
            return null;

        if (value is int intValue)
            return intValue;

        if (value is double doubleValue)
            return (int)doubleValue;

        if (int.TryParse(value.ToString(), out var result))
            return result;

        return null;
    }

    private static DateTime? ParseDateTime(ExcelWorksheet worksheet, int row, Dictionary<string, int> headers, string columnName)
    {
        if (!headers.ContainsKey(columnName))
            return null;

        var value = worksheet.Cells[row, headers[columnName]].Value;
        
        if (value == null)
            return null;

        if (value is DateTime dateValue)
            return dateValue;

        if (DateTime.TryParse(value.ToString(), out var result))
            return result;

        return null;
    }

    private static DateTime? ParseDateTimeOrNull(ExcelWorksheet worksheet, int row, Dictionary<string, int> headers, string columnName)
    {
        return ParseDateTime(worksheet, row, headers, columnName);
    }
}
