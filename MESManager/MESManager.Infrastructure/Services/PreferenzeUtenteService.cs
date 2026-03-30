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

    private const string GlobalUserId = "GLOBAL";

    public async Task<string?> GetAsync(string userId, string chiave)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Preferenza specifica dell'utente
        var preferenza = await context.PreferenzeUtente
            .FirstOrDefaultAsync(p => p.UserId == userId && !p.IsGlobal && p.Chiave == chiave);
        if (preferenza != null)
            return preferenza.ValoreJson;

        // Fallback: default globale impostato da Admin
        var globale = await context.PreferenzeUtente
            .FirstOrDefaultAsync(p => p.IsGlobal && p.Chiave == chiave);
        return globale?.ValoreJson;
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
            .Where(p => p.UserId == userId && !p.IsGlobal)
            .ToListAsync();
        context.PreferenzeUtente.RemoveRange(preferenze);
        await context.SaveChangesAsync();
    }

    public async Task<string?> GetGlobalAsync(string chiave)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var globale = await context.PreferenzeUtente
            .FirstOrDefaultAsync(p => p.IsGlobal && p.Chiave == chiave);
        return globale?.ValoreJson;
    }

    public async Task SaveGlobalAsync(string chiave, string valoreJson)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var globale = await context.PreferenzeUtente
            .FirstOrDefaultAsync(p => p.IsGlobal && p.Chiave == chiave);

        if (globale == null)
        {
            globale = new PreferenzaUtente
            {
                Id             = Guid.NewGuid(),
                UserId         = GlobalUserId,
                Chiave         = chiave,
                ValoreJson     = valoreJson,
                IsGlobal       = true,
                DataCreazione  = DateTime.Now,
                UltimaModifica = DateTime.Now
            };
            context.PreferenzeUtente.Add(globale);
        }
        else
        {
            globale.ValoreJson     = valoreJson;
            globale.UltimaModifica = DateTime.Now;
        }

        await context.SaveChangesAsync();
    }
}
