using Microsoft.EntityFrameworkCore;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;

namespace MESManager.Infrastructure.Services;

/// <summary>
/// Servizio per la gestione delle preferenze utente
/// </summary>
public class PreferenzeUtenteService : IPreferenzeUtenteService
{
    private readonly MesManagerDbContext _context;

    public PreferenzeUtenteService(MesManagerDbContext context)
    {
        _context = context;
    }

    public async Task<string?> GetAsync(Guid utenteId, string chiave)
    {
        var preferenza = await _context.PreferenzeUtente
            .FirstOrDefaultAsync(p => p.UtenteAppId == utenteId && p.Chiave == chiave);
        
        return preferenza?.ValoreJson;
    }

    public async Task<Dictionary<string, string>> GetAllAsync(Guid utenteId)
    {
        return await _context.PreferenzeUtente
            .Where(p => p.UtenteAppId == utenteId)
            .ToDictionaryAsync(p => p.Chiave, p => p.ValoreJson);
    }

    public async Task SaveAsync(Guid utenteId, string chiave, string valoreJson)
    {
        var preferenza = await _context.PreferenzeUtente
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
            _context.PreferenzeUtente.Add(preferenza);
        }
        else
        {
            preferenza.ValoreJson = valoreJson;
            preferenza.UltimaModifica = DateTime.Now;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(Guid utenteId, string chiave)
    {
        var preferenza = await _context.PreferenzeUtente
            .FirstOrDefaultAsync(p => p.UtenteAppId == utenteId && p.Chiave == chiave);
        
        if (preferenza == null) return false;

        _context.PreferenzeUtente.Remove(preferenza);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task DeleteAllAsync(Guid utenteId)
    {
        var preferenze = await _context.PreferenzeUtente
            .Where(p => p.UtenteAppId == utenteId)
            .ToListAsync();

        _context.PreferenzeUtente.RemoveRange(preferenze);
        await _context.SaveChangesAsync();
    }
}
