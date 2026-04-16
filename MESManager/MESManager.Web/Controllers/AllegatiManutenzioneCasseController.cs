using Microsoft.AspNetCore.Mvc;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;

namespace MESManager.Web.Controllers;

/// <summary>
/// Controller per upload/download/delete allegati delle schede manutenzione casse d'anima.
/// </summary>
[ApiController]
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

        if (file.Length > 20 * 1024 * 1024)
            return BadRequest("File troppo grande (max 20 MB)");

        _logger.LogInformation("Upload allegato cassa: SchedaId={SchedaId}, File={Nome}, Size={Size}",
            schedaId, file.FileName, file.Length);

        using var stream = file.OpenReadStream();
        var result = await _service.UploadAsync(schedaId, stream, file.FileName, file.Length, descrizione);
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
