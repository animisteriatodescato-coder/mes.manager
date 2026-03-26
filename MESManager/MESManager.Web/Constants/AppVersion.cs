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
    public const string Current = "1.60.7";

    /// <summary>
    /// Versione con prefisso 'v' per display UI
    /// </summary>
    public const string Display = "v" + Current; // v1.60.7  Cascade tinting solo su drawer/appbar (no primary), ColorTokenPicker Hint param, label ImpostazioniGenerali, BIBBIA v4.1
}