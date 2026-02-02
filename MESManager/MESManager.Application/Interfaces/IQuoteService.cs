using MESManager.Application.DTOs;
using MESManager.Domain.Entities;
using MESManager.Domain.Enums;

namespace MESManager.Application.Interfaces;

/// <summary>
/// Servizio per gestione preventivi
/// </summary>
public interface IQuoteService
{
    /// <summary>
    /// Ottiene lista preventivi con filtri e paginazione
    /// </summary>
    Task<QuoteListResult> GetListAsync(QuoteListFilter filter);
    
    /// <summary>
    /// Ottiene dettaglio completo preventivo
    /// </summary>
    Task<QuoteDto?> GetByIdAsync(Guid id);
    
    /// <summary>
    /// Crea nuovo preventivo
    /// </summary>
    Task<QuoteDto> CreateAsync(QuoteSaveDto dto, string? userId = null);
    
    /// <summary>
    /// Aggiorna preventivo esistente
    /// </summary>
    Task<QuoteDto> UpdateAsync(QuoteSaveDto dto, string? userId = null);
    
    /// <summary>
    /// Elimina preventivo
    /// </summary>
    Task<bool> DeleteAsync(Guid id);
    
    /// <summary>
    /// Duplica preventivo esistente creando una nuova bozza
    /// </summary>
    Task<QuoteDto> DuplicateAsync(Guid sourceId, string? userId = null);
    
    /// <summary>
    /// Cambia stato del preventivo
    /// </summary>
    Task<QuoteDto> ChangeStatusAsync(QuoteStatusChangeDto dto, string? userId = null);
    
    /// <summary>
    /// Genera prossimo numero preventivo
    /// </summary>
    Task<string> GenerateNextNumberAsync();
    
    /// <summary>
    /// Verifica e aggiorna preventivi scaduti
    /// </summary>
    Task<int> UpdateExpiredQuotesAsync();
}

/// <summary>
/// Filtri per lista preventivi
/// </summary>
public class QuoteListFilter
{
    public string? SearchText { get; set; }
    public Guid? ClienteId { get; set; }
    public QuoteStatus? Status { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public decimal? TotalMin { get; set; }
    public decimal? TotalMax { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
}

/// <summary>
/// Risultato paginato lista preventivi
/// </summary>
public class QuoteListResult
{
    public List<QuoteListDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
