using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace MESManager.E2E.Tests;

[Collection("PlaywrightTests")]
[Trait("Category", "Functional")]
[Trait("Feature", "Preventivi")]
public class PreventiviTests : PlaywrightTestBase
{
    public PreventiviTests(ITestOutputHelper output) : base(output)
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

    [Fact(DisplayName = "Preventivi > Catalogo preventivi visibile")]
    public async Task CatalogoPreventivi_PageLoads()
    {
        await Page.GotoAsync($"{BaseUrl}/preventivi");
        await AssertVisibleByTestId("page-preventivi");
        await AssertVisibleByTestId("tabs-preventivi");
        await AssertVisibleByTestId("btn-vai-analisi-prezzi");
        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Preventivi > Analisi Prezzi visibile e filtro commesse aperte")]
    public async Task AnalisiPrezzi_PageLoadsAndOpenOrdersFilterWorks()
    {
        await Page.GotoAsync($"{BaseUrl}/analisi-prezzi");
        await AssertVisibleByTestId("page-analisi-prezzi");
        await AssertVisibleByTestId("input-soglia");
        await AssertVisibleByTestId("input-filter");
        await AssertVisibleByTestId("btn-analizza-commesse-aperte");

        var resultsOrEmpty = Page.GetByTestId("table-analisi-prezzi")
            .Or(Page.GetByTestId("alert-analisi-prezzi-empty"));
        await resultsOrEmpty.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 15000
        });

        await Page.GetByTestId("btn-analizza-commesse-aperte").ClickAsync();
        await AssertVisibleByTestId("alert-filtro-commesse-aperte", 15000);

        var errorSnackbars = await Page.Locator(".mud-snackbar:has-text('Errore')").CountAsync();
        Assert.Equal(0, errorSnackbars);

        await AssertNoConsoleErrors();
    }
}
