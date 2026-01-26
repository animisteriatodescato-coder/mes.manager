using Microsoft.EntityFrameworkCore;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;

namespace MESManager.Infrastructure.Services;

/// <summary>
/// Servizio per la gestione delle preferenze utente.
/// Usa IDbContextFactory per creare un nuovo contesto per ogni operazione,
/// evitando problemi di concorrenza in Blazor Server.
/// </summary>
public class PreferenzeUtenteService : IPreferenzeUtenteService
{
    private readonly IDbContextFactory<MesManagerDbContext> _contextFactory;

    public PreferenzeUtenteService(IDbContextFactory<MesManagerDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<string?> GetAsync(Guid utenteId, string chiave)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var preferenza = await context.PreferenzeUtente
            .FirstOrDefaultAsync(p => p.UtenteAppId == utenteId && p.Chiave == chiave);
        
        return preferenza?.ValoreJson;
    }

    public async Task<Dictionary<string, string>> GetAllAsync(Guid utenteId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PreferenzeUtente
            .Where(p => p.UtenteAppId == utenteId)
            .ToDictionaryAsync(p => p.Chiave, p => p.ValoreJson);
    }

    public async Task SaveAsync(Guid utenteId, string chiave, string valoreJson)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var preferenza = await context.PreferenzeUtente
            .FirstOrDefaultAsync(p => p.UtenteAppId == utenteId && p.Chiave == chiave);

        if (preferenza == null)
        {
            preferenza = new PreferenzaUtente
            {
                Id = Guid.NewGuid(),
                UtenteAppId = utenteId,
                Chiave = chiave,
                ValoreJson = valoreJson,
                DataCreazione = DateTime.Now,
                UltimaModifica = DateTime.Now
            };
            context.PreferenzeUtente.Add(preferenza);
        }
        else
        {
            preferenza.ValoreJson = valoreJson;
            preferenza.UltimaModifica = DateTime.Now;
        }

        await context.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(Guid utenteId, string chiave)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var preferenza = await context.PreferenzeUtente
            .FirstOrDefaultAsync(p => p.UtenteAppId == utenteId && p.Chiave == chiave);
        
        if (preferenza == null) return false;

        context.PreferenzeUtente.Remove(preferenza);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task DeleteAllAsync(Guid utenteId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var preferenze = await context.PreferenzeUtente
            .Where(p => p.UtenteAppId == utenteId)
            .ToListAsync();

        context.PreferenzeUtente.RemoveRange(preferenze);
        await context.SaveChangesAsync();
    }
}
