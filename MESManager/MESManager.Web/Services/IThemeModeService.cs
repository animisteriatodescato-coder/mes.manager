namespace MESManager.Web.Services;

/// <summary>
/// Servizio iniettabile (Scoped — una istanza per circuito Blazor Server = sessione utente)
/// che espone lo stato dark/light mode e notifica tutti i subscriber quando cambia.
///
/// PUNTO CENTRALIZZATO per la modalità grafica dell'app.
/// Qualsiasi componente che dipende da IsDarkMode DEVE iniettare questo servizio
/// invece di leggere _isDarkMode direttamente da MainLayout.
///
/// Chi aggiorna la modalità: solo MainLayout (tramite ToggleTheme e OnAppSettingsChanged).
/// Chi legge la modalità: qualsiasi componente Blazor che richiede adattamento grafico.
///
/// Esempio d'uso in un componente:
/// <code>
/// [Inject] IThemeModeService ThemeMode { get; set; } = default!;
///
/// protected override void OnInitialized()
///     => ThemeMode.OnModeChanged += StateHasChanged;
///
/// public void Dispose()
///     => ThemeMode.OnModeChanged -= StateHasChanged;
/// </code>
/// </summary>
public interface IThemeModeService
{
    /// <summary>
    /// True se la modalità scura è attiva per questa sessione utente.
    /// </summary>
    bool IsDarkMode { get; }

    /// <summary>
    /// Evento notificato ogni volta che la modalità cambia.
    /// I subscriber devono chiamare <c>StateHasChanged()</c> o <c>InvokeAsync(StateHasChanged)</c>.
    /// </summary>
    event Action? OnModeChanged;

    /// <summary>
    /// Aggiorna la modalità. Notifica automaticamente tutti i subscriber tramite OnModeChanged.
    /// Chiamare da MainLayout dopo ogni toggle o sync con AppSettings.
    /// </summary>
    void UpdateMode(bool isDark);
}
