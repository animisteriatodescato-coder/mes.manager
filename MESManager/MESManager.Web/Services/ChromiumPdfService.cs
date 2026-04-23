using System.Diagnostics;
using System.Text;

namespace MESManager.Web.Services;

/// <summary>
/// Genera PDF da HTML usando Chrome/Edge in modalità headless.
/// Non richiede NuGet aggiuntivi: usa l'eseguibile già installato sul PC.
/// v1.65.56
/// </summary>
public class ChromiumPdfService
{
    private static readonly string[] _candidates =
    [
        @"C:\Program Files\Google\Chrome\Application\chrome.exe",
        @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
        @"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
        @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
    ];

    public bool IsAvailable() => FindExecutable() != null;

    private static string? FindExecutable() => _candidates.FirstOrDefault(File.Exists);

    /// <summary>
    /// Converte HTML in PDF A4 via headless Chrome/Edge.
    /// Ritorna i byte del PDF, oppure null se fallisce.
    /// </summary>
    public async Task<byte[]?> GeneratePdfAsync(string html, CancellationToken ct = default)
    {
        var exe = FindExecutable();
        if (exe == null) return null;

        var tempDir   = Path.GetTempPath();
        var inputFile  = Path.Combine(tempDir, $"mesprev_{Guid.NewGuid():N}.html");
        var outputFile = Path.Combine(tempDir, $"mesprev_{Guid.NewGuid():N}.pdf");
        var tempProfile = Path.Combine(tempDir, $"mesprev_p_{Guid.NewGuid():N}");

        try
        {
            await File.WriteAllTextAsync(inputFile, html, Encoding.UTF8, ct);

            // file:/// URI — Chrome richiede forward slash
            var uri = "file:///" + inputFile.Replace('\\', '/');

            // --virtual-time-budget: aspetta il rendering CSS/font prima di stampare
            // I margini sono gestiti dal CSS @page nel documento HTML
            var args = $"--headless=new " +
                       $"--print-to-pdf=\"{outputFile}\" " +
                       $"--print-to-pdf-no-header-footer " +
                       $"--run-all-compositor-stages-before-draw " +
                       $"--virtual-time-budget=5000 " +
                       $"--disable-gpu " +
                       $"--no-first-run " +
                       $"--disable-extensions " +
                       $"--user-data-dir=\"{tempProfile}\" " +
                       $"\"{uri}\"";

            var psi = new ProcessStartInfo(exe, args)
            {
                UseShellExecute = false,
                CreateNoWindow  = true,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
            };

            using var proc = Process.Start(psi);
            if (proc == null) return null;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(30));
            await proc.WaitForExitAsync(cts.Token);

            if (!File.Exists(outputFile)) return null;
            var bytes = await File.ReadAllBytesAsync(outputFile, ct);
            // Un PDF valido ha sempre più di 500 byte
            return bytes.Length > 500 ? bytes : null;
        }
        finally
        {
            try { if (File.Exists(inputFile))  File.Delete(inputFile);  } catch { /* ignore */ }
            try { if (File.Exists(outputFile)) File.Delete(outputFile); } catch { /* ignore */ }
            try { if (Directory.Exists(tempProfile)) Directory.Delete(tempProfile, recursive: true); } catch { /* ignore */ }
        }
    }
}
