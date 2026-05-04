using Microsoft.EntityFrameworkCore;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;

namespace MESManager.Infrastructure.Repositories;

public class AllegatoNonConformitaRepository : IAllegatoNonConformitaService
{
    private readonly IDbContextFactory<MesManagerDbContext> _factory;

    public AllegatoNonConformitaRepository(IDbContextFactory<MesManagerDbContext> factory)
        => _factory = factory;

    public async Task<List<AllegatoNonConformitaDto>> GetByNcIdAsync(Guid nonConformitaId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.AllegatiNonConformita
            .Where(a => a.NonConformitaId == nonConformitaId)
            .OrderByDescending(a => a.DataCaricamento)
            .Select(a => new AllegatoNonConformitaDto
            {
                Id               = a.Id,
                NonConformitaId  = a.NonConformitaId,
                NomeFile         = a.NomeFile,
                ContentType      = a.ContentType,
                DimensioneBytes  = a.DimensioneBytes,
                DataCaricamento  = a.DataCaricamento
            })
            .ToListAsync();
    }

    public async Task<AllegatoNonConformitaDto> AddAsync(Guid nonConformitaId, string nomeFile, string contentType, byte[] dati)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var entity = new AllegatoNonConformita
        {
            NonConformitaId = nonConformitaId,
            NomeFile        = nomeFile,
            ContentType     = contentType,
            Dati            = dati,
            DimensioneBytes = dati.LongLength,
            DataCaricamento = DateTime.Now
        };
        db.AllegatiNonConformita.Add(entity);
        await db.SaveChangesAsync();
        return new AllegatoNonConformitaDto
        {
            Id              = entity.Id,
            NonConformitaId = entity.NonConformitaId,
            NomeFile        = entity.NomeFile,
            ContentType     = entity.ContentType,
            DimensioneBytes = entity.DimensioneBytes,
            DataCaricamento = entity.DataCaricamento
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var entity = await db.AllegatiNonConformita.FindAsync(id);
        if (entity == null) return false;
        db.AllegatiNonConformita.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<(byte[] Dati, string ContentType, string NomeFile)?> GetFileAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var entity = await db.AllegatiNonConformita.FindAsync(id);
        if (entity == null) return null;
        return (entity.Dati, entity.ContentType, entity.NomeFile);
    }
}
