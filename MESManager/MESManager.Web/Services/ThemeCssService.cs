using Microsoft.JSInterop;
using MESManager.Web.Constants;

namespace MESManager.Web.Services;

/// <summary>
/// Servizio Scoped che aggiorna le CSS custom properties (--mes-*) via JavaScript Interop.
/// Consente live preview senza Blazor re-render: chiama ApplyAsync() dopo ogni cambio di tema.
///
/// PUNTO CENTRALIZZATO: unica fonte di verità per la mappatura AppSettings → CSS vars.
/// Il blocco :root{} in MainLayout.razor fornisce i valori iniziali SSR;
/// questo servizio li aggiorna dinamicamente dopo il primo render.
///
/// Usato da:
///   - MainLayout.razor.cs → OnAfterRenderAsync, ToggleTheme, OnAppSettingsChanged
///   - ImpostazioniGenerali.razor → live preview ad ogni cambio colore nel draft
/// </summary>
public class ThemeCssService
{
    /// <summary>
    /// Applica tutte le CSS custom properties al document :root tramite JS setProperty.
    /// Sicuro solo in/dopo OnAfterRenderAsync.
    /// </summary>
    public async Task ApplyAsync(IJSRuntime js, AppSettings settings, bool isDarkMode)
    {
        var vars = BuildVars(settings, isDarkMode);
        await js.InvokeVoidAsync("mesTheme.apply", vars);
    }

    /// <summary>
    /// Costruisce il dizionario di tutte le CSS custom properties dell'app.
    /// Dipende SOLO da AppSettings e dall'attuale dark/light mode.
    /// </summary>
    public Dictionary<string, string> BuildVars(AppSettings s, bool isDarkMode)
    {
        double panelOp = s.ThemePanelOpacity;

        // Risolve colore effettivo AppBar: in dark mode prevale la variante dark (se impostata),
        // poi il colore light, poi il default del token. Zero duplicazione: stessa logica in MainLayout.razor.
        string appBarBg = (isDarkMode && !string.IsNullOrEmpty(s.ThemeAppBarBgColorDark))
            ? s.ThemeAppBarBgColorDark
            : (!string.IsNullOrEmpty(s.ThemeAppBarBgColor) ? s.ThemeAppBarBgColor : MesDesignTokens.AppBarBg(isDarkMode));

        // Risolve colore effettivo Drawer: stessa priorità, fallback su AppBar effettivo.
        string drawerBg = (isDarkMode && !string.IsNullOrEmpty(s.ThemeDrawerBgColorDark))
            ? s.ThemeDrawerBgColorDark
            : (!string.IsNullOrEmpty(s.ThemeDrawerBgColor) ? s.ThemeDrawerBgColor : appBarBg);

        // Sorgente tinting righe: cascade drawer → appbar.
        // Se entrambi sono acromatici (grigio, negro, default), righe neutrali (token fisso).
        // Il tinting si attiva SOLO quando un colore menu/appbar saturo è esplicitamente impostato.
        // Non viene mai usato ThemePrimaryColor: cambiare il Primary non deve cambiare le righe.
        string? rowTintColor = MesDesignTokens.IsSufficientlyChromatic(drawerBg) ? drawerBg
                             : MesDesignTokens.IsSufficientlyChromatic(appBarBg)  ? appBarBg
                             : null;

        string buttonColor = !string.IsNullOrEmpty(s.ThemeButtonColor)
            ? s.ThemeButtonColor
            : s.ThemePrimaryColor;
        string buttonText  = !string.IsNullOrEmpty(s.ThemeButtonTextColor)
            ? s.ThemeButtonTextColor
            : AppSettingsService.ComputeTextOnBackground(buttonColor);
        string navText     = string.IsNullOrEmpty(s.ThemeNavTextColor)
            ? MesDesignTokens.NavTextAuto(isDarkMode)
            : s.ThemeNavTextColor;

        return new Dictionary<string, string>
        {
            // ── Colori tema principali ────────────────────────────────────────
            [MesDesignTokens.CssVarTextOnPrimary]   = s.ThemeTextOnPrimary,
            [MesDesignTokens.CssVarPrimary]         = s.ThemePrimaryColor,
            [MesDesignTokens.CssVarPrimaryText]     = s.ThemePrimaryTextColor,
            [MesDesignTokens.CssVarNavText]         = navText,
            [MesDesignTokens.CssVarBgOpacity]       = s.ThemeBgOpacity.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
            [MesDesignTokens.CssVarPanelOpacity]    = s.ThemePanelOpacity.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
            [MesDesignTokens.CssVarAppBarBg]        = appBarBg,
            [MesDesignTokens.CssVarDrawerBg]        = drawerBg,
            [MesDesignTokens.CssVarButtonColor]     = buttonColor,
            [MesDesignTokens.CssVarButtonText]      = buttonText,

            // ── Tabelle / Griglie — tinting da drawer/appbar saturo; se acromatici usa token fisso ─────
            [MesDesignTokens.CssVarRowOdd]          = rowTintColor is null ? MesDesignTokens.RowOdd(isDarkMode)  : MesDesignTokens.RowOddFromColor(rowTintColor, isDarkMode),
            [MesDesignTokens.CssVarRowEven]         = rowTintColor is null ? MesDesignTokens.RowEven(isDarkMode) : MesDesignTokens.RowEvenFromColor(rowTintColor, isDarkMode),
            [MesDesignTokens.CssVarRowText]         = MesDesignTokens.RowText(isDarkMode),
            [MesDesignTokens.CssVarGridHeaderBg]    = drawerBg,
            [MesDesignTokens.CssVarGlassGrid]       = MesDesignTokens.GlassGrid(isDarkMode, panelOp),
            [MesDesignTokens.CssVarTextPrimary]     = MesDesignTokens.RowText(isDarkMode),
            [MesDesignTokens.CssVarBgSecondary]     = isDarkMode ? MesDesignTokens.AgPanelBgDark   : "#F5F5F5",
            [MesDesignTokens.CssVarBgSurface]       = isDarkMode ? MesDesignTokens.AgInputBgDark   : "#FFFFFF",
            [MesDesignTokens.CssVarGridBorder]      = isDarkMode ? MesDesignTokens.AgInputBorderDark : "#e0e0e0",
            [MesDesignTokens.CssVarAgPanelBg]       = isDarkMode ? MesDesignTokens.AgPanelBgDark   : "#FFFFFF",
            [MesDesignTokens.CssVarAgInputBg]       = isDarkMode ? MesDesignTokens.AgInputBgDark   : "#FFFFFF",
            [MesDesignTokens.CssVarAgInputBorder]   = isDarkMode ? MesDesignTokens.AgInputBorderDark : "#e0e0e0",

            // ── AG Grid celle condizionali (cellClassRules) ───────────────────
            ["--mes-readonly-cell-bg"]   = isDarkMode ? "#1a1a2e" : "#f5f5f5",
            ["--mes-stato-aperta-bg"]    = isDarkMode ? "#1b3a22" : "#e8f5e9",
            ["--mes-stato-aperta-color"] = isDarkMode ? "#80c783" : "#2e7d32",
            ["--mes-stato-chiusa-bg"]    = isDarkMode ? "#3a1828" : "#fce4ec",
            ["--mes-stato-chiusa-color"] = isDarkMode ? "#f48fb1" : "#c2185b",
            ["--mes-count-foto-bg"]      = isDarkMode ? "#1b3a22" : "#e8f5e9",
            ["--mes-count-foto-color"]   = isDarkMode ? "#80c783" : "#2e7d32",
            ["--mes-count-doc-bg"]       = isDarkMode ? "#0d2740" : "#e3f2fd",
            ["--mes-count-doc-color"]    = isDarkMode ? "#90caf9" : "#1565c0",

            // ── Glass effect + Dashboard machine card ─────────────────────────
            ["--mes-glass-panel"]              = MesDesignTokens.GlassPanel(isDarkMode, panelOp),
            ["--mes-machine-card-bg"]          = MesDesignTokens.MachineCardBg(isDarkMode),
            ["--mes-machine-card-text"]        = MesDesignTokens.MachineCardText(isDarkMode),
            ["--mes-machine-card-text-muted"]  = MesDesignTokens.MachineCardTextMuted(isDarkMode),
            ["--mes-machine-card-number"]      = MesDesignTokens.MachineCardNumber(isDarkMode),
        };
    }
}
