using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MESManager.Application.Interfaces;
using MESManager.Domain.Constants;
using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;

namespace MESManager.Infrastructure.Repositories;

/// <summary>
/// Repository per la gestione degli allegati articolo nel database locale
/// </summary>
public class AllegatoArticoloRepository : IAllegatoArticoloRepository
{
    private readonly MesManagerDbContext _context;
    private readonly ILogger<AllegatoArticoloRepository> _logger;

    public AllegatoArticoloRepository(MesManagerDbContext context, ILogger<AllegatoArticoloRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<AllegatoArticolo>> GetByCodiceArticoloAsync(string codiceArticolo)
    {
        _logger.LogDebug("GetByCodiceArticoloAsync: {CodiceArticolo}", codiceArticolo);
        
        return await _context.AllegatiArticoli
            .Where(a => a.CodiceArticolo == codiceArticolo)
            .OrderBy(a => a.Priorita)
            .ThenBy(a => a.NomeFile)
            .ToListAsync();
    }

    public async Task<IEnumerable<AllegatoArticolo>> GetByArchivioAsync(string archivio, int idArchivio)
    {
        _logger.LogDebug("GetByArchivioAsync: Archivio={Archivio}, IdArchivio={IdArchivio}", archivio, idArchivio);
        
        return await _context.AllegatiArticoli
            .Where(a => a.Archivio == archivio && a.IdArchivio == idArchivio)
            .OrderBy(a => a.Priorita)
            .ThenBy(a => a.NomeFile)
            .ToListAsync();
    }

    public async Task<AllegatoArticolo?> GetByIdAsync(int id)
    {
        return await _context.AllegatiArticoli.FindAsync(id);
    }

    public async Task<AllegatoArticolo?> GetByIdGanttOriginaleAsync(int idGanttOriginale)
    {
        return await _context.AllegatiArticoli
            .FirstOrDefaultAsync(a => a.IdGanttOriginale == idGanttOriginale);
    }

    public async Task<AllegatoArticolo> AddAsync(AllegatoArticolo allegato)
    {
        _logger.LogInformation("AddAsync: Adding allegato {NomeFile} for {CodiceArticolo}", 
            allegato.NomeFile, allegato.CodiceArticolo);
        
        _context.AllegatiArticoli.Add(allegato);
        await _context.SaveChangesAsync();
        
        return allegato;
    }

    public async Task AddRangeAsync(IEnumerable<AllegatoArticolo> allegati)
    {
        var list = allegati.ToList();
        _logger.LogInformation("AddRangeAsync: Adding {Count} allegati", list.Count);
        
        _context.AllegatiArticoli.AddRange(list);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(AllegatoArticolo allegato)
    {
        _logger.LogInformation("UpdateAsync: Updating allegato Id={Id}", allegato.Id);
        
        _context.AllegatiArticoli.Update(allegato);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var allegato = await GetByIdAsync(id);
        if (allegato != null)
        {
            _logger.LogInformation("DeleteAsync: Deleting allegato Id={Id}, NomeFile={NomeFile}", 
                id, allegato.NomeFile);
            
            _context.AllegatiArticoli.Remove(allegato);
            await _context.SaveChangesAsync();
        }
        else
        {
            _logger.LogWarning("DeleteAsync: Allegato Id={Id} not found", id);
        }
    }

    public async Task<int> CountImportatiDaGanttAsync()
    {
        return await _context.AllegatiArticoli
            .CountAsync(a => a.ImportatoDaGantt);
    }

    public async Task<IEnumerable<AllegatoArticolo>> GetAllAsync()
    {
        return await _context.AllegatiArticoli
            .OrderBy(a => a.CodiceArticolo)
            .ThenBy(a => a.Priorita)
            .ToListAsync();
    }

    public async Task<IEnumerable<AllegatoArticolo>> GetFotoByCodiceArticoloAsync(string codiceArticolo)
    {
        return await _context.AllegatiArticoli
            .Where(a => a.CodiceArticolo == codiceArticolo && a.TipoFile == "Foto")
            .OrderBy(a => a.Priorita)
            .ThenBy(a => a.NomeFile)
            .ToListAsync();
    }

    public async Task<IEnumerable<AllegatoArticolo>> GetDocumentiByCodiceArticoloAsync(string codiceArticolo)
    {
        return await _context.AllegatiArticoli
            .Where(a => a.CodiceArticolo == codiceArticolo && a.TipoFile == "Documento")
            .OrderBy(a => a.Priorita)
            .ThenBy(a => a.NomeFile)
            .ToListAsync();
    }

    public async Task<Dictionary<string, (int Foto, int Documenti)>> GetConteggioPerArticoloAsync()
    {
        var result = await _context.AllegatiArticoli
            .Where(a => a.CodiceArticolo != null)
            .GroupBy(a => a.CodiceArticolo!)
            .Select(g => new
            {
                CodiceArticolo = g.Key,
                Foto = g.Count(a => a.TipoFile == "Foto"),
                Documenti = g.Count(a => a.TipoFile == "Documento")
            })
            .ToListAsync();

        return result.ToDictionary(
            x => x.CodiceArticolo,
            x => (x.Foto, x.Documenti)
        );
    }
}
