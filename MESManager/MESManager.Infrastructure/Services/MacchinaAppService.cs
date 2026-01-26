using Microsoft.EntityFrameworkCore;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;

namespace MESManager.Infrastructure.Services;

public class MacchinaAppService : IMacchinaAppService
{
    private readonly MesManagerDbContext _context;
    
    public MacchinaAppService(MesManagerDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<MacchinaDto>> GetListaAsync()
    {
        return await _context.Macchine
            .Select(m => new MacchinaDto
            {
                Id = m.Id,
                Codice = m.Codice,
                Nome = m.Nome,
                AttivaInGantt = m.AttivaInGantt,
                OrdineVisualizazione = m.OrdineVisualizazione,
                IndirizzoPLC = m.IndirizzoPLC
            })
            .ToListAsync();
    }
    
    public async Task<MacchinaDto?> GetByIdAsync(Guid id)
    {
        var macchina = await _context.Macchine.FindAsync(id);
        if (macchina == null) return null;
        
        return new MacchinaDto
        {
            Id = macchina.Id,
            Codice = macchina.Codice,
            Nome = macchina.Nome,
            AttivaInGantt = macchina.AttivaInGantt,
            OrdineVisualizazione = macchina.OrdineVisualizazione,
            IndirizzoPLC = macchina.IndirizzoPLC
        };
    }
    
    public async Task<MacchinaDto> CreaAsync(MacchinaDto dto)
    {
        var macchina = new Macchina
        {
            Codice = dto.Codice,
            Nome = dto.Nome,
            AttivaInGantt = dto.AttivaInGantt,
            OrdineVisualizazione = dto.OrdineVisualizazione,
            IndirizzoPLC = dto.IndirizzoPLC
        };
        
        _context.Macchine.Add(macchina);
        await _context.SaveChangesAsync();
        
        return new MacchinaDto
        {
            Id = macchina.Id,
            Codice = macchina.Codice,
            Nome = macchina.Nome,
            AttivaInGantt = macchina.AttivaInGantt,
            OrdineVisualizazione = macchina.OrdineVisualizazione,
            IndirizzoPLC = macchina.IndirizzoPLC
        };
    }
    
    public async Task<MacchinaDto> AggiornaAsync(Guid id, MacchinaDto dto)
    {
        var macchina = await _context.Macchine.FindAsync(id);
        if (macchina == null) throw new Exception("Macchina non trovata");
        
        macchina.Codice = dto.Codice;
        macchina.Nome = dto.Nome;
        macchina.AttivaInGantt = dto.AttivaInGantt;
        macchina.OrdineVisualizazione = dto.OrdineVisualizazione;
        macchina.IndirizzoPLC = dto.IndirizzoPLC;
        
        await _context.SaveChangesAsync();
        
        return new MacchinaDto
        {
            Id = macchina.Id,
            Codice = macchina.Codice,
            Nome = macchina.Nome,
            AttivaInGantt = macchina.AttivaInGantt,
            OrdineVisualizazione = macchina.OrdineVisualizazione,
            IndirizzoPLC = macchina.IndirizzoPLC
        };
    }
    
    public async Task EliminaAsync(Guid id)
    {
        var macchina = await _context.Macchine.FindAsync(id);
        if (macchina == null) throw new Exception("Macchina non trovata");
        
        _context.Macchine.Remove(macchina);
        await _context.SaveChangesAsync();
    }
}
