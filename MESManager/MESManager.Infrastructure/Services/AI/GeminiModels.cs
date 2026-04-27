using System.Text.Json;
using System.Text.Json.Serialization;

namespace MESManager.Infrastructure.Services.AI;

// ─────────────────────────────────────────────────────────────────────────────
// Modelli specifici per l'API REST Google Gemini (v1beta).
// NON compatibili con OpenAI: formato request/response completamente diverso.
// Visibilità: internal — uso esclusivo di GeminiProvider e AiAssistantService.
// ─────────────────────────────────────────────────────────────────────────────

// ── Request ───────────────────────────────────────────────────────────────────

internal sealed record GeminiRequest
{
    public GeminiContent?      SystemInstruction { get; init; }
    public List<GeminiContent> Contents          { get; init; } = [];
    public List<GeminiTool>?   Tools             { get; init; }
    public GeminiToolConfig?   ToolConfig        { get; init; }
}

internal sealed record GeminiContent
{
    public string?           Role  { get; init; }   // "user" | "model"
    public List<GeminiPart>  Parts { get; init; } = [];
}

internal sealed record GeminiPart
{
    public string?                 Text             { get; init; }
    // Gemini REST API returns camelCase in responses — explicit mapping required
    // (SnakeCaseLower naming policy + PropertyNameCaseInsensitive can't resolve underscore vs camelCase)
    [JsonPropertyName("functionCall")]
    public GeminiFunctionCall?     FunctionCall     { get; init; }
    [JsonPropertyName("functionResponse")]
    public GeminiFunctionResponse? FunctionResponse { get; init; }
    // gemini-2.5-flash/pro (thinking models) return thoughtSignature in function call parts.
    // MUST be preserved and sent back in the second turn, otherwise Gemini returns 400 INVALID_ARGUMENT.
    // See: https://ai.google.dev/gemini-api/docs/thought-signatures
    [JsonPropertyName("thoughtSignature")]
    public string?                 ThoughtSignature { get; init; }
}

internal sealed record GeminiFunctionCall
{
    public string      Name { get; init; } = "";
    public JsonElement Args { get; init; }
}

internal sealed record GeminiFunctionResponse
{
    public string                        Name     { get; init; } = "";
    public GeminiFunctionResponseContent Response { get; init; } = new();
}

internal sealed record GeminiFunctionResponseContent
{
    public string Content { get; init; } = "";
}

internal sealed record GeminiTool
{
    public List<GeminiFunctionDeclaration> FunctionDeclarations { get; init; } = [];
}

internal sealed record GeminiFunctionDeclaration
{
    public string            Name        { get; init; } = "";
    public string            Description { get; init; } = "";
    public GeminiParameters? Parameters  { get; init; }
}

internal sealed record GeminiParameters
{
    public string                                Type       { get; init; } = "object";  // Gemini richiede minuscolo
    public Dictionary<string, GeminiParamProp>?  Properties { get; init; }
    public string[]?                             Required   { get; init; }
}

internal sealed record GeminiParamProp
{
    public string Type        { get; init; } = "string";  // Gemini richiede minuscolo
    public string Description { get; init; } = "";
}

internal sealed record GeminiToolConfig
{
    public GeminiFunctionCallingConfig? FunctionCallingConfig { get; init; }
}

internal sealed record GeminiFunctionCallingConfig
{
    public string Mode { get; init; } = "AUTO";
}

// ── Response ──────────────────────────────────────────────────────────────────

internal sealed record GeminiResponse
{
    public List<GeminiCandidate>? Candidates { get; init; }
}

internal sealed record GeminiCandidate
{
    public GeminiContent? Content      { get; init; }
    [JsonPropertyName("finishReason")]
    public string?        FinishReason { get; init; }
}

// ── Health check: GET /v1beta/models ─────────────────────────────────────────

internal sealed record GeminiModelsListResponse
{
    public List<GeminiModelInfo>? Models { get; init; }
}

internal sealed record GeminiModelInfo
{
    public string? Name        { get; init; }
    public string? DisplayName { get; init; }
}
