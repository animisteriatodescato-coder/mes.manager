namespace MESManager.Domain.Entities;

/// <summary>
/// Listino prezzi versionato
/// Ogni import crea una nuova versione, preservando lo storico
/// </summary>
public class PriceList
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Nome identificativo del listino (es. "Listino Standard", "Listino Clienti Speciali")
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Versione del listino (es. "2026.01", "v1.0", "20260202")
    /// </summary>
    public string Version { get; set; } = string.Empty;
    
    /// <summary>
    /// Descrizione opzionale
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Data di inizio validità
    /// </summary>
    public DateTime? ValidFrom { get; set; }
    
    /// <summary>
    /// Data di fine validità (null = sempre valido)
    /// </summary>
    public DateTime? ValidTo { get; set; }
    
    /// <summary>
    /// Sorgente del listino (es. nome file Excel, "Manual")
    /// </summary>
    public string? Source { get; set; }
    
    /// <summary>
    /// Se true, è il listino attivo di default per nuovi preventivi
    /// </summary>
    public bool IsDefault { get; set; }
    
    /// <summary>
    /// Se true, il listino non è più utilizzabile per nuovi preventivi
    /// </summary>
    public bool IsArchived { get; set; }
    
    /// <summary>
    /// Numero totale di item importati/presenti
    /// </summary>
    public int ItemCount { get; set; }
    
    // Audit fields
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    
    // Navigazioni
    public ICollection<PriceListItem> Items { get; set; } = new List<PriceListItem>();
    public ICollection<Quote> Quotes { get; set; } = new List<Quote>();
}
