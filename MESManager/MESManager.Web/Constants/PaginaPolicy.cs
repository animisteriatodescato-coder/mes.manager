namespace MESManager.Web.Constants;

/// <summary>
/// Definizioni centralizzate per le policy di accesso alle pagine Produzione.
/// Ogni utente con ruolo Visualizzazione può accedere solo alle pagine
/// per cui ha un claim "pagina=&lt;valore&gt;" nel proprio profilo.
/// Admin / Produzione / Manutenzione hanno accesso completo a tutte le pagine.
/// </summary>
public static class PaginaPolicy
{
    /// <summary>Tipo claim usato in AspNetUserClaims per controllo pagina.</summary>
    public const string ClaimType = "pagina";

    // ── Nomi policy (usate in @attribute [Authorize(Policy = "...")] e AddAuthorization) ──
    public const string Dashboard    = "pagina-dashboard";
    public const string PlcRealtime  = "pagina-plc-realtime";
    public const string PlcStorico   = "pagina-plc-storico";
    public const string GanttStorico = "pagina-gantt-storico";
    public const string Incollaggio  = "pagina-incollaggio";

    /// <summary>
    /// Mappa: claim value → (Label display, URL relativo, Policy name).
    /// Fonte di verità unica: usata da NavMenu, GestioneAccessi e dal seed policy.
    /// </summary>
    public static readonly IReadOnlyList<PaginaInfo> PagineProduzione =
    [
        new("dashboard",     "Dashboard",     "/produzione/dashboard",     Dashboard),
        new("plc-realtime",  "PLC Realtime",  "/produzione/plc-realtime",  PlcRealtime),
        new("plc-storico",   "PLC Storico",   "/produzione/plc-storico",   PlcStorico),
        new("gantt-storico", "Gantt Storico", "/produzione/gantt-storico", GanttStorico),
        new("incollaggio",   "Incollaggio",   "/produzione/incollaggio",   Incollaggio),
    ];

    /// <summary>
    /// Restituisce true se l'utente può vedere la pagina specificata.
    /// Admin/Produzione/Manutenzione → sempre true.
    /// Visualizzazione → solo se ha il claim corrispondente.
    /// </summary>
    public static bool CanSee(System.Security.Claims.ClaimsPrincipal user, string claimValue) =>
        user.IsInRole("Admin") ||
        user.IsInRole("Produzione") ||
        user.IsInRole("Manutenzione") ||
        user.HasClaim(ClaimType, claimValue);
}

/// <param name="ClaimValue">Valore del claim (es. "dashboard").</param>
/// <param name="Label">Nome display nell'UI.</param>
/// <param name="Url">Route relativo della pagina.</param>
/// <param name="PolicyName">Nome policy ASP.NET Core (es. "pagina-dashboard").</param>
public record PaginaInfo(string ClaimValue, string Label, string Url, string PolicyName);
