using MESManager.Domain.Entities;

namespace MESManager.Application.Services;

/// <summary>
/// Servizio per gestire l'utente corrente selezionato nella sessione.
/// Questo servizio è registrato come Scoped, quindi ogni richiesta ha la sua istanza.
/// L'utente selezionato viene persistito tramite JavaScript localStorage nel browser.
/// </summary>
public class CurrentUserService
{
    private UtenteApp? _currentUser;
    private Guid? _currentUserId;

    /// <summary>
    /// Utente attualmente selezionato
    /// </summary>
    public UtenteApp? CurrentUser
    {
        get => _currentUser;
        set
        {
            _currentUser = value;
            _currentUserId = value?.Id;
        }
    }

    /// <summary>
    /// ID dell'utente corrente
    /// </summary>
    public Guid? CurrentUserId
    {
        get => _currentUserId ?? _currentUser?.Id;
        set => _currentUserId = value;
    }

    /// <summary>
    /// Verifica se un utente è selezionato
    /// </summary>
    public bool HasUser => _currentUser != null || _currentUserId.HasValue;

    /// <summary>
    /// Nome dell'utente corrente o "Seleziona utente" se nessuno selezionato
    /// </summary>
    public string CurrentUserName => _currentUser?.Nome ?? "Seleziona utente";

    /// <summary>
    /// Evento per notificare il cambio utente
    /// </summary>
    public event Action? OnUserChanged;

    /// <summary>
    /// Imposta l'utente corrente
    /// </summary>
    public void SetCurrentUser(UtenteApp? utente)
    {
        _currentUser = utente;
        _currentUserId = utente?.Id;
        OnUserChanged?.Invoke();
    }

    /// <summary>
    /// Notifica il cambio utente
    /// </summary>
    public void NotifyUserChanged()
    {
        OnUserChanged?.Invoke();
    }
}
