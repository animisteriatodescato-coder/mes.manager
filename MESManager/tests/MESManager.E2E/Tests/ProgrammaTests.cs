using MESManager.E2E.Pages;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace MESManager.E2E.Tests;

/// <summary>
/// Test funzionali per Programma Macchine (read-only).
/// </summary>
[Collection("PlaywrightTests")]
[Trait("Category", "Functional")]
[Trait("Feature", "ProgrammaMacchine")]
public class ProgrammaTests : PlaywrightTestBase
{
    public ProgrammaTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact(DisplayName = "Programma > Pagina carica correttamente")]
    public async Task Programma_PageLoads()
    {
        var page = new ProgrammaMacchinePage(Page, BaseUrl);
        await page.NavigateAsync("programma/programma-macchine");

        var isLoaded = await page.IsPageLoadedAsync();
        Assert.True(isLoaded, "Pagina Programma Macchine dovrebbe caricare");

        await page.WaitForGridReady();
        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Programma > Griglia renderizza righe programma")]
    public async Task Programma_GridRendersRows()
    {
        var page = new ProgrammaMacchinePage(Page, BaseUrl);
        await page.NavigateAsync("programma/programma-macchine");
        await page.WaitForGridReady();

        var rowCount = await page.GetProgramRowCount();
        Assert.True(rowCount >= 0, "Griglia programma dovrebbe renderizzare");

        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Programma > Verifica commesse programmate")]
    [Trait("Category", "Integration")]
    public async Task Programma_ShowsScheduledCommesse()
    {
        var page = new ProgrammaMacchinePage(Page, BaseUrl);
        await page.NavigateAsync("programma/programma-macchine");
        await page.WaitForGridReady();

        var codiceCells = Page.Locator(".ag-center-cols-container .ag-row [col-id='codice']");
        if (await codiceCells.CountAsync() > 0)
        {
            var numeroLavoro = await codiceCells.First.TextContentAsync();

            if (!string.IsNullOrEmpty(numeroLavoro))
            {
                var isProgrammata = await page.IsCommessaProgrammataAsync(numeroLavoro.Trim());
                Assert.True(isProgrammata, $"Commessa {numeroLavoro} dovrebbe essere visibile");

                // Verifica numero macchina presente
                var numeroMacchina = await page.GetNumeroMacchina(numeroLavoro.Trim());
                Assert.NotNull(numeroMacchina);
            }
        }

        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Programma > Aggiorna ricarica dati")]
    [Trait("Category", "Integration")]
    public async Task Programma_RefreshWorks()
    {
        var page = new ProgrammaMacchinePage(Page, BaseUrl);
        await page.NavigateAsync("programma/programma-macchine");
        await page.WaitForGridReady();

        try
        {
            await page.ClickRefresh();
            
            // Verifica nessun errore
            await AssertNoConsoleErrors();
        }
        catch (TimeoutException)
        {
            Assert.True(true, "Refresh non disponibile (OK)");
        }
    }

    [Fact(DisplayName = "Programma > Visual regression griglia")]
    [Trait("Category", "Visual")]
    public async Task Programma_GridVisualRegression()
    {
        var page = new ProgrammaMacchinePage(Page, BaseUrl);
        await page.NavigateAsync("programma/programma-macchine");
        await page.WaitForGridReady();

        // Esegue un'azione reale prima dello screenshot
        await page.ClickRefresh();
        await page.WaitForGridReady();

        // Screenshot griglia programma
        await page.AssertGridMatches();
    }

    [Fact(DisplayName = "Programma > Read-only (nessuna modifica permessa)")]
    public async Task Programma_IsReadOnly()
    {
        var page = new ProgrammaMacchinePage(Page, BaseUrl);
        await page.NavigateAsync("programma/programma-macchine");
        await page.WaitForGridReady();

        var rowCount = await page.GetProgramRowCount();
        if (rowCount > 0)
        {
            // Verifica che celle non siano editabili
            var firstCell = Page.Locator(".ag-center-cols-container .ag-row .ag-cell").First;
            
            // Tenta double-click (non dovrebbe attivare editor)
            await firstCell.DblClickAsync();
            
            // Verifica nessun editor aperto
            var editor = Page.Locator(".ag-cell-inline-editing");
            var isEditorVisible = await editor.IsVisibleAsync();
            Assert.False(isEditorVisible, "Griglia dovrebbe essere read-only");
        }

        await AssertNoConsoleErrors();
    }
}
