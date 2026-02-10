using MESManager.E2E.Pages;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace MESManager.E2E.Tests;

/// <summary>
/// Test funzionali per Home Page e navigazione principale.
/// </summary>
[Collection("PlaywrightTests")]
[Trait("Category", "Functional")]
[Trait("Feature", "Navigation")]
public class HomeTests : PlaywrightTestBase
{
    public HomeTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact(DisplayName = "Home > AppBar visibile e funzionante")]
    public async Task Home_AppBarIsVisible()
    {
        var homePage = new HomePage(Page, BaseUrl);
        await homePage.NavigateAsync();

        var isVisible = await homePage.IsAppBarVisibleAsync();
        Assert.True(isVisible, "AppBar dovrebbe essere visibile");

        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Home > Navigazione a Pianificazione funzionante")]
    public async Task Home_NavigateToPianificazione()
    {
        var homePage = new HomePage(Page, BaseUrl);
        await homePage.NavigateAsync();

        await homePage.NavigateToPage("Pianificazione");

        // Verifica URL cambiato
        Assert.Contains("/pianificazione", Page.Url.ToLower());

        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Home > Navigazione a Commesse Aperte funzionante")]
    public async Task Home_NavigateToCommesseAperte()
    {
        var homePage = new HomePage(Page, BaseUrl);
        await homePage.NavigateAsync();

        await homePage.NavigateToPage("Commesse Aperte");

        // Verifica URL cambiato
        Assert.Contains("/commesse-aperte", Page.Url.ToLower());

        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Home > Navigazione a Programma Macchine funzionante")]
    public async Task Home_NavigateToProgrammaMacchine()
    {
        var homePage = new HomePage(Page, BaseUrl);
        await homePage.NavigateAsync();

        await homePage.NavigateToPage("Programma Macchine");

        // Verifica URL cambiato
        Assert.Contains("/programma-macchine", Page.Url.ToLower());

        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Home > Visual regression check layout")]
    [Trait("Category", "Visual")]
    public async Task Home_LayoutVisualRegression()
    {
        var homePage = new HomePage(Page, BaseUrl);
        await homePage.NavigateAsync();

        await homePage.IsPageLoadedAsync();
        
        // Screenshot baseline comparison
        await homePage.AssertLayoutMatches();
    }
}
