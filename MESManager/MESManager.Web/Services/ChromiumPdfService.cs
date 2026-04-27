using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace MESManager.Web.Services;

/// <summary>
/// Genera PDF da HTML usando Chrome/Edge in modalità headless.
/// Non richiede NuGet aggiuntivi: usa l'eseguibile già installato sul PC.
/// v1.65.58
/// </summary>
public class ChromiumPdfService(ILogger<ChromiumPdfService> logger)
{
    // Path di sistema e per-utente (Edge su Win11 si installa spesso in AppData)
    private static IEnumerable<string> GetCandidates()
    {
        var localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return
        [
            @"C:\Program Files\Google\Chrome\Application\chrome.exe",
            @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
            Path.Combine(localApp, @"Google\Chrome\Application\chrome.exe"),
            @"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
            @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
            Path.Combine(localApp, @"Microsoft\Edge\Application\msedge.exe"),
        ];
    }

    public bool IsAvailable() => FindExecutable() != null;

    private string? FindExecutable()
    {
        var exe = GetCandidates().FirstOrDefault(File.Exists);
        if (exe != null)
            logger.LogInformation("ChromiumPdfService: trovato browser → {Exe}", exe);
        else
            logger.LogWarning("ChromiumPdfService: Chrome/Edge non trovato nei path cercati: {Paths}",
                string.Join(", ", GetCandidates()));
        return exe;
    }

    /// <summary>
    /// Converte HTML in PDF A4 via headless Chrome/Edge.
    /// Ritorna (bytes, errorMessage): bytes=null e errorMessage valorizzato se fallisce.
    /// </summary>
    public async Task<(byte[]? Bytes, string? Error)> GeneratePdfAsync(string html, CancellationToken ct = default)
    {
        var exe = FindExecutable();
        if (exe == null)
            return (null, "Chrome/Edge non trovato.");

        var tempDir     = Path.GetTempPath();
        var inputFile   = Path.Combine(tempDir, $"mesprev_{Guid.NewGuid():N}.html");
        var outputFile  = Path.Combine(tempDir, $"mesprev_{Guid.NewGuid():N}.pdf");
        var tempProfile = Path.Combine(tempDir, $"mesprev_p_{Guid.NewGuid():N}");

        try
        {
            await File.WriteAllTextAsync(inputFile, html, Encoding.UTF8, ct);

            // file:/// URI — Chrome richiede forward slash
            var uri = "file:///" + inputFile.Replace('\\', '/');

            // ArgumentList evita ogni problema di quoting su Windows
            var psi = new ProcessStartInfo(exe)
            {
                UseShellExecute        = false,
                CreateNoWindow         = true,
                RedirectStandardOutput = false,   // non ci serve, non redirigere per evitare deadlock
                RedirectStandardError  = true,
            };
            psi.ArgumentList.Add("--headless=new");  // Chrome 112+: no header/footer di default, no flag --print-to-pdf-no-header-footer necessario
            psi.ArgumentList.Add($"--print-to-pdf={outputFile}");
            psi.ArgumentList.Add("--run-all-compositor-stages-before-draw");
            psi.ArgumentList.Add("--virtual-time-budget=5000");
            psi.ArgumentList.Add("--disable-gpu");
            psi.ArgumentList.Add("--no-first-run");
            psi.ArgumentList.Add("--disable-extensions");
            psi.ArgumentList.Add($"--user-data-dir={tempProfile}");
            psi.ArgumentList.Add(uri);

            logger.LogInformation("ChromiumPdfService: avvio {Exe} con outputFile={Out}", exe, outputFile);

            using var proc = Process.Start(psi);
            if (proc == null)
                return (null, "Process.Start ha restituito null.");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            // Legge stderr in parallelo con WaitForExitAsync per evitare deadlock
            var stderrTask = proc.StandardError.ReadToEndAsync(cts.Token);
            await proc.WaitForExitAsync(cts.Token);
            var stdErr = await stderrTask;

            logger.LogInformation("ChromiumPdfService: exitCode={Code}, stderr={Err}", proc.ExitCode, stdErr);

            if (!File.Exists(outputFile))
                return (null, $"File PDF non creato. ExitCode={proc.ExitCode}. Stderr: {stdErr}");

            var bytes = await File.ReadAllBytesAsync(outputFile, ct);
            logger.LogInformation("ChromiumPdfService: PDF generato {Bytes} byte", bytes.Length);

            return bytes.Length > 500
                ? (bytes, null)
                : (null, $"PDF troppo piccolo ({bytes.Length} byte). Stderr: {stdErr}");
        }
        finally
        {
            try { if (File.Exists(inputFile))        File.Delete(inputFile);              } catch { }
            try { if (File.Exists(outputFile))       File.Delete(outputFile);             } catch { }
            try { if (Directory.Exists(tempProfile)) Directory.Delete(tempProfile, true); } catch { }
        }
    }
}
