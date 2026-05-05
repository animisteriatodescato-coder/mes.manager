using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.JSInterop;
using MudBlazor;
using MESManager.Application.Interfaces;
using MESManager.Application.Services;
using MESManager.Infrastructure.Entities;
using MESManager.Web.Constants;
using MESManager.Web.Services;

namespace MESManager.Web.Components.Layout;

public partial class MainLayout : IDisposable
{
    [CascadingParameter]
    private Task<AuthenticationState>? AuthStateTask { get; set; }

    [Inject]
    private PreferencesService PreferencesService { get; set; } = default!;
    
    [Inject]
    private IPageToolbarService PageToolbarService { get; set; } = default!;
    
    [Inject]
    private NavigationManager NavManager { get; set; } = default!;
    
    [Inject]
    private AppBarContentService AppBarContentService { get; set; } = default!;

    [Inject]
    private AppSettingsService AppSettingsService { get; set; } = default!;

    [Inject]
    private UserThemeService UserThemeService { get; set; } = default!;

    /// <summary>
    /// Servizio dark mode iniettabile. Sincronizzato con _isDarkMode a ogni cambiamento.
    /// I componenti figli iniettano questo servizio per reagire al cambio di tema.
    /// </summary>
    [Inject]
    private IThemeModeService ThemeModeService { get; set; } = default!;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    [Inject]
    private ThemeCssService ThemeCssService { get; set; } = default!;

    [Inject]
    private UserManager<ApplicationUser> UserManager { get; set; } = default!;

    [Inject]
    private CurrentUserService CurrentUserService { get; set; } = default!;

    [Inject]
    private INonConformitaService NcService { get; set; } = default!;

    private bool _isDarkMode = false;
    private bool _drawerOpen = false;
    private bool _aiPanelOpen = false;
    private int _ncAperteCount = 0;
    /// <summary>True se l'utente ha SOLO ruolo Visualizzazione (nessun ruolo write). Propagato come CascadingValue alle pagine figlie.</summary>
    private bool _isReadOnly = false;
    /// <summary>True se l'utente ha ruolo Admin. Propagato come CascadingValue alle pagine figlie per funzioni riservate (es. Imposta Default Globale).</summary>
    private bool _isAdmin = false;
    private string _currentCategory = string.Empty;

    // Nav group expanded state — max 2 aperte contemporaneamente (auto-collapse)
    private bool _expandedProg = true;
    private bool _expandedProd = false;
    private bool _expandedCat  = false;
    private bool _expandedMan  = false;
    private bool _expandedSync = false;
    private bool _expandedStat = false;
    private bool _expandedImp  = false;
    private readonly List<string> _openGroups = new() { "prog" };

    private void OnNavGroupToggled(string groupId, bool expanded)
    {
        // Sincronizza lo stato locale con l'azione dell'utente
        switch (groupId)
        {
            case "prog": _expandedProg = expanded; break;
            case "prod": _expandedProd = expanded; break;
            case "cat":  _expandedCat  = expanded; break;
            case "man":  _expandedMan  = expanded; break;
            case "sync": _expandedSync = expanded; break;
            case "stat": _expandedStat = expanded; break;
            case "imp":  _expandedImp  = expanded; break;
        }

        if (expanded)
        {
            if (!_openGroups.Contains(groupId)) _openGroups.Add(groupId);
            while (_openGroups.Count > 2)
            {
                var toClose = _openGroups[0];
                _openGroups.RemoveAt(0);
                switch (toClose)
                {
                    case "prog": _expandedProg = false; break;
                    case "prod": _expandedProd = false; break;
                    case "cat":  _expandedCat  = false; break;
                    case "man":  _expandedMan  = false; break;
                    case "sync": _expandedSync = false; break;
                    case "stat": _expandedStat = false; break;
                    case "imp":  _expandedImp  = false; break;
                }
            }
        }
        else
        {
            _openGroups.Remove(groupId);
        }
    }

    private void ToggleAiPanel() => _aiPanelOpen = !_aiPanelOpen;
    private ErrorBoundary? _errorBoundary;

    // Tema dinamico — costruito dai colori in AppSettings.
    // NON readonly: viene ricostruito quando l'utente cambia la palette in ImpostazioniGenerali.
    private MudTheme _theme = new();
    
    protected override async Task OnInitializedAsync()
    {
        // 🔒 Verifica autenticazione — redirect alla pagina di login se non autenticato
        if (AuthStateTask != null)
        {
            var authState = await AuthStateTask;
            if (authState.User.Identity?.IsAuthenticated != true)
            {
                var pathAndQuery = new Uri(NavManager.Uri).PathAndQuery;
                NavManager.NavigateTo(
                    $"/Account/Login?ReturnUrl={Uri.EscapeDataString(pathAndQuery)}",
                    forceLoad: true);
                return;
            }

            // Popola CurrentUserService con i dati dell'utente autenticato
            var userId = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                var appUser = await UserManager.FindByIdAsync(userId);
                CurrentUserService.SetUser(userId, appUser?.Nome ?? appUser?.UserName, appUser?.Colore);
                if (appUser != null)
                {
                    var roles = await UserManager.GetRolesAsync(appUser);
                    _isReadOnly = roles.Contains("Visualizzazione") &&
                                  !roles.Any(r => r is "Admin" or "Produzione" or "Manutenzione" or "Ufficio");
                    _isAdmin = roles.Contains("Admin");
                }
            }
        }

        // Costruisce il tema dai colori salvati in AppSettings
        var settings = AppSettingsService.GetSettings();
        _theme = BuildThemeFromSettings(settings);
        _isDarkMode = settings.ThemeIsDarkMode;

        // Retrocompatibilità: se ThemeIsDarkMode è ancora false (default) ma l'utente
        // aveva salvato la preferenza nel vecchio sistema, usa quella.
        if (!settings.ThemeIsDarkMode)
        {
            var savedDarkMode = await PreferencesService.GetAsync<bool?>("isDarkMode");
            if (savedDarkMode.HasValue)
            {
                _isDarkMode = savedDarkMode.Value;
            }
        }

        // Sincronizza il servizio iniettabile con la modalità iniziale
        ThemeModeService.UpdateMode(_isDarkMode);

        // Ascolta i cambiamenti delle impostazioni globali e del tema per-utente
        AppSettingsService.OnSettingsChanged += OnAppSettingsChanged;
        UserThemeService.OnUserThemeChanged += OnUserThemeChanged;

        // Sottoscrivi ai cambiamenti di pagina
        PageToolbarService.OnPageChanged += OnPageChanged;
        
        // Sottoscrivi ai cambiamenti di navigazione per aggiornare il titolo
        NavManager.LocationChanged += OnLocationChanged;
        
        // Sottoscrivi ai cambiamenti dell'AppBar
        AppBarContentService.OnChange += OnAppBarContentChanged;
    }
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Applica subito le CSS vars via JS (live update da primo render)
            var initSettings = UserThemeService.GetEffectiveSettings();
            await ThemeCssService.ApplyAsync(JS, initSettings, _isDarkMode);

            // Carica il tema personale dell'utente (richiede JS — non disponibile prima)
            await UserThemeService.LoadUserThemeAsync();
            // Se l'utente ha un tema personale, ricostruisci il tema con quei valori
            if (UserThemeService.HasUserTheme)
            {
                var userSettings = UserThemeService.GetEffectiveSettings();
                _theme = BuildThemeFromSettings(userSettings);
                _isDarkMode = userSettings.ThemeIsDarkMode;
                ThemeModeService.UpdateMode(_isDarkMode);
                await ThemeCssService.ApplyAsync(JS, userSettings, _isDarkMode);
                await InvokeAsync(StateHasChanged);
            }

            // Carica conteggio NC aperte per badge menu (non bloccante)
            try
            {
                var ncAperte = await NcService.GetAperteAsync();
                _ncAperteCount = ncAperte.Count;
                if (_ncAperteCount > 0)
                    await InvokeAsync(StateHasChanged);
            }
            catch { /* badge NC non bloccante */ }
        }
    }

    public void Dispose()
    {
        AppSettingsService.OnSettingsChanged -= OnAppSettingsChanged;
        UserThemeService.OnUserThemeChanged -= OnUserThemeChanged;
        PageToolbarService.OnPageChanged -= OnPageChanged;
        NavManager.LocationChanged -= OnLocationChanged;
        AppBarContentService.OnChange -= OnAppBarContentChanged;
    }

    private void Logout()
    {
        NavManager.NavigateTo("/Account/Logout", forceLoad: true);
    }

    private void OnAppSettingsChanged()
    {
        var settings = UserThemeService.GetEffectiveSettings();
        _theme = BuildThemeFromSettings(settings);
        _isDarkMode = settings.ThemeIsDarkMode;
        ThemeModeService.UpdateMode(_isDarkMode);
        InvokeAsync(async () =>
        {
            await ThemeCssService.ApplyAsync(JS, settings, _isDarkMode);
            StateHasChanged();
        });
    }

    private void OnUserThemeChanged()
    {
        InvokeAsync(async () =>
        {
            // Se non è ancora caricato (utente appena cambiato), ricarica dal DB
            if (!UserThemeService.IsLoaded)
                await UserThemeService.LoadUserThemeAsync();

            var settings = UserThemeService.GetEffectiveSettings();
            _theme = BuildThemeFromSettings(settings);
            _isDarkMode = settings.ThemeIsDarkMode;
            ThemeModeService.UpdateMode(_isDarkMode);
            await ThemeCssService.ApplyAsync(JS, settings, _isDarkMode);
            StateHasChanged();
        });
    }

    /// <summary>
    /// Costruisce un MudTheme dai colori salvati in AppSettings.
    /// PUNTO CENTRALIZZATO per la creazione del tema — non costruire MudTheme altrove.
    /// </summary>
    private static MudTheme BuildThemeFromSettings(AppSettings settings)
    {
        var primary   = settings.ThemePrimaryColor;
        var secondary = settings.ThemeSecondaryColor;
        var accent    = settings.ThemeAccentColor;
        var textOnPrimary = settings.ThemeTextOnPrimary;

        // In dark palette, Primary is lightened → compute its contrast text accordingly
        var darkPrimary = LightenHex(primary, 0.35f);
        var textOnDarkPrimary = AppSettingsService.ComputeTextOnBackground(darkPrimary);

        return new MudTheme
        {
            PaletteLight = new PaletteLight
            {
                Primary              = primary,
                Secondary            = secondary,
                Tertiary             = accent,
                AppbarBackground     = primary,
                AppbarText           = textOnPrimary,       // Testo AppBar — calcolato da luminanza
                PrimaryContrastText  = textOnPrimary,       // Testo bottoni Filled Primary / Chip Primary
                Surface              = "#f8f9fa",
                Background           = "#ffffff"
            },
            PaletteDark = new PaletteDark
            {
                Primary              = darkPrimary,
                Secondary            = secondary,
                AppbarBackground     = "#1e1e1e",
                AppbarText           = "rgba(255,255,255,0.95)",
                PrimaryContrastText  = textOnDarkPrimary,   // Testo bottoni Filled Primary in dark mode
                TextPrimary          = "rgba(255,255,255,0.95)",
                TextSecondary        = "rgba(255,255,255,0.85)",
                TextDisabled         = "rgba(255,255,255,0.6)",
                ActionDisabled       = "rgba(255,255,255,0.5)",
                Divider              = "rgba(255,255,255,0.2)",
                Surface              = "#2d2d2d",
                Background           = "#1a1a1a",
                DrawerBackground     = "#1e1e1e"
            }
        };
    }

    /// <summary>
    /// Schiarisce un colore hex per adattarlo alla dark palette
    /// (aumenta la luminosità in HSL fino a targetL).
    /// </summary>
    private static string LightenHex(string hex, float targetL)
    {
        try
        {
            hex = hex.TrimStart('#');
            if (hex.Length != 6) return hex.Insert(0, "#");
            byte r = Convert.ToByte(hex[..2], 16);
            byte g = Convert.ToByte(hex[2..4], 16);
            byte b = Convert.ToByte(hex[4..6], 16);

            float rf = r / 255f, gf = g / 255f, bf = b / 255f;
            float max = MathF.Max(rf, MathF.Max(gf, bf));
            float min = MathF.Min(rf, MathF.Min(gf, bf));
            float delta = max - min;
            float l = (max + min) / 2f;
            float s = delta < 0.001f ? 0f : delta / (1f - MathF.Abs(2f * l - 1f));
            float h = 0f;
            if (delta > 0.001f)
            {
                if (max == rf)      h = 60f * (((gf - bf) / delta) % 6f);
                else if (max == gf) h = 60f * (((bf - rf) / delta) + 2f);
                else                h = 60f * (((rf - gf) / delta) + 4f);
            }
            if (h < 0) h += 360f;

            // Forza luminosità target (max 0.85 per non sfiorare il bianco)
            l = MathF.Min(targetL + 0.35f, 0.85f);

            float c = (1f - MathF.Abs(2f * l - 1f)) * s;
            float x = c * (1f - MathF.Abs((h / 60f) % 2f - 1f));
            float m = l - c / 2f;
            float ro, go, bo;
            if      (h < 60)  { ro = c; go = x; bo = 0; }
            else if (h < 120) { ro = x; go = c; bo = 0; }
            else if (h < 180) { ro = 0; go = c; bo = x; }
            else if (h < 240) { ro = 0; go = x; bo = c; }
            else if (h < 300) { ro = x; go = 0; bo = c; }
            else              { ro = c; go = 0; bo = x; }

            int ri = Math.Clamp((int)MathF.Round((ro + m) * 255f), 0, 255);
            int gi = Math.Clamp((int)MathF.Round((go + m) * 255f), 0, 255);
            int bi2 = Math.Clamp((int)MathF.Round((bo + m) * 255f), 0, 255);
            return $"#{ri:X2}{gi:X2}{bi2:X2}";
        }
        catch
        {
            return hex.StartsWith('#') ? hex : $"#{hex}";
        }
    }
    
    private void OnPageChanged()
    {
        InvokeAsync(StateHasChanged);
    }
    
    private void OnLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
    {
        // Ripristina AppBar visibile via JS (rimuove mes-appbar-hidden dal body)
        // Il drawer resta aperto: l'utente lo chiude esplicitamente con il pulsante menu.
        InvokeAsync(async () =>
        {
            StateHasChanged();
            try { await JS.InvokeVoidAsync("mesMobile.showAppBar"); } catch { /* JS non ancora pronto */ }
        });
    }
    
    private void OnAppBarContentChanged()
    {
        InvokeAsync(StateHasChanged);
    }
    
    private bool IsHomePage()
    {
        var currentPath = NavManager.ToBaseRelativePath(NavManager.Uri).ToLower();
        return string.IsNullOrEmpty(currentPath) || currentPath == "/";
    }

    private string GetPageTitle()
    {
        // Usa sempre l'URL corrente per determinare il titolo
        var currentPath = NavManager.ToBaseRelativePath(NavManager.Uri).ToLower();
        return currentPath switch
        {
            "" or "/" => "Todescato MES",
            
            // Produzione
            "produzione/dashboard" => "Dashboard Produzione",
            "produzione/plc-realtime" => "PLC Realtime",
            "produzione/plc-storico" => "PLC Storico",
            "produzione/incollaggio" => "Incollaggio",
            
            // Programma
            "programma/gantt-macchine" => "Gantt Macchine",
            "programma/commesse-aperte" => "Commesse Aperte",
            "programma/programma-macchine" => "Programma Macchine",
            
            // Cataloghi
            "cataloghi/commesse" => "Catalogo Commesse",
            "cataloghi/anime" => "Catalogo Anime",
            "cataloghi/articoli" => "Catalogo Articoli",
            "cataloghi/clienti" => "Catalogo Clienti",
            "cataloghi/ricette" => "Catalogo Ricette",
            "cataloghi/foto" => "Catalogo Foto",
            "cataloghi/non-conformita" => "Non Conformità",
            
            // Manutenzioni
            "manutenzioni/alert" => "Alert Manutenzioni",
            "manutenzioni/catalogo" => "Catalogo Manutenzioni",
            
            // Sync
            "sync/gantt" => "Sync Gantt",
            "sync/mago" => "Sync Mago",
            "sync/macchine" => "Sync Macchine",
            "sync/google" => "Sync Google",
            
            // Statistiche
            "statistiche/produzione" => "Statistiche Produzione",
            "statistiche/ordini" => "Statistiche Ordini",
            "statistiche/plc-storico" => "Analisi PLC Storico",
            
            // Impostazioni
            "impostazioni/gantt" => "Gantt Macchine",
            "impostazioni/calendario" => "Calendario",
            "impostazioni/tabelle" => "Tabelle",
            "impostazioni/utenti" => "Utenti",
            "impostazioni/operatori" => "Operatori",
            "impostazioni/generali" => "Impostazioni Generali",
            "impostazioni/accessi" => "Gestione Accessi",
            
            _ => "MESManager"
        };
    }
    
    // Metodi per gestire gli eventi dalla AppBar per le pagine con toolbar
    private async Task OnToolbarSearch(string value)
    {
        var pageKey = PageToolbarService.GetCurrentPageKey();
        if (pageKey != null)
        {
            var page = PageToolbarService.GetActivePage(pageKey) as dynamic;
            if (page != null)
            {
                await page.OnSearchDebounced_Public(value);
            }
        }
    }
    
    private async Task OnToolbarToggleColumns()
    {
        var pageKey = PageToolbarService.GetCurrentPageKey();
        if (pageKey != null)
        {
            var page = PageToolbarService.GetActivePage(pageKey) as dynamic;
            if (page != null)
            {
                await page.ToggleColumnPanel_Public();
            }
        }
    }
    
    private async Task OnToolbarReset()
    {
        var pageKey = PageToolbarService.GetCurrentPageKey();
        if (pageKey != null)
        {
            var page = PageToolbarService.GetActivePage(pageKey) as dynamic;
            if (page != null)
            {
                await page.ResetGrid_Public();
            }
        }
    }
    
    private async Task OnToolbarExport()
    {
        var pageKey = PageToolbarService.GetCurrentPageKey();
        if (pageKey != null)
        {
            var page = PageToolbarService.GetActivePage(pageKey) as dynamic;
            if (page != null)
            {
                await page.ExportCsv_Public();
            }
        }
    }
    
    private void OnToolbarToggleSettings()
    {
        var pageKey = PageToolbarService.GetCurrentPageKey();
        if (pageKey != null)
        {
            var page = PageToolbarService.GetActivePage(pageKey) as dynamic;
            if (page != null)
            {
                page.ToggleSettings_Public();
            }
        }
    }
    
    private async Task ToggleTheme()
    {
        _isDarkMode = !_isDarkMode;
        // Propaga ai componenti figli che usano IThemeModeService
        ThemeModeService.UpdateMode(_isDarkMode);
        await PreferencesService.SetAsync("isDarkMode", _isDarkMode);

        // Aggiorna ThemeIsDarkMode nelle impostazioni EFFETTIVE (utente o globali).
        // CRITICO: se l'utente ha preferenze personali, bisogna aggiornare quelle —
        // altrimenti OnAppSettingsChanged (triggerato da SaveSettingsAsync globale) leggerebbe
        // le impostazioni utente ancora col vecchio ThemeIsDarkMode e revertivrebbe il toggle.
        var effectiveSettings = UserThemeService.GetEffectiveSettings();
        effectiveSettings.ThemeIsDarkMode = _isDarkMode;

        if (UserThemeService.HasUserTheme)
            await UserThemeService.SaveUserThemeAsync(effectiveSettings);
        else
        {
            var globalSettings = AppSettingsService.GetSettings();
            globalSettings.ThemeIsDarkMode = _isDarkMode;
            await AppSettingsService.SaveSettingsAsync(globalSettings);
        }

        // Aggiorna CSS vars live senza aspettare il re-render Blazor
        await ThemeCssService.ApplyAsync(JS, effectiveSettings, _isDarkMode);
    }

    private void ToggleDrawer()
    {
        _drawerOpen = !_drawerOpen;
    }
    
    private void SetCategory(string category)
    {
        _currentCategory = category;
    }
    
    private List<MenuItem> GetMenuItems(string category)
    {
        return category switch
        {
            "Produzione" => new List<MenuItem>
            {
                new("Dashboard", "/produzione/dashboard"),
                new("PLC Realtime", "/produzione/plc-realtime"),
                new("PLC Storico", "/produzione/plc-storico"),
                new("Incollaggio", "/produzione/incollaggio")
            },
            "Programma" => new List<MenuItem>
            {
                new("Gantt Macchine", "/programma/gantt-macchine"),
                new("Commesse Aperte", "/programma/commesse-aperte"),
                new("Programma Macchine", "/programma/programma-macchine")
            },
            "Cataloghi" => new List<MenuItem>
            {
                new("Commesse", "/cataloghi/commesse"),
                new("Articoli", "/cataloghi/articoli"),
                new("Clienti", "/cataloghi/clienti"),
                new("Ricette", "/cataloghi/ricette"),
                new("Anime", "/cataloghi/anime"),
                new("Foto", "/cataloghi/foto"),
                new("Preventivi", "/preventivi"),
                new("Non Conformità", "/cataloghi/non-conformita")
            },
            "Manutenzioni" => new List<MenuItem>
            {
                new("Alert", "/manutenzioni/alert"),
                new("Catalogo", "/manutenzioni/catalogo")
            },
            "Sync" => new List<MenuItem>
            {
                new("Mago", "/sync/mago"),
                new("Macchine", "/sync/macchine"),
                new("Google", "/sync/google")
            },
            "Statistiche" => new List<MenuItem>
            {
                new("Produzione", "/statistiche/produzione"),
                new("Ordini", "/statistiche/ordini")
            },
            "Impostazioni" => new List<MenuItem>
            {
                new("Calendario", "/impostazioni/calendario"),
                new("Utenti", "/impostazioni/utenti"),
                new("Generali", "/impostazioni/generali")
            },
            _ => new List<MenuItem>()
        };
    }
    
    private class MenuItem
    {
        public string Title { get; set; }
        public string Url { get; set; }
        
        public MenuItem(string title, string url)
        {
            Title = title;
            Url = url;
        }
    }
    
    private void RecoverFromError()
    {
        _errorBoundary?.Recover();
    }
}
