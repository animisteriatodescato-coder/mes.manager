using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using ImageMagick;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace MESManager.Web.Controllers;

/// <summary>
/// Controller per upload/download/delete allegati delle schede manutenzione casse d'anima.
/// </summary>
[ApiController]
[Authorize]
[Route("api/allegati-manutenzione-casse")]
public class AllegatiManutenzioneCasseController : ControllerBase
{
    private readonly IManutenzioneCassaAllegatoService _service;
    private readonly ILogger<AllegatiManutenzioneCasseController> _logger;

    public AllegatiManutenzioneCasseController(
        IManutenzioneCassaAllegatoService service,
        ILogger<AllegatiManutenzioneCasseController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Ottiene tutti gli allegati (foto + documenti) per una scheda.</summary>
    [HttpGet("scheda/{schedaId:guid}")]
    public async Task<ActionResult<AllegatiManutenzioneCassaResponse>> GetByScheda(Guid schedaId)
    {
        var result = await _service.GetAllegatiBySchedaAsync(schedaId);
        return Ok(result);
    }

    /// <summary>Scarica il contenuto di un allegato.</summary>
    [HttpGet("{id:int}/file")]
    public async Task<IActionResult> GetFile(int id)
    {
        var result = await _service.GetFileContentAsync(id);
        if (result == null) return NotFound();

        // Converti HEIC/HEIF → JPEG al volo usando Magick.NET (file caricati prima del fix server-side)
        var ext = Path.GetExtension(result.Value.FileName).ToLowerInvariant();
        if (ext == ".heic" || ext == ".heif")
        {
            try
            {
                using var magick = new MagickImage(result.Value.Content);
                magick.Quality = 82;
                magick.Format = MagickFormat.Jpeg;
                var jpgBytes = magick.ToByteArray();
                var jpgName = Path.ChangeExtension(result.Value.FileName, ".jpg");
                return File(jpgBytes, "image/jpeg", jpgName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Conversione HEIC→JPEG (Magick) fallita per id={Id}: {Err}. Provo a servire come JPEG.", id, ex.Message);
                // Fallback: il file potrebbe essere già JPEG con nome .heic (upload da iOS con conversione parziale)
                // Servi con content-type jpeg così il browser può decodificarlo
                var jpgNameFallback = Path.ChangeExtension(result.Value.FileName, ".jpg");
                return File(result.Value.Content, "image/jpeg", jpgNameFallback);
            }
        }

        return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }

    /// <summary>Carica un nuovo allegato per una scheda.</summary>
    [HttpPost("upload/{schedaId:guid}")]
    public async Task<ActionResult<ManutenzioneCassaAllegatoDto>> Upload(
        Guid schedaId,
        IFormFile file,
        [FromQuery] string? descrizione = null)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File non valido");

        if (file.Length > 10 * 1024 * 1024)
            return BadRequest("File troppo grande (max 10 MB)");

        _logger.LogInformation("Upload allegato cassa: SchedaId={SchedaId}, File={Nome}, Size={Size}",
            schedaId, file.FileName, file.Length);

        using var rawStream = file.OpenReadStream();
        using var ms = new MemoryStream();
        await rawStream.CopyToAsync(ms);
        ms.Position = 0;

        string uploadName = file.FileName;
        Stream uploadStream = ms;
        MemoryStream? compressedMs = null;

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        bool isHeic = ext == ".heic" || ext == ".heif";
        bool isImage = file.ContentType?.StartsWith("image/") == true || isHeic;

        // Converti HEIC/HEIF → JPEG con Magick.NET (ha il codec HEIC nativo)
        // Comprimi anche immagini > 2 MB con ImageSharp
        if (isHeic)
        {
            try
            {
                ms.Position = 0;
                using var magick = new MagickImage(ms);
                magick.Quality = 82;
                magick.Format = MagickFormat.Jpeg;
                compressedMs = new MemoryStream(magick.ToByteArray());
                compressedMs.Position = 0;
                uploadName = Path.ChangeExtension(file.FileName, ".jpg");
                uploadStream = compressedMs;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Conversione HEIC→JPEG (Magick) fallita per {Nome}: {Err}", file.FileName, ex.Message);
                ms.Position = 0;
            }
        }
        else if (isImage && ms.Length > 2 * 1024 * 1024)
        {
            try
            {
                ms.Position = 0;
                using var img = await Image.LoadAsync(ms);
                compressedMs = new MemoryStream();
                var encoder = new JpegEncoder { Quality = 82 };
                await img.SaveAsync(compressedMs, encoder);
                compressedMs.Position = 0;
                uploadName = Path.ChangeExtension(file.FileName, ".jpg");
                uploadStream = compressedMs;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Compressione immagine fallita per {Nome}: {Err}", file.FileName, ex.Message);
                ms.Position = 0;
            }
        }

        var result = await _service.UploadAsync(schedaId, uploadStream, uploadName, uploadStream.Length, descrizione);
        compressedMs?.Dispose();
        return Ok(result);
    }

    /// <summary>Elimina un allegato.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _service.DeleteAsync(id);
        if (!ok) return NotFound();
        return NoContent();
    }
}
