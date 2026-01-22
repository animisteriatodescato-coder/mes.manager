using MESManager.Domain.Entities;

namespace MESManager.Application.Interfaces;

/// <summary>
/// Interfaccia per la gestione delle preferenze utente
/// </summary>
public interface IPreferenzeUtenteService
{
    /// <summary>
    /// Ottiene una preferenza per utente e chiave
    /// </summary>
    Task<string?> GetAsync(Guid utenteId, string chiave);
    
    /// <summary>
    /// Ottiene tutte le preferenze di un utente
    /// </summary>
    Task<Dictionary<string, string>> GetAllAsync(Guid utenteId);
    
    /// <summary>
    /// Salva una preferenza (crea o aggiorna)
    /// </summary>
    Task SaveAsync(Guid utenteId, string chiave, string valoreJson);
    
    /// <summary>
    /// Elimina una preferenza
    /// </summary>
    Task<bool> DeleteAsync(Guid utenteId, string chiave);
    
    /// <summary>
    /// Elimina tutte le preferenze di un utente
    /// </summary>
    Task DeleteAllAsync(Guid utenteId);
}
