namespace MESManager.Domain.Constants;

/// <summary>
/// Costanti per la gestione dei file nell'applicazione
/// </summary>
public static class FileConstants
{
    /// <summary>
    /// Estensioni considerate come immagini/foto
    /// </summary>
    public static readonly HashSet<string> FotoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif"
    };

    /// <summary>
    /// Estensioni documenti comuni
    /// </summary>
    public static readonly HashSet<string> DocumentExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".csv"
    };

    /// <summary>
    /// Path di rete predefinito per allegati (produzione)
    /// </summary>
    public const string DefaultNetworkPath = @"P:\Documenti\AA SCHEDE PRODUZIONE\foto cel";

    /// <summary>
    /// Sottocartella per upload allegati
    /// </summary>
    public const string UploadSubfolder = "uploads/allegati";

    /// <summary>
    /// Verifica se un file è una foto basandosi sull'estensione
    /// </summary>
    public static bool IsFoto(string fileName)
    {
        var ext = Path.GetExtension(fileName);
        return !string.IsNullOrEmpty(ext) && FotoExtensions.Contains(ext);
    }

    /// <summary>
    /// Verifica se un file è un documento basandosi sull'estensione
    /// </summary>
    public static bool IsDocument(string fileName)
    {
        var ext = Path.GetExtension(fileName);
        return !string.IsNullOrEmpty(ext) && DocumentExtensions.Contains(ext);
    }

    /// <summary>
    /// Ottiene il path base per gli allegati, con fallback a cartella locale
    /// </summary>
    public static string GetAllegatiBasePath(string? configuredPath = null)
    {
        var basePath = configuredPath ?? DefaultNetworkPath;
        
        // Se il path di rete è accessibile, usalo
        if (Directory.Exists(basePath))
        {
            return basePath;
        }

        // Fallback a cartella locale
        var localPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", UploadSubfolder);
        Directory.CreateDirectory(localPath);
        return localPath;
    }
}
