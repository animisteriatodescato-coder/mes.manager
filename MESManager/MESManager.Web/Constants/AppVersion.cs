namespace MESManager.Web.Constants;

/// <summary>
/// Versione centralizzata dell'applicazione.
/// IMPORTANTE: Aggiornare questo file ad ogni release/deploy.
/// </summary>
public static class AppVersion
{
    /// <summary>
    /// Versione corrente dell'applicazione (formato: major.minor.patch)
    /// </summary>
    public const string Current = "1.60.34";

    /// <summary>
    /// Versione con prefisso 'v' per display UI
    /// </summary>
    public const string Display = "v" + Current; // v1.60.33  Fix dark mode DEFINITIVO: CSS vars AG Grid + cascade order (app.css dopo ag-grid CDN)
}