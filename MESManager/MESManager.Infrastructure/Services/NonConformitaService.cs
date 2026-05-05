using Microsoft.EntityFrameworkCore;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;

namespace MESManager.Infrastructure.Services;

public class NonConformitaService : INonConformitaService
{
    private readonly IDbContextFactory<MesManagerDbContext> _dbFactory;

    public NonConformitaService(IDbContextFactory<MesManagerDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<List<NonConformitaDto>> GetAllAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.NonConformita
            .OrderByDescending(x => x.DataSegnalazione)
            .Select(x => MapToDto(x))
            .ToListAsync();
    }

    public async Task<List<NonConformitaDto>> GetByCodiceArticoloAsync(string codiceArticolo)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.NonConformita
            .Where(x => x.CodiceArticolo == codiceArticolo)
            .OrderByDescending(x => x.DataSegnalazione)
            .Select(x => MapToDto(x))
            .ToListAsync();
    }

    public async Task<List<NonConformitaDto>> GetAperteAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.NonConformita
            .Where(x => x.Stato != "Chiusa")
            .OrderByDescending(x => x.DataSegnalazione)
            .Select(x => MapToDto(x))
            .ToListAsync();
    }

    public async Task<NonConformitaDto?> GetByIdAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entity = await db.NonConformita.FindAsync(id);
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
            MotivoProblema       = dto.MotivoProblema?.Trim(),
            AzioneCorrettiva     = dto.AzioneCorrettiva?.Trim(),
            TipologiaNc          = dto.TipologiaNc?.Trim(),
            Esito                = dto.Esito,
            DataEsito            = dto.Esito != null ? DateTime.Today : null,
            Stato                = dto.Stato,
            CreatoDa             = userId,
            CreatoIl             = DateTime.UtcNow
        };
        // Logica esito automatico
        if (dto.Esito == "Positivo") { entity.Stato = "Chiusa"; entity.DataChiusura = DateTime.Today; }
        else if (dto.Esito == "Negativo" && entity.Stato == "Aperta") entity.Stato = "InGestione";
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.NonConformita.Add(entity);
        await db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<NonConformitaDto?> UpdateAsync(NonConformitaDto dto, string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entity = await db.NonConformita.FindAsync(dto.Id);
        if (entity == null) return null;

        entity.CodiceArticolo      = dto.CodiceArticolo.Trim();
        entity.DescrizioneArticolo = dto.DescrizioneArticolo?.Trim();
        entity.Cliente             = dto.Cliente?.Trim();
        entity.DataSegnalazione    = dto.DataSegnalazione;
        entity.Tipo                = dto.Tipo;
        entity.Gravita             = dto.Gravita;
        entity.Descrizione         = dto.Descrizione.Trim();
        entity.MotivoProblema      = dto.MotivoProblema?.Trim();
        entity.AzioneCorrettiva    = dto.AzioneCorrettiva?.Trim();
        entity.TipologiaNc         = dto.TipologiaNc?.Trim();
        entity.Esito               = dto.Esito;
        entity.Stato               = dto.Stato;
        entity.ModificatoDa        = userId;
        entity.ModificatoIl        = DateTime.UtcNow;

        // Aggiorna DataEsito solo quando cambia
        if (dto.Esito != null && entity.DataEsito == null)
            entity.DataEsito = DateTime.Today;

        // Logica esito automatico
        if (dto.Esito == "Positivo") { entity.Stato = "Chiusa"; entity.DataChiusura = DateTime.Today; }
        else if (dto.Esito == "Negativo" && entity.Stato == "Aperta") entity.Stato = "InGestione";
        else if (dto.Stato == "Chiusa" && entity.DataChiusura == null) entity.DataChiusura = DateTime.Today;

        await db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entity = await db.NonConformita.FindAsync(id);
        if (entity == null) return false;
        db.NonConformita.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<NonConformitaDto?> ChiudiAsync(Guid id, string? azioneCorrettiva, string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entity = await db.NonConformita.FindAsync(id);
        if (entity == null) return null;

        entity.Stato             = "Chiusa";
        entity.DataChiusura      = DateTime.Today;
        entity.AzioneCorrettiva  = azioneCorrettiva?.Trim() ?? entity.AzioneCorrettiva;
        entity.ModificatoDa      = userId;
        entity.ModificatoIl      = DateTime.UtcNow;

        await db.SaveChangesAsync();
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
        MotivoProblema      = x.MotivoProblema,
        AzioneCorrettiva    = x.AzioneCorrettiva,
        TipologiaNc         = x.TipologiaNc,
        Esito               = x.Esito,
        DataEsito           = x.DataEsito,
        Stato               = x.Stato,
        CreatoDa            = x.CreatoDa,
        CreatoIl            = x.CreatoIl,
        DataChiusura        = x.DataChiusura
    };
}
