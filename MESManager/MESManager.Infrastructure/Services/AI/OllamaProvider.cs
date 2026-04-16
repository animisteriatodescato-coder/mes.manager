using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using MESManager.Application.DTOs;

namespace MESManager.Infrastructure.Services.AI;

/// <summary>
/// Provider AI per Ollama locale (API OpenAI-compatible su /v1/chat/completions).
/// Supporta tool calling per modelli compatibili (llama3.1+, qwen2.5+, mistral).
/// In caso di modelli senza supporto tools, fornisce fallback a prompt-only.
/// </summary>
internal sealed class OllamaProvider
{
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition      = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>Chiama l'API chat completions di Ollama (OpenAI-compatible).</summary>
    /// <exception cref="HttpRequestException">Se il modello non supporta tools (HTTP 400).</exception>
    internal async Task<OaiResponse> CallAsync(OaiRequest request, string baseUrl, CancellationToken ct)
    {
        var endpoint = $"{baseUrl.TrimEnd('/')}/v1/chat/completions";
        // Ollama locale — timeout più alto per modelli grandi
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };

        return await OpenAiProvider.PostChatAsync(http, endpoint, request, ct);
    }

    /// <summary>
    /// Health check: chiama GET /api/tags per ottenere i modelli installati.
    /// Ritorna IsAvailable=false se Ollama non è raggiungibile.
    /// </summary>
    internal async Task<AiHealthResult> CheckHealthAsync(string baseUrl, CancellationToken ct)
    {
        var tagsUrl = $"{baseUrl.TrimEnd('/')}/api/tags";
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        try
        {
            var resp = await http.GetFromJsonAsync<OllamaTagsResponse>(
                tagsUrl,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                ct);

            var models = resp?.Models?.Select(m => m.Name).ToList() ?? [];
            return new AiHealthResult(true, models, "Ollama");
        }
        catch (Exception ex)
        {
            var msg = ex is HttpRequestException or SocketException or OperationCanceledException
                ? "Ollama non raggiungibile su " + baseUrl
                : ex.Message;
            return new AiHealthResult(false, [], "Ollama", msg);
        }
    }
}
