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
    protected string BaseUrl => "http://127.0.0.1:5156";
    protected ITestOutputHelper _output { get; private set; }
    
    private readonly List<string> _consoleErrors = new();
    private readonly List<string> _pageErrors = new();
    private Process? _webAppProcess;

    public PlaywrightTestBase(ITestOutputHelper output)
    {
        _output = output;
    }
    
    private static readonly bool IsHeaded = Environment.GetEnvironmentVariable("PLAYWRIGHT_HEADED") == "1";
    private static readonly int SlowMo = int.TryParse(Environment.GetEnvironmentVariable("PLAYWRIGHT_SLOWMO"), out var slowmo) ? slowmo : 0;

    public virtual async Task InitializeAsync()
    {
        // Avvia l'applicazione web come processo separato
        var projectPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "MESManager.Web"));
        
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
        var timeout = DateTime.Now.AddSeconds(30);
        
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
            throw new Exception("Web server failed to start within 30 seconds");
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
                _consoleErrors.Add($"Console Error: {msg.Text}");
            }
        };

        // Cattura page errors (eccezioni JavaScript non gestite)
        Page.PageError += (_, exception) =>
        {
            _pageErrors.Add($"Page Error: {exception}");
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
}
