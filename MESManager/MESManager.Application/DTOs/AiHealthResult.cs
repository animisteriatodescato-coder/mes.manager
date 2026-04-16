namespace MESManager.Application.DTOs;

/// <summary>Risultato del check salute di un provider AI.</summary>
public record AiHealthResult(
    bool IsAvailable,
    IList<string> Models,
    string ProviderName,
    string? Error = null);
