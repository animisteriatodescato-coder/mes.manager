using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;

namespace MESManager.Infrastructure.Services;

/// <summary>
/// Servizio per gestione listini prezzi
/// </summary>
public class PriceListService : IPriceListService
{
    private readonly MesManagerDbContext _context;
    private readonly ILogger<PriceListService> _logger;

    public PriceListService(MesManagerDbContext context, ILogger<PriceListService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<PriceListDto>> GetAllAsync(bool includeArchived = false)
    {
        var query = _context.PriceLists.AsQueryable();
        
        if (!includeArchived)
        {
            query = query.Where(pl => !pl.IsArchived);
        }
        
        return await query
            .OrderByDescending(pl => pl.IsDefault)
            .ThenByDescending(pl => pl.CreatedAt)
            .Select(pl => new PriceListDto
            {
                Id = pl.Id,
                Name = pl.Name,
                Version = pl.Version,
                Description = pl.Description,
                ValidFrom = pl.ValidFrom,
                ValidTo = pl.ValidTo,
                Source = pl.Source,
                IsDefault = pl.IsDefault,
                IsArchived = pl.IsArchived,
                ItemCount = pl.ItemCount,
                CreatedAt = pl.CreatedAt,
                CreatedBy = pl.CreatedBy
            })
            .ToListAsync();
    }

    public async Task<List<PriceListSelectDto>> GetForSelectionAsync()
    {
        return await _context.PriceLists
            .Where(pl => !pl.IsArchived)
            .Where(pl => pl.ValidTo == null || pl.ValidTo >= DateTime.Today)
            .OrderByDescending(pl => pl.IsDefault)
            .ThenByDescending(pl => pl.CreatedAt)
            .Select(pl => new PriceListSelectDto
            {
                Id = pl.Id,
                Name = pl.Name,
                Version = pl.Version,
                IsDefault = pl.IsDefault,
                ItemCount = pl.ItemCount
            })
            .ToListAsync();
    }

    public async Task<PriceListDto?> GetByIdAsync(Guid id)
    {
        return await _context.PriceLists
            .Where(pl => pl.Id == id)
            .Select(pl => new PriceListDto
            {
                Id = pl.Id,
                Name = pl.Name,
                Version = pl.Version,
                Description = pl.Description,
                ValidFrom = pl.ValidFrom,
                ValidTo = pl.ValidTo,
                Source = pl.Source,
                IsDefault = pl.IsDefault,
                IsArchived = pl.IsArchived,
                ItemCount = pl.ItemCount,
                CreatedAt = pl.CreatedAt,
                CreatedBy = pl.CreatedBy
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<PriceListItemDto>> GetItemsAsync(Guid priceListId)
    {
        return await _context.PriceListItems
            .Where(i => i.PriceListId == priceListId && !i.IsDisabled)
            .OrderBy(i => i.Category)
            .ThenBy(i => i.SortOrder)
            .ThenBy(i => i.Code)
            .Select(i => new PriceListItemDto
            {
                Id = i.Id,
                PriceListId = i.PriceListId,
                Code = i.Code,
                Description = i.Description,
                Unit = i.Unit,
                UnitPrice = i.BasePrice,
                VatRate = i.VatRate,
                Category = i.Category,
                Notes = i.Notes,
                SortOrder = i.SortOrder,
                IsDisabled = i.IsDisabled
            })
            .ToListAsync();
    }

    public async Task<List<PriceListItemSelectDto>> SearchItemsAsync(Guid priceListId, string searchText, int maxResults = 20)
    {
        var query = _context.PriceListItems
            .Where(i => i.PriceListId == priceListId && !i.IsDisabled);
        
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var search = searchText.Trim().ToLower();
            query = query.Where(i => 
                i.Code.ToLower().Contains(search) ||
                i.Description.ToLower().Contains(search) ||
                (i.Category != null && i.Category.ToLower().Contains(search)));
        }
        
        return await query
            .OrderBy(i => i.Code)
            .Take(maxResults)
            .Select(i => new PriceListItemSelectDto
            {
                Id = i.Id,
                Code = i.Code,
                Description = i.Description,
                Unit = i.Unit,
                UnitPrice = i.BasePrice,
                VatRate = i.VatRate,
                Category = i.Category
            })
            .ToListAsync();
    }

    public async Task<PriceListItemDto?> GetItemByIdAsync(Guid itemId)
    {
        return await _context.PriceListItems
            .Where(i => i.Id == itemId)
            .Select(i => new PriceListItemDto
            {
                Id = i.Id,
                PriceListId = i.PriceListId,
                Code = i.Code,
                Description = i.Description,
                Unit = i.Unit,
                UnitPrice = i.BasePrice,
                VatRate = i.VatRate,
                Category = i.Category,
                Notes = i.Notes,
                SortOrder = i.SortOrder,
                IsDisabled = i.IsDisabled
            })
            .FirstOrDefaultAsync();
    }

    public async Task<PriceListDto> CreateAsync(PriceListCreateDto dto, string? userId = null)
    {
        // Genera versione se non specificata
        var version = dto.Version ?? DateTime.Now.ToString("yyyyMMdd.HHmm");
        
        // Se è default, rimuovi flag dagli altri
        if (dto.IsDefault)
        {
            await RemoveDefaultFlagAsync();
        }
        
        var priceList = new PriceList
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Version = version,
            Description = dto.Description,
            ValidFrom = dto.ValidFrom,
            ValidTo = dto.ValidTo,
            IsDefault = dto.IsDefault,
            IsArchived = false,
            ItemCount = 0,
            CreatedAt = DateTime.Now,
            CreatedBy = userId
        };
        
        _context.PriceLists.Add(priceList);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Creato listino {Name} versione {Version}", dto.Name, version);
        
        return new PriceListDto
        {
            Id = priceList.Id,
            Name = priceList.Name,
            Version = priceList.Version,
            Description = priceList.Description,
            ValidFrom = priceList.ValidFrom,
            ValidTo = priceList.ValidTo,
            IsDefault = priceList.IsDefault,
            IsArchived = priceList.IsArchived,
            ItemCount = priceList.ItemCount,
            CreatedAt = priceList.CreatedAt,
            CreatedBy = priceList.CreatedBy
        };
    }

    public async Task<bool> SetDefaultAsync(Guid id)
    {
        var priceList = await _context.PriceLists.FindAsync(id);
        if (priceList == null || priceList.IsArchived)
        {
            return false;
        }
        
        await RemoveDefaultFlagAsync();
        
        priceList.IsDefault = true;
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Impostato listino {Name} v{Version} come default", priceList.Name, priceList.Version);
        
        return true;
    }

    public async Task<bool> ArchiveAsync(Guid id)
    {
        var priceList = await _context.PriceLists.FindAsync(id);
        if (priceList == null)
        {
            return false;
        }
        
        priceList.IsArchived = true;
        priceList.IsDefault = false;
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Archiviato listino {Name} v{Version}", priceList.Name, priceList.Version);
        
        return true;
    }

    public async Task<PriceListSelectDto?> GetDefaultAsync()
    {
        return await _context.PriceLists
            .Where(pl => pl.IsDefault && !pl.IsArchived)
            .Select(pl => new PriceListSelectDto
            {
                Id = pl.Id,
                Name = pl.Name,
                Version = pl.Version,
                IsDefault = pl.IsDefault,
                ItemCount = pl.ItemCount
            })
            .FirstOrDefaultAsync();
    }

    private async Task RemoveDefaultFlagAsync()
    {
        var currentDefault = await _context.PriceLists
            .Where(pl => pl.IsDefault)
            .ToListAsync();
        
        foreach (var pl in currentDefault)
        {
            pl.IsDefault = false;
        }
    }
}
