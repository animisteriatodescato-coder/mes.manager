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
}
