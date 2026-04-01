using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MESManager.Application.Interfaces;
using MESManager.Infrastructure.Data;
using System.Net;

namespace MESManager.Infrastructure.Services;

/// <summary>
/// Invia la scheda produttiva PDF alla macchina via FTP.
/// Flusso:
///   1. Genera PDF anima tramite IAnimePdfService
///   2. Carica su ftp://[MacchinaIP]/{codicePdf}.pdf  (codicePdf = SaleOrdId commessa)
/// Credenziali configurabili in appsettings: FtpSettings.Username / Password
/// </summary>
public class AnimeFtpService : IAnimeFtpService
{
    private readonly MesManagerDbContext _context;
    private readonly IAnimePdfService _pdfService;
    private readonly ILogger<AnimeFtpService> _logger;
    private readonly string _ftpUser;
    private readonly string _ftpPassword;

    public AnimeFtpService(
        MesManagerDbContext context,
        IAnimePdfService pdfService,
        IConfiguration configuration,
        ILogger<AnimeFtpService> logger)
    {
        _context = context;
        _pdfService = pdfService;
        _logger = logger;
        _ftpUser = configuration["FtpSettings:Username"] ?? "anonymous";
        _ftpPassword = configuration["FtpSettings:Password"] ?? "anonymous";
    }

    public async Task<AnimeFtpResult> SendSchedaToMacchinaAsync(
        string codiceArticolo,
        Guid macchinaId,
        int codicePdf,
        CancellationToken ct = default)
    {
        var result = new AnimeFtpResult { CodicePDF = codicePdf };

        try
        {
            _logger.LogInformation(
                "📤 [FTP-SCHEDA] Invio scheda articolo {Codice} → macchina {Id} | PDF={NomePdf}.pdf",
                codiceArticolo, macchinaId, codicePdf);

            // 1. Legge IP macchina
            var macchina = await _context.Macchine.FindAsync(new object[] { macchinaId }, ct);
            if (macchina == null || string.IsNullOrEmpty(macchina.IndirizzoPLC))
            {
                result.ErrorMessage = $"Macchina {macchinaId} non trovata o IP non configurato";
                _logger.LogWarning("⚠️ [FTP-SCHEDA] {Error}", result.ErrorMessage);
                return result;
            }
            result.MacchinaIp = macchina.IndirizzoPLC;

            // 2. Ottieni AnimeId dal CodiceArticolo
            var anime = await _context.Anime
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.CodiceArticolo == codiceArticolo, ct);
            if (anime == null)
            {
                result.ErrorMessage = $"Anima non trovata per articolo {codiceArticolo}";
                _logger.LogWarning("⚠️ [FTP-SCHEDA] {Error}", result.ErrorMessage);
                return result;
            }

            // 3. Genera PDF
            var pdfStream = await _pdfService.GenerateSchedaAsync(anime.Id);
            if (pdfStream == null)
            {
                result.ErrorMessage = $"Generazione PDF fallita per anima ID {anime.Id}";
                _logger.LogError("❌ [FTP-SCHEDA] {Error}", result.ErrorMessage);
                return result;
            }

            // 4. Carica su FTP come {codicePdf}.pdf
            var nomeFile = $"{codicePdf}.pdf";
            result.NomeFile = nomeFile;
            await UploadFtpAsync(macchina.IndirizzoPLC, nomeFile, pdfStream, ct);

            result.Success = true;
            _logger.LogInformation(
                "✅ [FTP-SCHEDA] Scheda {NomeFile} inviata a ftp://{Ip}/",
                nomeFile, macchina.IndirizzoPLC);
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "❌ [FTP-SCHEDA] Eccezione invio scheda articolo {Codice}", codiceArticolo);
        }

        return result;
    }

    private async Task UploadFtpAsync(string ip, string nomeFile, Stream content, CancellationToken ct)
    {
        var uri = new Uri($"ftp://{ip}/{nomeFile}");
        var request = (FtpWebRequest)WebRequest.Create(uri);
        request.Method = WebRequestMethods.Ftp.UploadFile;
        request.Credentials = new NetworkCredential(_ftpUser, _ftpPassword);
        request.UseBinary = true;
        request.KeepAlive = false;
        request.Timeout = 15_000;

        using var requestStream = await request.GetRequestStreamAsync();
        await content.CopyToAsync(requestStream, ct);
        requestStream.Close();

        using var response = (FtpWebResponse)await request.GetResponseAsync();
        _logger.LogDebug("[FTP-SCHEDA] Upload completato: {Status}", response.StatusDescription?.TrimEnd());
    }
}
