using Microsoft.EntityFrameworkCore;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;

namespace MESManager.Infrastructure.Services;

public class RicettaAppService : IRicettaAppService
{
    private readonly MesManagerDbContext _context;
    
    public RicettaAppService(MesManagerDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<RicettaDto>> GetListaAsync()
    {
        return await _context.Ricette
            .Include(r => r.Articolo)
            .Select(r => new RicettaDto
            {
                Id = r.Id,
                ArticoloId = r.ArticoloId
            })
            .ToListAsync();
    }
    
    public async Task<RicettaDto?> GetByIdAsync(Guid id)
    {
        var ricetta = await _context.Ricette
            .Include(r => r.Articolo)
            .FirstOrDefaultAsync(r => r.Id == id);
            
        if (ricetta == null) return null;
        
        return new RicettaDto
        {
            Id = ricetta.Id,
            ArticoloId = ricetta.ArticoloId
        };
    }
    
    public async Task<RicettaDto> CreaAsync(RicettaDto dto)
    {
        var ricetta = new Ricetta
        {
            ArticoloId = dto.ArticoloId
        };
        
        _context.Ricette.Add(ricetta);
        await _context.SaveChangesAsync();
        
        return new RicettaDto
        {
            Id = ricetta.Id,
            ArticoloId = ricetta.ArticoloId
        };
    }
    
    public async Task<RicettaDto> AggiornaAsync(Guid id, RicettaDto dto)
    {
        var ricetta = await _context.Ricette.FindAsync(id);
        if (ricetta == null) throw new Exception("Ricetta non trovata");
        
        ricetta.ArticoloId = dto.ArticoloId;
        
        await _context.SaveChangesAsync();
        
        return new RicettaDto
        {
            Id = ricetta.Id,
            ArticoloId = ricetta.ArticoloId
        };
    }
    
    public async Task EliminaAsync(Guid id)
    {
        var ricetta = await _context.Ricette.FindAsync(id);
        if (ricetta == null) throw new Exception("Ricetta non trovata");
        
        _context.Ricette.Remove(ricetta);
        await _context.SaveChangesAsync();
    }
}
