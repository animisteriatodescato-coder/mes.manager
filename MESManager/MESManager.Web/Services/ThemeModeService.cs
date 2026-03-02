namespace MESManager.Web.Services;

/// <summary>
/// Implementazione di <see cref="IThemeModeService"/>.
/// Registrata come Scoped (una istanza per circuito Blazor Server = una per utente/sessione).
/// </summary>
public sealed class ThemeModeService : IThemeModeService
{
    private bool _isDarkMode;

    /// <inheritdoc/>
    public bool IsDarkMode => _isDarkMode;

    /// <inheritdoc/>
    public event Action? OnModeChanged;

    /// <inheritdoc/>
    public void UpdateMode(bool isDark)
    {
        if (_isDarkMode == isDark) return;
        _isDarkMode = isDark;
        OnModeChanged?.Invoke();
    }
}
