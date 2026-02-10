using Microsoft.Playwright;

namespace MESManager.E2E.Pages;

/// <summary>
/// Page Object per la home page (MainLayout + Dashboard).
/// </summary>
public class HomePage : BasePage
{
    // Selettori data-testid
    private ILocator AppBar => GetByTestId("main-appbar");
    private ILocator NavMenu => GetByTestId("nav-menu");
    private ILocator PageContent => GetByTestId("page-home");

    public HomePage(IPage page, string baseUrl) 
        : base(page, baseUrl, "HomePage")
    {
    }

    public override async Task<bool> IsPageLoadedAsync()
    {
        try
        {
            await WaitForVisible(AppBar, 5000);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Naviga verso una pagina specifica tramite menu.
    /// </summary>
    public async Task NavigateToPage(string pageName)
    {
        // Click su NavMenu toggle se su mobile
        var menuButton = Page.Locator("button[aria-label='Toggle navigation']");
        if (await menuButton.IsVisibleAsync())
        {
            await menuButton.ClickAsync();
        }

        // Click su link con testo
        var link = Page.Locator($"a:has-text('{pageName}')");
        await ClickAsync(link);
        await WaitForPageLoad();
    }

    /// <summary>
    /// Verifica visibilità AppBar.
    /// </summary>
    public async Task<bool> IsAppBarVisibleAsync()
    {
        return await AppBar.IsVisibleAsync();
    }

    /// <summary>
    /// Visual regression check su layout principale.
    /// </summary>
    public async Task AssertLayoutMatches()
    {
        await AssertVisuallyMatches("home-layout");
    }
}
