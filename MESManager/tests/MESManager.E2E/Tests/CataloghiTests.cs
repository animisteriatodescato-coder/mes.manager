using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace MESManager.E2E.Tests;

[Collection("PlaywrightTests")]
[Trait("Category", "Functional")]
[Trait("Feature", "Cataloghi")]
public class CataloghiTests : PlaywrightTestBase
{
    public CataloghiTests(ITestOutputHelper output) : base(output)
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

    [Fact(DisplayName = "Catalogo Anime > Pagina e griglia visibili")]
    public async Task CatalogoAnime_PageLoads()
    {
        await Page.GotoAsync($"{BaseUrl}/cataloghi/anime");
        await AssertVisibleByTestId("page-catalogo-anime");
        await AssertVisibleByTestId("grid-catalogo-anime");
        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Catalogo Commesse > Pagina e griglia visibili")]
    public async Task CatalogoCommesse_PageLoads()
    {
        await Page.GotoAsync($"{BaseUrl}/cataloghi/commesse");
        await AssertVisibleByTestId("page-catalogo-commesse");
        await AssertVisibleByTestId("commesse-grid");
        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Catalogo Articoli > Pagina e griglia visibili")]
    public async Task CatalogoArticoli_PageLoads()
    {
        await Page.GotoAsync($"{BaseUrl}/cataloghi/articoli");
        await AssertVisibleByTestId("page-catalogo-articoli");
        await AssertVisibleByTestId("grid-catalogo-articoli");
        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Catalogo Clienti > Pagina e griglia visibili")]
    public async Task CatalogoClienti_PageLoads()
    {
        await Page.GotoAsync($"{BaseUrl}/cataloghi/clienti");
        await AssertVisibleByTestId("page-catalogo-clienti");
        await AssertVisibleByTestId("grid-catalogo-clienti");
        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Catalogo Preventivi > Pagina e tabella visibili")]
    public async Task CatalogoPreventivi_PageLoads()
    {
        await Page.GotoAsync($"{BaseUrl}/cataloghi/preventivi");
        await AssertVisibleByTestId("page-catalogo-preventivi");
        await AssertVisibleByTestId("table-preventivi");
        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Catalogo Preventivi > Apri e chiudi dialog import")]
    public async Task CatalogoPreventivi_ImportDialogWorks()
    {
        await Page.GotoAsync($"{BaseUrl}/cataloghi/preventivi");
        await Page.GetByTestId("btn-preventivi-importa").ClickAsync();
        await AssertVisibleByTestId("dlg-preventivi-importa");

        await Page.GetByTestId("btn-preventivi-importa-annulla").ClickAsync();
        await Page.GetByTestId("dlg-preventivi-importa")
            .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = 5000 });

        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Catalogo Preventivi > Nuovo preventivo apre pagina")]
    public async Task CatalogoPreventivi_NewQuoteOpens()
    {
        await Page.GotoAsync($"{BaseUrl}/cataloghi/preventivi");
        await Page.GetByTestId("btn-preventivi-nuovo").ClickAsync();

        await AssertVisibleByTestId("page-preventivo-dettaglio");
        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Catalogo Ricette > Pagina visibile")]
    public async Task CatalogoRicette_PageLoads()
    {
        await Page.GotoAsync($"{BaseUrl}/cataloghi/ricette");
        await AssertVisibleByTestId("page-catalogo-ricette");
        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Catalogo Foto > Pagina visibile")]
    public async Task CatalogoFoto_PageLoads()
    {
        await Page.GotoAsync($"{BaseUrl}/cataloghi/foto");
        await AssertVisibleByTestId("page-catalogo-foto");
        await AssertNoConsoleErrors();
    }
}
