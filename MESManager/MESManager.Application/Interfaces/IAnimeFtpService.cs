namespace MESManager.Application.Interfaces;

/// <summary>
/// Invia la scheda produttiva (PDF) via FTP alla macchina.
/// Il nome del file PDF = codicePdf (ID Mago / SaleOrdId), scritto anche a offset 160 della ricetta.
/// </summary>
public interface IAnimeFtpService
{
    /// <summary>
    /// Genera il PDF e lo carica su ftp://[MacchinaIP]/{codicePdf}.pdf
    /// codicePdf = SaleOrdId (ID Mago) della commessa corrente, da scrivere anche a offset 160.
    /// </summary>
    Task<AnimeFtpResult> SendSchedaToMacchinaAsync(string codiceArticolo, Guid macchinaId, int codicePdf, CancellationToken ct = default);
}

public class AnimeFtpResult
{
    public bool Success { get; set; }
    public string NomeFile { get; set; } = string.Empty;
    public int CodicePDF { get; set; }
    public string? MacchinaIp { get; set; }
    public string? ErrorMessage { get; set; }
}
