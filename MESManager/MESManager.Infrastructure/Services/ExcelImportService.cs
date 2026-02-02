using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;
using System.Globalization;

namespace MESManager.Infrastructure.Services;

/// <summary>
/// Servizio per import listini prezzi da file Excel.
/// Utilizza EPPlus per la lettura dei file .xlsx
/// </summary>
public class ExcelImportService : IExcelImportService
{
    private readonly MesManagerDbContext _context;
    private readonly ILogger<ExcelImportService> _logger;

    // Mapping colonne configurabile (inizialmente hardcoded per TABELLA PREZZI MODIFICATA.xlsx)
    // Il file ha struttura particolare: non è un listino standard ma un calcolatore costi
    // Questo mapping è configurato per un formato listino standard
    private static readonly Dictionary<string, string[]> ColumnMappings = new()
    {
        { "Code", new[] { "CODICE", "CODE", "COD", "SKU", "ARTICOLO" } },
        { "Description", new[] { "DESCRIZIONE", "DESCRIPTION", "DESC", "NOME", "PRODOTTO" } },
        { "Unit", new[] { "UM", "UNITA", "UNIT", "UDM" } },
        { "BasePrice", new[] { "PREZZO", "PRICE", "COSTO", "EURO", "€", "IMPORTO", "PR. VENDITA" } },
        { "VatRate", new[] { "IVA", "VAT", "ALIQUOTA" } },
        { "Category", new[] { "CATEGORIA", "CATEGORY", "GRUPPO", "FAMIGLIA" } }
    };

    public ExcelImportService(MesManagerDbContext context, ILogger<ExcelImportService> logger)
    {
        _context = context;
        _logger = logger;
        
        // EPPlus license (v7 richiede licenza, ma ha versione community)
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public async Task<ExcelImportResultDto> ImportPriceListAsync(
        Stream fileStream,
        string fileName,
        string? priceListName = null,
        string? version = null,
        string? userId = null)
    {
        var result = new ExcelImportResultDto();
        
        try
        {
            _logger.LogInformation("Inizio import listino da file: {FileName}", fileName);
            
            using var package = new ExcelPackage(fileStream);
            
            if (package.Workbook.Worksheets.Count == 0)
            {
                result.ErrorMessage = "Il file Excel non contiene fogli di lavoro.";
                return result;
            }
            
            // Nome listino (da parametro o nome file)
            var name = priceListName ?? Path.GetFileNameWithoutExtension(fileName);
            
            // Versione (da parametro o timestamp)
            var ver = version ?? DateTime.Now.ToString("yyyyMMdd.HHmm");
            
            // Verifica se esiste già un listino con stesso nome e versione
            var exists = await _context.PriceLists.AnyAsync(pl => pl.Name == name && pl.Version == ver);
            if (exists)
            {
                // Incrementa versione
                ver = $"{ver}.{DateTime.Now.Second:D2}";
            }
            
            // Crea il listino
            var priceList = new PriceList
            {
                Id = Guid.NewGuid(),
                Name = name,
                Version = ver,
                Description = $"Importato da {fileName}",
                Source = fileName,
                ValidFrom = DateTime.Today,
                IsDefault = false,
                IsArchived = false,
                CreatedAt = DateTime.Now,
                CreatedBy = userId
            };
            
            _context.PriceLists.Add(priceList);
            
            var items = new List<PriceListItem>();
            int sortOrder = 0;
            
            // Processa ogni foglio
            foreach (var worksheet in package.Workbook.Worksheets)
            {
                _logger.LogInformation("Processo foglio: {SheetName} ({Rows} righe, {Cols} colonne)",
                    worksheet.Name, worksheet.Dimension?.Rows ?? 0, worksheet.Dimension?.Columns ?? 0);
                
                if (worksheet.Dimension == null)
                {
                    _logger.LogWarning("Foglio {SheetName} vuoto, skip", worksheet.Name);
                    continue;
                }
                
                // Trova mapping colonne
                var columnMap = FindColumnMapping(worksheet);
                
                if (!columnMap.ContainsKey("Description") && !columnMap.ContainsKey("BasePrice"))
                {
                    // Prova import come foglio calcolatore (TABELLA PREZZI MODIFICATA)
                    var calculatorItems = ImportAsCalculatorSheet(worksheet, priceList.Id, ref sortOrder);
                    items.AddRange(calculatorItems);
                    result.TotalRowsRead += worksheet.Dimension.Rows - 1;
                    continue;
                }
                
                // Import standard con colonne mappate
                for (int row = 2; row <= worksheet.Dimension.Rows; row++)
                {
                    result.TotalRowsRead++;
                    
                    try
                    {
                        var item = ParseRow(worksheet, row, columnMap, priceList.Id, ref sortOrder);
                        
                        if (item != null)
                        {
                            items.Add(item);
                        }
                        else
                        {
                            result.RowsSkipped++;
                        }
                    }
                    catch (Exception ex)
                    {
                        result.RowsSkipped++;
                        result.Errors.Add(new ExcelImportErrorDto
                        {
                            SheetName = worksheet.Name,
                            RowNumber = row,
                            ErrorMessage = ex.Message,
                            RawData = GetRowAsString(worksheet, row)
                        });
                        
                        _logger.LogWarning("Errore parsing riga {Row} foglio {Sheet}: {Error}",
                            row, worksheet.Name, ex.Message);
                    }
                }
            }
            
            // Salva items
            if (items.Any())
            {
                _context.PriceListItems.AddRange(items);
                priceList.ItemCount = items.Count;
            }
            
            await _context.SaveChangesAsync();
            
            result.Success = true;
            result.PriceListId = priceList.Id;
            result.PriceListName = priceList.Name;
            result.PriceListVersion = priceList.Version;
            result.ImportedCount = items.Count;
            
            _logger.LogInformation("Import completato: {Imported}/{Total} righe, {Skipped} scartate, {Errors} errori",
                result.ImportedCount, result.TotalRowsRead, result.RowsSkipped, result.Errors.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante import listino da {FileName}", fileName);
            result.ErrorMessage = $"Errore durante l'import: {ex.Message}";
        }
        
        return result;
    }

    public async Task<ExcelImportResultDto> ImportPriceListFromFileAsync(
        string filePath,
        string? priceListName = null,
        string? version = null,
        string? userId = null)
    {
        if (!File.Exists(filePath))
        {
            return new ExcelImportResultDto
            {
                ErrorMessage = $"File non trovato: {filePath}"
            };
        }
        
        using var stream = File.OpenRead(filePath);
        return await ImportPriceListAsync(stream, Path.GetFileName(filePath), priceListName, version, userId);
    }

    public async Task<ExcelValidationResultDto> ValidateFileAsync(Stream fileStream, string fileName)
    {
        var result = new ExcelValidationResultDto();
        
        try
        {
            using var package = new ExcelPackage(fileStream);
            
            if (package.Workbook.Worksheets.Count == 0)
            {
                result.Errors.Add("Il file non contiene fogli di lavoro.");
                return result;
            }
            
            foreach (var worksheet in package.Workbook.Worksheets)
            {
                var sheet = new ExcelSheetPreviewDto
                {
                    Name = worksheet.Name,
                    RowCount = worksheet.Dimension?.Rows ?? 0,
                    ColumnCount = worksheet.Dimension?.Columns ?? 0
                };
                
                if (worksheet.Dimension != null)
                {
                    // Leggi intestazioni
                    for (int col = 1; col <= Math.Min(worksheet.Dimension.Columns, 20); col++)
                    {
                        var header = worksheet.Cells[1, col].Text?.Trim();
                        if (!string.IsNullOrEmpty(header))
                        {
                            sheet.Headers.Add(header);
                        }
                    }
                    
                    // Leggi prime 5 righe come sample
                    for (int row = 2; row <= Math.Min(worksheet.Dimension.Rows, 6); row++)
                    {
                        var rowData = new Dictionary<string, string>();
                        for (int col = 1; col <= Math.Min(worksheet.Dimension.Columns, 10); col++)
                        {
                            var header = col <= sheet.Headers.Count ? sheet.Headers[col - 1] : $"Col{col}";
                            rowData[header] = worksheet.Cells[row, col].Text ?? "";
                        }
                        sheet.SampleRows.Add(rowData);
                    }
                    
                    result.EstimatedItemCount += Math.Max(0, sheet.RowCount - 1);
                }
                
                result.Sheets.Add(sheet);
            }
            
            result.IsValid = true;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Errore lettura file: {ex.Message}");
        }
        
        return result;
    }

    /// <summary>
    /// Trova il mapping delle colonne basandosi sulla prima riga
    /// </summary>
    private Dictionary<string, int> FindColumnMapping(ExcelWorksheet worksheet)
    {
        var mapping = new Dictionary<string, int>();
        
        if (worksheet.Dimension == null) return mapping;
        
        for (int col = 1; col <= worksheet.Dimension.Columns; col++)
        {
            var header = worksheet.Cells[1, col].Text?.Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(header)) continue;
            
            foreach (var (fieldName, aliases) in ColumnMappings)
            {
                if (aliases.Any(a => header.Contains(a, StringComparison.OrdinalIgnoreCase)))
                {
                    if (!mapping.ContainsKey(fieldName))
                    {
                        mapping[fieldName] = col;
                    }
                    break;
                }
            }
        }
        
        return mapping;
    }

    /// <summary>
    /// Parsa una riga del foglio Excel
    /// </summary>
    private PriceListItem? ParseRow(ExcelWorksheet ws, int row, Dictionary<string, int> columnMap, Guid priceListId, ref int sortOrder)
    {
        // Descrizione è obbligatoria
        string description = "";
        if (columnMap.TryGetValue("Description", out int descCol))
        {
            description = ws.Cells[row, descCol].Text?.Trim() ?? "";
        }
        
        if (string.IsNullOrWhiteSpace(description))
        {
            return null; // Riga vuota o senza descrizione
        }
        
        sortOrder++;
        
        var item = new PriceListItem
        {
            Id = Guid.NewGuid(),
            PriceListId = priceListId,
            Description = description,
            SortOrder = sortOrder
        };
        
        // Codice (opzionale)
        if (columnMap.TryGetValue("Code", out int codeCol))
        {
            item.Code = ws.Cells[row, codeCol].Text?.Trim() ?? "";
        }
        if (string.IsNullOrEmpty(item.Code))
        {
            // Genera codice automatico
            item.Code = $"ITEM-{sortOrder:D4}";
        }
        
        // Unità di misura
        if (columnMap.TryGetValue("Unit", out int unitCol))
        {
            item.Unit = ws.Cells[row, unitCol].Text?.Trim();
        }
        item.Unit ??= "pz";
        
        // Prezzo base
        if (columnMap.TryGetValue("BasePrice", out int priceCol))
        {
            item.BasePrice = ParseDecimal(ws.Cells[row, priceCol]);
        }
        
        // Aliquota IVA
        if (columnMap.TryGetValue("VatRate", out int vatCol))
        {
            item.VatRate = ParseDecimal(ws.Cells[row, vatCol]);
        }
        else
        {
            item.VatRate = 22m; // Default Italia
        }
        
        // Categoria
        if (columnMap.TryGetValue("Category", out int catCol))
        {
            item.Category = ws.Cells[row, catCol].Text?.Trim();
        }
        
        return item;
    }

    /// <summary>
    /// Import speciale per fogli tipo "TABELLA PREZZI MODIFICATA" che sono calcolatori, non listini.
    /// Estrae i parametri chiave come items del listino.
    /// </summary>
    private List<PriceListItem> ImportAsCalculatorSheet(ExcelWorksheet ws, Guid priceListId, ref int sortOrder)
    {
        var items = new List<PriceListItem>();
        
        // Il foglio "TABELLA PREZZI MODIFICATA" ha una struttura particolare:
        // È un calcolatore costi con variabili in posizioni fisse
        // Estraiamo il "PR. VENDITA" calcolato come item principale
        
        string sheetName = ws.Name;
        
        // Cerca la cella "PR. VENDITA" o il risultato principale
        // Tipicamente in colonna F o G, riga 2
        decimal? priceValue = null;
        
        // Prova colonna G2 (DISTRIBUT)
        var g2 = ws.Cells[2, 7];
        if (g2.Value != null)
        {
            priceValue = ParseDecimalFromFormulaCell(ws, 2, 7);
        }
        
        // Prova colonna F2 (CERABEADS, SABBIA NORM)
        if (!priceValue.HasValue || priceValue == 0)
        {
            var f2 = ws.Cells[2, 6];
            if (f2.Value != null)
            {
                priceValue = ParseDecimalFromFormulaCell(ws, 2, 6);
            }
        }
        
        // Estrai parametri chiave come descrizione
        var peso = ParseDecimalFromCell(ws, 1, 2);   // B1 = PESO
        var figure = ParseDecimalFromCell(ws, 2, 2); // B2 = FIGURE
        var lotto = ParseDecimalFromCell(ws, 3, 2);  // B3 = LOTTO
        
        sortOrder++;
        var item = new PriceListItem
        {
            Id = Guid.NewGuid(),
            PriceListId = priceListId,
            Code = sheetName.ToUpperInvariant().Replace(" ", "-"),
            Description = $"{sheetName} - Peso {peso}kg, {figure} figura/e, Lotto {lotto}",
            Unit = "pz",
            BasePrice = priceValue ?? 0m,
            VatRate = 22m,
            Category = "Anime",
            SortOrder = sortOrder,
            Notes = $"Importato da calcolatore Excel. Peso={peso}, Figure={figure}, Lotto={lotto}"
        };
        
        items.Add(item);
        
        _logger.LogInformation("Estratto item da foglio calcolatore {Sheet}: {Code} @ {Price:C2}",
            sheetName, item.Code, item.BasePrice);
        
        return items;
    }

    /// <summary>
    /// Parsa un valore decimale da una cella Excel
    /// </summary>
    private decimal ParseDecimal(ExcelRange cell)
    {
        if (cell.Value == null) return 0m;
        
        // Se è già numerico
        if (cell.Value is double d) return (decimal)d;
        if (cell.Value is decimal dec) return dec;
        if (cell.Value is int i) return i;
        if (cell.Value is float f) return (decimal)f;
        
        // Prova parsing da stringa
        var text = cell.Text?.Trim();
        if (string.IsNullOrEmpty(text)) return 0m;
        
        // Rimuovi simboli valuta e spazi
        text = text.Replace("€", "").Replace("$", "").Replace(" ", "").Trim();
        
        // Gestisci separatore decimale italiano/inglese
        // Priorità: se c'è una virgola seguita da 1-2 cifre, è decimale italiano
        if (text.Contains(',') && !text.Contains('.'))
        {
            text = text.Replace(',', '.');
        }
        else if (text.Contains(',') && text.Contains('.'))
        {
            // Formato 1.234,56 (italiano)
            text = text.Replace(".", "").Replace(',', '.');
        }
        
        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }
        
        return 0m;
    }

    private decimal ParseDecimalFromCell(ExcelWorksheet ws, int row, int col)
    {
        return ParseDecimal(ws.Cells[row, col]);
    }

    private decimal ParseDecimalFromFormulaCell(ExcelWorksheet ws, int row, int col)
    {
        var cell = ws.Cells[row, col];
        
        // Se la cella ha una formula, EPPlus calcola automaticamente il risultato
        // se il file è stato salvato con valori cached
        if (cell.Value is double d) return (decimal)d;
        if (cell.Value is decimal dec) return dec;
        
        // Altrimenti prova a parsare il testo
        return ParseDecimal(cell);
    }

    private string GetRowAsString(ExcelWorksheet ws, int row)
    {
        var values = new List<string>();
        for (int col = 1; col <= Math.Min(ws.Dimension?.Columns ?? 10, 10); col++)
        {
            values.Add(ws.Cells[row, col].Text ?? "");
        }
        return string.Join(" | ", values);
    }
}
