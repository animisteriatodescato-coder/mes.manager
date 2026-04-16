using System.Text.Json.Serialization;

namespace MESManager.Infrastructure.Services.AI;

// ─────────────────────────────────────────────────────────────────────────────
// Modelli OpenAI/Ollama (formato identico — Ollama è OpenAI-compatible)
// Visibilità: internal — uso esclusivo di OpenAiProvider, OllamaProvider e
// AiAssistantService (tutti nello stesso assembly Infrastructure).
// ─────────────────────────────────────────────────────────────────────────────

internal sealed record OaiRequest
{
    public string           Model      { get; init; } = "gpt-4o-mini";
    public List<OaiMessage> Messages   { get; init; } = [];
    public List<OaiTool>?   Tools      { get; init; }
    public string?          ToolChoice { get; init; }
}

internal sealed record OaiMessage
{
    public string         Role       { get; init; } = "user";
    public string?        Content    { get; init; }
    public OaiToolCall[]? ToolCalls  { get; init; }
    public string?        ToolCallId { get; init; }
}

internal sealed record OaiToolCall
{
    public string              Id       { get; init; } = "";
    public string              Type     { get; init; } = "function";
    public OaiToolCallFunction Function { get; init; } = new();
}

internal sealed record OaiToolCallFunction
{
    public string Name      { get; init; } = "";
    public string Arguments { get; init; } = "";
}

internal sealed record OaiTool
{
    public string      Type     { get; init; } = "function";
    public OaiFunction Function { get; init; } = new();
}

internal sealed record OaiFunction
{
    public string         Name        { get; init; } = "";
    public string         Description { get; init; } = "";
    public OaiParameters  Parameters  { get; init; } = new();
}

internal sealed record OaiParameters
{
    public string                                  Type       { get; init; } = "object";
    public Dictionary<string, OaiParamProp>        Properties { get; init; } = [];
    public string[]                                Required   { get; init; } = [];
}

internal sealed record OaiParamProp
{
    public string Type        { get; init; } = "string";
    public string Description { get; init; } = "";
}

internal sealed record OaiResponse
{
    public OaiChoice[]? Choices { get; init; }
    public OaiUsage?    Usage   { get; init; }
}

internal sealed record OaiChoice
{
    public OaiMessage? Message      { get; init; }
    public string?     FinishReason { get; init; }
}

internal sealed record OaiUsage
{
    public int PromptTokens     { get; init; }
    public int CompletionTokens { get; init; }
    public int TotalTokens      { get; init; }
}

// ── Risposta GET /api/tags di Ollama ─────────────────────────────────────────

internal sealed record OllamaTagsResponse
{
    [JsonPropertyName("models")]
    public List<OllamaModelInfo>? Models { get; init; }
}

internal sealed record OllamaModelInfo
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("size")]
    public long Size { get; init; }
}
