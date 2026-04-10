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
    public const string Current = "1.65.0";

    /// <summary>
    /// Versione con prefisso 'v' per display UI
    /// </summary>
    public const string Display = "v" + Current; // v1.65.0 Multi-lotto preventivi: fino a 4 lotti per preventivo con calcolo prezzo/pz per lotto
}