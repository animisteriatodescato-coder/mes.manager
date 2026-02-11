using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using MESManager.Web.Services;

namespace MESManager.Web.Components.Layout;

public partial class MainLayout : IDisposable
{
    [Inject]
    private PreferencesService PreferencesService { get; set; } = default!;
    
    [Inject]
    private IPageToolbarService PageToolbarService { get; set; } = default!;
    
    [Inject]
    private NavigationManager NavManager { get; set; } = default!;
    
    [Inject]
    private AppBarContentService AppBarContentService { get; set; } = default!;
    
    private bool _isDarkMode = true;
    private bool _drawerOpen = false;
    private string _currentCategory = string.Empty;
    private string _toolbarSearchText = string.Empty;
    private ErrorBoundary? _errorBoundary;
    
    private readonly MudTheme _theme = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#0a2f6e",        // Blu notte sfumato - ancora più scuro e professionale
            Secondary = "#424242",
            AppbarBackground = "#0a2f6e",  // AppBar con stesso blu notte
            Surface = "#f8f9fa",           // Superficie grigio chiaro
            Background = "#ffffff"         // Background bianco
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#90caf9",
            Secondary = "#e0e0e0",  // Grigio quasi bianco
            AppbarBackground = "#1e1e1e",
            TextPrimary = "rgba(255,255,255,0.95)",   // Testo principale quasi bianco
            TextSecondary = "rgba(255,255,255,0.85)", // Testo secondario grigio chiarissimo
            TextDisabled = "rgba(255,255,255,0.6)",   // Testo disabilitato più visibile
            ActionDisabled = "rgba(255,255,255,0.5)", // Azioni disabilitate più visibili
            Divider = "rgba(255,255,255,0.2)",        // Divisori più visibili
            Surface = "#2d2d2d",                       // Superficie leggermente più chiara
            Background = "#1a1a1a",                    // Background scuro
            DrawerBackground = "#1e1e1e"              // Drawer stesso colore appbar
        }
    };
    
    protected override async Task OnInitializedAsync()
    {
        // Carica il tema salvato
        var savedDarkMode = await PreferencesService.GetAsync<bool?>("isDarkMode");
        if (savedDarkMode.HasValue)
        {
            _isDarkMode = savedDarkMode.Value;
        }
        
        // Sottoscrivi ai cambiamenti di pagina
        PageToolbarService.OnPageChanged += OnPageChanged;
        
        // Sottoscrivi ai cambiamenti di navigazione per aggiornare il titolo
        NavManager.LocationChanged += OnLocationChanged;
        
        // Sottoscrivi ai cambiamenti dell'AppBar
        AppBarContentService.OnChange += OnAppBarContentChanged;
    }
    
    public void Dispose()
    {
        PageToolbarService.OnPageChanged -= OnPageChanged;
        NavManager.LocationChanged -= OnLocationChanged;
        AppBarContentService.OnChange -= OnAppBarContentChanged;
    }
    
    private void OnPageChanged()
    {
        InvokeAsync(StateHasChanged);
    }
    
    private void OnLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
    {
        InvokeAsync(StateHasChanged);
    }
    
    private void OnAppBarContentChanged()
    {
        InvokeAsync(StateHasChanged);
    }
    
    private bool HasToolbar()
    {
        // Escludi pagine che hanno toolbar personalizzate
        var currentPath = NavManager.ToBaseRelativePath(NavManager.Uri).ToLower();
        if (currentPath.Contains("plc-realtime") || 
            currentPath.Contains("plc-storico"))
        {
            return false;
        }
        
        var pageKey = PageToolbarService.GetCurrentPageKey();
        return !string.IsNullOrEmpty(pageKey);
    }
    
    private bool IsHomePage()
    {
        var currentPath = NavManager.ToBaseRelativePath(NavManager.Uri).ToLower();
        return string.IsNullOrEmpty(currentPath) || currentPath == "/";
    }
    
    private bool IsPlcRealtimePage()
    {
        try
        {
            var currentPath = NavManager.ToBaseRelativePath(NavManager.Uri).ToLower();
            if (!currentPath.Contains("plc-realtime"))
            {
                return false;
            }
            
            var pageKey = PageToolbarService.GetCurrentPageKey();
            var page = PageToolbarService.GetActivePage("plc-realtime");
            return pageKey == "plc-realtime" && page != null;
        }
        catch
        {
            return false;
        }
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
    
    // Metodi specifici per PLC Realtime - RIMOSSI: sync e autorefresh ora sono solo in /sync/macchine
    
    #pragma warning disable ASP0006
    private RenderFragment RenderPlcRealtimeToolbar() => builder =>
    {
        try
        {
            var page = PageToolbarService.GetActivePage("plc-realtime") as dynamic;
            if (page == null) return;
            
            int seq = 0;
            
            // Campo di ricerca
            builder.OpenComponent<MudTextField<string>>(seq++);
            builder.AddAttribute(seq++, "Value", (string)page.SearchText);
            builder.AddAttribute(seq++, "ValueChanged", EventCallback.Factory.Create<string>(this, async (value) =>
            {
                _toolbarSearchText = value;
                await OnToolbarSearch(value);
            }));
            builder.AddAttribute(seq++, "Placeholder", "Cerca...");
            builder.AddAttribute(seq++, "Variant", Variant.Outlined);
            builder.AddAttribute(seq++, "Margin", Margin.Dense);
            builder.AddAttribute(seq++, "Style", "width: 200px; background-color: white; color: black;");
            builder.AddAttribute(seq++, "Immediate", true);
            builder.AddAttribute(seq++, "DebounceInterval", 280);
            builder.AddAttribute(seq++, "OnDebounceIntervalElapsed", EventCallback.Factory.Create<string>(this, OnToolbarSearch));
            builder.CloseComponent();
            
            // Pulsante Colonne
            builder.OpenComponent<MudButton>(seq++);
            builder.AddAttribute(seq++, "Variant", Variant.Text);
            builder.AddAttribute(seq++, "StartIcon", Icons.Material.Filled.ViewColumn);
            builder.AddAttribute(seq++, "OnClick", EventCallback.Factory.Create(this, OnToolbarToggleColumns));
            builder.AddAttribute(seq++, "Color", Color.Inherit);
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(b2 => b2.AddContent(0, "Colonne")));
            builder.CloseComponent();
            
            // Pulsante Reset
            builder.OpenComponent<MudButton>(seq++);
            builder.AddAttribute(seq++, "Variant", Variant.Text);
            builder.AddAttribute(seq++, "StartIcon", Icons.Material.Filled.Refresh);
            builder.AddAttribute(seq++, "OnClick", EventCallback.Factory.Create(this, OnToolbarReset));
            builder.AddAttribute(seq++, "Color", Color.Inherit);
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(b2 => b2.AddContent(0, "Reset")));
            builder.CloseComponent();
            
            // Pulsante Impostazioni
            builder.OpenComponent<MudButton>(seq++);
            builder.AddAttribute(seq++, "Variant", Variant.Text);
            builder.AddAttribute(seq++, "StartIcon", Icons.Material.Filled.Settings);
            builder.AddAttribute(seq++, "OnClick", EventCallback.Factory.Create(this, OnToolbarToggleSettings));
            builder.AddAttribute(seq++, "Color", Color.Inherit);
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(b2 => b2.AddContent(0, "Impostazioni")));
            builder.CloseComponent();
        }
        catch (ObjectDisposedException)
        {
            // Pagina disposta durante il render, ignora silenziosamente
            return;
        }
        catch
        {
            // Error rendering PlcRealtime toolbar - silently ignore
        }
    };
    #pragma warning restore ASP0006
    
    private async Task ToggleTheme()
    {
        _isDarkMode = !_isDarkMode;
        // Salva la preferenza
        await PreferencesService.SetAsync("isDarkMode", _isDarkMode);
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
                new("Foto", "/cataloghi/foto")
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
