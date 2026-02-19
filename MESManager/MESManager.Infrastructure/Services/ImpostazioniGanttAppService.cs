using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MESManager.Infrastructure.Services;

public class ImpostazioniGanttAppService : IImpostazioniGanttAppService
{
    private readonly MesManagerDbContext _context;

    public ImpostazioniGanttAppService(MesManagerDbContext context)
    {
        _context = context;
    }

    public async Task<ImpostazioniGanttDto?> GetAsync()
    {
        var entity = await _context.ImpostazioniGantt.FirstOrDefaultAsync();
        if (entity == null)
        {
            // Crea impostazioni di default
            entity = new ImpostazioniGantt();
            _context.ImpostazioniGantt.Add(entity);
            await _context.SaveChangesAsync();
        }
        return MapToDto(entity);
    }

    public async Task<ImpostazioniGanttDto> SalvaAsync(ImpostazioniGanttDto dto)
    {
        var entity = await _context.ImpostazioniGantt.FirstOrDefaultAsync();
        
        if (entity == null)
        {
            entity = new ImpostazioniGantt();
            _context.ImpostazioniGantt.Add(entity);
        }

        entity.AbilitaTempoAttrezzaggio = dto.AbilitaTempoAttrezzaggio;
        entity.TempoAttrezzaggioMinutiDefault = dto.TempoAttrezzaggioMinutiDefault;
        entity.BufferInizioProduzioneMinuti = dto.BufferInizioProduzioneMinuti;
        entity.DataModifica = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToDto(entity);
    }

    private ImpostazioniGanttDto MapToDto(ImpostazioniGantt entity)
    {
        return new ImpostazioniGanttDto
        {
            Id = entity.Id,
            AbilitaTempoAttrezzaggio = entity.AbilitaTempoAttrezzaggio,
            TempoAttrezzaggioMinutiDefault = entity.TempoAttrezzaggioMinutiDefault,
            BufferInizioProduzioneMinuti = entity.BufferInizioProduzioneMinuti
        };
    }
}
