namespace MESManager.Domain.Constants;

/// <summary>
/// Tabelle di lookup centralizzate per valori predefiniti dell'applicazione.
/// Queste tabelle sono usate per dropdown, mappatura codici e visualizzazione.
/// I valori vengono inizializzati con i default hardcoded e possono essere
/// aggiornati a runtime da TabelleService (persistenza su tabelle-config.json).
/// </summary>
public static class LookupTables
{
    /// <summary>
    /// Aggiorna i dizionari in-memory con i valori caricati da TabelleService.
    /// Chiamato all'avvio e ad ogni salvataggio dall'utente.
    /// </summary>
    public static void Aggiorna(
        Dictionary<string, string> colla,
        Dictionary<string, string> vernice,
        Dictionary<string, string> sabbia,
        Dictionary<string, string> imballo)
    {
        Colla.Clear();
        foreach (var kv in colla)   Colla[kv.Key]   = kv.Value;

        Vernice.Clear();
        foreach (var kv in vernice)  Vernice[kv.Key]  = kv.Value;

        Sabbia.Clear();
        foreach (var kv in sabbia)   Sabbia[kv.Key]   = kv.Value;

        Imballo.Clear();
        foreach (var kv in imballo)  Imballo[kv.Key]  = kv.Value;

        // Aggiorna anche ImballoInt (compatibilità legacy con AnimeService)
        ImballoInt.Clear();
        foreach (var kv in imballo)
        {
            if (int.TryParse(kv.Key, out var k))
                ImballoInt[k] = kv.Value;
        }
    }

    /// <summary>
    /// Tipi di colla disponibili
    /// </summary>
    public static Dictionary<string, string> Colla = new()
    {
        { "-1", "BIANCA" },
        { "-2", "A CALDO" },
        { "-3", "ROSSA S.G" }
    };

    /// <summary>
    /// Tipi di vernice disponibili
    /// </summary>
    public static Dictionary<string, string> Vernice = new()
    {
        { "-1", "" },
        { "-2", "YELLOW COVER" },
        { "-3", "CASTING COVER ZR" },
        { "-4", "CASTING COVER RK" },
        { "-5", "CASTINGCOVER 2001" },
        { "-6", "ARCOPAL 9030" },
        { "-7", "HYDRO COVER 22 Z" },
        { "-8", "FGR 55" }
    };

    /// <summary>
    /// Tipi di sabbia disponibili
    /// </summary>
    public static Dictionary<string, string> Sabbia = new()
    {
        { "", "(nessuna)" },
        { "310/60", "310/60" },
        { "33BD600X", "33BD600X" },
        { "360/10", "360/10" },
        { "44/10", "SCARANELLO" },
        { "C1/30D", "C1/30D" },
        { "CP35B", "CP35B" },
        { "NB30B", "NB30B" },
        { "OLIVINA", "OLIVINA" },
        { "UP30B", "UP30B" }
    };

    /// <summary>
    /// Tipi di imballo disponibili (chiave string per uniformità API)
    /// </summary>
    public static Dictionary<string, string> Imballo = new()
    {
        { "-1", "CASSA GRANDE" },
        { "-2", "CASSA PICCOLA" },
        { "-3", "CASSA LUNGA" },
        { "-4", "PIANALE EURO" },
        { "-5", "PIANALE QUADRATO" },
        { "-6", "CARRELLI A PIANI" },
        { "-7", "CARRELLI GRANDI" },
        { "-8", "SCATOLE" }
    };

    /// <summary>
    /// Tipi di imballo con chiave intera (per compatibilità legacy)
    /// </summary>
    public static Dictionary<int, string> ImballoInt = new()
    {
        { -1, "CASSA GRANDE" },
        { -2, "CASSA PICCOLA" },
        { -3, "CASSA LUNGA" },
        { -4, "PIANALE EURO" },
        { -5, "PIANALE QUADRATO" },
        { -6, "CARRELLI A PIANI" },
        { -7, "CARRELLI GRANDI" },
        { -8, "SCATOLE" }
    };

    /// <summary>
    /// Converte una lista lookup in formato DTO per API
    /// </summary>
    public static List<LookupItem> ToList(Dictionary<string, string> lookup)
    {
        return lookup.Select(kv => new LookupItem { Codice = kv.Key, Descrizione = kv.Value }).ToList();
    }
}

/// <summary>
/// Item generico per liste dropdown
/// </summary>
public class LookupItem
{
    public string Codice { get; set; } = string.Empty;
    public string Descrizione { get; set; } = string.Empty;
}
