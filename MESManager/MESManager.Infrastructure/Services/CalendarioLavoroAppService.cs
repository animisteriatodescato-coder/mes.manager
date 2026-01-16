using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MESManager.Infrastructure.Services;

public class CalendarioLavoroAppService : ICalendarioLavoroAppService
{
    private readonly MesManagerDbContext _context;

    public CalendarioLavoroAppService(MesManagerDbContext context)
    {
        _context = context;
    }

    public async Task<CalendarioLavoroDto?> GetAsync()
    {
        var entity = await _context.CalendarioLavoro.FirstOrDefaultAsync();
        if (entity == null)
        {
            // Crea un calendario di default
            entity = new CalendarioLavoro();
            _context.CalendarioLavoro.Add(entity);
            await _context.SaveChangesAsync();
        }
        return MapToDto(entity);
    }

    public async Task<CalendarioLavoroDto> SalvaAsync(CalendarioLavoroDto dto)
    {
        var entity = await _context.CalendarioLavoro.FirstOrDefaultAsync();
        
        if (entity == null)
        {
            entity = new CalendarioLavoro();
            _context.CalendarioLavoro.Add(entity);
        }

        entity.Lunedi = dto.Lunedi;
        entity.Martedi = dto.Martedi;
        entity.Mercoledi = dto.Mercoledi;
        entity.Giovedi = dto.Giovedi;
        entity.Venerdi = dto.Venerdi;
        entity.Sabato = dto.Sabato;
        entity.Domenica = dto.Domenica;
        entity.OraInizio = dto.OraInizio;
        entity.OraFine = dto.OraFine;
        entity.DataModifica = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToDto(entity);
    }

    private CalendarioLavoroDto MapToDto(CalendarioLavoro entity)
    {
        return new CalendarioLavoroDto
        {
            Id = entity.Id,
            Lunedi = entity.Lunedi,
            Martedi = entity.Martedi,
            Mercoledi = entity.Mercoledi,
            Giovedi = entity.Giovedi,
            Venerdi = entity.Venerdi,
            Sabato = entity.Sabato,
            Domenica = entity.Domenica,
            OraInizio = entity.OraInizio,
            OraFine = entity.OraFine
        };
    }
}
