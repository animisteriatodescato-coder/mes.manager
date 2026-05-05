using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;

namespace MESManager.Web.Controllers;

/// <summary>
/// Controller per la gestione degli allegati articolo (catalogo locale)
/// </summary>
[ApiController]
[Route("api/allegati-articoli")]
[Authorize]
public class AllegatiArticoliController : ControllerBase
{
    private readonly IAllegatoArticoloService _service;
    private readonly ILogger<AllegatiArticoliController> _logger;

    public AllegatiArticoliController(
        IAllegatoArticoloService service,
        ILogger<AllegatiArticoliController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Ottiene gli allegati per un articolo, separati in Foto e Documenti
    /// </summary>
    [HttpGet("articolo/{**codiceArticolo}")]
    public async Task<ActionResult<AllegatiArticoloResponse>> GetByArticolo(
        string codiceArticolo,
        [FromQuery] int? idArchivio = null)
    {
        _logger.LogInformation("GET allegati-articoli/articolo/{CodiceArticolo}", codiceArticolo);
        
        var result = await _service.GetAllegatiByArticoloAsync(codiceArticolo, idArchivio);
        return Ok(result);
    }

    /// <summary>
    /// Ottiene un allegato per ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AllegatoArticoloDto>> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null)
            return NotFound();
        
        return Ok(result);
    }

    /// <summary>
    /// Scarica il contenuto di un file allegato
    /// </summary>
    [HttpGet("{id:int}/file")]
    public async Task<IActionResult> GetFile(int id)
    {
        var result = await _service.GetFileContentAsync(id);
        if (result == null)
            return NotFound();
        
        var (content, contentType, fileName) = result.Value;
        return File(content, contentType, fileName);
    }

    /// <summary>
    /// Carica un nuovo allegato
    /// </summary>
    [HttpPost("upload/{**codiceArticolo}")]
    public async Task<ActionResult<AllegatoArticoloDto>> Upload(
        string codiceArticolo,
        IFormFile file,
        [FromQuery] int? idArchivio = null,
        [FromQuery] string? descrizione = null,
        [FromQuery] int priorita = 0)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File non valido");
        
        _logger.LogInformation("POST upload/{CodiceArticolo} - File: {FileName}, Size: {Size}", 
            codiceArticolo, file.FileName, file.Length);
        
        var request = new UploadAllegatoRequest
        {
            CodiceArticolo = codiceArticolo,
            IdArchivio = idArchivio,
            Descrizione = descrizione,
            Priorita = priorita
        };
        
        using var stream = file.OpenReadStream();
        var result = await _service.UploadAsync(stream, file.FileName, file.Length, request);
        return Ok(result);
    }

    /// <summary>
    /// Elimina un allegato
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        _logger.LogInformation("DELETE allegati-articoli/{Id}", id);
        
        var success = await _service.DeleteAsync(id);
        if (!success)
            return NotFound();
        
        return NoContent();
    }

    /// <summary>
    /// Aggiorna la priorità di un allegato
    /// </summary>
    [HttpPut("{id:int}/priorita")]
    public async Task<IActionResult> UpdatePriorita(int id, [FromBody] int priorita)
    {
        _logger.LogInformation("PUT allegati-articoli/{Id}/priorita = {Priorita}", id, priorita);
        
        var success = await _service.UpdatePrioritaAsync(id, priorita);
        if (!success)
            return NotFound();
        
        return NoContent();
    }

    /// <summary>
    /// Importa allegati da Gantt per un articolo specifico
    /// </summary>
    [HttpPost("import/{**codiceArticolo}")]
    public async Task<ActionResult<ImportAllegatiResult>> ImportForArticolo(string codiceArticolo)
    {
        _logger.LogInformation("POST import/{CodiceArticolo}", codiceArticolo);
        
        var result = await _service.ImportFromGanttAsync(codiceArticolo);
        return Ok(result);
    }

    /// <summary>
    /// Importa tutti gli allegati da Gantt
    /// </summary>
    [HttpPost("import-all")]
    public async Task<ActionResult<ImportAllegatiResult>> ImportAll()
    {
        _logger.LogInformation("POST import-all");
        
        var result = await _service.ImportAllFromGanttAsync();
        return Ok(result);
    }

    /// <summary>
    /// Ottiene statistiche sugli allegati
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<object>> GetStats()
    {
        var totale = await _service.CountAsync();
        var importati = await _service.CountImportatiDaGanttAsync();
        
        return Ok(new
        {
            Totale = totale,
            ImportatiDaGantt = importati,
            CaricatiLocalmente = totale - importati
        });
    }

    /// <summary>
    /// Ottiene il conteggio foto/documenti per ogni articolo
    /// </summary>
    [HttpGet("conteggio")]
    public async Task<ActionResult<Dictionary<string, object>>> GetConteggio()
    {
        var conteggi = await _service.GetConteggioPerArticoloAsync();
        
        // Converti in un formato JSON-friendly
        var result = conteggi.ToDictionary(
            x => x.Key,
            x => new { Foto = x.Value.Foto, Documenti = x.Value.Documenti }
        );
        
        return Ok(result);
    }
}
