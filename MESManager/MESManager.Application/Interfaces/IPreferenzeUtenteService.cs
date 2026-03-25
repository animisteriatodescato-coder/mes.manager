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
}
