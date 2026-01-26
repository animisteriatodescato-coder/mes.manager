using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MESManager.Application.Interfaces;
using MESManager.Application.DTOs;
using MESManager.Application.Services;
using MESManager.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace MESManager.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize] // Temporaneamente disabilitato per sviluppo - riabilitare in produzione
public class AnimeController : ControllerBase
{
    private readonly IAnimeService _service;
    private readonly AnimeImportService _importService;
    private readonly AnimeExcelImportService _excelImportService;
    private readonly IAllegatoArticoloService _allegatoService;
    private readonly ILogger<AnimeController> _logger;
    
    public AnimeController(
        IAnimeService service, 
        AnimeImportService importService, 
        AnimeExcelImportService excelImportService,
        IAllegatoArticoloService allegatoService,
        ILogger<AnimeController> logger)
    {
        _service = service;
        _importService = importService;
        _excelImportService = excelImportService;
        _allegatoService = allegatoService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<AnimeDto>>> Get()
    {
        _logger.LogInformation("GET /api/Anime - fetching all anime");
        var result = await _service.GetAllAsync();
        
        // Arricchisci con conteggio foto/documenti
        var conteggi = await _allegatoService.GetConteggioPerArticoloAsync();
        foreach (var anime in result)
        {
            if (conteggi.TryGetValue(anime.CodiceArticolo, out var counts))
            {
                anime.NumeroFoto = counts.Foto;
                anime.NumeroDocumenti = counts.Documenti;
            }
        }
        
        _logger.LogInformation("GET /api/Anime - returning {Count} anime", result.Count);
        
        if (result.Count == 0)
        {
            _logger.LogWarning("GET /api/Anime - RETURNED ZERO RECORDS - Database may be empty");
        }
        
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AnimeDto>> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    [HttpGet("by-codice/{codiceArticolo}")]
    public async Task<ActionResult<AnimeDto>> GetByCodiceArticolo(string codiceArticolo)
    {
        var result = await _service.GetByCodiceArticoloAsync(codiceArticolo);
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    [HttpPost("import")]
    public async Task<ActionResult<int>> ImportFromGantt()
    {
        try
        {
            _logger.LogInformation("POST /api/Anime/import - starting Gantt import");
            var count = await _importService.ImportFromGanttAsync();
            _logger.LogInformation("POST /api/Anime/import - completed, count={Count}", count);
            return Ok(new { ImportedCount = count, Message = $"{count} anime importati da Gantt" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "POST /api/Anime/import - FAILED");
            return BadRequest(new { Error = ex.Message, Details = ex.InnerException?.Message });
        }
    }

    [HttpPost("import-excel")]
    public async Task<ActionResult<int>> ImportFromExcel(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Error = "Nessun file caricato" });

            if (!file.FileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase) && 
                !file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { Error = "Il file deve essere in formato Excel (.xls o .xlsx)" });

            using var stream = file.OpenReadStream();
            var count = await _excelImportService.ImportFromExcelAsync(stream);
            return Ok(new { ImportedCount = count, Message = $"{count} anime importati da Excel" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Errore durante importazione Excel: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
            
            return BadRequest(new { Error = ex.Message, Details = ex.InnerException?.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<AnimeDto>> Create([FromBody] AnimeDto dto)
    {
        var result = await _service.AddAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AnimeDto>> Update(int id, [FromBody] AnimeDto dto)
    {
        _logger.LogInformation("PUT /api/Anime/{Id} - updating anime", id);
        
        // Imposta tracking modifiche locali
        dto.DataUltimaModificaLocale = DateTime.Now;
        dto.ModificatoLocalmente = true;
        // TODO: impostare UtenteUltimaModificaLocale da contesto utente autenticato
        
        var result = await _service.UpdateAsync(id, dto);
        if (result == null)
        {
            _logger.LogWarning("PUT /api/Anime/{Id} - NOT FOUND", id);
            return NotFound();
        }
        
        _logger.LogInformation("PUT /api/Anime/{Id} - updated successfully", id);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var success = await _service.DeleteAsync(id);
        if (!success)
            return NotFound();
        return NoContent();
    }
}
