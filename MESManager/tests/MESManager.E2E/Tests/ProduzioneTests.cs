using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace MESManager.E2E.Tests;

[Collection("PlaywrightTests")]
[Trait("Category", "Functional")]
[Trait("Feature", "Produzione")]
public class ProduzioneTests : PlaywrightTestBase
{
    public ProduzioneTests(ITestOutputHelper output) : base(output)
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

    [Fact(DisplayName = "Produzione Dashboard > Pagina visibile")]
    public async Task DashboardProduzione_PageLoads()
    {
        await Page.GotoAsync($"{BaseUrl}/produzione/dashboard");
        await AssertVisibleByTestId("page-dashboard-produzione");
        await AssertVisibleByTestId("toggle-dashboard-autorefresh");
        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "PLC Realtime > Pagina e griglia visibili")]
    public async Task PlcRealtime_PageLoads()
    {
        await Page.GotoAsync($"{BaseUrl}/produzione/plc-realtime");
        await AssertVisibleByTestId("page-plc-realtime");
        await AssertVisibleByTestId("grid-plc-realtime");
        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "PLC Realtime > Lifecycle completo (load + navigate away)")]
    public async Task PlcRealtime_LifecycleComplete()
    {
        // Carica PLC Realtime
        await Page.GotoAsync($"{BaseUrl}/produzione/plc-realtime");
        await AssertVisibleByTestId("page-plc-realtime");
        await Page.WaitForTimeoutAsync(1000); // Attende render completo
        
        // Naviga via (trigger dispose)
        await Page.GotoAsync($"{BaseUrl}/cataloghi/commesse");
        await Page.WaitForTimeoutAsync(1000); // Attende cleanup
        
        // Naviga di nuovo (test reinitialization)
        await Page.GotoAsync($"{BaseUrl}/produzione/plc-realtime");
        await AssertVisibleByTestId("page-plc-realtime");
        
        // Finale: naviga ad altra pagina per dispose finale
        await Page.GotoAsync($"{BaseUrl}/");
        
        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "PLC Realtime > Navigazione menu completa (tutte pagine Produzione)")]
    public async Task PlcRealtime_NavigazioneProduzione()
    {
        // Test navigazione completa sezione Produzione
        var promuzionePages = new[]
        {
            "/produzione/dashboard",
            "/produzione/plc-realtime",
            "/produzione/plc-storico",
            "/produzione/incollaggio"
        };

        foreach (var page in promuzionePages)
        {
            await Page.GotoAsync($"{BaseUrl}{page}");
            await Page.WaitForTimeoutAsync(500); // Attende stabilizzazione
        }
        
        // Naviga via dalla sezione
        await Page.GotoAsync($"{BaseUrl}/");
        
        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "PLC Storico > Pagina e griglia visibili")]
    public async Task PlcStorico_PageLoads()
    {
        await Page.GotoAsync($"{BaseUrl}/produzione/plc-storico");
        await AssertVisibleByTestId("page-plc-storico");
        await AssertVisibleByTestId("grid-plc-storico");

        // Esegue un'azione reale (selezione filtro + ricerca)
        await Page.GetByTestId("select-plc-storico-colonna").ClickAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("Enter");
        await Page.GetByTestId("btn-plc-storico-cerca").ClickAsync();

        await AssertNoConsoleErrors();
    }

    [Fact(DisplayName = "Incollaggio > Pagina visibile")]
    public async Task Incollaggio_PageLoads()
    {
        await Page.GotoAsync($"{BaseUrl}/produzione/incollaggio");
        await AssertVisibleByTestId("page-incollaggio");
        await AssertNoConsoleErrors();
    }
}
