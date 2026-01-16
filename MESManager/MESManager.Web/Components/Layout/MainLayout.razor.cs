using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
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
    
    private readonly MudTheme _theme = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#1976d2",
            Secondary = "#424242",
            AppbarBackground = "#1976d2"
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#90caf9",
            Secondary = "#757575",
            AppbarBackground = "#1e1e1e"
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
            currentPath.Contains("plc-storico") ||
            currentPath.Contains("mes-stato"))
        {
            return false;
        }
        
        var pageKey = PageToolbarService.GetCurrentPageKey();
        return !string.IsNullOrEmpty(pageKey);
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
            "" or "/" => "Dashboard",
            
            // Produzione
            "produzione/dashboard" => "Dashboard Produzione",
            "produzione/gantt-macchine" => "Gantt Macchine",
            "produzione/mes-stato" => "MES Stato Realtime",
            "produzione/plc-realtime" => "PLC Realtime",
            "produzione/plc-storico" => "PLC Storico",
            "produzione/incollaggio" => "Incollaggio",
            
            // Programma
            "programma/commesse-aperte" => "Commesse Aperte",
            "programma/programma-macchine" => "Programma Macchine",
            "programma/stampa" => "Stampa Programma",
            
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
            
            // Tabelle
            "tabelle/vernici" => "Tabella Vernici",
            "tabelle/sabbie" => "Tabella Sabbie",
            "tabelle/imballi" => "Tabella Imballi",
            "tabelle/operatori" => "Tabella Operatori",
            "tabelle/colle" => "Tabella Colle",
            
            // Sync
            "sync/gantt" => "Sync Gantt",
            "sync/mago" => "Sync Mago",
            "sync/macchine" => "Sync Macchine",
            "sync/google" => "Sync Google",
            
            // Statistiche
            "statistiche/produzione" => "Statistiche Produzione",
            "statistiche/ordini" => "Statistiche Ordini",
            
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
    
    // Metodi specifici per PLC Realtime
    private async Task OnPlcSyncMachines()
    {
        var page = PageToolbarService.GetActivePage("plc-realtime") as dynamic;
        if (page != null)
        {
            await page.SincronizzaMacchine_Public();
        }
    }
    
    private void OnPlcToggleAutoRefresh()
    {
        var page = PageToolbarService.GetActivePage("plc-realtime") as dynamic;
        if (page != null)
        {
            page.ToggleAutoRefresh_Public();
            StateHasChanged();
        }
    }
    
    #pragma warning disable ASP0006
    private RenderFragment RenderPlcRealtimeToolbar() => builder =>
    {
        try
        {
            var page = PageToolbarService.GetActivePage("plc-realtime") as dynamic;
            if (page == null) return;
            
            int seq = 0;
            
            // Pulsante Sincronizza Macchine
            builder.OpenComponent<MudButton>(seq++);
            builder.AddAttribute(seq++, "Variant", Variant.Filled);
            builder.AddAttribute(seq++, "Color", Color.Success);
            builder.AddAttribute(seq++, "StartIcon", Icons.Material.Filled.Sync);
            builder.AddAttribute(seq++, "OnClick", EventCallback.Factory.Create(this, OnPlcSyncMachines));
            builder.AddAttribute(seq++, "Disabled", (bool)page.IsSyncing);
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(b2 =>
            {
                int s2 = 0;
                if ((bool)page.IsSyncing)
                {
                    b2.OpenComponent<MudProgressCircular>(s2++);
                    b2.AddAttribute(s2++, "Size", Size.Small);
                    b2.AddAttribute(s2++, "Indeterminate", true);
                    b2.AddAttribute(s2++, "Class", "mr-2");
                    b2.CloseComponent();
                    
                    b2.OpenElement(s2++, "span");
                    b2.AddContent(s2++, "Sincronizzazione...");
                    b2.CloseElement();
                }
                else
                {
                    b2.OpenElement(s2++, "span");
                    b2.AddContent(s2++, "Sincronizza Macchine");
                    b2.CloseElement();
                }
            }));
            builder.CloseComponent();
            
            // Switch Auto-refresh
            builder.OpenComponent<MudSwitch<bool>>(seq++);
            builder.AddAttribute(seq++, "Checked", (bool)page.AutoRefreshEnabled);
            builder.AddAttribute(seq++, "CheckedChanged", EventCallback.Factory.Create<bool>(this, _ => OnPlcToggleAutoRefresh()));
            builder.AddAttribute(seq++, "Color", Color.Primary);
            builder.AddAttribute(seq++, "Label", "Auto-refresh");
            builder.CloseComponent();
            
            // Testo intervallo (se auto-refresh è attivo)
            if ((bool)page.AutoRefreshEnabled)
            {
                builder.OpenComponent<MudText>(seq++);
                builder.AddAttribute(seq++, "Typo", Typo.caption);
                builder.AddAttribute(seq++, "Class", "mr-2");
                builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(b2 =>
                {
                    b2.AddContent(0, $"Ogni {(int)page.RefreshInterval} s");
                }));
                builder.CloseComponent();
            }
            
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
        catch (Exception ex)
        {
            Console.WriteLine($"[MainLayout] Error rendering PlcRealtime toolbar: {ex.Message}");
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
                new("Gantt Macchine", "/produzione/gantt-macchine"),
                new("MES Stato", "/produzione/mes-stato"),
                new("PLC Realtime", "/produzione/plc-realtime"),
                new("PLC Storico", "/produzione/plc-storico"),
                new("Incollaggio", "/produzione/incollaggio")
            },
            "Programma" => new List<MenuItem>
            {
                new("Commesse Aperte", "/programma/commesse-aperte"),
                new("Programma Macchine", "/programma/programma-macchine"),
                new("Stampa", "/programma/stampa")
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
            "Tabelle" => new List<MenuItem>
            {
                new("Vernici", "/tabelle/vernici"),
                new("Sabbie", "/tabelle/sabbie"),
                new("Imballi", "/tabelle/imballi"),
                new("Operatori", "/tabelle/operatori"),
                new("Colle", "/tabelle/colle")
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
}
