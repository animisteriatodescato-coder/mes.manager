using Microsoft.EntityFrameworkCore;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;

namespace MESManager.Infrastructure.Services;

/// <summary>
/// Servizio per la gestione degli utenti app.
/// Usa IDbContextFactory per creare un nuovo contesto per ogni operazione,
/// evitando problemi di concorrenza in Blazor Server.
/// </summary>
public class UtenteAppService : IUtenteAppService
{
    private readonly IDbContextFactory<MesManagerDbContext> _contextFactory;

    public UtenteAppService(IDbContextFactory<MesManagerDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<UtenteApp>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.UtentiApp
            .Where(u => u.Attivo)
            .OrderBy(u => u.Ordine)
            .ThenBy(u => u.Nome)
            .ToListAsync();
    }

    public async Task<UtenteApp?> GetByIdAsync(Guid id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.UtentiApp.FindAsync(id);
    }

    public async Task<UtenteApp?> GetByNomeAsync(string nome)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.UtentiApp
            .FirstOrDefaultAsync(u => u.Nome.ToUpper() == nome.ToUpper());
    }

    public async Task<UtenteApp> CreateAsync(string nome)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var maxOrdine = await context.UtentiApp.MaxAsync(u => (int?)u.Ordine) ?? 0;
        
        var utente = new UtenteApp
        {
            Id = Guid.NewGuid(),
            Nome = nome.ToUpper(),
            Attivo = true,
            Ordine = maxOrdine + 1,
            DataCreazione = DateTime.Now,
            UltimaModifica = DateTime.Now
        };

        context.UtentiApp.Add(utente);
        await context.SaveChangesAsync();
        
        return utente;
    }

    public async Task<UtenteApp?> UpdateAsync(Guid id, string nome, bool attivo, int ordine)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var utente = await context.UtentiApp.FindAsync(id);
        if (utente == null) return null;

        utente.Nome = nome.ToUpper();
        utente.Attivo = attivo;
        utente.Ordine = ordine;
        utente.UltimaModifica = DateTime.Now;

        await context.SaveChangesAsync();
        return utente;
    }

    public async Task<UtenteApp?> UpdateAsync(UtenteApp utenteUpdate)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var utente = await context.UtentiApp.FindAsync(utenteUpdate.Id);
        if (utente == null) return null;

        utente.Nome = utenteUpdate.Nome.ToUpper();
        utente.Attivo = utenteUpdate.Attivo;
        utente.Ordine = utenteUpdate.Ordine;
        utente.Colore = utenteUpdate.Colore;
        utente.UltimaModifica = DateTime.Now;

        await context.SaveChangesAsync();
        return utente;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var utente = await context.UtentiApp.FindAsync(id);
        if (utente == null) return false;

        // Soft delete - disattiva invece di eliminare
        utente.Attivo = false;
        utente.UltimaModifica = DateTime.Now;
        
        await context.SaveChangesAsync();
        return true;
    }
}
