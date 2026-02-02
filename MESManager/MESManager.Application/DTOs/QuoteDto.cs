using MESManager.Domain.Entities;
using MESManager.Domain.Enums;

namespace MESManager.Application.DTOs;

/// <summary>
/// DTO per la lista preventivi (vista archivio)
/// </summary>
public class QuoteListDto
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public DateTime? ValidUntil { get; set; }
    public Guid? ClienteId { get; set; }
    public string? ClienteName { get; set; }
    public string? Subject { get; set; }
    public QuoteStatus Status { get; set; }
    public string StatusText => Status switch
    {
        QuoteStatus.Draft => "Bozza",
        QuoteStatus.Sent => "Inviato",
        QuoteStatus.Accepted => "Accettato",
        QuoteStatus.Rejected => "Rifiutato",
        QuoteStatus.Expired => "Scaduto",
        _ => Status.ToString()
    };
    public decimal TotalGross { get; set; }
    public int RowCount { get; set; }
    public int AttachmentCount { get; set; }
    public string? PriceListName { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}

/// <summary>
/// DTO completo per dettaglio/editing preventivo
/// </summary>
public class QuoteDto
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public DateTime? ValidUntil { get; set; }
    public Guid? ClienteId { get; set; }
    public string? ClienteName { get; set; } // Nome cliente per visualizzazione
    public string? ContactName { get; set; }
    public string? Subject { get; set; } // Oggetto del preventivo
    public string? PaymentTerms { get; set; }
    public string? Notes { get; set; }
    public QuoteStatus Status { get; set; }
    public Guid? PriceListId { get; set; }
    public string? PriceListName { get; set; }
    public string? PriceListVersion { get; set; }
    public string? PriceListVersionSnapshot { get; set; }
    
    // Totali - nomi allineati con UI
    public decimal TotalNet { get; set; }      // Imponibile
    public decimal TotalVat { get; set; }      // Totale IVA
    public decimal TotalGross { get; set; }    // Totale lordo (imponibile + IVA)
    public decimal DiscountTotal { get; set; } // Totale sconti applicati
    
    // Audit
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    
    // Righe e allegati
    public List<QuoteRowDto> Rows { get; set; } = new();
    public List<QuoteAttachmentDto> Attachments { get; set; } = new();
}

/// <summary>
/// DTO per creazione/update preventivo
/// </summary>
public class QuoteSaveDto
{
    public Guid? Id { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public DateTime? ValidUntil { get; set; }
    public Guid? ClienteId { get; set; }
    public string? ContactName { get; set; }
    public string? PaymentTerms { get; set; }
    public string? Notes { get; set; }
    public QuoteStatus Status { get; set; } = QuoteStatus.Draft;
    public Guid? PriceListId { get; set; }
    public List<QuoteRowSaveDto> Rows { get; set; } = new();
}

/// <summary>
/// DTO per cambio stato preventivo
/// </summary>
public class QuoteStatusChangeDto
{
    public Guid QuoteId { get; set; }
    public QuoteStatus NewStatus { get; set; }
    public string? Notes { get; set; }
}
