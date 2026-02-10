using Microsoft.Playwright;

namespace MESManager.E2E.Pages;

/// <summary>
/// Page Object per Gantt Macchine (Syncfusion Gantt).
/// </summary>
public class GanttMacchinePage : BasePage
{
    // Selettori data-testid
    private ILocator PageMarker => GetByTestId("page-gantt-macchine");
    private ILocator GanttContainer => GetByTestId("gantt-container");
    private ILocator BtnRicalcolaTutto => GetByTestId("btn-ricalcola-tutto");
    private ILocator BtnSalvaImpostazioni => GetByTestId("btn-salva-impostazioni");

    public GanttMacchinePage(IPage page, string baseUrl) 
        : base(page, baseUrl, "GanttMacchinePage")
    {
    }

    public override async Task<bool> IsPageLoadedAsync()
    {
        try
        {
            await WaitForVisible(PageMarker, 10000);
            await WaitForVisible(GanttContainer, 15000);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Attende caricamento Syncfusion Gantt.
    /// </summary>
    public async Task WaitForGanttReady()
    {
        // Vis-Timeline crea .vis-timeline dentro #gantt-chart quando pronto
        await Page.WaitForSelectorAsync("#gantt-chart .vis-timeline",
            new PageWaitForSelectorOptions { Timeout = 30000 });
        
        // Attende anche animazioni
        await Page.WaitForTimeoutAsync(1000);
    }

    /// <summary>
    /// Click su "Ricalcola Tutto".
    /// </summary>
    public async Task ClickRicalcolaTutto()
    {
        await ClickAsync(BtnRicalcolaTutto);
        
        // Attende ricalcolo (Gantt refresh)
        await WaitForGanttReady();
    }

    /// <summary>
    /// Click su "Salva Impostazioni".
    /// </summary>
    public async Task ClickSalvaImpostazioni()
    {
        await ClickAsync(BtnSalvaImpostazioni);
        
        // Attende conferma
        await Page.WaitForTimeoutAsync(1000);
    }

    /// <summary>
    /// Drag & drop commessa nel Gantt (advanced).
    /// </summary>
    public async Task DragCommessaToPosition(string numeroLavoro, int xOffset, int yOffset)
    {
        var taskBar = Page.Locator($".vis-item:has-text('{numeroLavoro}')");
        await WaitForVisible(taskBar);
        
        await taskBar.DragToAsync(taskBar, new LocatorDragToOptions
        {
            SourcePosition = new SourcePosition { X = 10, Y = 5 },
            TargetPosition = new TargetPosition { X = 10 + xOffset, Y = 5 + yOffset }
        });
        
        await Page.WaitForTimeoutAsync(500);
    }

    /// <summary>
    /// Verifica presenza task nel Gantt.
    /// </summary>
    public async Task<bool> IsTaskPresentAsync(string numeroLavoro)
    {
        var task = Page.Locator($".vis-item:has-text('{numeroLavoro}')");
        return await task.IsVisibleAsync();
    }

    /// <summary>
    /// Ottiene numero task visibili nel Gantt.
    /// </summary>
    public async Task<int> GetTaskCount()
    {
        var tasks = Page.Locator(".vis-item");
        return await tasks.CountAsync();
    }

    /// <summary>
    /// Visual regression check su Gantt chart.
    /// </summary>
    public async Task AssertGanttMatches(string screenshotName = "gantt-macchine-chart")
    {
        await WaitForGanttReady();
        await AssertVisuallyMatches(screenshotName, GanttContainer);
    }
}
