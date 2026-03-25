namespace MESManager.Application.Services;

/// <summary>
/// Servizio scoped che espone i dati dell'utente autenticato corrente.
/// Viene popolato da MainLayout dopo l'ottenimento dell'AuthenticationState.
/// Unica fonte di verità — non usa più UtenteApp o localStorage.
/// </summary>
public class CurrentUserService
{
    private string? _userId;
    private string? _userName;
    private string? _userColor;

    /// <summary>ID Identity dell'utente autenticato (AspNetUsers.Id)</summary>
    public string? UserId => _userId;

    /// <summary>Display name o username dell'utente</summary>
    public string UserName => _userName ?? "Utente";

    /// <summary>Colore hex personalizzato (es. #FF5733)</summary>
    public string? UserColor => _userColor;

    /// <summary>True se un utente autenticato è disponibile nella sessione corrente</summary>
    public bool HasUser => !string.IsNullOrEmpty(_userId);

    /// <summary>Evento sollevato quando i dati dell'utente cambiano</summary>
    public event Action? OnUserChanged;

    /// <summary>
    /// Popola il servizio con i dati dell'utente autenticato.
    /// Chiamare da MainLayout.OnInitializedAsync dopo aver ottenuto l'AuthenticationState.
    /// </summary>
    public void SetUser(string? userId, string? userName, string? color = null)
    {
        _userId = userId;
        _userName = userName;
        _userColor = color;
        OnUserChanged?.Invoke();
    }

    public void NotifyUserChanged() => OnUserChanged?.Invoke();
}
