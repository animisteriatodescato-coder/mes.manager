using MESManager.Domain.Entities;
using MESManager.Domain.Enums;

namespace MESManager.Application.DTOs;

/// <summary>
/// DTO per riga preventivo (visualizzazione)
/// </summary>
public class QuoteRowDto
{
    public Guid Id { get; set; }
    public Guid QuoteId { get; set; }
    public int SortOrder { get; set; }
    public QuoteRowType RowType { get; set; }
    public string RowTypeText => RowType == QuoteRowType.FromPriceList ? "Da Listino" : "Manuale";
    public Guid? PriceListItemId { get; set; }
    public string? Code { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal VatPercent { get; set; }
    public decimal RowTotal { get; set; }
    public decimal VatAmount { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO per salvataggio riga preventivo
/// </summary>
public class QuoteRowSaveDto
{
    public Guid? Id { get; set; }
    public int SortOrder { get; set; }
    public QuoteRowType RowType { get; set; }
    public Guid? PriceListItemId { get; set; }
    public string? Code { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal VatPercent { get; set; } = 22m;
    public string? Notes { get; set; }
}
