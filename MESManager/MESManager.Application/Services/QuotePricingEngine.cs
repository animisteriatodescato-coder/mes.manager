using Microsoft.Extensions.Logging;
using MESManager.Application.Interfaces;

namespace MESManager.Application.Services;

/// <summary>
/// Motore di calcolo prezzi centralizzato per preventivi.
/// TUTTE le formule di calcolo sono qui - la UI chiama solo questo servizio.
/// </summary>
public class QuotePricingEngine : IQuotePricingEngine
{
    private readonly ILogger<QuotePricingEngine> _logger;
    
    // Numero di decimali per arrotondamento
    private const int DECIMALS = 2;
    
    public QuotePricingEngine(ILogger<QuotePricingEngine> logger)
    {
        _logger = logger;
    }
    
    /// <inheritdoc />
    public decimal Round(decimal value)
    {
        // MidpointRounding.AwayFromZero: 2.5 -> 3, -2.5 -> -3
        return Math.Round(value, DECIMALS, MidpointRounding.AwayFromZero);
    }
    
    /// <inheritdoc />
    public QuoteRowCalculationResult CalculateRow(QuoteRowCalculationInput input)
    {
        var result = new QuoteRowCalculationResult
        {
            RowId = input.RowId,
            SortOrder = input.SortOrder,
            Quantity = input.Quantity,
            UnitPrice = input.UnitPrice,
            DiscountPercent = input.DiscountPercent,
            VatPercent = input.VatPercent
        };
        
        // Validazione input
        if (input.Quantity < 0)
        {
            _logger.LogWarning("Quantità negativa in calcolo riga: {Quantity}", input.Quantity);
        }
        
        if (input.UnitPrice < 0)
        {
            _logger.LogWarning("Prezzo unitario negativo in calcolo riga: {UnitPrice}", input.UnitPrice);
        }
        
        // Calcolo importo lordo: Qty * PrezzoUnitario
        result.GrossAmount = Round(input.Quantity * input.UnitPrice);
        
        // Calcolo sconto
        if (input.DiscountPercent.HasValue && input.DiscountPercent.Value > 0)
        {
            // Validazione sconto
            var discount = Math.Min(input.DiscountPercent.Value, 100m); // Max 100%
            discount = Math.Max(discount, 0m); // Min 0%
            
            result.DiscountAmount = Round(result.GrossAmount * (discount / 100m));
        }
        else
        {
            result.DiscountAmount = 0m;
        }
        
        // Totale riga netto = Lordo - Sconto
        result.RowTotal = Round(result.GrossAmount - result.DiscountAmount);
        
        // Calcolo IVA
        var vatPercent = Math.Max(0m, input.VatPercent); // IVA non può essere negativa
        result.VatAmount = Round(result.RowTotal * (vatPercent / 100m));
        
        // Totale con IVA
        result.RowTotalWithVat = Round(result.RowTotal + result.VatAmount);
        
        return result;
    }
    
    /// <inheritdoc />
    public QuoteTotalsResult CalculateTotals(IEnumerable<QuoteRowCalculationResult> rows)
    {
        var result = new QuoteTotalsResult();
        var rowList = rows.ToList();
        
        if (!rowList.Any())
        {
            return result;
        }
        
        // Somme base
        result.Subtotal = Round(rowList.Sum(r => r.GrossAmount));
        result.DiscountTotal = Round(rowList.Sum(r => r.DiscountAmount));
        result.TaxableAmount = Round(rowList.Sum(r => r.RowTotal));
        result.VatTotal = Round(rowList.Sum(r => r.VatAmount));
        result.GrandTotal = Round(result.TaxableAmount + result.VatTotal);
        
        // Riepilogo IVA per aliquota
        var vatGroups = rowList
            .GroupBy(r => r.VatPercent)
            .OrderBy(g => g.Key);
        
        foreach (var group in vatGroups)
        {
            result.VatSummary[group.Key] = new VatSummaryItem
            {
                VatPercent = group.Key,
                TaxableAmount = Round(group.Sum(r => r.RowTotal)),
                VatAmount = Round(group.Sum(r => r.VatAmount))
            };
        }
        
        return result;
    }
    
    /// <inheritdoc />
    public QuoteCalculationResult CalculateQuote(IEnumerable<QuoteRowCalculationInput> rows)
    {
        var result = new QuoteCalculationResult();
        
        var rowInputs = rows.ToList();
        
        // Calcola ogni riga
        foreach (var input in rowInputs.OrderBy(r => r.SortOrder))
        {
            var rowResult = CalculateRow(input);
            result.Rows.Add(rowResult);
        }
        
        // Calcola totali
        result.Totals = CalculateTotals(result.Rows);
        
        return result;
    }
}
