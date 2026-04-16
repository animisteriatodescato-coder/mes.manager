using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;

namespace MESManager.Infrastructure.Services;

/// <summary>
/// Gestisce upload/download/delete degli allegati delle schede manutenzione casse d'anima.
/// File salvati sotto: {ContentRoot}/uploads/manutenzioni-casse/{nomeFileUnivoco}
/// </summary>
public class ManutenzioneCassaAllegatoService : IManutenzioneCassaAllegatoService
{
    private readonly MesManagerDbContext _db;
    private readonly ILogger<ManutenzioneCassaAllegatoService> _logger;
    private readonly string _basePath;

    private static readonly HashSet<string> _fotoExt = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".heic", ".heif" };

    public ManutenzioneCassaAllegatoService(
        MesManagerDbContext db,
        ILogger<ManutenzioneCassaAllegatoService> logger,
        IHostEnvironment env)
    {
        _db = db;
        _logger = logger;
        _basePath = Path.Combine(env.ContentRootPath, "uploads", "manutenzioni-casse");
        Directory.CreateDirectory(_basePath);
    }

    public async Task<AllegatiManutenzioneCassaResponse> GetAllegatiBySchedaAsync(Guid schedaId)
    {
        var list = await _db.ManutenzioneCasseAllegati
            .Where(a => a.SchedaId == schedaId)
            .OrderBy(a => a.TipoFile).ThenBy(a => a.DataCaricamento)
            .ToListAsync();

        var response = new AllegatiManutenzioneCassaResponse();
        foreach (var a in list)
        {
            var dto = MapToDto(a);
            if (dto.IsFoto) response.Foto.Add(dto);
            else response.Documenti.Add(dto);
        }
        return response;
    }

    public async Task<ManutenzioneCassaAllegatoDto?> GetByIdAsync(int id)
    {
        var entity = await _db.ManutenzioneCasseAllegati.FindAsync(id);
        return entity == null ? null : MapToDto(entity);
    }

    public async Task<(byte[] Content, string ContentType, string FileName)?> GetFileContentAsync(int id)
    {
        var entity = await _db.ManutenzioneCasseAllegati.FindAsync(id);
        if (entity == null || !File.Exists(entity.PathFile))
            return null;

        var bytes = await File.ReadAllBytesAsync(entity.PathFile);
        var mime = GetMimeType(entity.Estensione);
        return (bytes, mime, entity.NomeFile);
    }

    public async Task<ManutenzioneCassaAllegatoDto> UploadAsync(
        Guid schedaId, Stream fileStream, string fileName, long fileSize, string? descrizione)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var tipoFile = _fotoExt.Contains(ext) ? "Foto" : "Documento";

        // Nome univoco: schedaId_timestamp_guid.ext
        var nomeUnivoco = $"{schedaId:N}_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(_basePath, nomeUnivoco);

        await using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
            await fileStream.CopyToAsync(fs);

        var entity = new ManutenzioneCassaAllegato
        {
            SchedaId = schedaId,
            NomeFile = Path.GetFileName(fileName),
            PathFile = fullPath,
            TipoFile = tipoFile,
            Estensione = ext,
            Descrizione = descrizione,
            DimensioneBytes = fileSize,
            DataCaricamento = DateTime.UtcNow
        };

        _db.ManutenzioneCasseAllegati.Add(entity);
        await _db.SaveChangesAsync();

        _logger.LogInformation("[MANUT-CASSA-ALLEGATI] Upload ok: Id={Id}, SchedaId={SchedaId}, File={Nome}, Tipo={Tipo}",
            entity.Id, schedaId, fileName, tipoFile);

        return MapToDto(entity);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _db.ManutenzioneCasseAllegati.FindAsync(id);
        if (entity == null) return false;

        // Cancella file dal disco
        if (File.Exists(entity.PathFile))
        {
            try { File.Delete(entity.PathFile); }
            catch (Exception ex) { _logger.LogWarning(ex, "Impossibile cancellare file: {Path}", entity.PathFile); }
        }

        _db.ManutenzioneCasseAllegati.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static ManutenzioneCassaAllegatoDto MapToDto(ManutenzioneCassaAllegato e) => new()
    {
        Id = e.Id,
        SchedaId = e.SchedaId,
        NomeFile = e.NomeFile,
        // Ricalcola TipoFile dall'estensione: gestisce file storici salvati come "Documento" (es. .heic)
        TipoFile = _fotoExt.Contains(e.Estensione ?? "") ? "Foto" : e.TipoFile,
        Estensione = e.Estensione,
        Descrizione = e.Descrizione,
        DimensioneBytes = e.DimensioneBytes,
        DataCaricamento = e.DataCaricamento.ToLocalTime()
    };

    private static string GetMimeType(string ext) => ext.ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".bmp" => "image/bmp",
        ".webp" => "image/webp",
        ".heic" => "image/heic",
        ".heif" => "image/heif",
        ".doc" => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".xls" => "application/vnd.ms-excel",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        _ => "application/octet-stream"
    };
}
