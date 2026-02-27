using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace MESManager.E2E.Tests;

[Collection("PlaywrightTests")]
[Trait("Category", "Functional")]
[Trait("Feature", "Impostazioni")]
public class ImpostazioniTests : PlaywrightTestBase
{
    public ImpostazioniTests(ITestOutputHelper output) : base(output)
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

    [Fact(DisplayName = "Gestione Utenti > Pagina visibile")]
    public async Task GestioneUtenti_PageLoads()
    {
        await Page.GotoAsync($"{BaseUrl}/impostazioni/utenti");
        await AssertVisibleByTestId("page-gestione-utenti");
        await AssertVisibleByTestId("btn-utente-aggiungi");
        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Gestione Festivi > Pagina visibile")]
    public async Task GestioneFestivi_PageLoads()
    {
        await Page.GotoAsync($"{BaseUrl}/impostazioni/festivi");
        await AssertVisibleByTestId("page-gestione-festivi");
        await AssertVisibleByTestId("btn-festivi-aggiungi");
        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Gestione Festivi > Dialog aggiunta apre e chiude")]
    public async Task GestioneFestivi_AddDialogWorks()
    {
        await Page.GotoAsync($"{BaseUrl}/impostazioni/festivi");
        await Page.GetByTestId("btn-festivi-aggiungi").ClickAsync();
        await AssertVisibleByTestId("dlg-festivi-aggiungi");

        await Page.GetByTestId("btn-festivi-aggiungi-annulla").ClickAsync();
        await Page.GetByTestId("dlg-festivi-aggiungi")
            .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = 5000 });

        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Impostazioni Gantt > Pagina visibile")]
    public async Task ImpostazioniGantt_PageLoads()
    {
        await Page.GotoAsync($"{BaseUrl}/impostazioni/gantt");
        await AssertVisibleByTestId("page-impostazioni-gantt");
        await AssertVisibleByTestId("btn-gantt-salva-macchine");
        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Impostazioni Generali > Pagina visibile")]
    public async Task ImpostazioniGenerali_PageLoads()
    {
        await Page.GotoAsync($"{BaseUrl}/impostazioni/generali");
        await AssertVisibleByTestId("page-impostazioni-generali");
        await AssertVisibleByTestId("btn-generali-salva");
        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Impostazioni Tabelle > Pagina visibile")]
    public async Task ImpostazioniTabelle_PageLoads()
    {
        await Page.GotoAsync($"{BaseUrl}/impostazioni/tabelle");
        await AssertVisibleByTestId("page-impostazioni-tabelle");
        await AssertVisibleByTestId("btn-tabelle-colla-add");
        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Gestione Operatori > Pagina visibile")]
    public async Task GestioneOperatori_PageLoads()
    {
        await Page.GotoAsync($"{BaseUrl}/impostazioni/operatori");
        await AssertVisibleByTestId("page-gestione-operatori");
        await AssertVisibleByTestId("btn-operatori-nuovo");
        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Gestione Operatori > Dialog nuovo operatore apre e chiude")]
    public async Task GestioneOperatori_DialogWorks()
    {
        await Page.GotoAsync($"{BaseUrl}/impostazioni/operatori");
        await Page.GetByTestId("btn-operatori-nuovo").ClickAsync();
        await AssertVisibleByTestId("dlg-operatori-edit");

        await Page.GetByTestId("btn-operatori-annulla").ClickAsync();
        await Page.GetByTestId("dlg-operatori-edit")
            .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = 5000 });

        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Calendario Produzione > Pagina visibile")]
    public async Task CalendarioProduzione_PageLoads()
    {
        await Page.GotoAsync($"{BaseUrl}/impostazioni/calendario");
        await AssertVisibleByTestId("page-calendario-produzione");
        await AssertVisibleByTestId("btn-calendario-salva");
        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Gestione Utenti > MudTable opacità (visual)")]
    [Trait("Category", "Visual")]
    public async Task GestioneUtenti_TableOpacity()
    {
        await Page.GotoAsync($"{BaseUrl}/impostazioni/utenti");
        await AssertVisibleByTestId("table-utenti");
        
        // Attendi rendering completo
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(500);

        // Screenshot della tabella
        var table = Page.GetByTestId("table-utenti");
        var visualHelper = new VisualRegressionHelper(Page, "GestioneUtenti");
        await visualHelper.AssertMatchesBaseline("mudtable-opacity", table);
        
        await AssertNoConsoleErrors();
    }
}
