using Microsoft.Playwright;

namespace MESManager.E2E.Pages;

/// <summary>
/// Classe base per tutti i Page Objects.
/// Fornisce metodi comuni per navigazione, wait, screenshot.
/// </summary>
public abstract class BasePage
{
    protected readonly IPage Page;
    protected readonly string BaseUrl;
    protected readonly VisualRegressionHelper VisualHelper;

    protected BasePage(IPage page, string baseUrl, string pageName)
    {
        Page = page;
        BaseUrl = baseUrl;
        VisualHelper = new VisualRegressionHelper(page, pageName);
    }

    /// <summary>
    /// Naviga alla pagina specifica.
    /// </summary>
    public virtual async Task NavigateAsync(string path = "")
    {
        var url = string.IsNullOrEmpty(path) ? BaseUrl : $"{BaseUrl}/{path.TrimStart('/')}";
        await Page.GotoAsync(url, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 30000
        });
    }

    /// <summary>
    /// Attende che un locator sia visibile.
    /// </summary>
    protected async Task WaitForVisible(ILocator locator, int timeoutMs = 10000)
    {
        await locator.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = timeoutMs
        });
    }

    /// <summary>
    /// Attende che la pagina sia completamente caricata (NetworkIdle).
    /// </summary>
    protected async Task WaitForPageLoad()
    {
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions
        {
            Timeout = 15000
        });
    }

    /// <summary>
    /// Click su elemento con wait automatico.
    /// </summary>
    protected async Task ClickAsync(ILocator locator, int timeoutMs = 5000)
    {
        await locator.ClickAsync(new LocatorClickOptions
        {
            Timeout = timeoutMs
        });
    }

    /// <summary>
    /// Fill input con wait automatico.
    /// </summary>
    protected async Task FillAsync(ILocator locator, string value, int timeoutMs = 5000)
    {
        await locator.FillAsync(value, new LocatorFillOptions
        {
            Timeout = timeoutMs
        });
    }

    /// <summary>
    /// Verifica assenza errori console/page.
    /// </summary>
    protected void AssertNoErrors(List<string> consoleErrors, List<string> pageErrors)
    {
        if (consoleErrors.Any())
        {
            throw new Exception($"Console errors detected:\n{string.Join("\n", consoleErrors)}");
        }

        if (pageErrors.Any())
        {
            throw new Exception($"Page errors detected:\n{string.Join("\n", pageErrors)}");
        }
    }

    /// <summary>
    /// Screenshot baseline comparison per visual regression.
    /// </summary>
    protected async Task AssertVisuallyMatches(string screenshotName, ILocator? locator = null)
    {
        await VisualHelper.AssertMatchesBaseline(screenshotName, locator);
    }

    /// <summary>
    /// Ottiene locator da data-testid (standard naming convention).
    /// </summary>
    protected ILocator GetByTestId(string testId) => Page.GetByTestId(testId);

    /// <summary>
    /// Verifica che la pagina sia caricata tramite data-testid.
    /// </summary>
    public abstract Task<bool> IsPageLoadedAsync();
}
