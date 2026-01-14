using Microsoft.AspNetCore.Components;
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
    }
    
    public void Dispose()
    {
        PageToolbarService.OnPageChanged -= OnPageChanged;
        NavManager.LocationChanged -= OnLocationChanged;
    }
    
    private void OnPageChanged()
    {
        InvokeAsync(StateHasChanged);
    }
    
    private void OnLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
    {
        InvokeAsync(StateHasChanged);
    }
    
    private bool HasToolbar()
    {
        var pageKey = PageToolbarService.GetCurrentPageKey();
        return !string.IsNullOrEmpty(pageKey);
    }
    
    private string GetPageTitle()
    {
        // Prima prova a usare il pageKey se disponibile
        var pageKey = PageToolbarService.GetCurrentPageKey();
        if (!string.IsNullOrEmpty(pageKey))
        {
            return pageKey switch
            {
                "articoli" => "Catalogo Articoli",
                "commesse" => "Catalogo Commesse",
                "clienti" => "Catalogo Clienti",
                "ricette" => "Catalogo Ricette",
                "plc-realtime" => "PLC Realtime",
                "plc-storico" => "PLC Storico",
                _ => "MESManager"
            };
        }
        
        // Altrimenti usa l'URL corrente
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
            "sync/mago" => "Sync Mago",
            "sync/macchine" => "Sync Macchine",
            "sync/google" => "Sync Google",
            
            // Statistiche
            "statistiche/produzione" => "Statistiche Produzione",
            "statistiche/ordini" => "Statistiche Ordini",
            
            // Impostazioni
            "impostazioni/calendario" => "Calendario",
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
