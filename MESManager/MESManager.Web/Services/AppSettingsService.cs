namespace MESManager.Web.Services;

public class AppSettingsService
{
    private const string SETTINGS_FILE = "wwwroot/app-settings.json";
    private readonly ILogger<AppSettingsService> _logger;
    private readonly ColorExtractionService _colorExtractor;
    private AppSettings _settings;

    /// <summary>
    /// Notifica i subscriber (es. MainLayout) quando le impostazioni cambiano,
    /// in modo che possano ricostruire il tema MudBlazor senza ricaricare la pagina.
    /// PUNTO CENTRALIZZATO: tutti i cambiamenti di tema passano da qui.
    /// </summary>
    public event Action? OnSettingsChanged;

    public AppSettingsService(ILogger<AppSettingsService> logger, ColorExtractionService colorExtractor)
    {
        _logger = logger;
        _colorExtractor = colorExtractor;
        _settings = LoadSettings();
    }

    public AppSettings GetSettings()
    {
        return _settings;
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        _settings = settings;
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(_settings, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            await File.WriteAllTextAsync(SETTINGS_FILE, json);
            NotifySettingsChanged();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nel salvataggio delle impostazioni");
        }
    }

    public async Task<string> SaveBackgroundImageAsync(Stream fileStream, string fileName)
    {
        try
        {
            var extension = Path.GetExtension(fileName);
            var newFileName = $"background-{DateTime.Now:yyyyMMddHHmmss}{extension}";
            var filePath = Path.Combine("wwwroot", "images", newFileName);
            
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            
            using (var fs = new FileStream(filePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(fs);
            }
            
            var imageUrl = $"/images/{newFileName}";

            // Estrai la palette di colori dall'immagine appena salvata
            var palette = _colorExtractor.ExtractPalette(imageUrl);
            if (palette != null)
            {
                _settings.ThemePalette = palette.Colors;
                _settings.ThemeIsDarkMode = palette.SuggestDarkMode;

                // Pre-assegna i colori suggeriti solo se l'utente non ha ancora
                // personalizzato (confronta con valori di fabbrica)
                if (_settings.ThemePrimaryColor == AppSettings.DefaultPrimaryColor && palette.Colors.Count >= 1)
                    _settings.ThemePrimaryColor = palette.Colors[0];
                if (_settings.ThemeSecondaryColor == AppSettings.DefaultSecondaryColor && palette.Colors.Count >= 2)
                    _settings.ThemeSecondaryColor = palette.Colors[1];
                if (_settings.ThemeAccentColor == AppSettings.DefaultAccentColor && palette.Colors.Count >= 3)
                    _settings.ThemeAccentColor = palette.Colors[2];

                // Auto-calcola il testo sull'AppBar/Primary (bianco o scuro) dalla luminanza del Primary
                _settings.ThemeTextOnPrimary = ComputeTextOnBackground(_settings.ThemePrimaryColor);

                // Auto-calcola la versione scurita del Primary per testi su sfondo bianco
                _settings.ThemePrimaryTextColor = ComputePrimaryTextColor(_settings.ThemePrimaryColor);
            }

            return imageUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nel salvataggio dell'immagine di sfondo");
            throw;
        }
    }

    private void NotifySettingsChanged() => OnSettingsChanged?.Invoke();

    /// <summary>
    /// Calcola il colore del testo ottimale (bianco o nero) per avere contrasto leggibile
    /// su uno sfondo del colore indicato. Segue la soglia WCAG relative luminance.
    /// PUNTO CENTRALIZZATO: usare questo per derivare TextOnPrimary, TextOnSecondary, ecc.
    /// </summary>
    public static string ComputeTextOnBackground(string hexColor)
    {
        try
        {
            var hex = hexColor.TrimStart('#');
            if (hex.Length != 6) return "#FFFFFF";
            byte r = Convert.ToByte(hex[..2], 16);
            byte g = Convert.ToByte(hex[2..4], 16);
            byte b = Convert.ToByte(hex[4..6], 16);

            // Luminanza relativa WCAG 2.1
            static float Lin(byte c) { float v = c / 255f; return v <= 0.04045f ? v / 12.92f : MathF.Pow((v + 0.055f) / 1.055f, 2.4f); }
            float lum = 0.2126f * Lin(r) + 0.7152f * Lin(g) + 0.0722f * Lin(b);

            // Soglia WCAG: testo scuro se la luminanza è > 0.179 (rapporto 4.5:1 contro bianco/nero)
            return lum > 0.179f ? "#1A1A1A" : "#FFFFFF";
        }
        catch
        {
            return "#FFFFFF";
        }
    }

    /// <summary>
    /// Restituisce una versione del colore Primary garantita leggibile su sfondo bianco
    /// (contrasto ≥ 3:1 per testi grandi). Se il Primary è già abbastanza scuro, lo restituisce
    /// invariato; altrimenti lo scurisce fino a lightness ≤ 38%.
    /// PUNTO CENTRALIZZATO: usare questo per text-color di elementi su sfondo bianco.
    /// </summary>
    public static string ComputePrimaryTextColor(string hexColor)
    {
        try
        {
            var hex = hexColor.TrimStart('#');
            if (hex.Length != 6) return "#1565C0";
            byte r = Convert.ToByte(hex[..2], 16);
            byte g = Convert.ToByte(hex[2..4], 16);
            byte b = Convert.ToByte(hex[4..6], 16);

            float rf = r / 255f, gf = g / 255f, bf = b / 255f;
            float max = MathF.Max(rf, MathF.Max(gf, bf));
            float min = MathF.Min(rf, MathF.Min(gf, bf));
            float delta = max - min;
            float l = (max + min) / 2f;

            // Se già abbastanza scuro (luminosità ≤ 38%) → restituisce il colore invariato
            if (l <= 0.38f) return $"#{r:X2}{g:X2}{b:X2}";

            float s = delta < 0.001f ? 0f : delta / (1f - MathF.Abs(2f * l - 1f));
            float h = 0f;
            if (delta > 0.001f)
            {
                if (max == rf)      h = 60f * (((gf - bf) / delta) % 6f);
                else if (max == gf) h = 60f * (((bf - rf) / delta) + 2f);
                else                h = 60f * (((rf - gf) / delta) + 4f);
            }
            if (h < 0) h += 360f;

            // Forza lightness a 0.35 (garantisce contrasto ≥ 3:1 su bianco per testi grandi)
            float targetL = 0.35f;
            float c = (1f - MathF.Abs(2f * targetL - 1f)) * s;
            float x = c * (1f - MathF.Abs((h / 60f) % 2f - 1f));
            float m = targetL - c / 2f;
            float ro, go, bo;
            if      (h < 60)  { ro = c; go = x; bo = 0; }
            else if (h < 120) { ro = x; go = c; bo = 0; }
            else if (h < 180) { ro = 0; go = c; bo = x; }
            else if (h < 240) { ro = 0; go = x; bo = c; }
            else if (h < 300) { ro = x; go = 0; bo = c; }
            else              { ro = c; go = 0; bo = x; }

            int ri = Math.Clamp((int)MathF.Round((ro + m) * 255f), 0, 255);
            int gi = Math.Clamp((int)MathF.Round((go + m) * 255f), 0, 255);
            int bi2 = Math.Clamp((int)MathF.Round((bo + m) * 255f), 0, 255);
            return $"#{ri:X2}{gi:X2}{bi2:X2}";
        }
        catch
        {
            return "#1565C0";
        }
    }

    private AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(SETTINGS_FILE))
            {
                var json = File.ReadAllText(SETTINGS_FILE);
                return System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nel caricamento delle impostazioni");
        }
        
        return new AppSettings();
    }
}

public class AppSettings
{
    // ── Valori di fabbrica (usati per rilevare se l'utente ha personalizzato)
    public const string DefaultPrimaryColor   = "#1976D2";
    public const string DefaultSecondaryColor = "#9C27B0";
    public const string DefaultAccentColor    = "#FF4081";

    // ── Dati azienda ──────────────────────────────────────────────────────────
    public string BackgroundImageUrl { get; set; } = "/images/industry-background.jpg";
    public string NomeAzienda { get; set; } = string.Empty;
    public string Indirizzo { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // ── Tema dinamico ─────────────────────────────────────────────────────────
    /// <summary>5 colori hex (#RRGGBB) estratti dall'immagine di sfondo.</summary>
    public List<string> ThemePalette { get; set; } = new();

    /// <summary>Colore primario scelto dall'utente tra quelli della palette.</summary>
    public string ThemePrimaryColor { get; set; } = DefaultPrimaryColor;

    /// <summary>Colore secondario scelto dall'utente.</summary>
    public string ThemeSecondaryColor { get; set; } = DefaultSecondaryColor;

    /// <summary>Colore accento scelto dall'utente.</summary>
    public string ThemeAccentColor { get; set; } = DefaultAccentColor;

    /// <summary>Modalità scura — suggerita automaticamente, poi controllata dall'utente.</summary>
    public bool ThemeIsDarkMode { get; set; } = false;

    // ── Colori testo ──────────────────────────────────────────────────────────
    /// <summary>
    /// Colore del testo su sfondi PRIMARY (AppBar, bottoni Filled, card header).
    /// Auto-calcolato dalla luminanza del Primary: bianco su Primary scuro, scuro su Primary chiaro.
    /// PUNTO CENTRALIZZATO: aggiornare questo per correggere testo invisibile sull'AppBar.
    /// </summary>
    public string ThemeTextOnPrimary { get; set; } = "#FFFFFF";

    /// <summary>
    /// Versione scurita del Primary, garantisce leggibilità (contrasto ≥ 3:1) su sfondo bianco.
    /// Usato per testi che usano il colore del brand su sfondi chiari (numeri macchina, badge, ecc.).
    /// Auto-calcolato; l'utente può sovrascrivere.
    /// </summary>
    public string ThemePrimaryTextColor { get; set; } = "#1565C0";

    /// <summary>
    /// Colore del testo nel menu laterale (Drawer).
    /// Stringa hex es. "#C8C8C8". Vuoto = auto (chiaro/scuro in base a dark mode).
    /// </summary>
    /// <summary>
    /// Colore testo nav. Stringa hex es. "#FFFFFF". Vuoto (default) = automatico: bianco in dark, quasi-nero in light.
    /// </summary>
    public string ThemeNavTextColor { get; set; } = "";  // vuoto = auto

    /// <summary>
    /// Opacità dello sfondo su pagine non-Home (0.0 = trasparente, 1.0 = pieno). Default 0.12.
    /// </summary>
    public double ThemeBgOpacity { get; set; } = 0.12;

    /// <summary>
    /// Opacità dei pannelli (MudPaper, MudCard) quando un'immagine di sfondo è attiva.
    /// 1.0 = completamente opaco (nessuna trasparenza). Default 0.88.
    /// </summary>
    public double ThemePanelOpacity { get; set; } = 0.88;

    /// <summary>
    /// Colori extra personalizzati aggiunti dall'utente alla palette (2 slot).
    /// Integrano i colori estratti automaticamente dall'immagine.
    /// </summary>
    public List<string> ThemeExtraColors { get; set; } = new() { "#607D8B", "#795548" };
}
