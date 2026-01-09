using System.Text.Json;

namespace MESManager.Sync.Backup;

public class JsonBackupService
{
    private readonly string _basePath;

    public JsonBackupService(string basePath)
    {
        _basePath = basePath;
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> SaveBackupAsync<T>(string modulo, IEnumerable<T> dati)
    {
        var dir = Path.Combine(_basePath, modulo);
        Directory.CreateDirectory(dir);

        var fileName = $"{modulo}_sync_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        var fullPath = Path.Combine(dir, fileName);

        var json = JsonSerializer.Serialize(dati, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(fullPath, json);
        return fullPath;
    }
}
