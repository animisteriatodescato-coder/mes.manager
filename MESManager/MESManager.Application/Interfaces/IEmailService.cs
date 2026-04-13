namespace MESManager.Application.Interfaces;

/// <summary>
/// Servizio invio email per i preventivi (v1.65.7).
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Invia il preventivo come corpo HTML al destinatario specificato.
    /// Restituisce true se l'invio ha avuto successo.
    /// </summary>
    Task<bool> InviaPreventivoPdfAsync(
        string destinatario,
        string htmlContent,
        string clienteName,
        int numeroPreventivo,
        CancellationToken ct = default);

    /// <summary>Verifica che la configurazione SMTP sia presente e valida.</summary>
    bool IsConfigured { get; }
}
