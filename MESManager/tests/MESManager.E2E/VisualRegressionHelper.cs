using Microsoft.Playwright;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Globalization;

namespace MESManager.E2E;

/// <summary>
/// Helper per visual regression testing con screenshot baseline e diff detection.
/// Usa SixLabors.ImageSharp per confronto pixel-by-pixel.
/// </summary>
public class VisualRegressionHelper
{
    private readonly IPage _page;
    private readonly string _baselineDir;
    private readonly string _diffDir;
    private readonly bool _updateBaselines;
    private readonly float _diffThreshold;

    public VisualRegressionHelper(IPage page, string testName)
    {
        _page = page;
        
        // Directory struttura: VisualBaselines/chromium/{testName}/
        var projectRoot = GetProjectRoot();

        // Leggi variabile ambiente o flag file per aggiornare baseline
        var updateFromEnv = Environment.GetEnvironmentVariable("UPDATE_BASELINES") == "true";
        var updateFromFlag = File.Exists(Path.Combine(projectRoot, "UPDATE_BASELINES.flag"));
        _updateBaselines = updateFromEnv || updateFromFlag;
        
        // Threshold predefinito 1% (configurabile via env var)
        var thresholdStr = Environment.GetEnvironmentVariable("VISUAL_DIFF_THRESHOLD") ?? "0.01";
        _diffThreshold = float.Parse(thresholdStr, CultureInfo.InvariantCulture);

        _baselineDir = Path.Combine(projectRoot, "VisualBaselines", "chromium", testName);
        _diffDir = Path.Combine(projectRoot, "playwright-results", "visual-diffs", testName);
        
        Directory.CreateDirectory(_baselineDir);
        Directory.CreateDirectory(_diffDir);
    }

    /// <summary>
    /// Confronta screenshot corrente con baseline.
    /// Se UPDATE_BASELINES=true, salva come nuova baseline.
    /// Se diff > threshold, genera immagine diff e lancia eccezione.
    /// </summary>
    public async Task AssertMatchesBaseline(string screenshotName, ILocator? locator = null)
    {
        var baselinePath = Path.Combine(_baselineDir, $"{screenshotName}.png");
        var actualPath = Path.Combine(_diffDir, $"{screenshotName}-actual.png");
        var diffPath = Path.Combine(_diffDir, $"{screenshotName}-diff.png");

        // Cattura screenshot
        byte[] actualBytes;
        if (locator != null)
        {
            actualBytes = await locator.ScreenshotAsync(new LocatorScreenshotOptions
            {
                Animations = ScreenshotAnimations.Disabled,
                Timeout = 5000
            });
        }
        else
        {
            actualBytes = await _page.ScreenshotAsync(new PageScreenshotOptions
            {
                FullPage = true,
                Animations = ScreenshotAnimations.Disabled,
                Timeout = 10000
            });
        }

        // Modalità UPDATE_BASELINES: salva e esci
        if (_updateBaselines)
        {
            await File.WriteAllBytesAsync(baselinePath, actualBytes);
            Console.WriteLine($"✅ Baseline aggiornata: {baselinePath}");
            return;
        }

        // Verifica baseline esiste
        if (!File.Exists(baselinePath))
        {
            await File.WriteAllBytesAsync(actualPath, actualBytes);
            throw new FileNotFoundException(
                $"❌ BASELINE MANCANTE: {baselinePath}\n" +
                $"Esegui: UPDATE_BASELINES=true dotnet test --filter \"{screenshotName}\""
            );
        }

        // Confronto pixel-by-pixel
        var baselineBytes = await File.ReadAllBytesAsync(baselinePath);
        var diffPercentage = await CompareImages(baselineBytes, actualBytes, diffPath);

        if (diffPercentage > _diffThreshold)
        {
            await File.WriteAllBytesAsync(actualPath, actualBytes);
            throw new Exception(
                $"❌ VISUAL REGRESSION DETECTED\n" +
                $"Screenshot: {screenshotName}\n" +
                $"Diff: {diffPercentage:P2} (threshold: {_diffThreshold:P2})\n" +
                $"Files:\n" +
                $"  - Baseline: {baselinePath}\n" +
                $"  - Actual: {actualPath}\n" +
                $"  - Diff: {diffPath}\n\n" +
                $"Per aggiornare baseline:\n" +
                $"  UPDATE_BASELINES=true dotnet test --filter \"{screenshotName}\""
            );
        }

        Console.WriteLine($"✅ Visual check passed: {screenshotName} (diff: {diffPercentage:P2})");
    }

    /// <summary>
    /// Confronta due immagini pixel-by-pixel e genera diff highlight.
    /// Restituisce percentuale di pixel diversi (0.0 = identici, 1.0 = completamente diversi).
    /// </summary>
    private async Task<float> CompareImages(byte[] baselineBytes, byte[] actualBytes, string diffOutputPath)
    {
        using var baseline = Image.Load<Rgba32>(baselineBytes);
        using var actual = Image.Load<Rgba32>(actualBytes);

        // Controlla dimensioni
        if (baseline.Width != actual.Width || baseline.Height != actual.Height)
        {
            // Dimensioni diverse = 100% diff
            return 1.0f;
        }

        var diffImage = new Image<Rgba32>(baseline.Width, baseline.Height);
        var totalPixels = baseline.Width * baseline.Height;
        var differentPixels = 0;

        // Confronto pixel-by-pixel con tolleranza
        const int tolerance = 10; // Tolleranza RGB per anti-aliasing

        for (int y = 0; y < baseline.Height; y++)
        {
            for (int x = 0; x < baseline.Width; x++)
            {
                var baselinePixel = baseline[x, y];
                var actualPixel = actual[x, y];

                var rDiff = Math.Abs(baselinePixel.R - actualPixel.R);
                var gDiff = Math.Abs(baselinePixel.G - actualPixel.G);
                var bDiff = Math.Abs(baselinePixel.B - actualPixel.B);

                if (rDiff > tolerance || gDiff > tolerance || bDiff > tolerance)
                {
                    differentPixels++;
                    // Evidenzia diff in ROSSO brillante
                    diffImage[x, y] = new Rgba32(255, 0, 0, 255);
                }
                else
                {
                    // Pixel identico -> grigio chiaro (per vedere contesto)
                    var gray = (byte)((baselinePixel.R + baselinePixel.G + baselinePixel.B) / 3);
                    diffImage[x, y] = new Rgba32(gray, gray, gray, 128);
                }
            }
        }

        var diffPercentage = (float)differentPixels / totalPixels;

        // Salva immagine diff solo se ci sono differenze
        if (diffPercentage > 0.001f)
        {
            await diffImage.SaveAsPngAsync(diffOutputPath);
        }

        return diffPercentage;
    }

    private static string GetProjectRoot()
    {
        var currentDir = Directory.GetCurrentDirectory();
        while (currentDir != null && !File.Exists(Path.Combine(currentDir, "MESManager.E2E.csproj")))
        {
            currentDir = Directory.GetParent(currentDir)?.FullName;
        }
        return currentDir ?? throw new DirectoryNotFoundException("Impossibile trovare project root");
    }
}
