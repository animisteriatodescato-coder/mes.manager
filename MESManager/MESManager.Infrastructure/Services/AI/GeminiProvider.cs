using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MESManager.Application.DTOs;

namespace MESManager.Infrastructure.Services.AI;

/// <summary>
/// Provider AI per Google Gemini (API REST v1beta).
/// Auth: API key via query param ?key=...  (NON Bearer header)
/// Formato request/response diverso da OpenAI — vedi GeminiModels.cs.
/// </summary>
internal sealed class GeminiProvider
{
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta";

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNamingPolicy   = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Chiama generateContent con il modello specificato.
    /// Usato sia per la prima chiamata (con tools) sia per la seconda (tool results).
    /// </summary>
    internal async Task<GeminiResponse> CallAsync(
        GeminiRequest request, string model, string apiKey, CancellationToken ct)
    {
        var url = $"{BaseUrl}/models/{model}:generateContent?key={Uri.EscapeDataString(apiKey)}";
        using var http  = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };

        var json        = JsonSerializer.Serialize(request, _jsonOpts);
        using var body  = new StringContent(json, Encoding.UTF8, "application/json");

        var resp = await http.PostAsync(url, body, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var errBody = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Gemini HTTP {(int)resp.StatusCode}: {errBody}");
}

        var responseBody = await resp.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<GeminiResponse>(responseBody, _jsonOpts)
               ?? throw new InvalidOperationException("Risposta Gemini nulla o non deserializzabile.");
    }

    /// <summary>
    /// Health check: GET /v1beta/models?key={key} per verificare la chiave
    /// e ottenere i modelli Gemini disponibili.
    /// </summary>
    internal async Task<AiHealthResult> CheckHealthAsync(string apiKey, string model, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("YOUR_"))
            return new AiHealthResult(false, [], "Gemini",
                "API Key Gemini non configurata in appsettings.Secrets.json (campo Gemini:ApiKey)");

        var url = $"{BaseUrl}/models?key={Uri.EscapeDataString(apiKey)}";
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        try
        {
            var resp = await http.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode)
                return new AiHealthResult(false, [], "Gemini",
                    $"HTTP {(int)resp.StatusCode} — API key non valida o quota esaurita");

            var body       = await resp.Content.ReadAsStringAsync(ct);
            var modelsResp = JsonSerializer.Deserialize<GeminiModelsListResponse>(body, _jsonOpts);

            var models = modelsResp?.Models?
                .Where(m => m.Name != null && m.Name.Contains("gemini"))
                .Select(m => m.Name!.Replace("models/", ""))
                .Where(m => !m.Contains("embedding") && !m.Contains("aqa"))
                .OrderBy(m => m)
                .ToList() ?? [];

            return new AiHealthResult(true, models, "Gemini");
        }
        catch (Exception ex)
        {
            return new AiHealthResult(false, [], "Gemini", ex.Message);
        }
    }
}
