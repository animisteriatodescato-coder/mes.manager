using Microsoft.EntityFrameworkCore;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;

namespace MESManager.Infrastructure.Services;

/// <summary>
/// Servizio per la gestione degli utenti app
/// </summary>
public class UtenteAppService : IUtenteAppService
{
    private readonly MesManagerDbContext _context;

    public UtenteAppService(MesManagerDbContext context)
    {
        _context = context;
    }

    public async Task<List<UtenteApp>> GetAllAsync()
    {
        return await _context.UtentiApp
            .Where(u => u.Attivo)
            .OrderBy(u => u.Ordine)
            .ThenBy(u => u.Nome)
            .ToListAsync();
    }

    public async Task<UtenteApp?> GetByIdAsync(Guid id)
    {
        return await _context.UtentiApp.FindAsync(id);
    }

    public async Task<UtenteApp?> GetByNomeAsync(string nome)
    {
        return await _context.UtentiApp
            .FirstOrDefaultAsync(u => u.Nome.ToUpper() == nome.ToUpper());
    }

    public async Task<UtenteApp> CreateAsync(string nome)
    {
        var maxOrdine = await _context.UtentiApp.MaxAsync(u => (int?)u.Ordine) ?? 0;
        
        var utente = new UtenteApp
        {
            Id = Guid.NewGuid(),
            Nome = nome.ToUpper(),
            Attivo = true,
            Ordine = maxOrdine + 1,
            DataCreazione = DateTime.Now,
            UltimaModifica = DateTime.Now
        };

        _context.UtentiApp.Add(utente);
        await _context.SaveChangesAsync();
        
        return utente;
    }

    public async Task<UtenteApp?> UpdateAsync(Guid id, string nome, bool attivo, int ordine)
    {
        var utente = await _context.UtentiApp.FindAsync(id);
        if (utente == null) return null;

        utente.Nome = nome.ToUpper();
        utente.Attivo = attivo;
        utente.Ordine = ordine;
        utente.UltimaModifica = DateTime.Now;

        await _context.SaveChangesAsync();
        return utente;
    }

    public async Task<UtenteApp?> UpdateAsync(UtenteApp utenteUpdate)
    {
        var utente = await _context.UtentiApp.FindAsync(utenteUpdate.Id);
        if (utente == null) return null;

        utente.Nome = utenteUpdate.Nome.ToUpper();
        utente.Attivo = utenteUpdate.Attivo;
        utente.Ordine = utenteUpdate.Ordine;
        utente.UltimaModifica = DateTime.Now;

        await _context.SaveChangesAsync();
        return utente;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var utente = await _context.UtentiApp.FindAsync(id);
        if (utente == null) return false;

        // Soft delete - disattiva invece di eliminare
        utente.Attivo = false;
        utente.UltimaModifica = DateTime.Now;
        
        await _context.SaveChangesAsync();
        return true;
    }
}
