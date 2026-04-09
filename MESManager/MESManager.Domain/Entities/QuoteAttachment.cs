namespace MESManager.Domain.Entities;

public class QuoteAttachment
{
    public Guid Id { get; set; }
    public Guid QuoteId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string RelativePath { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ContentHash { get; set; }
    public string? UploadedBy { get; set; }

    public Quote Quote { get; set; } = null!;
}
