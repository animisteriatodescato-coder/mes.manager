using MESManager.Domain.Entities;

namespace MESManager.Application.Interfaces;

/// <summary>
/// Interfaccia per la gestione degli utenti app
/// </summary>
public interface IUtenteAppService
{
    /// <summary>
    /// Ottiene tutti gli utenti attivi ordinati per ordine
    /// </summary>
    Task<List<UtenteApp>> GetAllAsync();
    
    /// <summary>
    /// Ottiene un utente per ID
    /// </summary>
    Task<UtenteApp?> GetByIdAsync(Guid id);
    
    /// <summary>
    /// Ottiene un utente per nome
    /// </summary>
    Task<UtenteApp?> GetByNomeAsync(string nome);
    
    /// <summary>
    /// Crea un nuovo utente
    /// </summary>
    Task<UtenteApp> CreateAsync(string nome);
    
    /// <summary>
    /// Aggiorna un utente esistente
    /// </summary>
    Task<UtenteApp?> UpdateAsync(Guid id, string nome, bool attivo, int ordine);
    
    /// <summary>
    /// Aggiorna un utente esistente (overload con entità)
    /// </summary>
    Task<UtenteApp?> UpdateAsync(UtenteApp utente);
    
    /// <summary>
    /// Elimina un utente (soft delete - disattiva)
    /// </summary>
    Task<bool> DeleteAsync(Guid id);
}
