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

    [Fact(DisplayName = "Home > Assistente AI visibile e ridimensionabile")]
    [Trait("Feature", "AI")]
    public async Task Home_AiAssistantDrawerIsWideAndResizable()
    {
        await Page.GotoAsync(BaseUrl + "/", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await Page.EvaluateAsync("localStorage.removeItem('mes-ai-assistant-width')");

        var aiButton = Page.GetByTestId("ai-assistant-btn");
        await aiButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
        await aiButton.ClickAsync();

        var drawer = Page.GetByTestId("ai-assistant-drawer");
        await drawer.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('[data-testid=\"ai-assistant-drawer\"]')?.getBoundingClientRect().width > 400",
            new PageWaitForFunctionOptions { Timeout = 10000 });

        var initialWidth = await drawer.EvaluateAsync<float>("el => el.getBoundingClientRect().width");
        Assert.True(initialWidth > 400, $"Il drawer AI dovrebbe essere largo oltre 400px, trovato {initialWidth}px");

        var handle = Page.Locator(".ai-drawer-resize-handle");
        var box = await handle.BoundingBoxAsync();
        Assert.NotNull(box);

        var dragY = box!.Y + box.Height / 2;
        await Page.Mouse.MoveAsync(box.X + box.Width / 2, dragY);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(box.X - 120, dragY, new() { Steps = 6 });
        await Page.Mouse.UpAsync();

        var resizedWidth = await drawer.EvaluateAsync<float>("el => el.getBoundingClientRect().width");
        Assert.True(resizedWidth > initialWidth + 10,
            $"Il drag del bordo dovrebbe allargare il drawer AI. Prima: {initialWidth}px, dopo: {resizedWidth}px");

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
