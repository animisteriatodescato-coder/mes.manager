using Microsoft.EntityFrameworkCore;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;

namespace MESManager.Infrastructure.Services;

/// <summary>
/// Gestione preferenze utente. Usa string userId (AspNetUsers.Id di Identity).
/// IDbContextFactory per thread-safety in Blazor Server.
/// </summary>
public class PreferenzeUtenteService : IPreferenzeUtenteService
{
    private readonly IDbContextFactory<MesManagerDbContext> _contextFactory;

    public PreferenzeUtenteService(IDbContextFactory<MesManagerDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<string?> GetAsync(string userId, string chiave)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var preferenza = await context.PreferenzeUtente
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Chiave == chiave);
        return preferenza?.ValoreJson;
    }

    public async Task<Dictionary<string, string>> GetAllAsync(string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PreferenzeUtente
            .Where(p => p.UserId == userId)
            .ToDictionaryAsync(p => p.Chiave, p => p.ValoreJson);
    }

    public async Task SaveAsync(string userId, string chiave, string valoreJson)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var preferenza = await context.PreferenzeUtente
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Chiave == chiave);

        if (preferenza == null)
        {
            preferenza = new PreferenzaUtente
            {
                Id            = Guid.NewGuid(),
                UserId        = userId,
                Chiave        = chiave,
                ValoreJson    = valoreJson,
                DataCreazione = DateTime.Now,
                UltimaModifica = DateTime.Now
            };
            context.PreferenzeUtente.Add(preferenza);
        }
        else
        {
            preferenza.ValoreJson    = valoreJson;
            preferenza.UltimaModifica = DateTime.Now;
        }

        await context.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(string userId, string chiave)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var preferenza = await context.PreferenzeUtente
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Chiave == chiave);

        if (preferenza == null) return false;
        context.PreferenzeUtente.Remove(preferenza);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task DeleteAllAsync(string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var preferenze = await context.PreferenzeUtente
            .Where(p => p.UserId == userId)
            .ToListAsync();
        context.PreferenzeUtente.RemoveRange(preferenze);
        await context.SaveChangesAsync();
    }
}
