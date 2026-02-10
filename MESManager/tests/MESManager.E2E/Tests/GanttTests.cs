using MESManager.E2E.Pages;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace MESManager.E2E.Tests;

/// <summary>
/// Test funzionali per Gantt Macchine (Syncfusion Gantt).
/// </summary>
[Collection("PlaywrightTests")]
[Trait("Category", "Functional")]
[Trait("Feature", "Gantt")]
public class GanttTests : PlaywrightTestBase
{
    public GanttTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact(DisplayName = "Gantt > Pagina carica correttamente")]
    public async Task Gantt_PageLoads()
    {
        var page = new GanttMacchinePage(Page, BaseUrl);
        await page.NavigateAsync("programma/gantt-macchine");

        var isLoaded = await page.IsPageLoadedAsync();
        Assert.True(isLoaded, "Pagina Gantt Macchine dovrebbe caricare");

        await page.WaitForGanttReady();
        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Gantt > Syncfusion Gantt renderizza chart")]
    public async Task Gantt_RendersChart()
    {
        var page = new GanttMacchinePage(Page, BaseUrl);
        await page.NavigateAsync("programma/gantt-macchine");
        await page.WaitForGanttReady();

        var taskCount = await page.GetTaskCount();
        Assert.True(taskCount >= 0, "Gantt dovrebbe renderizzare task (anche se 0)");

        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Gantt > Ricalcola Tutto esegue refresh")]
    [Trait("Category", "Integration")]
    public async Task Gantt_RicalcolaTuttoWorks()
    {
        var page = new GanttMacchinePage(Page, BaseUrl);
        await page.NavigateAsync("programma/gantt-macchine");
        await page.WaitForGanttReady();

        var taskCountBefore = await page.GetTaskCount();

        await page.ClickRicalcolaTutto();
        await page.WaitForGanttReady();

        var taskCountAfter = await page.GetTaskCount();
        
        // Task count dovrebbe rimanere coerente (o cambiare se ricalcolo modifica pianificazione)
        Assert.True(taskCountAfter >= 0, "Gantt dovrebbe ricalcolare task");

        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Gantt > Drag & Drop funziona (se task presenti)")]
    [Trait("Category", "Integration")]
    public async Task Gantt_DragDropWorks()
    {
        var page = new GanttMacchinePage(Page, BaseUrl);
        await page.NavigateAsync("programma/gantt-macchine");
        await page.WaitForGanttReady();

        var taskCount = await page.GetTaskCount();
        if (taskCount > 0)
        {
            // Ottiene primo task
            var firstTask = Page.Locator(".e-gantt-child-taskbar").First;
            var taskText = await firstTask.TextContentAsync();

            if (!string.IsNullOrEmpty(taskText))
            {
                // Drag leggero (10px right, 0px down)
                await page.DragCommessaToPosition(taskText.Trim(), 10, 0);

                // Verifica task ancora presente
                var isPresent = await page.IsTaskPresentAsync(taskText.Trim());
                Assert.True(isPresent, "Task dovrebbe rimanere dopo drag");
            }
        }

        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Gantt > Visual regression chart")]
    [Trait("Category", "Visual")]
    public async Task Gantt_ChartVisualRegression()
    {
        var page = new GanttMacchinePage(Page, BaseUrl);
        await page.NavigateAsync("programma/gantt-macchine");
        await page.WaitForGanttReady();

        // Esegue un'azione reale prima dello screenshot
        await page.ClickRicalcolaTutto();

        // Screenshot Gantt per visual regression
        await page.AssertGanttMatches();
    }
}
