using Microsoft.EntityFrameworkCore;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;

namespace MESManager.Infrastructure.Services;

public class ArticoloAppService : IArticoloAppService
{
    private readonly MesManagerDbContext _context;

    public ArticoloAppService(MesManagerDbContext context)
    {
        _context = context;
    }

    public async Task<List<ArticoloDto>> GetListaAsync()
    {
        return await _context.Articoli
            .Select(a => new ArticoloDto
            {
                Id = a.Id,
                Codice = a.Codice,
                Descrizione = a.Descrizione,
                Prezzo = a.Prezzo,
                Attivo = a.Attivo,
                UltimaModifica = a.UltimaModifica,
                TimestampSync = a.TimestampSync
            })
            .ToListAsync();
    }

    public async Task<ArticoloDto?> GetByIdAsync(Guid id)
    {
        var articolo = await _context.Articoli.FindAsync(id);
        if (articolo == null) return null;

        return new ArticoloDto
        {
            Id = articolo.Id,
            Codice = articolo.Codice,
            Descrizione = articolo.Descrizione
        };
    }

    public async Task<ArticoloDto> CreaAsync(ArticoloDto dto)
    {
        var articolo = new Articolo
        {
            Id = Guid.NewGuid(),
            Codice = dto.Codice,
            Descrizione = dto.Descrizione
        };

        _context.Articoli.Add(articolo);
        await _context.SaveChangesAsync();

        dto.Id = articolo.Id;
        return dto;
    }

    public async Task<ArticoloDto> AggiornaAsync(Guid id, ArticoloDto dto)
    {
        var articolo = await _context.Articoli.FindAsync(id);
        if (articolo == null) throw new Exception("Articolo non trovato");

        articolo.Codice = dto.Codice;
        articolo.Descrizione = dto.Descrizione;

        await _context.SaveChangesAsync();
        return dto;
    }

    public async Task EliminaAsync(Guid id)
    {
        var articolo = await _context.Articoli.FindAsync(id);
        if (articolo != null)
        {
            _context.Articoli.Remove(articolo);
            await _context.SaveChangesAsync();
        }
    }
}
