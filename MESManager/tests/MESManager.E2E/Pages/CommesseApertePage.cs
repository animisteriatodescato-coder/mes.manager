using Microsoft.Playwright;

namespace MESManager.E2E.Pages;

/// <summary>
/// Page Object per Commesse Aperte (con auto-scheduler v1.31).
/// </summary>
public class CommesseApertePage : BasePage
{
    // Selettori data-testid
    private ILocator PageMarker => GetByTestId("page-commesse-aperte");
    private ILocator Grid => GetByTestId("grid-commesse");
    private ILocator BtnCaricaSuGantt => GetByTestId("btn-carica-su-gantt");
    private ILocator BtnRefresh => GetByTestId("btn-refresh");
    private ILocator StatusMessage => GetByTestId("status-message");

    public CommesseApertePage(IPage page, string baseUrl) 
        : base(page, baseUrl, "CommesseApertePage")
    {
    }

    public override async Task<bool> IsPageLoadedAsync()
    {
        try
        {
            await WaitForVisible(PageMarker, 10000);
            await WaitForVisible(Grid, 10000);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Attende caricamento griglia (AG-Grid).
    /// </summary>
    public async Task WaitForGridReady()
    {
        // Attende che AG-Grid sia montata
        await Page.WaitForSelectorAsync(".ag-root", new PageWaitForSelectorOptions { Timeout = 30000 });
        await Page.WaitForSelectorAsync(".ag-body-viewport", new PageWaitForSelectorOptions { Timeout = 30000 });
    }

    /// <summary>
    /// Seleziona commessa per numero lavoro (checkbox AG-Grid).
    /// </summary>
    public async Task SelectCommessa(string numeroLavoro)
    {
        var row = Page.Locator($".ag-row:has-text('{numeroLavoro}')");
        await WaitForVisible(row);
        
        var checkbox = row.Locator(".ag-selection-checkbox");
        await ClickAsync(checkbox);
    }

    /// <summary>
    /// Seleziona multiple commesse.
    /// </summary>
    public async Task SelectCommesse(params string[] numeriLavoro)
    {
        foreach (var numero in numeriLavoro)
        {
            await SelectCommessa(numero);
        }
    }

    /// <summary>
    /// Click su "🚀 Carica su Gantt" (auto-scheduler v1.31).
    /// </summary>
    public async Task ClickCaricaSuGantt()
    {
        await ClickAsync(BtnCaricaSuGantt);
        
        // Attende messaggio di conferma o errore
        await WaitForVisible(StatusMessage, 10000);
    }

    /// <summary>
    /// Verifica presenza bottone auto-scheduler.
    /// </summary>
    public async Task<bool> IsAutoSchedulerButtonVisibleAsync()
    {
        return await BtnCaricaSuGantt.IsVisibleAsync();
    }

    /// <summary>
    /// Ottiene testo messaggio di stato.
    /// </summary>
    public async Task<string> GetStatusMessageAsync()
    {
        if (await StatusMessage.IsVisibleAsync())
        {
            return await StatusMessage.TextContentAsync() ?? string.Empty;
        }
        return string.Empty;
    }

    /// <summary>
    /// Ottiene numero righe visibili in griglia.
    /// </summary>
    public async Task<int> GetGridRowCount()
    {
        var rows = Page.Locator(".ag-center-cols-container .ag-row");
        return await rows.CountAsync();
    }

    /// <summary>
    /// Click su refresh per ricaricare dati.
    /// </summary>
    public async Task ClickRefresh()
    {
        await ClickAsync(BtnRefresh);
        await WaitForPageLoad();
    }

    /// <summary>
    /// Visual regression check su griglia commesse.
    /// </summary>
    public async Task AssertGridMatches(string screenshotName = "commesse-aperte-grid")
    {
        await AssertVisuallyMatches(screenshotName, Grid);
    }
}
