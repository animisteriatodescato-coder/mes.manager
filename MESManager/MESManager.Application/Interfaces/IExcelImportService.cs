using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces;

/// <summary>
/// Servizio per import listini da file Excel
/// </summary>
public interface IExcelImportService
{
    /// <summary>
    /// Importa listino da file Excel (stream)
    /// </summary>
    /// <param name="fileStream">Stream del file Excel</param>
    /// <param name="fileName">Nome file originale</param>
    /// <param name="priceListName">Nome listino (opzionale, default da file)</param>
    /// <param name="version">Versione da assegnare (opzionale, auto-generata)</param>
    /// <param name="userId">Utente che esegue l'import</param>
    Task<ExcelImportResultDto> ImportPriceListAsync(
        Stream fileStream, 
        string fileName,
        string? priceListName = null,
        string? version = null,
        string? userId = null);
    
    /// <summary>
    /// Importa listino da file su filesystem
    /// </summary>
    Task<ExcelImportResultDto> ImportPriceListFromFileAsync(
        string filePath,
        string? priceListName = null,
        string? version = null,
        string? userId = null);
    
    /// <summary>
    /// Valida file Excel senza importare (preview)
    /// </summary>
    Task<ExcelValidationResultDto> ValidateFileAsync(Stream fileStream, string fileName);
}

/// <summary>
/// Risultato validazione file Excel
/// </summary>
public class ExcelValidationResultDto
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<ExcelSheetPreviewDto> Sheets { get; set; } = new();
    public int EstimatedItemCount { get; set; }
}

/// <summary>
/// Preview foglio Excel
/// </summary>
public class ExcelSheetPreviewDto
{
    public string Name { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public int ColumnCount { get; set; }
    public List<string> Headers { get; set; } = new();
    public List<Dictionary<string, string>> SampleRows { get; set; } = new();
}
