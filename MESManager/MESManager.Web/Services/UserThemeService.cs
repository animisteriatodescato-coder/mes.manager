using MESManager.Application.Services;

namespace MESManager.Web.Services;

/// <summary>
/// Servizio scoped che gestisce il tema grafico per-utente.
/// Quando un utente è selezionato, carica e salva le sue preferenze personali nel database.
/// In assenza di preferenze personali, usa il tema globale (AppSettingsService) come fallback.
/// </summary>
public class UserThemeService : IDisposable
{
    private const string PREF_KEY = "user-theme-settings";

    private readonly PreferencesService _prefs;
    private readonly AppSettingsService _globalSettings;
    private readonly CurrentUserService _currentUserService;

    private AppSettings? _userSettings;
    private bool _loaded;

    /// <summary>
    /// Evento sollevato quando il tema effettivo cambia (caricamento, salvataggio o cambio utente).
    /// </summary>
    public event Action? OnUserThemeChanged;

    public UserThemeService(
        PreferencesService prefs,
        AppSettingsService globalSettings,
        CurrentUserService currentUserService)
    {
        _prefs = prefs;
        _globalSettings = globalSettings;
        _currentUserService = currentUserService;
        _currentUserService.OnUserChanged += HandleUserChanged;
    }

    /// <summary>
    /// True se il caricamento delle preferenze è già stato tentato.
    /// </summary>
    public bool IsLoaded => _loaded;

    /// <summary>
    /// True se l'utente ha impostazioni personali memorizzate e caricate.
    /// </summary>
    public bool HasUserTheme => _loaded && _userSettings != null;

    /// <summary>
    /// Carica le impostazioni personali dell'utente dal database (o localStorage).
    /// Richiede il JS runtime — chiamare da OnAfterRenderAsync, non da OnInitialized.
    /// Non solleva OnUserThemeChanged: il chiamante gestisce il proprio aggiornamento UI.
    /// </summary>
    public async Task LoadUserThemeAsync()
    {
        _userSettings = await _prefs.GetAsync<AppSettings>(PREF_KEY);
        _loaded = true;
        // Non solleviamo OnUserThemeChanged qui per evitare loop ricorsivi.
        // I chiamanti (MainLayout.OnAfterRenderAsync, OnUserThemeChanged handler) gestiscono il re-render.
    }

    /// <summary>
    /// Restituisce le impostazioni effettive: personali dell'utente se disponibili,
    /// altrimenti il tema globale di sistema.
    /// Restituisce il riferimento diretto per consentire la modifica live (es. color picker).
    /// </summary>
    public AppSettings GetEffectiveSettings()
        => _loaded && _userSettings != null ? _userSettings : _globalSettings.GetSettings();

    /// <summary>
    /// Salva le impostazioni di tema come preferenza personale dell'utente.
    /// Se nessun utente è selezionato, usa localStorage come fallback.
    /// </summary>
    public async Task SaveUserThemeAsync(AppSettings settings)
    {
        await _prefs.SetAsync(PREF_KEY, settings);
        _userSettings = settings;
        _loaded = true;
        OnUserThemeChanged?.Invoke();
    }

    private void HandleUserChanged()
    {
        // L'utente è cambiato: azzera le impostazioni personali in cache.
        // MainLayout reagirà all'evento e ricaricherà via LoadUserThemeAsync.
        _userSettings = null;
        _loaded = false;
        OnUserThemeChanged?.Invoke();
    }

    public void Dispose()
    {
        _currentUserService.OnUserChanged -= HandleUserChanged;
    }
}
