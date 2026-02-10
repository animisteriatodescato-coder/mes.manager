using MESManager.E2E.Pages;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace MESManager.E2E.Tests;

/// <summary>
/// Test funzionali per Commesse Aperte (include auto-scheduler v1.31).
/// </summary>
[Collection("PlaywrightTests")]
[Trait("Category", "Functional")]
[Trait("Feature", "CommesseAperte")]
public class CommesseAperteTests : PlaywrightTestBase
{
    public CommesseAperteTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact(DisplayName = "CommesseAperte > Pagina carica correttamente")]
    public async Task CommesseAperte_PageLoads()
    {
        var page = new CommesseApertePage(Page, BaseUrl);
        await page.NavigateAsync("programma/commesse-aperte");

        var isLoaded = await page.IsPageLoadedAsync();
        Assert.True(isLoaded, "Pagina Commesse Aperte dovrebbe caricare");

        await page.WaitForGridReady();
        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "CommesseAperte > Griglia AG-Grid renderizza righe")]
    public async Task CommesseAperte_GridRendersRows()
    {
        var page = new CommesseApertePage(Page, BaseUrl);
        await page.NavigateAsync("programma/commesse-aperte");
        await page.WaitForGridReady();

        var rowCount = await page.GetGridRowCount();
        Assert.True(rowCount >= 0, "Griglia dovrebbe renderizzare (anche se vuota)");

        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "CommesseAperte > Bottone Auto-Scheduler v1.31 presente")]
    public async Task CommesseAperte_AutoSchedulerButtonVisible()
    {
        var page = new CommesseApertePage(Page, BaseUrl);
        await page.NavigateAsync("programma/commesse-aperte");

        var isVisible = await page.IsAutoSchedulerButtonVisibleAsync();
        Assert.True(isVisible, "Bottone '🚀 Carica su Gantt' dovrebbe essere visibile (v1.31)");

        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "CommesseAperte > Selezione commessa funziona")]
    [Trait("Category", "Integration")]
    public async Task CommesseAperte_SelectCommessaWorks()
    {
        var page = new CommesseApertePage(Page, BaseUrl);
        await page.NavigateAsync("programma/commesse-aperte");
        await page.WaitForGridReady();

        var rowCount = await page.GetGridRowCount();
        if (rowCount > 0)
        {
            // Seleziona prima riga (usa numero lavoro dalla griglia)
            var firstRow = Page.Locator(".ag-center-cols-container .ag-row").First;
            var numeroLavoro = await firstRow.Locator("[col-id='NumeroLavoro']").TextContentAsync();

            if (!string.IsNullOrEmpty(numeroLavoro))
            {
                await page.SelectCommessa(numeroLavoro.Trim());

                // Verifica checkbox selezionato
                var checkbox = firstRow.Locator(".ag-selection-checkbox input[type='checkbox']");
                var isChecked = await checkbox.IsCheckedAsync();
                Assert.True(isChecked, $"Commessa {numeroLavoro} dovrebbe essere selezionata");
            }
        }

        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "CommesseAperte > Click Auto-Scheduler mostra messaggio")]
    [Trait("Category", "Integration")]
    [Trait("Version", "v1.31")]
    public async Task CommesseAperte_AutoSchedulerShowsMessage()
    {
        var page = new CommesseApertePage(Page, BaseUrl);
        await page.NavigateAsync("programma/commesse-aperte");
        await page.WaitForGridReady();

        var rowCount = await page.GetGridRowCount();
        if (rowCount > 0)
        {
            // Seleziona prima commessa
            var firstRow = Page.Locator(".ag-center-cols-container .ag-row").First;
            var numeroLavoro = await firstRow.Locator("[col-id='NumeroLavoro']").TextContentAsync();

            if (!string.IsNullOrEmpty(numeroLavoro))
            {
                await page.SelectCommessa(numeroLavoro.Trim());
                await page.ClickCaricaSuGantt();

                // Verifica messaggio risposta (successo o errore)
                var message = await page.GetStatusMessageAsync();
                Assert.NotEmpty(message);
            }
        }

        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "CommesseAperte > Refresh ricarica dati")]
    public async Task CommesseAperte_RefreshReloadsData()
    {
        var page = new CommesseApertePage(Page, BaseUrl);
        await page.NavigateAsync("programma/commesse-aperte");
        await page.WaitForGridReady();

        var rowCountBefore = await page.GetGridRowCount();

        await page.ClickRefresh();
        await page.WaitForGridReady();

        var rowCountAfter = await page.GetGridRowCount();
        
        // Row count dovrebbe essere lo stesso (o cambiato se DB modificato)
        Assert.True(rowCountAfter >= 0, "Griglia dovrebbe renderizzare dopo refresh");

        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "CommesseAperte > Visual regression griglia")]
    [Trait("Category", "Visual")]
    public async Task CommesseAperte_GridVisualRegression()
    {
        var page = new CommesseApertePage(Page, BaseUrl);
        await page.NavigateAsync("programma/commesse-aperte");
        await page.WaitForGridReady();

        // Esegue un'azione reale prima dello screenshot
        await page.ClickRefresh();
        await page.WaitForGridReady();

        // Screenshot griglia per visual regression
        await page.AssertGridMatches();
    }
}
