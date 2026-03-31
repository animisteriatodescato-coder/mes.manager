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
    public const string Current = "1.60.37";

    /// <summary>
    /// Versione con prefisso 'v' per display UI
    /// </summary>
    public const string Display = "v" + Current; // v1.60.37  AnimeEditDialog: Ubicazione in riga 2, Codice/Descrizione allargati
}