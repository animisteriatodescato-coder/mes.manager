using System.IO;
using System.Text.Json;

public static class ConfigLoader
{
    public static T Load<T>(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"File di configurazione non trovato: {path}");

        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json)!;
    }
}
