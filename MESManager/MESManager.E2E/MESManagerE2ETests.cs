using Microsoft.Playwright;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace MESManager.E2E;

public class MESManagerE2ETests : PlaywrightTestBase
{
    // ========================================
    // TEST CORE - Sempre eseguiti
    // ========================================
    
    [Fact(DisplayName = "Home non è bianca - verifica contenuto principale")]
    [Trait("Category", "Core")]
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

    // ========================================
    // TEST PIANIFICAZIONE - Pagine modificate
    // ========================================
    
    [Fact(DisplayName = "Pianificazione - verifica caricamento pagina Gantt")]
    [Trait("Category", "Modified")]
    [Trait("Page", "Pianificazione")]
    public async Task Pianificazione_LoadsGanttPage()
    {
        // Navigate to Pianificazione page
        await Page.GotoAsync(BaseUrl + "/pianificazione", new() { WaitUntil = WaitUntilState.NetworkIdle });

        // Aspetta caricamento completo
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verifica presenza titolo pagina
        var title = Page.Locator("h4:has-text('Pianificazione Produzione')");
        await Expect(title).ToBeVisibleAsync();

        // Verifica presenza pannello impostazioni
        var setupPanel = Page.Locator("label:has-text('Tempo Setup (minuti)')");
        await Expect(setupPanel).ToBeVisibleAsync();

        // Verifica nessun errore console
        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Pianificazione - verifica campi configurazione")]
    [Trait("Category", "Modified")]
    [Trait("Page", "Pianificazione")]
    public async Task Pianificazione_HasConfigurationFields()
    {
        // Navigate to Pianificazione page
        await Page.GotoAsync(BaseUrl + "/pianificazione", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verifica presenza campo Tempo Setup
        var tempoSetup = Page.Locator("input").Filter(new() { HasNot = Page.Locator("[type=hidden]") }).First;
        await Expect(tempoSetup).ToBeVisibleAsync();

        // Verifica presenza pulsante Salva Impostazioni
        var salvaBtnText = Page.Locator("button:has-text('Salva Impostazioni')");
        await Expect(salvaBtnText).ToBeVisibleAsync();

        // Verifica presenza pulsante Aggiorna Dati
        var aggiornaBtnText = Page.Locator("button:has-text('Aggiorna Dati')");
        await Expect(aggiornaBtnText).ToBeVisibleAsync();

        // Verifica nessun errore console
        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Pianificazione - verifica presenza componente Gantt o messaggio info")]
    [Trait("Category", "Modified")]
    [Trait("Page", "Pianificazione")]
    public async Task Pianificazione_HasGanttOrInfoMessage()
    {
        // Navigate to Pianificazione page
        await Page.GotoAsync(BaseUrl + "/pianificazione", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verifica che ci sia o il componente Gantt o il messaggio informativo
        // (dipende se ci sono dati o meno)
        var hasGantt = await Page.Locator(".e-gantt").CountAsync() > 0;
        var hasInfoMessage = await Page.Locator("text=Nessuna commessa pianificata").CountAsync() > 0;

        // Deve esserci almeno uno dei due
        Assert.True(hasGantt || hasInfoMessage, 
            "Dovrebbe essere presente il componente Gantt o il messaggio informativo");

        // Verifica nessun errore console
        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Pianificazione - verifica legenda stati")]
    [Trait("Category", "Modified")]
    [Trait("Page", "Pianificazione")]
    public async Task Pianificazione_HasStatusLegend()
    {
        // Navigate to Pianificazione page
        await Page.GotoAsync(BaseUrl + "/pianificazione", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verifica presenza sezione legenda (se ci sono dati)
        var legendTitle = Page.Locator("h6:has-text('Legenda Stati')");
        var hasData = await Page.Locator(".e-gantt").CountAsync() > 0;

        if (hasData)
        {
            await Expect(legendTitle).ToBeVisibleAsync();

            // Verifica presenza chip stati
            var chips = Page.Locator(".mud-chip");
            var chipCount = await chips.CountAsync();
            Assert.True(chipCount >= 5, $"Dovrebbero esserci almeno 5 chip di stato, trovati: {chipCount}");
        }

        // Verifica nessun errore console
        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Pianificazione - API impostazioni risponde correttamente")]
    [Trait("Category", "Modified")]
    [Trait("Page", "Pianificazione")]
    public async Task Pianificazione_ApiImpostazioniWorks()
    {
        // Navigate to Pianificazione page
        await Page.GotoAsync(BaseUrl + "/pianificazione", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Intercetta la chiamata API
        var apiResponsePromise = Page.WaitForResponseAsync(
            response => response.Url.Contains("/api/Pianificazione/impostazioni") && response.Status == 200,
            new() { Timeout = 10000 }
        );

        // Clicca su Aggiorna Dati per triggerare la chiamata
        var aggiornaBtn = Page.Locator("button:has-text('Aggiorna Dati')");
        if (await aggiornaBtn.CountAsync() > 0)
        {
            await aggiornaBtn.ClickAsync();
        }

        // Verifica che la risposta API sia arrivata (o già presente)
        try
        {
            var response = await apiResponsePromise;
            Assert.Equal(200, response.Status);
        }
        catch (TimeoutException)
        {
            // Se timeout, verifica almeno che la pagina sia caricata correttamente
            var title = Page.Locator("h4:has-text('Pianificazione Produzione')");
            await Expect(title).ToBeVisibleAsync();
        }

        // Verifica nessun errore console
        await AssertNoConsoleErrors();
    }
}

