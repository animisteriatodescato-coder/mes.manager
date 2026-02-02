namespace MESManager.Application.DTOs;

/// <summary>
/// DTO per item listino prezzi
/// </summary>
public class PriceListItemDto
{
    public Guid Id { get; set; }
    public Guid PriceListId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public decimal UnitPrice { get; set; } // Prezzo unitario (rinominato da BasePrice)
    public decimal VatRate { get; set; }
    public string? Category { get; set; }
    public string? Notes { get; set; }
    public int SortOrder { get; set; }
    public bool IsDisabled { get; set; }
}

/// <summary>
/// DTO per autocomplete item listino
/// </summary>
public class PriceListItemSelectDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DisplayText => string.IsNullOrEmpty(Code) ? Description : $"[{Code}] {Description}";
    public string? Unit { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; }
    public string? Category { get; set; }
}

/// <summary>
/// Risultato import Excel
/// </summary>
public class ExcelImportResultDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? PriceListId { get; set; }
    public string? PriceListName { get; set; }
    public string? PriceListVersion { get; set; }
    public int TotalRowsRead { get; set; }
    public int ImportedCount { get; set; }
    public int RowsSkipped { get; set; }
    public List<ExcelImportErrorDto> Errors { get; set; } = new();
}

/// <summary>
/// Errore singola riga import
/// </summary>
public class ExcelImportErrorDto
{
    public string SheetName { get; set; } = string.Empty;
    public int RowNumber { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string? RawData { get; set; }
}
