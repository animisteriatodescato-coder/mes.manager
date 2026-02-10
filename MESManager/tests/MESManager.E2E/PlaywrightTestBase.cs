using Microsoft.Playwright;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Xunit.Abstractions;

namespace MESManager.E2E;

public class PlaywrightTestBase : IAsyncLifetime
{
    protected IPlaywright Playwright { get; private set; } = null!;
    protected IBrowser Browser { get; private set; } = null!;
    protected IBrowserContext Context { get; private set; } = null!;
    protected IPage Page { get; private set; } = null!;
    protected string BaseUrl => Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:5156";
    protected ITestOutputHelper _output { get; private set; }
    
    private readonly List<string> _consoleErrors = new();
    private readonly List<string> _pageErrors = new();
    private Process? _webAppProcess;

    private static readonly string[] IgnoredErrorSnippets =
    {
        "AG Grid: cannot get grid to draw rows when it is in the middle of drawing rows"
    };

    public PlaywrightTestBase(ITestOutputHelper output)
    {
        _output = output;
    }
    
    private static readonly bool IsHeaded = Environment.GetEnvironmentVariable("PLAYWRIGHT_HEADED") == "1";
    private static readonly bool UseExistingServer =
        (Environment.GetEnvironmentVariable("E2E_USE_EXISTING_SERVER") ?? "").Equals("1", StringComparison.OrdinalIgnoreCase) ||
        (Environment.GetEnvironmentVariable("E2E_USE_EXISTING_SERVER") ?? "").Equals("true", StringComparison.OrdinalIgnoreCase);
    private static readonly int SlowMo = int.TryParse(Environment.GetEnvironmentVariable("PLAYWRIGHT_SLOWMO"), out var slowmo) ? slowmo : 0;
    private static readonly int ServerStartupTimeoutSeconds = int.TryParse(
        Environment.GetEnvironmentVariable("WEBAPP_STARTUP_TIMEOUT_SECONDS"),
        out var timeoutSeconds) ? timeoutSeconds : 90;

    public virtual async Task InitializeAsync()
    {
        if (!UseExistingServer)
        {
            // Avvia l'applicazione web come processo separato
            var solutionRoot = FindSolutionRoot();
            var projectPath = Path.Combine(solutionRoot, "MESManager.Web");

            _webAppProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"run --urls {BaseUrl}",
                    WorkingDirectory = projectPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            _webAppProcess.Start();

            // Aspetta che il server sia pronto (controlla quando il log dice "Now listening")
            var serverReady = false;
            var timeout = DateTime.Now.AddSeconds(ServerStartupTimeoutSeconds);

            _webAppProcess.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null && e.Data.Contains("Now listening"))
                {
                    serverReady = true;
                }
            };

            _webAppProcess.BeginOutputReadLine();

            while (!serverReady && DateTime.Now < timeout)
            {
                await Task.Delay(100);
            }

            if (!serverReady)
            {
                throw new Exception($"Web server failed to start within {ServerStartupTimeoutSeconds} seconds");
            }
        }
        
        // Inizializza Playwright
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = !IsHeaded,
            SlowMo = SlowMo
        });

        Context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            IgnoreHTTPSErrors = true
        });

        Page = await Context.NewPageAsync();

        // Cattura errori console
        Page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                if (!IgnoredErrorSnippets.Any(snippet => msg.Text.Contains(snippet, StringComparison.OrdinalIgnoreCase)))
                {
                    _consoleErrors.Add($"Console Error: {msg.Text}");
                }
            }
        };

        // Cattura page errors (eccezioni JavaScript non gestite)
        Page.PageError += (_, exception) =>
        {
            var message = exception?.ToString() ?? string.Empty;
            if (!IgnoredErrorSnippets.Any(snippet => message.Contains(snippet, StringComparison.OrdinalIgnoreCase)))
            {
                _pageErrors.Add($"Page Error: {exception}");
            }
        };
    }

    public virtual async Task DisposeAsync()
    {
        // Cattura screenshot se ci sono errori
        var testName = GetCurrentTestName();
        
        if (_consoleErrors.Count > 0 || _pageErrors.Count > 0)
        {
            await CaptureFailureArtifacts(testName);
            
            var allErrors = string.Join(Environment.NewLine, _consoleErrors.Concat(_pageErrors));
            throw new Exception($"Test failed with console/page errors:{Environment.NewLine}{allErrors}");
        }

        if (Page != null) await Page.CloseAsync();
        if (Context != null) await Context.CloseAsync();
        if (Browser != null) await Browser.CloseAsync();
        Playwright?.Dispose();
        
        // Ferma il processo dell'app web
        if (_webAppProcess != null && !_webAppProcess.HasExited)
        {
            _webAppProcess.Kill(true);
            _webAppProcess.Dispose();
        }
    }

    protected async Task CaptureFailureArtifacts(string testName)
    {
        var sanitizedName = Regex.Replace(testName, @"[^a-zA-Z0-9_-]", "_");
        var outputDir = Path.Combine("TestResults", "Playwright", sanitizedName);
        Directory.CreateDirectory(outputDir);

        // Screenshot
        var screenshotPath = Path.Combine(outputDir, "screenshot.png");
        await Page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath, FullPage = true });

        // Salva errori console in un file
        var errorsPath = Path.Combine(outputDir, "errors.txt");
        var allErrors = string.Join(Environment.NewLine + "---" + Environment.NewLine, _consoleErrors.Concat(_pageErrors));
        await File.WriteAllTextAsync(errorsPath, allErrors);

        Console.WriteLine($"Artifacts saved to: {outputDir}");
    }

    private string GetCurrentTestName()
    {
        var stackTrace = Environment.StackTrace;
        var match = Regex.Match(stackTrace, @"at MESManager\.E2E\.\w+\.(\w+)");
        return match.Success ? match.Groups[1].Value : "UnknownTest";
    }

    protected Task AssertNoConsoleErrors()
    {
        if (_consoleErrors.Count > 0)
        {
            throw new Exception($"Console errors detected: {string.Join(", ", _consoleErrors)}");
        }
        return Task.CompletedTask;
    }

    private static string FindSolutionRoot()
    {
        var currentDir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(currentDir))
        {
            var slnPath = Path.Combine(currentDir, "MESManager.sln");
            if (File.Exists(slnPath))
            {
                return currentDir;
            }

            currentDir = Directory.GetParent(currentDir)?.FullName ?? string.Empty;
        }

        throw new DirectoryNotFoundException("Impossibile trovare MESManager.sln. Verifica la struttura del repository.");
    }
}
