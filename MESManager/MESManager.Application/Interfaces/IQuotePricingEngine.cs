using MESManager.Application.DTOs;
using MESManager.Domain.Entities;

namespace MESManager.Application.Interfaces;

/// <summary>
/// Motore di calcolo prezzi centralizzato per preventivi.
/// Tutte le logiche di calcolo passano da qui - la UI non deve contenere formule.
/// </summary>
public interface IQuotePricingEngine
{
    /// <summary>
    /// Calcola totali di una singola riga
    /// </summary>
    QuoteRowCalculationResult CalculateRow(QuoteRowCalculationInput input);
    
    /// <summary>
    /// Calcola tutti i totali del preventivo (subtotale, sconto, iva, totale)
    /// </summary>
    QuoteTotalsResult CalculateTotals(IEnumerable<QuoteRowCalculationResult> rows);
    
    /// <summary>
    /// Calcola riga e totali in un unico passaggio (per UI)
    /// </summary>
    QuoteCalculationResult CalculateQuote(IEnumerable<QuoteRowCalculationInput> rows);
    
    /// <summary>
    /// Applica arrotondamento standard (2 decimali, round half away from zero)
    /// </summary>
    decimal Round(decimal value);
}

/// <summary>
/// Input per calcolo singola riga
/// </summary>
public class QuoteRowCalculationInput
{
    public Guid? RowId { get; set; }
    public int SortOrder { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal VatPercent { get; set; } = 22m;
}

/// <summary>
/// Risultato calcolo singola riga
/// </summary>
public class QuoteRowCalculationResult
{
    public Guid? RowId { get; set; }
    public int SortOrder { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal VatPercent { get; set; }
    
    /// <summary>
    /// Importo lordo riga = Qty * UnitPrice
    /// </summary>
    public decimal GrossAmount { get; set; }
    
    /// <summary>
    /// Importo sconto = GrossAmount * (DiscountPercent / 100)
    /// </summary>
    public decimal DiscountAmount { get; set; }
    
    /// <summary>
    /// Totale riga netto = GrossAmount - DiscountAmount
    /// </summary>
    public decimal RowTotal { get; set; }
    
    /// <summary>
    /// Importo IVA = RowTotal * (VatPercent / 100)
    /// </summary>
    public decimal VatAmount { get; set; }
    
    /// <summary>
    /// Totale riga con IVA
    /// </summary>
    public decimal RowTotalWithVat { get; set; }
}

/// <summary>
/// Totali documento
/// </summary>
public class QuoteTotalsResult
{
    /// <summary>
    /// Somma importi lordi righe
    /// </summary>
    public decimal Subtotal { get; set; }
    
    /// <summary>
    /// Somma sconti riga
    /// </summary>
    public decimal DiscountTotal { get; set; }
    
    /// <summary>
    /// Imponibile = Subtotal - DiscountTotal
    /// </summary>
    public decimal TaxableAmount { get; set; }
    
    /// <summary>
    /// Totale IVA
    /// </summary>
    public decimal VatTotal { get; set; }
    
    /// <summary>
    /// Totale finale = TaxableAmount + VatTotal
    /// </summary>
    public decimal GrandTotal { get; set; }
    
    /// <summary>
    /// Riepilogo IVA per aliquota
    /// </summary>
    public Dictionary<decimal, VatSummaryItem> VatSummary { get; set; } = new();
}

/// <summary>
/// Dettaglio IVA per aliquota
/// </summary>
public class VatSummaryItem
{
    public decimal VatPercent { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal VatAmount { get; set; }
}

/// <summary>
/// Risultato calcolo completo preventivo
/// </summary>
public class QuoteCalculationResult
{
    public List<QuoteRowCalculationResult> Rows { get; set; } = new();
    public QuoteTotalsResult Totals { get; set; } = new();
}
