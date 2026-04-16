namespace MESManager.Application.DTOs;

/// <summary>Configurazione runtime del provider AI — letta da AppSettings.</summary>
public record AiProviderConfig
{
    /// <summary>"OpenAI" oppure "Ollama"</summary>
    public string ProviderType { get; init; } = "OpenAI";

    /// <summary>Base URL di Ollama (es. http://localhost:11434)</summary>
    public string OllamaBaseUrl { get; init; } = "http://localhost:11434";

    /// <summary>Modello Ollama (es. llama3.1:8b, qwen2.5:7b, mistral)</summary>
    public string OllamaModel { get; init; } = "llama3.1:8b";

    /// <summary>Modello OpenAI (override di IConfiguration["OpenAI:Model"])</summary>
    public string OpenAiModel { get; init; } = "gpt-4o-mini";
}
