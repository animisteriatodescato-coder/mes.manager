namespace MESManager.Domain.Entities;

using MESManager.Domain.Enums;

/// <summary>
/// Riga del preventivo
/// </summary>
public class QuoteRow
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Riferimento al preventivo padre
    /// </summary>
    public Guid QuoteId { get; set; }
    public Quote? Quote { get; set; }
    
    /// <summary>
    /// Ordine di visualizzazione della riga
    /// </summary>
    public int SortOrder { get; set; }
    
    /// <summary>
    /// Tipo di riga: da listino o manuale
    /// </summary>
    public QuoteRowType RowType { get; set; } = QuoteRowType.Manual;
    
    /// <summary>
    /// Riferimento all'item del listino (se riga da listino)
    /// </summary>
    public Guid? PriceListItemId { get; set; }
    public PriceListItem? PriceListItem { get; set; }
    
    /// <summary>
    /// Codice articolo/prodotto (opzionale, copiato da listino o manuale)
    /// </summary>
    public string? Code { get; set; }
    
    /// <summary>
    /// Descrizione (copiata da listino o inserita manualmente)
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Quantità richiesta
    /// </summary>
    public decimal Quantity { get; set; }
    
    /// <summary>
    /// Unità di misura (es. pz, kg, m, ore)
    /// </summary>
    public string? Unit { get; set; }
    
    /// <summary>
    /// Prezzo unitario
    /// </summary>
    public decimal UnitPrice { get; set; }
    
    /// <summary>
    /// Sconto percentuale sulla riga (0-100)
    /// </summary>
    public decimal? DiscountPercent { get; set; }
    
    /// <summary>
    /// Aliquota IVA percentuale (default 22%)
    /// </summary>
    public decimal VatPercent { get; set; } = 22m;
    
    /// <summary>
    /// Totale riga calcolato = (Qty * UnitPrice) * (1 - Discount/100)
    /// </summary>
    public decimal RowTotal { get; set; }
    
    /// <summary>
    /// Importo IVA calcolato sulla riga
    /// </summary>
    public decimal VatAmount { get; set; }
    
    /// <summary>
    /// Note specifiche della riga
    /// </summary>
    public string? Notes { get; set; }
}
