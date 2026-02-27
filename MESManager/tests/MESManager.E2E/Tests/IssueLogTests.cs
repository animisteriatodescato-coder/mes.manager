using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace MESManager.E2E.Tests;

[Collection("PlaywrightTests")]
[Trait("Category", "Functional")]
[Trait("Feature", "Impostazioni")]
public class IssueLogTests : PlaywrightTestBase
{
    public IssueLogTests(ITestOutputHelper output) : base(output)
    {
    }

    private async Task AssertVisibleByTestId(string testId, int timeoutMs = 10000)
    {
        await Page.GetByTestId(testId).WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = timeoutMs
        });
    }

    [Fact(DisplayName = "Issue Log > Pagina visibile")]
    public async Task IssueLog_PageLoads()
    {
        await Page.GotoAsync($"{BaseUrl}/impostazioni/issue-log");
        await AssertVisibleByTestId("page-issue-log-list");
        await Page.GetByText("Nuovo Issue").WaitForAsync();
        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Issue Log > MudTable opacità (visual)")]
    [Trait("Category", "Visual")]
    public async Task IssueLog_TableOpacity()
    {
        await Page.GotoAsync($"{BaseUrl}/impostazioni/issue-log");
        await AssertVisibleByTestId("page-issue-log-list");
        
        // Attendi rendering completo e caricamento dati
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(500);

        // Screenshot della tabella principale
        var table = Page.Locator(".mud-table-root").First;
        var visualHelper = new VisualRegressionHelper(Page, "IssueLog");
        await visualHelper.AssertMatchesBaseline("mudtable-opacity", table);
        
        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Issue Log > Filtri Auto/Manuali funzionano")]
    public async Task IssueLog_FiltersWork()
    {
        await Page.GotoAsync($"{BaseUrl}/impostazioni/issue-log");
        await AssertVisibleByTestId("page-issue-log-list");
        
        // Click filtro Auto
        await Page.GetByText("Auto").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Click filtro Manuali
        await Page.GetByText("Manuali").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        await AssertNoConsoleErrors();
    }
}
