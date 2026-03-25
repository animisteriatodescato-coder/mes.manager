namespace MESManager.Web.Constants;

/// <summary>
/// Token di design centralizzati per MESManager.
///
/// UNICA FONTE DI VERITÀ per tutti i colori, sfondi e valori grafici hardcoded.
///
/// REGOLA INVIOLABILE: qualsiasi colore hardcoded nell'app DEVE provenire da qui.
/// Per aggiungere un nuovo token → aggiungi il metodo/costante qui, non hardcodare nei file.
/// Per modificare un colore → modificalo qui, si propaga automaticamente ovunque.
///
/// Usato da:
///   - MainLayout.razor → genera CSS vars nel blocco :root{}
///   - app.css           → referenzia le vars con var(--mes-*)
///   - BuildThemeFromSettings() in MainLayout.razor.cs
/// </summary>
public static class MesDesignTokens
{
    // ── Righe tabelle / griglie ───────────────────────────────────────────────

    /// <summary>Colore riga dispari (odd) fisso — fallback quando non è disponibile un colore tema.</summary>
    public static string RowOdd(bool dark)  => dark ? "#262636" : "#F0F0F8";

    /// <summary>Colore riga pari (even) fisso — fallback quando non è disponibile un colore tema.</summary>
    public static string RowEven(bool dark) => dark ? "#303042" : "#FAFAFD";

    /// <summary>Colore testo celle per MudTable e AG Grid.</summary>
    public static string RowText(bool dark) => dark ? "#E6E6F0" : "#1E1E28";

    // Soglia minima di saturazione perché derivare la hue abbia senso visivamente.
    // Sotto questa soglia (nero, grigio, bianco) non esiste un colore dominante:
    // si usa il fallback fisso invece di produrre una strana tinta rosso/neutra.
    private const float RowTintSaturationThreshold = 0.12f;

    /// <summary>
    /// Colore riga dispari calcolato dal colore del menu laterale/AppBar.
    /// Estrae la hue e produce una tinta chiara (light) o scura (dark) vivida ma discreta.
    /// Se hexColor non è un hex valido o il colore è quasi acromatico (grigio/nero/bianco),
    /// usa il fallback fisso RowOdd(dark) per evitare tinte indesiderate.
    /// </summary>
    public static string RowOddFromColor(string hexColor, bool dark)
    {
        if (!TryParseHex(hexColor, out byte r, out byte g, out byte b))
            return RowOdd(dark);
        HexToHsl(r, g, b, out float h, out float s, out _);
        if (s < RowTintSaturationThreshold)
            return RowOdd(dark);
        // Light: tinta visibile ma chiara (lightness alta, saturazione moderata)
        // Dark:  tinta visibile ma scura (lightness bassa, saturazione medio-alta)
        float targetS = Math.Min(s * 0.6f + 0.08f, dark ? 0.45f : 0.35f);
        float targetL = dark ? 0.20f : 0.93f;
        return HslToHex(h, targetS, targetL);
    }

    /// <summary>
    /// Colore riga pari calcolato dal colore del menu laterale/AppBar.
    /// Più neutro di RowOddFromColor per creare l'effetto zebra: lightness ancora più alta
    /// (light) o più bassa (dark) con saturazione dimezzata rispetto alla riga odd.
    /// Se hexColor non è un hex valido o il colore è quasi acromatico, usa il fallback fisso.
    /// </summary>
    public static string RowEvenFromColor(string hexColor, bool dark)
    {
        if (!TryParseHex(hexColor, out byte r, out byte g, out byte b))
            return RowEven(dark);
        HexToHsl(r, g, b, out float h, out float s, out _);
        if (s < RowTintSaturationThreshold)
            return RowEven(dark);
        float targetS = Math.Min(s * 0.25f + 0.03f, dark ? 0.18f : 0.12f);
        float targetL = dark ? 0.13f : 0.98f;
        return HslToHex(h, targetS, targetL);
    }

    /// <summary>
    /// Controlla se la stringa è un colore hex valido (#RGB, #RRGGBB o #RRGGBBAA).
    /// Usare per decidere la sorgente del tinting senza out parameters.
    /// </summary>
    public static bool IsValidHexColor(string hex)
    {
        if (string.IsNullOrEmpty(hex)) return false;
        if (hex.StartsWith("var(") || hex.StartsWith("rgba") || hex.StartsWith("rgb(")) return false;
        return hex.StartsWith('#') && (hex.Length == 4 || hex.Length == 7 || hex.Length == 9);
    }

    /// <summary>
    /// Tenta il parsing di una stringa colore hex (#RGB, #RRGGBB o #RRGGBBAA) in componenti RGB.
    /// Ritorna false se la stringa non è un hex valido (es. "var(--mes-primary)", rgba, ecc.).
    /// Supporta hex a 8 caratteri (#RRGGBBAA): la componente alpha viene ignorata.
    /// CENTRALIZZATO: unico punto di parsing hex colore in MesDesignTokens.
    /// </summary>
    public static bool TryParseHex(string hex, out byte r, out byte g, out byte b)
    {
        r = g = b = 0;
        if (string.IsNullOrEmpty(hex)) return false;
        if (hex.StartsWith("var(") || hex.StartsWith("rgba") || hex.StartsWith("rgb(")) return false;
        hex = hex.TrimStart('#');
        if (hex.Length == 3) hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";
        if (hex.Length == 8) hex = hex[..6]; // strip alpha (#RRGGBBAA → #RRGGBB)
        if (hex.Length != 6) return false;
        try
        {
            r = Convert.ToByte(hex[..2], 16);
            g = Convert.ToByte(hex[2..4], 16);
            b = Convert.ToByte(hex[4..6], 16);
            return true;
        }
        catch { return false; }
    }

    /// <summary>Converte RGB (0-255) in HSL (H:0-360, S e L:0-1).</summary>
    private static void HexToHsl(byte r, byte g, byte b, out float h, out float s, out float l)
    {
        float rf = r / 255f, gf = g / 255f, bf = b / 255f;
        float max = MathF.Max(rf, MathF.Max(gf, bf));
        float min = MathF.Min(rf, MathF.Min(gf, bf));
        float delta = max - min;
        l = (max + min) / 2f;
        s = delta < 0.001f ? 0f : delta / (1f - MathF.Abs(2f * l - 1f));
        h = 0f;
        if (delta > 0.001f)
        {
            if      (max == rf) h = 60f * (((gf - bf) / delta) % 6f);
            else if (max == gf) h = 60f * (((bf - rf) / delta) + 2f);
            else                h = 60f * (((rf - gf) / delta) + 4f);
            if (h < 0) h += 360f;
        }
    }

    /// <summary>Converte HSL (H:0-360, S e L:0-1) in stringa hex #RRGGBB.</summary>
    private static string HslToHex(float h, float s, float l)
    {
        float c = (1f - MathF.Abs(2f * l - 1f)) * s;
        float x = c * (1f - MathF.Abs((h / 60f) % 2f - 1f));
        float m = l - c / 2f;
        float rf, gf, bf;
        if      (h < 60f)  { rf = c; gf = x; bf = 0; }
        else if (h < 120f) { rf = x; gf = c; bf = 0; }
        else if (h < 180f) { rf = 0; gf = c; bf = x; }
        else if (h < 240f) { rf = 0; gf = x; bf = c; }
        else if (h < 300f) { rf = x; gf = 0; bf = c; }
        else               { rf = c; gf = 0; bf = x; }
        byte rb = (byte)Math.Round((rf + m) * 255f);
        byte gb = (byte)Math.Round((gf + m) * 255f);
        byte bb = (byte)Math.Round((bf + m) * 255f);
        return $"#{rb:X2}{gb:X2}{bb:X2}";
    }

    // ── Header griglia ─────────────────────────────────────────────────────────

    /// <summary>Colore sfondo header colonne (MudTable thead + AG Grid header row).</summary>
    public static string GridHeaderBg(bool dark) => dark ? "#14182A" : "#1E2846";

    // ── Pannelli glass (con sfondo attivo) ─────────────────────────────────────

    /// <summary>Colore sfondo pannelli (MudPaper, MudCard) con effetto vetro abilitato.</summary>
    public static string GlassPanel(bool dark, double opacity)
        => dark
            ? FormattableString.Invariant($"rgba(18,18,28,{opacity:F2})")
            : FormattableString.Invariant($"rgba(255,255,255,{opacity:F2})");

    /// <summary>Colore sfondo griglie con effetto vetro abilitato.</summary>
    public static string GlassGrid(bool dark, double opacity)
        => dark
            ? FormattableString.Invariant($"rgba(28,28,40,{opacity:F2})")
            : FormattableString.Invariant($"rgba(248,248,252,{opacity:F2})");

    // ── AppBar / Drawer ────────────────────────────────────────────────────────

    /// <summary>Colore sfondo AppBar e Drawer. Dark: quasi-nero. Light: Primary dinamico.</summary>
    public static string AppBarBg(bool dark) => dark ? "#0E101C" : "var(--mes-primary)";

    // ── Testo nav (fallback automatico se l'utente non personalizza) ───────────

    /// <summary>Colore testo menu laterale e AppBar se non impostato dall'utente.</summary>
    public static string NavTextAuto(bool dark) => dark ? "#FFFFFF" : "#1A1A1A";

    // ── Machine card (Dashboard Produzione) ────────────────────────────────────

    /// <summary>Gradiente sfondo machine-card (Dashboard). Indipendente dallo sfondo globale.</summary>
    public static string MachineCardBg(bool dark)
        => dark
            ? "radial-gradient(ellipse at center, rgba(62,62,82,0.97) 0%, rgba(40,40,58,0.98) 55%, rgba(22,22,36,0.99) 100%)"
            : "radial-gradient(ellipse at center, #ffffff 30%, #e2e6ea 80%, #cfd5db 100%)";

    /// <summary>Colore testo principale machine-card.</summary>
    public static string MachineCardText(bool dark)
        => dark ? "rgba(230,230,240,0.95)" : "#1a1a1a";

    /// <summary>Colore testo secondario/muted machine-card (section-title).</summary>
    public static string MachineCardTextMuted(bool dark)
        => dark ? "rgba(200,200,215,0.80)" : "#333333";

    /// <summary>Colore numero macchina (machine-number) in machine-card.</summary>
    public static string MachineCardNumber(bool dark)
        => dark ? "rgba(255,255,255,0.98)" : "var(--mes-primary-text, var(--mud-palette-primary))";

    // ── Caption versione nel Drawer ────────────────────────────────────────────

    /// <summary>Colore testo caption versione app nel Drawer Header.</summary>
    public static string VersionTextColor(bool dark)
        => dark ? "rgba(230,230,230,0.5)" : "rgba(0,0,0,0.3)";

    // ── AG Grid Column/Filter panel — valori dark ──────────────────────────────
    // Referenziati come costanti perché app.css usa .mud-theme-dark (non C# runtime).
    // Queste costanti devono corrispondere ai valori che MainLayout inietta come CSS vars.

    /// <summary>Sfondo pannello colonne/filtro AG Grid in dark mode.</summary>
    public const string AgPanelBgDark     = "#1E2030";

    /// <summary>Sfondo input field AG Grid in dark mode.</summary>
    public const string AgInputBgDark     = "#262636";

    /// <summary>Bordo input field AG Grid in dark mode.</summary>
    public const string AgInputBorderDark = "#4a4a6a";

    // ── Nomi CSS custom properties (evita typo negli usi) ─────────────────────
    // Usare queste costanti se si referenzia un var() da codice C#.

    public const string CssVarRowOdd          = "--mes-row-odd";
    public const string CssVarRowEven         = "--mes-row-even";
    public const string CssVarRowText         = "--mes-row-text";
    public const string CssVarGridHeaderBg    = "--mes-grid-header-bg";
    public const string CssVarGlassGrid       = "--mes-glass-grid";
    public const string CssVarPrimary         = "--mes-primary";
    public const string CssVarPrimaryText     = "--mes-primary-text";
    public const string CssVarTextOnPrimary   = "--mes-text-on-primary";
    public const string CssVarNavText         = "--mes-nav-text";
    public const string CssVarBgOpacity       = "--mes-bg-opacity";
    public const string CssVarPanelOpacity    = "--mes-panel-opacity";
    public const string CssVarTextPrimary     = "--mes-text-primary";   // alias RowText — referenziato da pagine griglia
    public const string CssVarBgSecondary     = "--mes-bg-secondary";   // sfondo panel AG Grid
    public const string CssVarBgSurface       = "--mes-bg-surface";     // sfondo input AG Grid
    public const string CssVarGridBorder      = "--mes-grid-border";    // bordo input AG Grid
    public const string CssVarAgPanelBg       = "--mes-ag-panel-bg";
    public const string CssVarAgInputBg       = "--mes-ag-input-bg";
    public const string CssVarAgInputBorder   = "--mes-ag-input-border";
    public const string CssVarAppBarBg        = "--mes-appbar-bg";
    public const string CssVarDrawerBg        = "--mes-drawer-bg";
    public const string CssVarButtonColor     = "--mes-button-color";
    public const string CssVarButtonText      = "--mes-button-text";

    // ── PLC Machine State Colors ──────────────────────────────────────────────────
    // UNICA fonte di verità per i colori degli stati macchina PLC.
    // Usati da PlcController (PlcStatoColore()) per popolare PlcGanttSegmentoDto.Colore.
    // I CSS in DashboardProduzione.razor DEVONO corrispondere a questi valori.

    public const string PlcStatoAutomatico  = "#4CAF50";  // Verde       — AUTOMATICO / CICLO
    public const string PlcStatoAllarme     = "#FF9800";  // Arancione  — ALLARME
    public const string PlcStatoEmergenza   = "#F44336";  // Rosso      — EMERGENZA
    public const string PlcStatoManuale     = "#616161";  // Grigio sc. — MANUALE
    public const string PlcStatoSetup       = "#2196F3";  // Blu        — SETUP / ATTREZZAGGIO
    public const string PlcStatoNoConn      = "#9E9E9E";  // Grigio     — NON CONNESSA / default
    public const string PlcStatoSconosciuto = "#607D8B";  // Grigio-blu — Sconosciuto

    /// <summary>
    /// Mappa uno stato macchina PLC al relativo colore esadecimale.
    /// Usato da PlcController per arricchire PlcGanttSegmentoDto prima della serializzazione JSON.
    /// </summary>
    public static string PlcStatoColore(string? stato)
    {
        if (string.IsNullOrWhiteSpace(stato)) return PlcStatoNoConn;
        if (stato.Contains("AUTOMATICO",   StringComparison.OrdinalIgnoreCase) ||
            stato.Contains("CICLO",        StringComparison.OrdinalIgnoreCase)) return PlcStatoAutomatico;
        if (stato.Contains("EMERGENZA",    StringComparison.OrdinalIgnoreCase)) return PlcStatoEmergenza;
        if (stato.Contains("ALLARME",      StringComparison.OrdinalIgnoreCase)) return PlcStatoAllarme;
        if (stato.Contains("MANUALE",      StringComparison.OrdinalIgnoreCase)) return PlcStatoManuale;
        if (stato.Contains("SETUP",        StringComparison.OrdinalIgnoreCase) ||
            stato.Contains("ATTREZZAGGIO", StringComparison.OrdinalIgnoreCase)) return PlcStatoSetup;
        if (stato.Contains("NON CONNESSA", StringComparison.OrdinalIgnoreCase) ||
            stato.Contains("OFFLINE",      StringComparison.OrdinalIgnoreCase)) return PlcStatoNoConn;
        return PlcStatoSconosciuto;
    }
}
