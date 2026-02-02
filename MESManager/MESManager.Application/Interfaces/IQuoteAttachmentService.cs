using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces;

/// <summary>
/// Servizio per gestione allegati preventivo
/// </summary>
public interface IQuoteAttachmentService
{
    /// <summary>
    /// Ottiene allegati di un preventivo
    /// </summary>
    Task<List<QuoteAttachmentDto>> GetByQuoteIdAsync(Guid quoteId);
    
    /// <summary>
    /// Carica nuovo allegato
    /// </summary>
    Task<QuoteAttachmentUploadResult> UploadAsync(
        Guid quoteId,
        Stream fileStream,
        string fileName,
        string contentType,
        string? description = null,
        string? userId = null);
    
    /// <summary>
    /// Ottiene stream per download allegato
    /// </summary>
    Task<(Stream? stream, string? contentType, string? fileName)> DownloadAsync(Guid attachmentId);
    
    /// <summary>
    /// Elimina allegato
    /// </summary>
    Task<bool> DeleteAsync(Guid attachmentId);
    
    /// <summary>
    /// Verifica estensione file consentita
    /// </summary>
    bool IsAllowedExtension(string fileName);
    
    /// <summary>
    /// Verifica dimensione file consentita
    /// </summary>
    bool IsAllowedSize(long fileSize);
}
