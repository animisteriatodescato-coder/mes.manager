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
///   1. Legge CodicePDF (offset 160) da ParametroRicetta per l'articolo
///   2. Se non trovato o 0 → genera codice univoco e salva in DB
///   3. Genera PDF tramite IAnimePdfService
///   4. Carica su ftp://[MacchinaIP]/[CodicePDF].pdf
/// Credenziali configurabili in appsettings: FtpSettings.Username / Password
/// </summary>
public class AnimeFtpService : IAnimeFtpService
{
    private const int CodicePdfOffset = 160;

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
        CancellationToken ct = default)
    {
        var result = new AnimeFtpResult();

        try
        {
            _logger.LogInformation("📤 [FTP-SCHEDA] Avvio invio scheda per articolo {Codice} → macchina {Id}",
                codiceArticolo, macchinaId);

            // 1. Legge IP macchina
            var macchina = await _context.Macchine.FindAsync(new object[] { macchinaId }, ct);
            if (macchina == null || string.IsNullOrEmpty(macchina.IndirizzoPLC))
            {
                result.ErrorMessage = $"Macchina {macchinaId} non trovata o IP non configurato";
                _logger.LogWarning("⚠️ [FTP-SCHEDA] {Error}", result.ErrorMessage);
                return result;
            }
            result.MacchinaIp = macchina.IndirizzoPLC;

            // 2. Legge/assegna CodicePDF (offset 160)
            var codicePdf = await GetOrAssignCodicePdfAsync(codiceArticolo, ct);
            if (codicePdf <= 0)
            {
                result.ErrorMessage = $"Impossibile ottenere CodicePDF per articolo {codiceArticolo}";
                _logger.LogError("❌ [FTP-SCHEDA] {Error}", result.ErrorMessage);
                return result;
            }
            result.CodicePDF = codicePdf;

            // 3. Ottieni AnimeId dal CodiceArticolo per generare il PDF
            var anime = await _context.Anime
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.CodiceArticolo == codiceArticolo, ct);
            if (anime == null)
            {
                result.ErrorMessage = $"Anima non trovata per articolo {codiceArticolo}";
                _logger.LogWarning("⚠️ [FTP-SCHEDA] {Error}", result.ErrorMessage);
                return result;
            }

            // 4. Genera PDF
            var pdfStream = await _pdfService.GenerateSchedaAsync(anime.Id);
            if (pdfStream == null)
            {
                result.ErrorMessage = $"Generazione PDF fallita per anima ID {anime.Id}";
                _logger.LogError("❌ [FTP-SCHEDA] {Error}", result.ErrorMessage);
                return result;
            }

            // 5. Carica su FTP
            var nomeFile = $"{codicePdf}.pdf";
            result.NomeFile = nomeFile;

            await UploadFtpAsync(macchina.IndirizzoPLC, nomeFile, pdfStream, ct);

            result.Success = true;
            _logger.LogInformation(
                "✅ [FTP-SCHEDA] Scheda {NomeFile} inviata a ftp://{Ip}/ (CodicePDF={Codice})",
                nomeFile, macchina.IndirizzoPLC, codicePdf);
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "❌ [FTP-SCHEDA] Eccezione durante invio scheda per {Codice}", codiceArticolo);
        }

        return result;
    }

    // =====================================================================
    // Privati
    // =====================================================================

    /// <summary>
    /// Restituisce il CodicePDF (int) dal ParametroRicetta offset 160.
    /// Se mancante o 0 lo genera univocamente e lo persiste.
    /// </summary>
    private async Task<int> GetOrAssignCodicePdfAsync(string codiceArticolo, CancellationToken ct)
    {
        var parametro = await _context.ParametriRicetta
            .Include(p => p.Ricetta)
                .ThenInclude(r => r.Articolo)
            .FirstOrDefaultAsync(
                p => p.Ricetta.Articolo.Codice == codiceArticolo && p.Indirizzo == CodicePdfOffset, ct);

        if (parametro != null
            && int.TryParse(parametro.Valore, out var existing)
            && existing > 0)
        {
            return existing;
        }

        // Genera codice univoco: MAX+1 degli esistenti a offset 160
        var maxCodice = await _context.ParametriRicetta
            .Where(p => p.Indirizzo == CodicePdfOffset)
            .Select(p => p.Valore)
            .ToListAsync(ct);

        int nuovo = maxCodice
            .Select(v => int.TryParse(v, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;

        if (parametro != null)
        {
            // Aggiorna record esistente con valore 0
            parametro.Valore = nuovo.ToString();
        }
        else
        {
            // Crea record se non esiste affatto
            var ricetta = await _context.Ricette
                .Include(r => r.Articolo)
                .FirstOrDefaultAsync(r => r.Articolo.Codice == codiceArticolo, ct);

            if (ricetta == null)
            {
                _logger.LogWarning("⚠️ [FTP-SCHEDA] Nessuna ricetta per {Codice}: CodicePDF non salvabile in DB", codiceArticolo);
                return nuovo; // Usa comunque il codice generato anche senza ricetta
            }

            _context.ParametriRicetta.Add(new Domain.Entities.ParametroRicetta
            {
                RicettaId = ricetta.Id,
                NomeParametro = "CodicePDF",
                Valore = nuovo.ToString(),
                Indirizzo = CodicePdfOffset,
                Area = "Ricetta",
                Tipo = "INT",
                UnitaMisura = string.Empty
            });
        }

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("💾 [FTP-SCHEDA] CodicePDF {Codice} assegnato e salvato per articolo {Articolo}",
            nuovo, codiceArticolo);
        return nuovo;
    }

    private async Task UploadFtpAsync(string ip, string nomeFile, Stream content, CancellationToken ct)
    {
        var uri = new Uri($"ftp://{ip}/{nomeFile}");
        var request = (FtpWebRequest)WebRequest.Create(uri);
        request.Method = WebRequestMethods.Ftp.UploadFile;
        request.Credentials = new NetworkCredential(_ftpUser, _ftpPassword);
        request.UseBinary = true;
        request.KeepAlive = false;
        request.Timeout = 15_000; // 15 secondi

        using var requestStream = await request.GetRequestStreamAsync();
        await content.CopyToAsync(requestStream, ct);
        requestStream.Close();

        using var response = (FtpWebResponse)await request.GetResponseAsync();
        _logger.LogDebug("[FTP-SCHEDA] Upload completato: {Status}", response.StatusDescription?.TrimEnd());
    }
}
