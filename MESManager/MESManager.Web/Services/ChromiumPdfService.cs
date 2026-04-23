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

        var tempDir = Path.GetTempPath();
        var inputFile  = Path.Combine(tempDir, $"mesprev_{Guid.NewGuid():N}.html");
        var outputFile = Path.Combine(tempDir, $"mesprev_{Guid.NewGuid():N}.pdf");

        try
        {
            await File.WriteAllTextAsync(inputFile, html, Encoding.UTF8, ct);

            // file:/// URI (Chrome richiede forward slash)
            var uri = "file:///" + inputFile.Replace('\\', '/');

            // --print-to-pdf-no-header-footer: niente timbro URL/data in cima e fondo
            // --no-margins: rispetta i margini definiti dal CSS @page
            var args = $"--headless=new " +
                       $"--print-to-pdf=\"{outputFile}\" " +
                       $"--print-to-pdf-no-header-footer " +
                       $"--no-margins " +
                       $"--run-all-compositor-stages-before-draw " +
                       $"--disable-gpu " +
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

            // Timeout 30s per sicurezza
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(30));
            await proc.WaitForExitAsync(cts.Token);

            if (File.Exists(outputFile))
                return await File.ReadAllBytesAsync(outputFile, ct);

            return null;
        }
        finally
        {
            try { if (File.Exists(inputFile))  File.Delete(inputFile);  } catch { /* ignore */ }
            try { if (File.Exists(outputFile)) File.Delete(outputFile); } catch { /* ignore */ }
        }
    }
}
