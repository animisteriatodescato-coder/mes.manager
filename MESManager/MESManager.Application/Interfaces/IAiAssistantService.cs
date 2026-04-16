using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces;

public interface IAiAssistantService
{
    /// <summary>
    /// Invia un messaggio all'AI, esegue eventuali function calls sui dati reali del DB
    /// e restituisce la risposta in italiano.
    /// </summary>
    Task<string> AskAsync(IList<AiChatMessage> history, string userMessage, CancellationToken ct = default);

    /// <summary>
    /// Verifica se il provider AI attivo è raggiungibile e restituisce i modelli disponibili.
    /// </summary>
    Task<AiHealthResult> CheckHealthAsync(CancellationToken ct = default);
}
