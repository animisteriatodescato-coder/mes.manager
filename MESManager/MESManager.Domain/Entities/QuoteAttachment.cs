namespace MESManager.Domain.Entities;

/// <summary>
/// Allegato associato a un preventivo
/// </summary>
public class QuoteAttachment
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Riferimento al preventivo padre
    /// </summary>
    public Guid QuoteId { get; set; }
    public Quote? Quote { get; set; }
    
    /// <summary>
    /// Nome file originale (sanitizzato)
    /// </summary>
    public string FileName { get; set; } = string.Empty;
    
    /// <summary>
    /// Percorso relativo nel filesystem (es. quotes/2026/01/guid.pdf)
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Dimensione file in bytes
    /// </summary>
    public long FileSize { get; set; }
    
    /// <summary>
    /// Content-Type del file
    /// </summary>
    public string ContentType { get; set; } = string.Empty;
    
    /// <summary>
    /// Hash SHA256 del contenuto per verifica integrità
    /// </summary>
    public string? ContentHash { get; set; }
    
    /// <summary>
    /// Descrizione opzionale dell'allegato
    /// </summary>
    public string? Description { get; set; }
    
    // Audit fields
    public DateTime UploadedAt { get; set; }
    public string? UploadedBy { get; set; }
}
