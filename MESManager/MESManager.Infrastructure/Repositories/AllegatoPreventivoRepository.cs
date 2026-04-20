using Microsoft.EntityFrameworkCore;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;

namespace MESManager.Infrastructure.Repositories;

public class AllegatoPreventivoRepository : IAllegatoPreventivoService
{
    private readonly IDbContextFactory<MesManagerDbContext> _factory;

    public AllegatoPreventivoRepository(IDbContextFactory<MesManagerDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<AllegatoPreventivoDto>> GetByPreventivoIdAsync(Guid preventivoId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.AllegatiPreventivi
            .Where(a => a.PreventivoId == preventivoId)
            .OrderByDescending(a => a.DataCaricamento)
            .Select(a => new AllegatoPreventivoDto
            {
                Id = a.Id,
                PreventivoId = a.PreventivoId,
                NomeFile = a.NomeFile,
                ContentType = a.ContentType,
                DimensioneBytes = a.DimensioneBytes,
                DataCaricamento = a.DataCaricamento
            })
            .ToListAsync();
    }

    public async Task<AllegatoPreventivoDto> AddAsync(Guid preventivoId, string nomeFile, string contentType, byte[] dati)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var entity = new AllegatoPreventivo
        {
            PreventivoId = preventivoId,
            NomeFile = nomeFile,
            ContentType = contentType,
            Dati = dati,
            DimensioneBytes = dati.LongLength,
            DataCaricamento = DateTime.Now
        };
        db.AllegatiPreventivi.Add(entity);
        await db.SaveChangesAsync();
        return new AllegatoPreventivoDto
        {
            Id = entity.Id,
            PreventivoId = entity.PreventivoId,
            NomeFile = entity.NomeFile,
            ContentType = entity.ContentType,
            DimensioneBytes = entity.DimensioneBytes,
            DataCaricamento = entity.DataCaricamento
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var entity = await db.AllegatiPreventivi.FindAsync(id);
        if (entity == null) return false;
        db.AllegatiPreventivi.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<(byte[] Dati, string ContentType, string NomeFile)?> GetFileAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var entity = await db.AllegatiPreventivi.FindAsync(id);
        if (entity == null) return null;
        return (entity.Dati, entity.ContentType, entity.NomeFile);
    }
}
