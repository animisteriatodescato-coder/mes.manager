namespace MESManager.Application.Interfaces;

/// <summary>
/// Invia la scheda produttiva (PDF) via FTP alla macchina.
/// Legge/assegna il CodicePDF (offset 160 ricetta) e carica il file sul server FTP della macchina.
/// </summary>
public interface IAnimeFtpService
{
    /// <summary>
    /// Genera il PDF della scheda produttiva per l'articolo e lo invia via FTP
    /// all'IP della macchina specificata.
    /// Se CodicePDF (offset 160) mancante o 0, genera un codice univoco e lo salva in DB.
    /// </summary>
    Task<AnimeFtpResult> SendSchedaToMacchinaAsync(string codiceArticolo, Guid macchinaId, CancellationToken ct = default);
}

public class AnimeFtpResult
{
    public bool Success { get; set; }
    public string NomeFile { get; set; } = string.Empty;
    public int CodicePDF { get; set; }
    public string? MacchinaIp { get; set; }
    public string? ErrorMessage { get; set; }
}
