using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces;

/// <summary>
/// Servizio per gestione listini prezzi
/// </summary>
public interface IPriceListService
{
    /// <summary>
    /// Ottiene tutti i listini (con opzione di includere archiviati)
    /// </summary>
    Task<List<PriceListDto>> GetAllAsync(bool includeArchived = false);
    
    /// <summary>
    /// Ottiene listini per dropdown selezione
    /// </summary>
    Task<List<PriceListSelectDto>> GetForSelectionAsync();
    
    /// <summary>
    /// Ottiene dettaglio listino per ID
    /// </summary>
    Task<PriceListDto?> GetByIdAsync(Guid id);
    
    /// <summary>
    /// Ottiene items di un listino
    /// </summary>
    Task<List<PriceListItemDto>> GetItemsAsync(Guid priceListId);
    
    /// <summary>
    /// Cerca items nel listino (per autocomplete)
    /// </summary>
    Task<List<PriceListItemSelectDto>> SearchItemsAsync(Guid priceListId, string searchText, int maxResults = 20);
    
    /// <summary>
    /// Ottiene singolo item
    /// </summary>
    Task<PriceListItemDto?> GetItemByIdAsync(Guid itemId);
    
    /// <summary>
    /// Crea nuovo listino vuoto
    /// </summary>
    Task<PriceListDto> CreateAsync(PriceListCreateDto dto, string? userId = null);
    
    /// <summary>
    /// Imposta listino come default
    /// </summary>
    Task<bool> SetDefaultAsync(Guid id);
    
    /// <summary>
    /// Archivia listino (non più selezionabile per nuovi preventivi)
    /// </summary>
    Task<bool> ArchiveAsync(Guid id);
    
    /// <summary>
    /// Ottiene il listino default attivo
    /// </summary>
    Task<PriceListSelectDto?> GetDefaultAsync();
}
