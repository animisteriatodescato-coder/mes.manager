using Microsoft.Playwright;

namespace MESManager.E2E.Pages;

/// <summary>
/// Page Object per Programma Macchine (read-only grid).
/// </summary>
public class ProgrammaMacchinePage : BasePage
{
    // Selettori data-testid
    private ILocator PageMarker => GetByTestId("page-programma-macchine");
    private ILocator Grid => GetByTestId("grid-programma");
    private ILocator BtnRefresh => GetByTestId("btn-refresh");
    private ILocator StatusInfo => GetByTestId("status-info");

    public ProgrammaMacchinePage(IPage page, string baseUrl) 
        : base(page, baseUrl, "ProgrammaMacchinePage")
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
        await Page.WaitForSelectorAsync(".ag-root", new PageWaitForSelectorOptions { Timeout = 30000 });
        await Page.WaitForSelectorAsync(".ag-body-viewport", new PageWaitForSelectorOptions { Timeout = 30000 });
    }

    /// <summary>
    /// Ottiene numero righe programma.
    /// </summary>
    public async Task<int> GetProgramRowCount()
    {
        var rows = Page.Locator(".ag-center-cols-container .ag-row");
        return await rows.CountAsync();
    }

    /// <summary>
    /// Verifica che commessa sia presente nel programma.
    /// </summary>
    public async Task<bool> IsCommessaProgrammataAsync(string numeroLavoro)
    {
        var row = Page.Locator($".ag-row:has-text('{numeroLavoro}')");
        return await row.IsVisibleAsync();
    }

    /// <summary>
    /// Ottiene numero macchina per commessa specifica.
    /// </summary>
    public async Task<string?> GetNumeroMacchina(string numeroLavoro)
    {
        var row = Page.Locator($".ag-row:has-text('{numeroLavoro}')");
        if (!await row.IsVisibleAsync())
            return null;

        // Assume colonna "numeroMacchina" sia visibile
        var cell = row.Locator("[col-id='numeroMacchina']");
        return await cell.TextContentAsync();
    }

    /// <summary>
    /// Click su "Aggiorna" per ricaricare i dati.
    /// </summary>
    public async Task ClickRefresh()
    {
        await ClickAsync(BtnRefresh);
        await WaitForPageLoad();
    }

    /// <summary>
    /// Verifica messaggio info programma (es. "Programma aggiornato").
    /// </summary>
    public async Task<string> GetStatusInfoAsync()
    {
        if (await StatusInfo.IsVisibleAsync())
        {
            return await StatusInfo.TextContentAsync() ?? string.Empty;
        }
        return string.Empty;
    }

    /// <summary>
    /// Visual regression check su griglia programma.
    /// </summary>
    public async Task AssertGridMatches(string screenshotName = "programma-macchine-grid")
    {
        await AssertVisuallyMatches(screenshotName, Grid);
    }
}
