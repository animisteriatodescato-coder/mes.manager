namespace MESManager.Web.Constants;

/// <summary>
/// Sistema centralizzato per l'accesso claim-based a TUTTE le pagine.
/// I ruoli con accesso completo (BaseRoles per gruppo) passano sempre.
/// Il ruolo Visualizzazione accede solo alle pagine per cui ha un claim "pagina=&lt;valore&gt;".
/// </summary>
public static class PaginaPolicy
{
    /// <summary>Tipo claim usato in AspNetUserClaims per controllo pagina.</summary>
    public const string ClaimType = "pagina";

    // ── Produzione (backward compat: questi const sono usati nelle pagine .razor) ──
    public const string Dashboard    = "pagina-dashboard";
    public const string PlcRealtime  = "pagina-plc-realtime";
    public const string PlcStorico   = "pagina-plc-storico";
    public const string GanttStorico = "pagina-gantt-storico";
    public const string Incollaggio  = "pagina-incollaggio";

    // ── Programma ──
    public const string ProgrammaMacchine = "pagina-programma-macchine";
    public const string CommesseAperte    = "pagina-commesse-aperte";
    public const string GanttMacchine     = "pagina-gantt-macchine";

    // ── Cataloghi ──
    public const string CatCommesse   = "pagina-cat-commesse";
    public const string CatAnime      = "pagina-cat-anime";
    public const string CatArticoli   = "pagina-cat-articoli";
    public const string CatClienti    = "pagina-cat-clienti";
    public const string CatRicette    = "pagina-cat-ricette";
    public const string CatPreventivi = "pagina-cat-preventivi";

    // ── Manutenzioni ──
    public const string ManutenzioniAlert        = "pagina-manutenzioni-alert";
    public const string ManutenzioniCatalogo     = "pagina-manutenzioni-catalogo";
    public const string ManutenzioniGriglia      = "pagina-manutenzioni-griglia";
    public const string ManutenzioniImpostazioni = "pagina-manutenzioni-impostazioni";
    public const string ManutenzioniCasse        = "pagina-manutenzioni-casse";

    // ── Statistiche ──
    public const string StatProduzione  = "pagina-stat-produzione";
    public const string StatOrdini      = "pagina-stat-ordini";
    public const string StatPlcStorico  = "pagina-stat-plc-storico";
    public const string StatOperatori   = "pagina-stat-operatori";

    /// <summary>
    /// Gruppi di pagine con i ruoli base che hanno accesso completo.
    /// Fonte di verità unica per NavMenu, GestioneAccessi e registrazione policy.
    /// </summary>
    public static readonly IReadOnlyList<GruppoPageInfo> Gruppi =
    [
        new("Programma", ["Admin", "Produzione", "Ufficio", "Manutenzione"],
        [
            new("programma-macchine", "Programma",  "/programma/programma-macchine", ProgrammaMacchine),
            new("commesse-aperte",    "Commesse",   "/programma/commesse-aperte",    CommesseAperte),
            new("gantt-macchine",     "Gantt",      "/programma/gantt-macchine",     GanttMacchine),
        ]),
        new("Produzione", ["Admin", "Produzione", "Manutenzione"],
        [
            new("dashboard",     "Dashboard",     "/produzione/dashboard",     Dashboard),
            new("plc-realtime",  "PLC Realtime",  "/produzione/plc-realtime",  PlcRealtime),
            new("plc-storico",   "PLC Storico",   "/produzione/plc-storico",   PlcStorico),
            new("gantt-storico", "Gantt Storico", "/produzione/gantt-storico", GanttStorico),
            new("incollaggio",   "Incollaggio",   "/produzione/incollaggio",   Incollaggio),
        ]),
        new("Cataloghi", ["Admin", "Produzione", "Ufficio", "Manutenzione"],
        [
            new("cat-commesse",   "Commesse",   "/cataloghi/commesse",  CatCommesse),
            new("cat-anime",      "Anime",       "/cataloghi/anime",     CatAnime),
            new("cat-articoli",   "Articoli",    "/cataloghi/articoli",  CatArticoli),
            new("cat-clienti",    "Clienti",     "/cataloghi/clienti",   CatClienti),
            new("cat-ricette",    "Ricette",     "/cataloghi/ricette",   CatRicette),
            new("cat-preventivi", "Preventivi",  "/preventivi",         CatPreventivi),
        ]),
        new("Manutenzioni", ["Admin", "Manutenzione"],
        [
            new("manutenzioni-alert",        "Alert",                  "/manutenzioni/alert",         ManutenzioniAlert),
            new("manutenzioni-catalogo",     "Catalogo",               "/manutenzioni/catalogo",      ManutenzioniCatalogo),
            new("manutenzioni-griglia",      "Manutenzione Giornaliera", "/manutenzioni/griglia",     ManutenzioniGriglia),
            new("manutenzioni-casse",        "Casse d'Anima",           "/manutenzioni/casse",        ManutenzioniCasse),
            new("manutenzioni-impostazioni", "Impostazioni",            "/manutenzioni/impostazioni", ManutenzioniImpostazioni),
        ]),
        new("Statistiche", ["Admin", "Produzione", "Ufficio", "Manutenzione"],
        [
            new("stat-produzione",  "Produzione",  "/statistiche/produzione",  StatProduzione),
            new("stat-ordini",      "Ordini",       "/statistiche/ordini",      StatOrdini),
            new("stat-plc-storico", "PLC Storico",  "/statistiche/plc-storico", StatPlcStorico),
            new("stat-operatori",   "Operatori",    "/statistiche/operatori",   StatOperatori),
        ]),
    ];

    /// <summary>Backward compat: lista pagine Produzione.</summary>
    public static IReadOnlyList<PaginaInfo> PagineProduzione =>
        Gruppi.First(g => g.Nome == "Produzione").Pagine;

    /// <summary>Tutte le pagine di tutti i gruppi.</summary>
    public static IEnumerable<PaginaInfo> TutteLePagine =>
        Gruppi.SelectMany(g => g.Pagine);

    /// <summary>
    /// Restituisce true se l'utente può vedere la pagina specificata.
    /// Visualizzazione → solo se ha il claim corrispondente; tutti gli altri ruoli → sempre sì.
    /// </summary>
    public static bool CanSee(System.Security.Claims.ClaimsPrincipal user, string claimValue) =>
        !user.IsInRole("Visualizzazione") ||
        user.HasClaim(ClaimType, claimValue);
}

/// <param name="ClaimValue">Valore del claim (es. "dashboard").</param>
/// <param name="Label">Nome display nell'UI.</param>
/// <param name="Url">Route relativo della pagina.</param>
/// <param name="PolicyName">Nome policy ASP.NET Core (es. "pagina-dashboard").</param>
public record PaginaInfo(string ClaimValue, string Label, string Url, string PolicyName);

/// <param name="Nome">Nome del gruppo (es. "Produzione").</param>
/// <param name="BaseRoles">Ruoli con accesso completo indipendente dai claim.</param>
/// <param name="Pagine">Lista pagine del gruppo.</param>
public record GruppoPageInfo(string Nome, string[] BaseRoles, IReadOnlyList<PaginaInfo> Pagine);
