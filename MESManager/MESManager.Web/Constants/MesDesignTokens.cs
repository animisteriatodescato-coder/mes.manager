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

    /// <summary>Colore riga dispari (odd) per MudTable e AG Grid.</summary>
    public static string RowOdd(bool dark)  => dark ? "#262636" : "#F0F0F8";

    /// <summary>Colore riga pari (even) per MudTable e AG Grid.</summary>
    public static string RowEven(bool dark) => dark ? "#303042" : "#FAFAFD";

    /// <summary>Colore testo celle per MudTable e AG Grid.</summary>
    public static string RowText(bool dark) => dark ? "#E6E6F0" : "#1E1E28";

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
}
