using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MESManager.Infrastructure.Repositories;

public class RicettaRepository : IRicettaRepository
{
    private readonly MesManagerDbContext _context;
    private readonly ILogger<RicettaRepository> _logger;

    public RicettaRepository(MesManagerDbContext context, ILogger<RicettaRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Articolo>> GetArticoliConRicettaAsync(string? searchTerm = null, int maxResults = 50)
    {
        var query = _context.Articoli
            .Include(a => a.Ricetta)
                .ThenInclude(r => r!.Parametri)
            .Where(a => a.Ricetta != null);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(a => a.Codice.Contains(searchTerm) || a.Descrizione.Contains(searchTerm));
        }

        var result = await query
            .OrderBy(a => a.Codice)
            .Take(maxResults)
            .AsNoTracking()
            .ToListAsync();

        _logger.LogInformation("GetArticoliConRicettaAsync: found {Count} articoli", result.Count);
        return result;
    }

    public async Task<Articolo?> GetArticoloConRicettaByCodeAsync(string codiceArticolo)
    {
        var result = await _context.Articoli
            .Include(a => a.Ricetta)
                .ThenInclude(r => r!.Parametri)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Codice == codiceArticolo);

        _logger.LogDebug("GetArticoloConRicettaByCodeAsync({Codice}) returned {Found}",
            codiceArticolo, result != null ? "found" : "NOT FOUND");
        return result;
    }

    public async Task<int> CountArticoliConRicettaAsync()
    {
        return await _context.Articoli
            .Where(a => a.Ricetta != null)
            .CountAsync();
    }
    
    public async Task<Dictionary<Guid, RicettaInfo>> GetRicetteInfoByArticoloIdAsync(List<Guid> articoloIds)
    {
        if (!articoloIds.Any())
            return new Dictionary<Guid, RicettaInfo>();
        
        var result = await _context.Articoli
            .Where(a => articoloIds.Contains(a.Id))
            .Select(a => new
            {
                a.Id,
                HasRicetta = a.Ricetta != null,
                NumeroParametri = a.Ricetta != null ? a.Ricetta.Parametri.Count : 0,
                UltimaModifica = a.Ricetta != null ? a.UltimaModifica : (DateTime?)null
            })
            .AsNoTracking()
            .ToListAsync();
        
        return result
            .Where(x => x.HasRicetta)
            .ToDictionary(
                x => x.Id,
                x => new RicettaInfo
                {
                    NumeroParametri = x.NumeroParametri,
                    UltimaModifica = x.UltimaModifica
                });
    }

    public async Task<bool> UpdateValoreParametroAsync(Guid parametroId, int nuovoValore)
    {
        var parametro = await _context.ParametriRicetta.FindAsync(parametroId);
        if (parametro == null)
            return false;

        parametro.Valore = nuovoValore.ToString();
        await _context.SaveChangesAsync();
        return true;
    }
}
