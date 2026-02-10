using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.Playwright.Assertions;

namespace MESManager.E2E;

public class MESManagerE2ETests : PlaywrightTestBase
{
    public MESManagerE2ETests(ITestOutputHelper output) : base(output)
    {
    }

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
        await Page.GotoAsync(BaseUrl + "/pianificazione", new() { WaitUntil = WaitUntilState.DOMContentLoaded });

        // Verifica presenza titolo pagina
        var title = Page.Locator("text=Pianificazione Produzione");
        await Expect(title).ToBeVisibleAsync(new() { Timeout = 15000 });

        // Verifica presenza pannello impostazioni
        var tempoSetup = Page.GetByLabel("Tempo Setup (minuti)");
        await Expect(tempoSetup).ToBeVisibleAsync(new() { Timeout = 15000 });

        // Verifica nessun errore console
        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Pianificazione - verifica campi configurazione")]
    [Trait("Category", "Modified")]
    [Trait("Page", "Pianificazione")]
    public async Task Pianificazione_HasConfigurationFields()
    {
        // Navigate to Pianificazione page
        await Page.GotoAsync(BaseUrl + "/pianificazione", new() { WaitUntil = WaitUntilState.DOMContentLoaded });

        // Verifica presenza campo Tempo Setup
        var tempoSetup = Page.GetByLabel("Tempo Setup (minuti)");
        await Expect(tempoSetup).ToBeVisibleAsync(new() { Timeout = 15000 });

        // Verifica presenza pulsante Salva Impostazioni
        var salvaBtn = Page.GetByRole(AriaRole.Button, new() { Name = "Salva Impostazioni" });
        await Expect(salvaBtn).ToBeVisibleAsync(new() { Timeout = 15000 });

        // Verifica presenza pulsante Aggiorna Dati
        var aggiornaBtn = Page.GetByRole(AriaRole.Button, new() { Name = "Aggiorna Dati" });
        await Expect(aggiornaBtn).ToBeVisibleAsync(new() { Timeout = 15000 });

        // Verifica nessun errore console
        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Pianificazione - verifica presenza componente Gantt o messaggio info")]
    [Trait("Category", "Modified")]
    [Trait("Page", "Pianificazione")]
    public async Task Pianificazione_HasGanttOrInfoMessage()
    {
        // Navigate to Pianificazione page
        await Page.GotoAsync(BaseUrl + "/pianificazione", new() { WaitUntil = WaitUntilState.DOMContentLoaded });

        var title = Page.Locator("text=Pianificazione Produzione");
        await Expect(title).ToBeVisibleAsync(new() { Timeout = 15000 });

        await Page.WaitForFunctionAsync(
            "() => !!document.querySelector('.e-gantt') || document.body.textContent.includes('Nessuna commessa pianificata')",
            new PageWaitForFunctionOptions { Timeout = 15000 }
        );

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
        await Page.GotoAsync(BaseUrl + "/pianificazione", new() { WaitUntil = WaitUntilState.DOMContentLoaded });

        var title = Page.Locator("text=Pianificazione Produzione");
        await Expect(title).ToBeVisibleAsync(new() { Timeout = 15000 });

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
        // Intercetta la chiamata API prima della navigazione
        var apiResponsePromise = Page.WaitForResponseAsync(
            response => response.Url.Contains("/api/Pianificazione/impostazioni"),
            new() { Timeout = 15000 }
        );

        // Navigate to Pianificazione page
        await Page.GotoAsync(BaseUrl + "/pianificazione", new() { WaitUntil = WaitUntilState.DOMContentLoaded });

        // Verifica che la risposta API sia arrivata
        var response = await apiResponsePromise;
        Assert.Equal(200, response.Status);

        // Verifica nessun errore console
        await AssertNoConsoleErrors();
    }
}

