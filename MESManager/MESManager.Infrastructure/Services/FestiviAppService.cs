using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MESManager.Infrastructure.Services;

public class FestiviAppService : IFestiviAppService
{
    private readonly MesManagerDbContext _context;

    public FestiviAppService(MesManagerDbContext context)
    {
        _context = context;
    }

    public async Task<List<FestivoDto>> GetListaAsync()
    {
        return await _context.Festivi
            .OrderBy(f => f.Data)
            .Select(f => new FestivoDto
            {
                Id = f.Id,
                Data = f.Data,
                Descrizione = f.Descrizione,
                Ricorrente = f.Ricorrente,
                Anno = f.Anno
            })
            .ToListAsync();
    }

    public async Task<FestivoDto?> GetAsync(Guid id)
    {
        var festivo = await _context.Festivi.FindAsync(id);
        if (festivo == null) return null;

        return new FestivoDto
        {
            Id = festivo.Id,
            Data = festivo.Data,
            Descrizione = festivo.Descrizione,
            Ricorrente = festivo.Ricorrente,
            Anno = festivo.Anno
        };
    }

    public async Task<FestivoDto> CreaAsync(CreateFestivoRequest request)
    {
        var festivo = new Festivo
        {
            Id = Guid.NewGuid(),
            Data = request.Data,
            Descrizione = request.Descrizione,
            Ricorrente = request.Ricorrente,
            Anno = request.Ricorrente ? null : request.Data.Year,
            DataCreazione = DateTime.UtcNow
        };

        _context.Festivi.Add(festivo);
        await _context.SaveChangesAsync();

        return new FestivoDto
        {
            Id = festivo.Id,
            Data = festivo.Data,
            Descrizione = festivo.Descrizione,
            Ricorrente = festivo.Ricorrente,
            Anno = festivo.Anno
        };
    }

    public async Task AggiornaAsync(Guid id, CreateFestivoRequest request)
    {
        var festivo = await _context.Festivi.FindAsync(id);
        if (festivo == null)
            throw new Exception("Festivo non trovato");

        festivo.Data = request.Data;
        festivo.Descrizione = request.Descrizione;
        festivo.Ricorrente = request.Ricorrente;
        festivo.Anno = request.Ricorrente ? null : request.Data.Year;

        await _context.SaveChangesAsync();
    }

    public async Task EliminaAsync(Guid id)
    {
        var festivo = await _context.Festivi.FindAsync(id);
        if (festivo == null)
            throw new Exception("Festivo non trovato");

        _context.Festivi.Remove(festivo);
        await _context.SaveChangesAsync();
    }
}
