namespace MESManager.Domain.Entities;

public class PriceList
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Source { get; set; }
    public bool IsDefault { get; set; }
    public bool IsArchived { get; set; }
    public int ItemCount { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }

    public ICollection<PriceListItem> Items { get; set; } = new List<PriceListItem>();
}
