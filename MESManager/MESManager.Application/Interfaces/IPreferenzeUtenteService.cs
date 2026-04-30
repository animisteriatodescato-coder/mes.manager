namespace MESManager.Application.Interfaces;

/// <summary>
/// Gestione preferenze utente. Usa string userId (AspNetUsers.Id di Identity).
/// </summary>
public interface IPreferenzeUtenteService
{
    Task<string?> GetAsync(string userId, string chiave);
    Task<Dictionary<string, string>> GetAllAsync(string userId);
    Task SaveAsync(string userId, string chiave, string valoreJson);
    Task<bool> DeleteAsync(string userId, string chiave);
    Task DeleteAllAsync(string userId);

    /// <summary>Legge il default globale (fallback per tutti gli utenti).</summary>
    Task<string?> GetGlobalAsync(string chiave);

    /// <summary>Salva/aggiorna il default globale (solo Admin).</summary>
    Task SaveGlobalAsync(string chiave, string valoreJson);

    /// <summary>Elimina le preferenze personali di TUTTI gli utenti per una chiave (usato da Admin per forzare reset).</summary>
    Task DeleteAllUsersAsync(string chiave);
}
