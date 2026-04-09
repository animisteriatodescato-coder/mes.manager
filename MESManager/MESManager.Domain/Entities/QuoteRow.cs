namespace MESManager.Domain.Entities;

public class QuoteRow
{
    public Guid Id { get; set; }
    public Guid QuoteId { get; set; }
    public int RowType { get; set; }
    public int SortOrder { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Notes { get; set; }
    public string? Unit { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal RowTotal { get; set; }
    public decimal VatPercent { get; set; }
    public decimal VatAmount { get; set; }
    public Guid? PriceListItemId { get; set; }
    public Guid? WorkProcessingTypeId { get; set; }

    public Quote Quote { get; set; } = null!;
}
