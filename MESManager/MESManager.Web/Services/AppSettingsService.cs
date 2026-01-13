namespace MESManager.Web.Services;

public class AppSettingsService
{
    private const string SETTINGS_FILE = "wwwroot/app-settings.json";
    private readonly ILogger<AppSettingsService> _logger;
    private AppSettings _settings;

    public AppSettingsService(ILogger<AppSettingsService> logger)
    {
        _logger = logger;
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
            
            return $"/images/{newFileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nel salvataggio dell'immagine di sfondo");
            throw;
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
    public string BackgroundImageUrl { get; set; } = "/images/industry-background.jpg";
    public string NomeAzienda { get; set; } = string.Empty;
    public string Indirizzo { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
