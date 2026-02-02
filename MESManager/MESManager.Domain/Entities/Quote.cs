namespace MESManager.Domain.Entities;

using MESManager.Domain.Enums;

/// <summary>
/// Entità Preventivo - Testata documento
/// </summary>
public class Quote
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Numero preventivo progressivo (es. PRV-2026-0001)
    /// </summary>
    public string Number { get; set; } = string.Empty;
    
    /// <summary>
    /// Data creazione preventivo
    /// </summary>
    public DateTime Date { get; set; }
    
    /// <summary>
    /// Data di validità del preventivo
    /// </summary>
    public DateTime? ValidUntil { get; set; }
    
    /// <summary>
    /// Riferimento al cliente
    /// </summary>
    public Guid? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    
    /// <summary>
    /// Nome referente cliente (opzionale)
    /// </summary>
    public string? ContactName { get; set; }
    
    /// <summary>
    /// Condizioni di pagamento
    /// </summary>
    public string? PaymentTerms { get; set; }
    
    /// <summary>
    /// Note libere sul preventivo
    /// </summary>
    public string? Notes { get; set; }
    
    /// <summary>
    /// Stato del preventivo
    /// </summary>
    public QuoteStatus Status { get; set; } = QuoteStatus.Draft;
    
    /// <summary>
    /// Riferimento al listino usato (per congelare la versione)
    /// </summary>
    public Guid? PriceListId { get; set; }
    public PriceList? PriceList { get; set; }
    
    /// <summary>
    /// Versione listino congelata al momento della creazione
    /// </summary>
    public string? PriceListVersionSnapshot { get; set; }
    
    // Totali calcolati (salvati per performance e storicità)
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal VatTotal { get; set; }
    public decimal GrandTotal { get; set; }
    
    // Audit fields
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    
    // Navigazioni
    public ICollection<QuoteRow> Rows { get; set; } = new List<QuoteRow>();
    public ICollection<QuoteAttachment> Attachments { get; set; } = new List<QuoteAttachment>();
}
