using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;
using MESManager.Domain.Enums;
using MESManager.Infrastructure.Data;

namespace MESManager.Infrastructure.Services;

/// <summary>
/// Servizio per gestione preventivi (CRUD, duplicazione, cambio stato)
/// </summary>
public class QuoteService : IQuoteService
{
    private readonly MesManagerDbContext _context;
    private readonly IQuotePricingEngine _pricingEngine;
    private readonly ILogger<QuoteService> _logger;

    public QuoteService(
        MesManagerDbContext context,
        IQuotePricingEngine pricingEngine,
        ILogger<QuoteService> logger)
    {
        _context = context;
        _pricingEngine = pricingEngine;
        _logger = logger;
    }

    public async Task<QuoteListResult> GetListAsync(QuoteListFilter filter)
    {
        var query = _context.Quotes
            .Include(q => q.Cliente)
            .Include(q => q.PriceList)
            .AsQueryable();
        
        // Filtri
        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var search = filter.SearchText.Trim().ToLower();
            query = query.Where(q =>
                q.Number.ToLower().Contains(search) ||
                (q.Cliente != null && q.Cliente.RagioneSociale.ToLower().Contains(search)) ||
                (q.Notes != null && q.Notes.ToLower().Contains(search)));
        }
        
        if (filter.ClienteId.HasValue)
        {
            query = query.Where(q => q.ClienteId == filter.ClienteId.Value);
        }
        
        if (filter.Status.HasValue)
        {
            query = query.Where(q => q.Status == filter.Status.Value);
        }
        
        if (filter.DateFrom.HasValue)
        {
            query = query.Where(q => q.Date >= filter.DateFrom.Value.Date);
        }
        
        if (filter.DateTo.HasValue)
        {
            query = query.Where(q => q.Date <= filter.DateTo.Value.Date);
        }
        
        if (filter.TotalMin.HasValue)
        {
            query = query.Where(q => q.GrandTotal >= filter.TotalMin.Value);
        }
        
        if (filter.TotalMax.HasValue)
        {
            query = query.Where(q => q.GrandTotal <= filter.TotalMax.Value);
        }
        
        // Count totale
        var totalCount = await query.CountAsync();
        
        // Ordinamento
        query = filter.SortBy?.ToLower() switch
        {
            "number" => filter.SortDescending ? query.OrderByDescending(q => q.Number) : query.OrderBy(q => q.Number),
            "date" => filter.SortDescending ? query.OrderByDescending(q => q.Date) : query.OrderBy(q => q.Date),
            "cliente" => filter.SortDescending 
                ? query.OrderByDescending(q => q.Cliente != null ? q.Cliente.RagioneSociale : "") 
                : query.OrderBy(q => q.Cliente != null ? q.Cliente.RagioneSociale : ""),
            "total" => filter.SortDescending ? query.OrderByDescending(q => q.GrandTotal) : query.OrderBy(q => q.GrandTotal),
            "status" => filter.SortDescending ? query.OrderByDescending(q => q.Status) : query.OrderBy(q => q.Status),
            _ => query.OrderByDescending(q => q.Date).ThenByDescending(q => q.Number)
        };
        
        // Paginazione
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(q => new QuoteListDto
            {
                Id = q.Id,
                Number = q.Number,
                Date = q.Date,
                ValidUntil = q.ValidUntil,
                ClienteId = q.ClienteId,
                ClienteName = q.Cliente != null ? q.Cliente.RagioneSociale : null,
                Status = q.Status,
                TotalGross = q.GrandTotal,
                RowCount = q.Rows.Count,
                AttachmentCount = q.Attachments.Count,
                PriceListName = q.PriceList != null ? q.PriceList.Name : null,
                CreatedAt = q.CreatedAt,
                CreatedBy = q.CreatedBy
            })
            .ToListAsync();
        
        return new QuoteListResult
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<QuoteDto?> GetByIdAsync(Guid id)
    {
        var quote = await _context.Quotes
            .Include(q => q.Cliente)
            .Include(q => q.PriceList)
            .Include(q => q.Rows.OrderBy(r => r.SortOrder))
            .Include(q => q.Attachments.OrderBy(a => a.UploadedAt))
            .FirstOrDefaultAsync(q => q.Id == id);
        
        if (quote == null) return null;
        
        return MapToDto(quote);
    }

    public async Task<QuoteDto> CreateAsync(QuoteSaveDto dto, string? userId = null)
    {
        // Genera numero preventivo
        var number = await GenerateNextNumberAsync();
        
        // Prepara righe per calcolo
        var rowInputs = dto.Rows.Select(r => new QuoteRowCalculationInput
        {
            SortOrder = r.SortOrder,
            Quantity = r.Quantity,
            UnitPrice = r.UnitPrice,
            DiscountPercent = r.DiscountPercent,
            VatPercent = r.VatPercent
        }).ToList();
        
        // Calcola totali
        var calculation = _pricingEngine.CalculateQuote(rowInputs);
        
        // Congela versione listino
        string? priceListVersionSnapshot = null;
        if (dto.PriceListId.HasValue)
        {
            var priceList = await _context.PriceLists.FindAsync(dto.PriceListId.Value);
            priceListVersionSnapshot = priceList?.Version;
        }
        
        var quote = new Quote
        {
            Id = Guid.NewGuid(),
            Number = number,
            Date = dto.Date,
            ValidUntil = dto.ValidUntil,
            ClienteId = dto.ClienteId,
            ContactName = dto.ContactName,
            PaymentTerms = dto.PaymentTerms,
            Notes = dto.Notes,
            Status = dto.Status,
            PriceListId = dto.PriceListId,
            PriceListVersionSnapshot = priceListVersionSnapshot,
            Subtotal = calculation.Totals.Subtotal,
            DiscountTotal = calculation.Totals.DiscountTotal,
            TaxableAmount = calculation.Totals.TaxableAmount,
            VatTotal = calculation.Totals.VatTotal,
            GrandTotal = calculation.Totals.GrandTotal,
            CreatedAt = DateTime.Now,
            CreatedBy = userId
        };
        
        // Crea righe
        int sortOrder = 0;
        foreach (var rowDto in dto.Rows.OrderBy(r => r.SortOrder))
        {
            sortOrder++;
            var rowCalc = calculation.Rows.FirstOrDefault(r => r.SortOrder == rowDto.SortOrder) 
                ?? _pricingEngine.CalculateRow(new QuoteRowCalculationInput
                {
                    SortOrder = sortOrder,
                    Quantity = rowDto.Quantity,
                    UnitPrice = rowDto.UnitPrice,
                    DiscountPercent = rowDto.DiscountPercent,
                    VatPercent = rowDto.VatPercent
                });
            
            quote.Rows.Add(new QuoteRow
            {
                Id = Guid.NewGuid(),
                SortOrder = sortOrder,
                RowType = rowDto.RowType,
                PriceListItemId = rowDto.PriceListItemId,
                Code = rowDto.Code,
                Description = rowDto.Description,
                Quantity = rowDto.Quantity,
                Unit = rowDto.Unit,
                UnitPrice = rowDto.UnitPrice,
                DiscountPercent = rowDto.DiscountPercent,
                VatPercent = rowDto.VatPercent,
                RowTotal = rowCalc.RowTotal,
                VatAmount = rowCalc.VatAmount,
                Notes = rowDto.Notes
            });
        }
        
        _context.Quotes.Add(quote);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Creato preventivo {Number} per cliente {ClienteId}", number, dto.ClienteId);
        
        return (await GetByIdAsync(quote.Id))!;
    }

    public async Task<QuoteDto> UpdateAsync(QuoteSaveDto dto, string? userId = null)
    {
        if (!dto.Id.HasValue)
        {
            throw new ArgumentException("ID preventivo non specificato");
        }
        
        var quote = await _context.Quotes
            .Include(q => q.Rows)
            .FirstOrDefaultAsync(q => q.Id == dto.Id.Value);
        
        if (quote == null)
        {
            throw new InvalidOperationException($"Preventivo {dto.Id} non trovato");
        }
        
        // Verifica stato modificabile
        if (quote.Status != QuoteStatus.Draft)
        {
            throw new InvalidOperationException("Solo i preventivi in bozza possono essere modificati");
        }
        
        // Prepara righe per calcolo
        var rowInputs = dto.Rows.Select(r => new QuoteRowCalculationInput
        {
            SortOrder = r.SortOrder,
            Quantity = r.Quantity,
            UnitPrice = r.UnitPrice,
            DiscountPercent = r.DiscountPercent,
            VatPercent = r.VatPercent
        }).ToList();
        
        var calculation = _pricingEngine.CalculateQuote(rowInputs);
        
        // Aggiorna testata
        quote.Date = dto.Date;
        quote.ValidUntil = dto.ValidUntil;
        quote.ClienteId = dto.ClienteId;
        quote.ContactName = dto.ContactName;
        quote.PaymentTerms = dto.PaymentTerms;
        quote.Notes = dto.Notes;
        quote.PriceListId = dto.PriceListId;
        quote.Subtotal = calculation.Totals.Subtotal;
        quote.DiscountTotal = calculation.Totals.DiscountTotal;
        quote.TaxableAmount = calculation.Totals.TaxableAmount;
        quote.VatTotal = calculation.Totals.VatTotal;
        quote.GrandTotal = calculation.Totals.GrandTotal;
        quote.UpdatedAt = DateTime.Now;
        quote.UpdatedBy = userId;
        
        // Aggiorna versione listino se cambiato
        if (dto.PriceListId.HasValue && dto.PriceListId != quote.PriceListId)
        {
            var priceList = await _context.PriceLists.FindAsync(dto.PriceListId.Value);
            quote.PriceListVersionSnapshot = priceList?.Version;
        }
        
        // Rimuovi righe esistenti e ricrea
        _context.QuoteRows.RemoveRange(quote.Rows);
        quote.Rows.Clear();
        
        int sortOrder = 0;
        foreach (var rowDto in dto.Rows.OrderBy(r => r.SortOrder))
        {
            sortOrder++;
            var rowCalc = calculation.Rows.FirstOrDefault(r => r.SortOrder == rowDto.SortOrder)
                ?? _pricingEngine.CalculateRow(new QuoteRowCalculationInput
                {
                    SortOrder = sortOrder,
                    Quantity = rowDto.Quantity,
                    UnitPrice = rowDto.UnitPrice,
                    DiscountPercent = rowDto.DiscountPercent,
                    VatPercent = rowDto.VatPercent
                });
            
            quote.Rows.Add(new QuoteRow
            {
                Id = Guid.NewGuid(),
                SortOrder = sortOrder,
                RowType = rowDto.RowType,
                PriceListItemId = rowDto.PriceListItemId,
                Code = rowDto.Code,
                Description = rowDto.Description,
                Quantity = rowDto.Quantity,
                Unit = rowDto.Unit,
                UnitPrice = rowDto.UnitPrice,
                DiscountPercent = rowDto.DiscountPercent,
                VatPercent = rowDto.VatPercent,
                RowTotal = rowCalc.RowTotal,
                VatAmount = rowCalc.VatAmount,
                Notes = rowDto.Notes
            });
        }
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Aggiornato preventivo {Number}", quote.Number);
        
        return (await GetByIdAsync(quote.Id))!;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var quote = await _context.Quotes
            .Include(q => q.Rows)
            .Include(q => q.Attachments)
            .FirstOrDefaultAsync(q => q.Id == id);
        
        if (quote == null) return false;
        
        // Nota: gli allegati sul filesystem andrebbero eliminati separatamente
        // tramite QuoteAttachmentService
        
        _context.Quotes.Remove(quote);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Eliminato preventivo {Number}", quote.Number);
        
        return true;
    }

    public async Task<QuoteDto> DuplicateAsync(Guid sourceId, string? userId = null)
    {
        var source = await _context.Quotes
            .Include(q => q.Rows)
            .FirstOrDefaultAsync(q => q.Id == sourceId);
        
        if (source == null)
        {
            throw new InvalidOperationException($"Preventivo sorgente {sourceId} non trovato");
        }
        
        // Crea DTO per nuovo preventivo
        var dto = new QuoteSaveDto
        {
            Date = DateTime.Today,
            ValidUntil = source.ValidUntil.HasValue 
                ? DateTime.Today.AddDays((source.ValidUntil.Value - source.Date).TotalDays)
                : null,
            ClienteId = source.ClienteId,
            ContactName = source.ContactName,
            PaymentTerms = source.PaymentTerms,
            Notes = $"[Duplicato da {source.Number}] {source.Notes}",
            Status = QuoteStatus.Draft,
            PriceListId = source.PriceListId,
            Rows = source.Rows.OrderBy(r => r.SortOrder).Select(r => new QuoteRowSaveDto
            {
                SortOrder = r.SortOrder,
                RowType = r.RowType,
                PriceListItemId = r.PriceListItemId,
                Code = r.Code,
                Description = r.Description,
                Quantity = r.Quantity,
                Unit = r.Unit,
                UnitPrice = r.UnitPrice,
                DiscountPercent = r.DiscountPercent,
                VatPercent = r.VatPercent,
                Notes = r.Notes
            }).ToList()
        };
        
        var newQuote = await CreateAsync(dto, userId);
        
        _logger.LogInformation("Duplicato preventivo {Source} in {New}", source.Number, newQuote.Number);
        
        return newQuote;
    }

    public async Task<QuoteDto> ChangeStatusAsync(QuoteStatusChangeDto dto, string? userId = null)
    {
        var quote = await _context.Quotes.FindAsync(dto.QuoteId);
        
        if (quote == null)
        {
            throw new InvalidOperationException($"Preventivo {dto.QuoteId} non trovato");
        }
        
        var oldStatus = quote.Status;
        
        // Validazione transizioni stato
        ValidateStatusTransition(oldStatus, dto.NewStatus);
        
        quote.Status = dto.NewStatus;
        quote.UpdatedAt = DateTime.Now;
        quote.UpdatedBy = userId;
        
        if (!string.IsNullOrWhiteSpace(dto.Notes))
        {
            quote.Notes = string.IsNullOrEmpty(quote.Notes) 
                ? dto.Notes 
                : $"{quote.Notes}\n[{DateTime.Now:dd/MM/yyyy}] {dto.Notes}";
        }
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Cambiato stato preventivo {Number} da {Old} a {New}",
            quote.Number, oldStatus, dto.NewStatus);
        
        return (await GetByIdAsync(quote.Id))!;
    }

    public async Task<string> GenerateNextNumberAsync()
    {
        var year = DateTime.Now.Year;
        var prefix = $"PRV-{year}-";
        
        // Trova ultimo numero dell'anno
        var lastNumber = await _context.Quotes
            .Where(q => q.Number.StartsWith(prefix))
            .OrderByDescending(q => q.Number)
            .Select(q => q.Number)
            .FirstOrDefaultAsync();
        
        int nextSeq = 1;
        if (!string.IsNullOrEmpty(lastNumber))
        {
            var seqPart = lastNumber.Substring(prefix.Length);
            if (int.TryParse(seqPart, out int lastSeq))
            {
                nextSeq = lastSeq + 1;
            }
        }
        
        return $"{prefix}{nextSeq:D4}";
    }

    public async Task<int> UpdateExpiredQuotesAsync()
    {
        var today = DateTime.Today;
        
        var expiredQuotes = await _context.Quotes
            .Where(q => q.Status == QuoteStatus.Sent)
            .Where(q => q.ValidUntil.HasValue && q.ValidUntil.Value < today)
            .ToListAsync();
        
        foreach (var quote in expiredQuotes)
        {
            quote.Status = QuoteStatus.Expired;
            quote.UpdatedAt = DateTime.Now;
        }
        
        await _context.SaveChangesAsync();
        
        if (expiredQuotes.Any())
        {
            _logger.LogInformation("Aggiornati {Count} preventivi scaduti", expiredQuotes.Count);
        }
        
        return expiredQuotes.Count;
    }

    private void ValidateStatusTransition(QuoteStatus from, QuoteStatus to)
    {
        // Transizioni consentite
        var allowed = from switch
        {
            QuoteStatus.Draft => new[] { QuoteStatus.Sent },
            QuoteStatus.Sent => new[] { QuoteStatus.Accepted, QuoteStatus.Rejected, QuoteStatus.Expired, QuoteStatus.Draft },
            QuoteStatus.Accepted => Array.Empty<QuoteStatus>(), // Stato finale
            QuoteStatus.Rejected => new[] { QuoteStatus.Draft }, // Può tornare in bozza
            QuoteStatus.Expired => new[] { QuoteStatus.Draft }, // Può tornare in bozza
            _ => Array.Empty<QuoteStatus>()
        };
        
        if (!allowed.Contains(to))
        {
            throw new InvalidOperationException(
                $"Transizione non consentita da {from} a {to}");
        }
    }

    private QuoteDto MapToDto(Quote quote)
    {
        return new QuoteDto
        {
            Id = quote.Id,
            Number = quote.Number,
            Date = quote.Date,
            ValidUntil = quote.ValidUntil,
            ClienteId = quote.ClienteId,
            ClienteName = quote.Cliente?.RagioneSociale,
            ContactName = quote.ContactName,
            PaymentTerms = quote.PaymentTerms,
            Notes = quote.Notes,
            Status = quote.Status,
            PriceListId = quote.PriceListId,
            PriceListName = quote.PriceList?.Name,
            PriceListVersion = quote.PriceList?.Version,
            PriceListVersionSnapshot = quote.PriceListVersionSnapshot,
            TotalNet = quote.TaxableAmount,
            DiscountTotal = quote.DiscountTotal,
            TotalVat = quote.VatTotal,
            TotalGross = quote.GrandTotal,
            CreatedAt = quote.CreatedAt,
            CreatedBy = quote.CreatedBy,
            UpdatedAt = quote.UpdatedAt,
            UpdatedBy = quote.UpdatedBy,
            Rows = quote.Rows.OrderBy(r => r.SortOrder).Select(r => new QuoteRowDto
            {
                Id = r.Id,
                QuoteId = r.QuoteId,
                SortOrder = r.SortOrder,
                RowType = r.RowType,
                PriceListItemId = r.PriceListItemId,
                Code = r.Code,
                Description = r.Description,
                Quantity = r.Quantity,
                Unit = r.Unit,
                UnitPrice = r.UnitPrice,
                DiscountPercent = r.DiscountPercent ?? 0,
                VatPercent = r.VatPercent,
                RowTotal = r.RowTotal,
                VatAmount = r.VatAmount,
                Notes = r.Notes
            }).ToList(),
            Attachments = quote.Attachments.OrderBy(a => a.UploadedAt).Select(a => new QuoteAttachmentDto
            {
                Id = a.Id,
                QuoteId = a.QuoteId,
                FileName = a.FileName,
                FileSize = a.FileSize,
                ContentType = a.ContentType,
                Description = a.Description,
                UploadedAt = a.UploadedAt,
                UploadedBy = a.UploadedBy
            }).ToList()
        };
    }
}
