using Microsoft.Playwright;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace MESManager.E2E;

public class MESManagerE2ETests : PlaywrightTestBase
{
    [Fact(DisplayName = "Home non è bianca - verifica contenuto principale")]
    public async Task Home_IsNotBlank()
    {
        // Navigate to home
        await Page.GotoAsync(BaseUrl + "/", new() { WaitUntil = WaitUntilState.NetworkIdle });

        // Aspetta che il layout sia caricato - verifica presenza appbar principale
        var appBar = Page.GetByTestId("main-appbar");
        await Expect(appBar).ToBeVisibleAsync();

        // Verifica che ci sia contenuto visibile (body non vuoto)
        var body = Page.Locator("body");
        await Expect(body).Not.ToBeEmptyAsync();

        // Verifica presenza di elementi MudBlazor (indica che l'app è inizializzata)
        var mudLayout = Page.Locator(".mud-layout");
        await Expect(mudLayout).ToBeVisibleAsync();

        // Verifica nessun errore console
        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Navbar/AppBar non duplicata - verifica singola istanza")]
    public async Task NavBar_IsNotDuplicated()
    {
        // Navigate to Commesse page
        await Page.GotoAsync(BaseUrl + "/cataloghi/commesse", new() { WaitUntil = WaitUntilState.NetworkIdle });

        // Aspetta caricamento pagina
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verifica che esista SOLO una appbar principale
        var mainAppBars = Page.GetByTestId("main-appbar");
        await Expect(mainAppBars).ToHaveCountAsync(1);

        // Verifica che non ci siano appbar duplicate (usando classe MudAppBar)
        var allAppBars = Page.Locator(".mud-appbar[data-testid='main-appbar']");
        await Expect(allAppBars).ToHaveCountAsync(1);

        // Verifica nessun errore console
        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Commesse carica UI principale - verifica grid e titolo")]
    public async Task Commesse_LoadsMainUI()
    {
        // Navigate to Commesse page
        await Page.GotoAsync(BaseUrl + "/cataloghi/commesse", new() { WaitUntil = WaitUntilState.NetworkIdle });

        // Verifica presenza titolo pagina
        var title = Page.GetByTestId("commesse-title");
        await Expect(title).ToBeVisibleAsync();
        await Expect(title).ToHaveTextAsync("Commesse");

        // Verifica presenza grid
        var grid = Page.GetByTestId("commesse-grid");
        await Expect(grid).ToBeVisibleAsync();

        // Verifica che il grid abbia contenuto (header row di ag-Grid)
        var gridHeaders = Page.Locator(".ag-header-row");
        await Expect(gridHeaders.First).ToBeVisibleAsync();

        // Verifica che ci siano colonne (almeno una colonna header)
        var headerCells = Page.Locator(".ag-header-cell");
        await Expect(headerCells.First).ToBeVisibleAsync();

        // Verifica nessun errore console
        await AssertNoConsoleErrors();
    }
}
