using Microsoft.EntityFrameworkCore;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;

namespace MESManager.Infrastructure.Services;

public class NonConformitaService : INonConformitaService
{
    private readonly MesManagerDbContext _db;

    public NonConformitaService(MesManagerDbContext db) => _db = db;

    public async Task<List<NonConformitaDto>> GetAllAsync()
        => await _db.NonConformita
            .OrderByDescending(x => x.DataSegnalazione)
            .Select(x => MapToDto(x))
            .ToListAsync();

    public async Task<List<NonConformitaDto>> GetByCodiceArticoloAsync(string codiceArticolo)
        => await _db.NonConformita
            .Where(x => x.CodiceArticolo == codiceArticolo)
            .OrderByDescending(x => x.DataSegnalazione)
            .Select(x => MapToDto(x))
            .ToListAsync();

    public async Task<List<NonConformitaDto>> GetAperteAsync()
        => await _db.NonConformita
            .Where(x => x.Stato != "Chiusa")
            .OrderByDescending(x => x.DataSegnalazione)
            .Select(x => MapToDto(x))
            .ToListAsync();

    public async Task<NonConformitaDto?> GetByIdAsync(Guid id)
    {
        var entity = await _db.NonConformita.FindAsync(id);
        return entity == null ? null : MapToDto(entity);
    }

    public async Task<NonConformitaDto> CreateAsync(NonConformitaDto dto, string userId)
    {
        var entity = new NonConformita
        {
            Id                   = Guid.NewGuid(),
            CodiceArticolo       = dto.CodiceArticolo.Trim(),
            DescrizioneArticolo  = dto.DescrizioneArticolo?.Trim(),
            Cliente              = dto.Cliente?.Trim(),
            DataSegnalazione     = dto.DataSegnalazione,
            Tipo                 = dto.Tipo,
            Gravita              = dto.Gravita,
            Descrizione          = dto.Descrizione.Trim(),
            AzioneCorrettiva     = dto.AzioneCorrettiva?.Trim(),
            Stato                = dto.Stato,
            CreatoDa             = userId,
            CreatoIl             = DateTime.UtcNow
        };
        _db.NonConformita.Add(entity);
        await _db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<NonConformitaDto?> UpdateAsync(NonConformitaDto dto, string userId)
    {
        var entity = await _db.NonConformita.FindAsync(dto.Id);
        if (entity == null) return null;

        entity.CodiceArticolo      = dto.CodiceArticolo.Trim();
        entity.DescrizioneArticolo = dto.DescrizioneArticolo?.Trim();
        entity.Cliente             = dto.Cliente?.Trim();
        entity.DataSegnalazione    = dto.DataSegnalazione;
        entity.Tipo                = dto.Tipo;
        entity.Gravita             = dto.Gravita;
        entity.Descrizione         = dto.Descrizione.Trim();
        entity.AzioneCorrettiva    = dto.AzioneCorrettiva?.Trim();
        entity.Stato               = dto.Stato;
        entity.ModificatoDa        = userId;
        entity.ModificatoIl        = DateTime.UtcNow;

        if (dto.Stato == "Chiusa" && entity.DataChiusura == null)
            entity.DataChiusura = DateTime.Today;

        await _db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _db.NonConformita.FindAsync(id);
        if (entity == null) return false;
        _db.NonConformita.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<NonConformitaDto?> ChiudiAsync(Guid id, string? azioneCorrettiva, string userId)
    {
        var entity = await _db.NonConformita.FindAsync(id);
        if (entity == null) return null;

        entity.Stato             = "Chiusa";
        entity.DataChiusura      = DateTime.Today;
        entity.AzioneCorrettiva  = azioneCorrettiva?.Trim() ?? entity.AzioneCorrettiva;
        entity.ModificatoDa      = userId;
        entity.ModificatoIl      = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return MapToDto(entity);
    }

    private static NonConformitaDto MapToDto(NonConformita x) => new()
    {
        Id                  = x.Id,
        CodiceArticolo      = x.CodiceArticolo,
        DescrizioneArticolo = x.DescrizioneArticolo,
        Cliente             = x.Cliente,
        DataSegnalazione    = x.DataSegnalazione,
        Tipo                = x.Tipo,
        Gravita             = x.Gravita,
        Descrizione         = x.Descrizione,
        AzioneCorrettiva    = x.AzioneCorrettiva,
        Stato               = x.Stato,
        CreatoDa            = x.CreatoDa,
        CreatoIl            = x.CreatoIl,
        DataChiusura        = x.DataChiusura
    };
}
