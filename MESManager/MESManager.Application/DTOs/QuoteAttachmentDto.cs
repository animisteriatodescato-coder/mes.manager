namespace MESManager.Application.DTOs;

/// <summary>
/// DTO per allegato preventivo
/// </summary>
public class QuoteAttachmentDto
{
    public Guid Id { get; set; }
    public Guid QuoteId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileSizeFormatted => FormatFileSize(FileSize);
    public string ContentType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime UploadedAt { get; set; }
    public string? UploadedBy { get; set; }
    
    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }
}

/// <summary>
/// DTO per upload allegato
/// </summary>
public class QuoteAttachmentUploadDto
{
    public Guid QuoteId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>
/// Risultato upload allegato
/// </summary>
public class QuoteAttachmentUploadResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public QuoteAttachmentDto? Attachment { get; set; }
}
