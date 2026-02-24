using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MESManager.Web.Services;

/// <summary>
/// Estrae una palette di 5 colori dominanti da un'immagine e suggerisce
/// se usare la modalità scura in base alla luminosità media.
/// </summary>
public class ColorExtractionService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ColorExtractionService> _logger;

    /// <summary>Dimensione ridotta per il campionamento (performance).</summary>
    private const int SampleSize = 150;

    /// <summary>Numero di bucket di tonalità (360° / 30° = 12 bucket).</summary>
    private const int HueBuckets = 12;

    /// <summary>Numero di colori estratti nella palette.</summary>
    private const int PaletteSize = 5;

    /// <summary>Saturazione normalizzata per tutti i colori della palette (0-100).</summary>
    private const float NormalizedSaturation = 65f;

    /// <summary>Luminosità normalizzata per i colori della palette (0-100).</summary>
    private const float NormalizedLightness = 48f;

    /// <summary>Soglia luminosità sotto la quale si suggerisce dark mode.</summary>
    private const float DarkModeThreshold = 0.45f;

    public ColorExtractionService(IWebHostEnvironment env, ILogger<ColorExtractionService> logger)
    {
        _env = env;
        _logger = logger;
    }

    /// <summary>
    /// Estrae la palette di colori dominanti dall'immagine indicata.
    /// </summary>
    /// <param name="imageRelativeUrl">URL relativo tipo "/images/sfondo.jpg"</param>
    /// <returns>ColorPalette con 5 colori hex e suggerimento dark mode, oppure null se fallisce.</returns>
    public ColorPalette? ExtractPalette(string imageRelativeUrl)
    {
        try
        {
            // Costruisce percorso fisico da URL relativo  (taglia il "/" iniziale)
            var relativePath = imageRelativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(_env.WebRootPath, relativePath);

            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("ColorExtraction: file non trovato: {Path}", fullPath);
                return null;
            }

            using var image = Image.Load<Rgba32>(fullPath);

            // Ridimensiona a SampleSize × SampleSize per velocizzare il campionamento
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(SampleSize, SampleSize),
                Mode = ResizeMode.Max
            }));

            // Bucket per tonalità e accumulatori per luminosità media totale
            var hueBuckets = new float[HueBuckets];  // conteggio pixel per bucket di tonalità
            var hueSatSum = new float[HueBuckets];   // somma saturazione per media pesata
            var hueLumSum = new float[HueBuckets];   // somma luminosità per media pesata
            float totalLuminance = 0f;
            int pixelCount = 0;

            // Campiona ogni pixel sull'immagine ridimensionata
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    var pixel = image[x, y];
                    // Ignora pixel quasi trasparenti
                    if (pixel.A < 50) continue;

                    var (h, s, l) = RgbToHsl(pixel.R, pixel.G, pixel.B);

                    // Calcola luminanza percettiva
                    float luminance = 0.2126f * LinearizeChannel(pixel.R)
                                    + 0.7152f * LinearizeChannel(pixel.G)
                                    + 0.0722f * LinearizeChannel(pixel.B);
                    totalLuminance += luminance;
                    pixelCount++;

                    // Ignora colori quasi grigi (saturazione troppo bassa)
                    if (s < 0.08f) continue;

                    int bucket = (int)(h / (360f / HueBuckets)) % HueBuckets;
                    hueBuckets[bucket]++;
                    hueSatSum[bucket] += s;
                    hueLumSum[bucket] += l;
                }
            }

            // Seleziona i top PaletteSize bucket più popolati
            var bucketIndices = Enumerable.Range(0, HueBuckets)
                .Where(i => hueBuckets[i] > 0)
                .OrderByDescending(i => hueBuckets[i])
                .Take(PaletteSize)
                .ToList();

            // Se non ci sono abbastanza bucket colorati (immagine quasi in B/N),
            // integra con colori neutri generati dalla tonalità del primo bucket
            while (bucketIndices.Count < PaletteSize)
            {
                // Aggiunge un colore neutro blu-grigio come fallback
                bucketIndices.Add(-bucketIndices.Count - 1);
            }

            var colors = new List<string>();
            foreach (var idx in bucketIndices)
            {
                if (idx >= 0)
                {
                    float count = hueBuckets[idx];
                    float avgHue = idx * (360f / HueBuckets) + (360f / HueBuckets / 2f);

                    // Usa sat/lum normalizzate per coerenza visiva
                    var hex = HslToHex(avgHue, NormalizedSaturation / 100f, NormalizedLightness / 100f);
                    colors.Add(hex);
                }
                else
                {
                    // Fallback color — varianti da blu a viola neutro
                    float fallbackHue = 210f + (-idx - 1) * 30f;
                    colors.Add(HslToHex(fallbackHue % 360f, 0.40f, 0.50f));
                }
            }

            // Luminosità media relativa → suggerisce dark mode
            float avgLuminance = pixelCount > 0 ? totalLuminance / pixelCount : 0.5f;
            bool suggestDark = avgLuminance < DarkModeThreshold;

            _logger.LogInformation("ColorExtraction: estratti {Count} colori, avgLum={Lum:F3}, suggestDark={Dark}",
                colors.Count, avgLuminance, suggestDark);

            return new ColorPalette(colors, suggestDark);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ColorExtraction: errore durante l'estrazione da {Url}", imageRelativeUrl);
            return null;
        }
    }

    // ─── Helpers colore ───────────────────────────────────────────────────────

    /// <summary>Converte RGB (0-255) in HSL (H:0-360, S:0-1, L:0-1).</summary>
    private static (float H, float S, float L) RgbToHsl(byte r, byte g, byte b)
    {
        float rf = r / 255f, gf = g / 255f, bf = b / 255f;
        float max = MathF.Max(rf, MathF.Max(gf, bf));
        float min = MathF.Min(rf, MathF.Min(gf, bf));
        float delta = max - min;

        float l = (max + min) / 2f;
        float s = delta < 0.001f ? 0f : delta / (1f - MathF.Abs(2f * l - 1f));

        float h = 0f;
        if (delta > 0.001f)
        {
            if (max == rf)
                h = 60f * (((gf - bf) / delta) % 6f);
            else if (max == gf)
                h = 60f * (((bf - rf) / delta) + 2f);
            else
                h = 60f * (((rf - gf) / delta) + 4f);
        }

        if (h < 0) h += 360f;
        return (h, s, l);
    }

    /// <summary>Converte HSL (H:0-360, S:0-1, L:0-1) in hex string "#RRGGBB".</summary>
    private static string HslToHex(float h, float s, float l)
    {
        float c = (1f - MathF.Abs(2f * l - 1f)) * s;
        float x = c * (1f - MathF.Abs((h / 60f) % 2f - 1f));
        float m = l - c / 2f;

        float r, g, b;
        if (h < 60)       { r = c;  g = x;  b = 0f; }
        else if (h < 120) { r = x;  g = c;  b = 0f; }
        else if (h < 180) { r = 0f; g = c;  b = x; }
        else if (h < 240) { r = 0f; g = x;  b = c; }
        else if (h < 300) { r = x;  g = 0f; b = c; }
        else              { r = c;  g = 0f; b = x; }

        int ri = (int)MathF.Round((r + m) * 255f);
        int gi = (int)MathF.Round((g + m) * 255f);
        int bi = (int)MathF.Round((b + m) * 255f);

        return $"#{Math.Clamp(ri, 0, 255):X2}{Math.Clamp(gi, 0, 255):X2}{Math.Clamp(bi, 0, 255):X2}";
    }

    /// <summary>Linearizza un canale 0-255 per il calcolo della luminanza relativa.</summary>
    private static float LinearizeChannel(byte c)
    {
        float v = c / 255f;
        return v <= 0.04045f ? v / 12.92f : MathF.Pow((v + 0.055f) / 1.055f, 2.4f);
    }
}

/// <summary>Palette estratta da un'immagine.</summary>
/// <param name="Colors">Lista di 5 colori hex estratti (#RRGGBB).</param>
/// <param name="SuggestDarkMode">True se la luminosità media dell'immagine suggerisce la modalità scura.</param>
public record ColorPalette(List<string> Colors, bool SuggestDarkMode);
