namespace MESManager.Domain.Entities;

/// <summary>
/// Singolo item di un listino prezzi
/// </summary>
public class PriceListItem
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Riferimento al listino padre
    /// </summary>
    public Guid PriceListId { get; set; }
    public PriceList? PriceList { get; set; }
    
    /// <summary>
    /// Codice articolo/prodotto (univoco all'interno del listino)
    /// </summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// Descrizione dell'articolo/servizio
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Unità di misura (es. pz, kg, m, h)
    /// </summary>
    public string? Unit { get; set; }
    
    /// <summary>
    /// Prezzo base unitario
    /// </summary>
    public decimal BasePrice { get; set; }
    
    /// <summary>
    /// Aliquota IVA default (es. 22)
    /// </summary>
    public decimal VatRate { get; set; } = 22m;
    
    /// <summary>
    /// Categoria merceologica (opzionale, per raggruppamento)
    /// </summary>
    public string? Category { get; set; }
    
    /// <summary>
    /// Note aggiuntive
    /// </summary>
    public string? Notes { get; set; }
    
    /// <summary>
    /// Ordine di visualizzazione
    /// </summary>
    public int SortOrder { get; set; }
    
    /// <summary>
    /// Se true, l'item non è più selezionabile
    /// </summary>
    public bool IsDisabled { get; set; }
    
    // Navigazioni
    public ICollection<QuoteRow> QuoteRows { get; set; } = new List<QuoteRow>();
}
