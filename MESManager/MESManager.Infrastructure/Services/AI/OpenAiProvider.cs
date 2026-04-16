using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MESManager.Application.DTOs;

namespace MESManager.Infrastructure.Services.AI;

/// <summary>
/// Provider AI per OpenAI (api.openai.com).
/// Supporto completo a tool calling (function calling).
/// </summary>
internal sealed class OpenAiProvider
{
    private const string ChatEndpoint = "https://api.openai.com/v1/chat/completions";

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition      = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>Chiama l'API chat completions di OpenAI.</summary>
    internal async Task<OaiResponse> CallAsync(OaiRequest request, string apiKey, CancellationToken ct)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        return await PostChatAsync(http, ChatEndpoint, request, ct);
    }

    /// <summary>
    /// Health check: verifica che la API key sia configurata.
    /// Non effettua chiamate live per evitare costi.
    /// </summary>
    internal Task<AiHealthResult> CheckHealthAsync(string apiKey, string model, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("sk-your"))
            return Task.FromResult(new AiHealthResult(false, [], "OpenAI", "API Key non configurata"));

        return Task.FromResult(new AiHealthResult(true, [model], "OpenAI"));
    }

    // ── Helper HTTP condiviso ─────────────────────────────────────────────────

    internal static async Task<OaiResponse> PostChatAsync(
        HttpClient http, string endpoint, OaiRequest request, CancellationToken ct)
    {
        var body    = JsonSerializer.Serialize(request, _jsonOpts);
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using var resp    = await http.PostAsync(endpoint, content, ct);

        if (!resp.IsSuccessStatusCode)
        {
            var errorBody = await resp.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"HTTP {(int)resp.StatusCode}: {errorBody[..Math.Min(300, errorBody.Length)]}",
                null,
                resp.StatusCode);
        }

        return await resp.Content.ReadFromJsonAsync<OaiResponse>(_jsonOpts, ct)
               ?? throw new InvalidOperationException("Risposta vuota dal provider AI.");
    }
}
