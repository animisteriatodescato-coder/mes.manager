namespace MESManager.Domain.Entities;

public class PriceListItem
{
    public Guid Id { get; set; }
    public Guid PriceListId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public decimal VatRate { get; set; }
    public string? Unit { get; set; }
    public string? Category { get; set; }
    public string? Notes { get; set; }
    public int SortOrder { get; set; }
    public bool IsDisabled { get; set; }

    public PriceList PriceList { get; set; } = null!;
}
