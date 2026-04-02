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
///   3. Ripulisce la cartella FTP: mantiene al massimo 3 PDF, elimina il più vecchio
/// Credenziali configurabili in appsettings: FtpSettings.Username / Password
/// </summary>
public class AnimeFtpService : IAnimeFtpService
{
    private const int MaxPdfInCartella = 3;

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
        string? ftpIp = null;

        try
        {
            _logger.LogInformation(
                "📤 [FTP-SCHEDA] Invio scheda articolo {Codice} → macchina {Id} | PDF={NomePdf}.pdf",
                codiceArticolo, macchinaId, codicePdf);

            // 1. Legge IP macchina
            var macchina = await _context.Macchine.FindAsync(new object[] { macchinaId }, ct);
            if (macchina == null)
            {
                result.ErrorMessage = $"Macchina {macchinaId} non trovata";
                _logger.LogWarning("⚠️ [FTP-SCHEDA] {Error}", result.ErrorMessage);
                return result;
            }
            result.MacchinaIp = macchina.IndirizzoPLC;

            // Usa IndirizzoFtp se configurato (es. 192.168.17.126), altrimenti fallback su IndirizzoPLC
            ftpIp = !string.IsNullOrEmpty(macchina.IndirizzoFtp)
                ? macchina.IndirizzoFtp
                : macchina.IndirizzoPLC;

            if (string.IsNullOrEmpty(ftpIp))
            {
                result.ErrorMessage = $"Macchina {macchinaId}: né IndirizzoFtp né IndirizzoPLC configurati";
                _logger.LogWarning("⚠️ [FTP-SCHEDA] {Error}", result.ErrorMessage);
                return result;
            }

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
            await UploadFtpAsync(ftpIp, nomeFile, pdfStream, ct);

            result.Success = true;
            _logger.LogInformation(
                "✅ [FTP-SCHEDA] Scheda {NomeFile} inviata a ftp://{Ip}/",
                nomeFile, ftpIp);

            // 5. Pulizia cartella FTP: mantieni al massimo MaxPdfInCartella file
            await CleanupOldPdfsAsync(ftpIp, ct);
        }
        catch (Exception ex)
        {
            // Gestione 226 anche a livello outer per robustezza
            var msg = ex.Message ?? "";
            if (ex is WebException && (msg.Contains("226") || msg.Contains("250")))
            {
                // Upload riuscito — server ha inviato 2xx come WebException (comportamento server FTP embedded)
                result.Success = true;
                _logger.LogInformation(
                    "✅ [FTP-SCHEDA] Scheda {NomeFile} inviata a ftp://{Ip}/ (2xx via WebException)",
                    result.NomeFile, ftpIp);

                // Pulizia best-effort se FTP IP disponibile
                if (ftpIp != null)
                    try { await CleanupOldPdfsAsync(ftpIp, CancellationToken.None); } catch { /* ignora */ }
            }
            else
            {
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "❌ [FTP-SCHEDA] Eccezione invio scheda articolo {Codice}", codiceArticolo);
            }
        }

        return result;
    }

    // =====================================================================
    // Privati
    // =====================================================================

    private async Task UploadFtpAsync(string ip, string nomeFile, Stream content, CancellationToken ct)
    {
        var uri = new Uri($"ftp://{ip}/{nomeFile}");
        var request = (FtpWebRequest)WebRequest.Create(uri);
        request.Method = WebRequestMethods.Ftp.UploadFile;
        request.Credentials = new NetworkCredential(_ftpUser, _ftpPassword);
        request.UseBinary = true;
        request.UsePassive = true;
        request.KeepAlive = false;
        request.Timeout = 30_000;

        try
        {
            using var requestStream = await request.GetRequestStreamAsync();
            await content.CopyToAsync(requestStream, ct);
            requestStream.Close();

            using var response = (FtpWebResponse)await request.GetResponseAsync();
            _logger.LogDebug("[FTP-SCHEDA] Upload OK: {Status}", response.StatusDescription?.TrimEnd());
        }
        catch (WebException ex)
        {
            // Alcuni server FTP embedded (es. Siemens HMI) inviano "226 Transfer complete"
            // come WebException invece di risposta normale. 226/250 = successo.
            var msg = ex.Message ?? "";
            if (msg.Contains("226") || msg.Contains("250") ||
                (ex.Response is FtpWebResponse fr &&
                 (fr.StatusCode == FtpStatusCode.ClosingData || fr.StatusCode == FtpStatusCode.FileActionOK)))
            {
                _logger.LogDebug("[FTP-SCHEDA] Upload OK (FTP 2xx ricevuto come WebException: {Msg})", msg.Trim());
                return; // Upload riuscito
            }
            throw; // Errore reale
        }
    }

    /// <summary>
    /// Elenca i file .pdf nella root FTP e, se più di MaxPdfInCartella, elimina il più vecchio.
    /// Usa MLSD (machine listing) con fallback su LIST se il server non supporta MLSD.
    /// </summary>
    private async Task CleanupOldPdfsAsync(string ip, CancellationToken ct)
    {
        try
        {
            // Elenca file con data (MLSD è supportato dalla maggioranza dei server FTP embedded)
            var files = await ListPdfFilesAsync(ip, ct);

            if (files.Count <= MaxPdfInCartella)
                return;

            // Ordina per data ascending, elimina i più vecchi finché non se ne hanno <= Max
            var daEliminare = files
                .OrderBy(f => f.LastModified)
                .Take(files.Count - MaxPdfInCartella)
                .ToList();

            foreach (var f in daEliminare)
            {
                await DeleteFtpFileAsync(ip, f.Name, ct);
                _logger.LogInformation("🗑️ [FTP-CLEANUP] Eliminato PDF vecchio: {Name} ({Date})",
                    f.Name, f.LastModified);
            }
        }
        catch (Exception ex)
        {
            // La pulizia è best-effort: non blocca il flusso principale
            _logger.LogWarning(ex, "⚠️ [FTP-CLEANUP] Errore durante pulizia cartella {Ip}", ip);
        }
    }

    private async Task<List<FtpFileInfo>> ListPdfFilesAsync(string ip, CancellationToken ct)
    {
        var result = new List<FtpFileInfo>();

        // Prova MLSD per ottenere timestamp precisi
        try
        {
            var uri = new Uri($"ftp://{ip}/");
            var req = (FtpWebRequest)WebRequest.Create(uri);
            req.Method = "MLSD";
            req.Credentials = new NetworkCredential(_ftpUser, _ftpPassword);
            req.UsePassive = true;
            req.Timeout = 10_000;

            using var response = (FtpWebResponse)await req.GetResponseAsync();
            using var reader = new System.IO.StreamReader(response.GetResponseStream());
            string? line;
            while ((line = await reader.ReadLineAsync(ct)) != null)
            {
                // Formato MLSD: "modify=20260401123015;type=file;size=12345; 7687.pdf"
                var spaceIdx = line.IndexOf(' ');
                if (spaceIdx < 0) continue;
                var name = line[(spaceIdx + 1)..].Trim();
                if (!name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) continue;

                DateTime lastMod = DateTime.MinValue;
                var modifyTag = "modify=";
                var modIdx = line.IndexOf(modifyTag, StringComparison.OrdinalIgnoreCase);
                if (modIdx >= 0)
                {
                    var modVal = line[(modIdx + modifyTag.Length)..].Split(';')[0];
                    DateTime.TryParseExact(modVal, "yyyyMMddHHmmss",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out lastMod);
                }
                result.Add(new FtpFileInfo(name, lastMod));
            }
            return result;
        }
        catch
        {
            // Fallback: LIST (data meno precisa ma universale)
        }

        try
        {
            var uri = new Uri($"ftp://{ip}/");
            var req = (FtpWebRequest)WebRequest.Create(uri);
            req.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            req.Credentials = new NetworkCredential(_ftpUser, _ftpPassword);
            req.UsePassive = true;
            req.Timeout = 10_000;

            using var response = (FtpWebResponse)await req.GetResponseAsync();
            using var reader = new System.IO.StreamReader(response.GetResponseStream());
            string? line;
            while ((line = await reader.ReadLineAsync(ct)) != null)
            {
                // Formato Unix: "-rw-r--r-- 1 user group 12345 Apr  1 12:30 7687.pdf"
                if (!line.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) continue;
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 9) continue;
                var name = parts[^1];
                // Data approssimativa (solo per ordinamento relativo)
                DateTime.TryParse($"{parts[^3]} {parts[^4]} {parts[^2]}",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var lastMod);
                result.Add(new FtpFileInfo(name, lastMod));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[FTP-CLEANUP] Impossibile listare file su {Ip}", ip);
        }

        return result;
    }

    private async Task DeleteFtpFileAsync(string ip, string nomeFile, CancellationToken ct)
    {
        var uri = new Uri($"ftp://{ip}/{nomeFile}");
        var req = (FtpWebRequest)WebRequest.Create(uri);
        req.Method = WebRequestMethods.Ftp.DeleteFile;
        req.Credentials = new NetworkCredential(_ftpUser, _ftpPassword);
        req.UsePassive = true;
        req.Timeout = 10_000;
        using var response = (FtpWebResponse)await req.GetResponseAsync();
    }

    private record FtpFileInfo(string Name, DateTime LastModified);
}
