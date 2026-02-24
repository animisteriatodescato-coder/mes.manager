using Microsoft.Extensions.Logging;
using MESManager.Application.Configuration;
using MESManager.Domain.Constants;

namespace MESManager.Application.Services;

/// <summary>
/// Base class condivisa per tutti i service che gestiscono allegati su filesystem.
/// ================================================================================
/// UN UNICO punto di modifica per:
///   - ParsePathMappings  (parsing config "P:\Docs->C:\Dati\Docs")
///   - ConvertNetworkPath (conversione percorsi di rete → percorsi locali)
///   - GetMimeType        (estensione file → Content-Type HTTP)
///
/// Ereditato da: AllegatiAnimaService, AllegatoArticoloService
/// </summary>
public abstract class AllegatoFileServiceBase
{
    private readonly ILogger _logger;

    /// <summary>Percorso base cartella allegati su disco (da FileConfiguration.AllegatiBasePath).</summary>
    protected readonly string AllegatiBasePath;

    /// <summary>Mappature rete→locale lette da FileConfiguration.PathMappings.</summary>
    protected readonly IReadOnlyList<(string Source, string Target)> PathMappings;

    protected AllegatoFileServiceBase(ILogger logger, FileConfiguration fileConfig)
    {
        _logger = logger;
        AllegatiBasePath = FileConstants.GetAllegatiBasePath(fileConfig.AllegatiBasePath);
        PathMappings = ParsePathMappings(fileConfig.PathMappings);
    }

    // ── Path conversion ───────────────────────────────────────────────────────

    /// <summary>
    /// Converte un percorso di rete (es. P:\Documenti\...) nel percorso locale
    /// corrispondente secondo le PathMappings configurate.
    /// </summary>
    protected string ConvertNetworkPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        var converted = path;
        foreach (var (source, target) in PathMappings)
        {
            if (converted.StartsWith(source, StringComparison.OrdinalIgnoreCase))
            {
                converted = converted.Replace(source, target, StringComparison.OrdinalIgnoreCase);
                _logger.LogDebug("Path converted: {Original} -> {Converted}", path, converted);
                break;
            }
        }

        // Fallback: se il path convertito non esiste ma l'originale sì, usa l'originale
        if (!File.Exists(converted) && File.Exists(path))
        {
            _logger.LogDebug("Converted path not found, using original: {Path}", path);
            return path;
        }

        return converted;
    }

    // ── Path mapping parsing ──────────────────────────────────────────────────

    /// <summary>
    /// Parsa la lista di mapping "source->target" da appsettings.
    /// Fallback ai valori di default se nessuna mappatura è configurata.
    /// </summary>
    protected static List<(string Source, string Target)> ParsePathMappings(List<string>? mappings)
    {
        var result = new List<(string Source, string Target)>();

        if (mappings != null)
        {
            foreach (var mapping in mappings)
            {
                if (string.IsNullOrWhiteSpace(mapping) || !mapping.Contains("->"))
                    continue;

                var parts = mapping.Split("->", 2, StringSplitOptions.TrimEntries);
                if (parts.Length == 2
                    && !string.IsNullOrWhiteSpace(parts[0])
                    && !string.IsNullOrWhiteSpace(parts[1]))
                {
                    result.Add((parts[0], parts[1]));
                }
            }
        }

        // ⚙️ PERCORSI DEFAULT — cambiare qui per ogni nuovo cliente
        if (result.Count == 0)
        {
            result.Add((@"P:\Documenti", @"C:\Dati\Documenti"));
            result.Add((@"P:\",          @"C:\Dati\"));
        }

        return result;
    }

    // ── MIME types ────────────────────────────────────────────────────────────

    /// <summary>
    /// Ritorna il Content-Type HTTP per un'estensione file o percorso.
    /// Metodo statico — usabile anche senza istanza del service.
    /// </summary>
    public static string GetMimeType(string extensionOrPath)
    {
        var ext = Path.GetExtension(extensionOrPath).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext))
            ext = extensionOrPath.ToLowerInvariant();

        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png"            => "image/png",
            ".gif"            => "image/gif",
            ".bmp"            => "image/bmp",
            ".webp"           => "image/webp",
            ".tiff" or ".tif" => "image/tiff",
            ".pdf"            => "application/pdf",
            ".doc"            => "application/msword",
            ".docx"           => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls"            => "application/vnd.ms-excel",
            ".xlsx"           => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".txt"            => "text/plain",
            ".csv"            => "text/csv",
            ".zip"            => "application/zip",
            _                 => "application/octet-stream"
        };
    }
}
