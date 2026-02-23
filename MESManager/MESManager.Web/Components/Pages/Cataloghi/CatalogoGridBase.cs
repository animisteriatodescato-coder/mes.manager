using MESManager.Web.Models;
using MESManager.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;

namespace MESManager.Web.Components.Pages.Cataloghi;

/// <summary>
/// Base class condivisa per tutti i Catalog Grid (Articoli, Clienti, Commesse, Anime).
/// =====================================================================================
/// UN UNICO punto di modifica per:
///   - ApplyUiSettings (setUiVars → JS)
///   - SaveSettings / FixGridState / ResetToFixedState
///   - ToggleColumnPanel / ExportCsv
///   - UpdateGridStats (getStats → JS)
///   - Proprietà AppBar pubbliche (TotalRows, FilteredRows, ...)
///   - Pubblici _Public per chiamate dalla AppBar
///
/// Ogni pagina concreta definisce SOLO:
///   protected override string GridNamespace => "articoliGrid";
///   protected override string SettingsKey   => "articoli-grid";
///   protected override string PageKey       => "articoli";
/// </summary>
public abstract class CatalogoGridBase : ComponentBase, IAsyncDisposable
{
    [Inject] protected IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] protected PreferencesService PreferencesService { get; set; } = default!;
    [Inject] protected IPageToolbarService PageToolbarService { get; set; } = default!;

    // ── Identificatori per questa griglia ────────────────────────────────────
    /// <summary>Nome oggetto JS, es: "articoliGrid"</summary>
    protected abstract string GridNamespace { get; }

    /// <summary>Prefisso chiave localStorage, es: "articoli-grid" → "articoli-grid-settings"</summary>
    protected abstract string SettingsKey { get; }

    /// <summary>Chiave PageToolbarService, es: "articoli"</summary>
    protected abstract string PageKey { get; }

    // ── Stato condiviso ──────────────────────────────────────────────────────
    protected GridUiSettings settings = new();
    protected bool showSettings = false;
    protected string searchText = string.Empty;
    protected int totalRows = 0;
    protected int filteredRows = 0;
    protected int selectedRows = 0;
    protected DateTime lastUpdate = DateTime.MinValue;
    protected bool isLoading = false;
    protected string gridStatus = "Pronto";

    // ── UI Settings ──────────────────────────────────────────────────────────
    protected async Task ApplyUiSettings()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync($"{GridNamespace}.setUiVars",
                settings.FontSize,
                settings.RowHeight,
                settings.GetDensityPadding(),
                settings.Zebra,
                settings.GridLines);
            await SaveSettings();
            StateHasChanged();
        }
        catch { }
    }

    // ── Persist ──────────────────────────────────────────────────────────────
    protected async Task SaveSettings()
    {
        try
        {
            var state = await JSRuntime.InvokeAsync<string>($"{GridNamespace}.getState");
            settings.ColumnStateJson = state;
            await PreferencesService.SetAsync($"{SettingsKey}-settings", settings);
        }
        catch { }
    }

    protected async Task FixGridState()
    {
        try
        {
            var gridState = await JSRuntime.InvokeAsync<string>($"{GridNamespace}.getState");
            await PreferencesService.SetAsync($"{SettingsKey}-fixed-state", gridState);
            await PreferencesService.SetAsync($"{SettingsKey}-fixed-settings", JsonSerializer.Serialize(settings));
        }
        catch { }
    }

    protected async Task ResetToFixedState()
    {
        try
        {
            var fixedState      = await PreferencesService.GetAsync<string>($"{SettingsKey}-fixed-state");
            var fixedSettingsJson = await PreferencesService.GetAsync<string>($"{SettingsKey}-fixed-settings");

            if (!string.IsNullOrEmpty(fixedState))
                await JSRuntime.InvokeVoidAsync($"{GridNamespace}.setState", fixedState);
            else
                await JSRuntime.InvokeVoidAsync($"{GridNamespace}.resetState");

            settings = !string.IsNullOrEmpty(fixedSettingsJson)
                ? JsonSerializer.Deserialize<GridUiSettings>(fixedSettingsJson) ?? new()
                : new();

            await ApplyUiSettings();
            StateHasChanged();
        }
        catch { }
    }

    // ── Column Panel / Export ─────────────────────────────────────────────────
    protected async Task ToggleColumnPanel()
    {
        try { await JSRuntime.InvokeVoidAsync($"{GridNamespace}.toggleColumnPanel"); }
        catch { }
    }

    protected async Task ExportCsv()
    {
        try { await JSRuntime.InvokeVoidAsync($"{GridNamespace}.exportCsv"); }
        catch { }
    }

    // ── Stats ─────────────────────────────────────────────────────────────────
    protected async Task UpdateGridStats()
    {
        try
        {
            var stats = await JSRuntime.InvokeAsync<GridStats>($"{GridNamespace}.getStats");
            totalRows    = stats.Total;
            filteredRows = stats.Filtered;
            selectedRows = stats.Selected;
            lastUpdate   = DateTime.Now;
            StateHasChanged();
        }
        catch { }
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    /// <summary>Carica le impostazioni salvate. Da chiamare nell'OnInitializedAsync della pagina concreta.</summary>
    protected async Task LoadSavedSettings()
    {
        var saved = await PreferencesService.GetAsync<GridUiSettings>($"{SettingsKey}-settings");
        if (saved != null) settings = saved;
    }

    /// <summary>Init standard grid JS. Da chiamare nell'OnAfterRenderAsync dopo il Delay.</summary>
    protected async Task InitializeGridJs(object data, string? savedColumnState = null)
    {
        try
        {
            var exists = await JSRuntime.InvokeAsync<bool>("eval", $"typeof window.{GridNamespace} !== 'undefined'");
            if (!exists) return;
            await JSRuntime.InvokeVoidAsync($"{GridNamespace}.init", GridNamespace, data, savedColumnState ?? settings.ColumnStateJson);
            await ApplyUiSettings();
        }
        catch { }
    }

    public virtual async ValueTask DisposeAsync()
    {
        PageToolbarService.SetActivePage(PageKey, null);
        try { await SaveSettings(); } catch { }
    }

    // ── Metodi pubblici chiamati dalla AppBar ─────────────────────────────────
    public async Task ToggleColumnPanel_Public() => await ToggleColumnPanel();
    public async Task ResetGrid_Public()          => await ResetToFixedState();
    public async Task ExportCsv_Public()          => await ExportCsv();
    public void ToggleSettings_Public()           { showSettings = !showSettings; StateHasChanged(); }

    /// <summary>Chiamato dal template Razor (OnDebounceIntervalElapsed) e da AppBar.</summary>
    public async Task OnSearchDebounced(string value)
    {
        searchText = value;
        try
        {
            await JSRuntime.InvokeVoidAsync($"{GridNamespace}.setQuickFilter", value);
            await UpdateGridStats();
        }
        catch { }
    }

    public async Task OnSearchDebounced_Public(string value) => await OnSearchDebounced(value);

    // Proprietà read-only per AppBar
    public string   SearchText  => searchText;
    public int      TotalRows   => totalRows;
    public int      FilteredRows => filteredRows;
    public int      SelectedRows => selectedRows;
    public DateTime LastUpdate  => lastUpdate;
    public bool     IsLoading   => isLoading;
    public string   GridStatus  => gridStatus;
}
