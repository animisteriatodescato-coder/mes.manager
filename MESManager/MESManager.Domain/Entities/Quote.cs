namespace MESManager.Domain.Entities;

public class Quote
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? ValidUntil { get; set; }
    public int Status { get; set; }
    public Guid? ClienteId { get; set; }
    public Guid? PriceListId { get; set; }
    public string? ContactName { get; set; }
    public string? Notes { get; set; }
    public string? PaymentTerms { get; set; }
    public string? PriceListVersionSnapshot { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal VatTotal { get; set; }
    public decimal GrandTotal { get; set; }

    public Cliente? Cliente { get; set; }
    public ICollection<QuoteRow> Rows { get; set; } = new List<QuoteRow>();
}
