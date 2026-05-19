using System.Text.Json;

namespace MESManager.Infrastructure.Services;

internal sealed record PlcStoricoSnapshotValues(
    int CicliFatti,
    int QuantitaDaProdurre,
    int CicliScarti,
    int TempoMedioRilevato,
    int TempoMedio,
    int Figure,
    int BarcodeLavorazione,
    string? NuovaProduzioneTs,
    string? InizioSetupTs,
    string? FineSetupTs,
    string? InProduzioneTs);

internal static class PlcStoricoSnapshotParser
{
    internal static PlcStoricoSnapshotValues Parse(string? dati)
    {
        var values = new PlcStoricoSnapshotValues(0, 0, 0, 0, 0, 0, 0, null, null, null, null);
        if (string.IsNullOrWhiteSpace(dati)) return values;

        try
        {
            using var doc = JsonDocument.Parse(dati);
            var root = doc.RootElement;

            return values with
            {
                CicliFatti = GetInt(root, "CicliFatti"),
                QuantitaDaProdurre = GetInt(root, "QuantitaDaProdurre"),
                CicliScarti = GetInt(root, "CicliScarti"),
                TempoMedioRilevato = GetInt(root, "TempoMedioRilevato"),
                TempoMedio = GetInt(root, "TempoMedio"),
                Figure = GetInt(root, "Figure"),
                BarcodeLavorazione = GetInt(root, "BarcodeLavorazione"),
                NuovaProduzioneTs = GetString(root, "NuovaProduzioneTs"),
                InizioSetupTs = GetString(root, "InizioSetupTs"),
                FineSetupTs = GetString(root, "FineSetupTs"),
                InProduzioneTs = GetString(root, "InProduzioneTs")
            };
        }
        catch
        {
            return values;
        }
    }

    private static int GetInt(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value)) return 0;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number)) return number;
        return int.TryParse(value.GetString(), out var parsed) ? parsed : 0;
    }

    private static string? GetString(JsonElement root, string name)
    {
        return root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }
}
