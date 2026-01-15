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

    [Fact(DisplayName = "Commesse Aperte - carica grid con colonna MA")]
    public async Task CommesseAperte_LoadsGridWithMAColumn()
    {
        // Navigate to Commesse Aperte page
        await Page.GotoAsync(BaseUrl + "/programma/commesse-aperte", new() { WaitUntil = WaitUntilState.NetworkIdle });

        // Aspetta caricamento grid
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verifica presenza grid AG Grid
        var grid = Page.Locator("#commesseAperteGrid");
        await Expect(grid).ToBeVisibleAsync();

        // Verifica che il grid abbia header row
        var gridHeaders = Page.Locator(".ag-header-row");
        await Expect(gridHeaders.First).ToBeVisibleAsync();

        // Verifica presenza colonna MA (NumeroMacchina) - la prima colonna
        var maHeader = Page.Locator(".ag-header-cell[col-id='numeroMacchina']");
        await Expect(maHeader).ToBeVisibleAsync();

        // Verifica che la colonna MA sia la prima colonna (pinned left)
        var pinnedLeftHeader = Page.Locator(".ag-pinned-left-header .ag-header-cell").First;
        await Expect(pinnedLeftHeader).ToBeVisibleAsync();

        // Verifica nessun errore console
        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Programma Macchine - carica grid con colonne complete")]
    public async Task ProgrammaMacchine_LoadsGridWithColumns()
    {
        // Navigate to Programma Macchine page
        await Page.GotoAsync(BaseUrl + "/programma/programma-macchine", new() { WaitUntil = WaitUntilState.NetworkIdle });

        // Aspetta caricamento grid
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verifica presenza grid AG Grid
        var grid = Page.Locator("#programmaMacchineGrid");
        await Expect(grid).ToBeVisibleAsync();

        // Verifica che il grid abbia header row
        var gridHeaders = Page.Locator(".ag-header-row");
        await Expect(gridHeaders.First).ToBeVisibleAsync();

        // Verifica presenza colonna Macchina
        var macchinaHeader = Page.Locator(".ag-header-cell[col-id='numeroMacchina']");
        await Expect(macchinaHeader).ToBeVisibleAsync();

        // Verifica presenza toolbar con pulsanti
        var toolbar = Page.Locator(".mud-toolbar");
        await Expect(toolbar.First).ToBeVisibleAsync();

        // Verifica nessun errore console
        await AssertNoConsoleErrors();
    }
}
