using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MESManager.Infrastructure.Services;

public class FestiviAppService : IFestiviAppService
{
    private readonly MesManagerDbContext _context;
    private static readonly (int Month, int Day, string Description)[] ItalianStandardRecurringHolidays =
    [
        (1, 1, "Capodanno"),
        (1, 6, "Epifania"),
        (4, 25, "Festa della Liberazione"),
        (5, 1, "Festa del Lavoro"),
        (6, 2, "Festa della Repubblica"),
        (8, 15, "Ferragosto"),
        (11, 1, "Tutti i Santi"),
        (12, 8, "Immacolata Concezione"),
        (12, 25, "Natale"),
        (12, 26, "Santo Stefano")
    ];

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

        return MapToDto(festivo);
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

        return MapToDto(festivo);
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

    public async Task<bool> AnyAsync()
    {
        return await _context.Festivi.AnyAsync();
    }

    public async Task<int> InizializzaItalianiStandardRicorrentiAsync()
    {
        var festiviItaliani = ItalianStandardRecurringHolidays
            .Select(f => new Festivo
            {
                Id = Guid.NewGuid(),
                Data = new DateOnly(2000, f.Month, f.Day),
                Descrizione = f.Description,
                Ricorrente = true
            })
            .ToList();

        _context.Festivi.AddRange(festiviItaliani);
        await _context.SaveChangesAsync();

        return festiviItaliani.Count;
    }

    private static FestivoDto MapToDto(Festivo festivo)
    {
        return new FestivoDto
        {
            Id = festivo.Id,
            Data = festivo.Data,
            Descrizione = festivo.Descrizione,
            Ricorrente = festivo.Ricorrente,
            Anno = festivo.Anno
        };
    }
}
