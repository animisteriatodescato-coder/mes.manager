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
[Authorize]
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

    [HttpGet("by-codice/{**codiceArticolo}")]
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

            // Validazione magic bytes: verifica firma del file indipendentemente dall'estensione
            using var stream = file.OpenReadStream();
            var header = new byte[8];
            var bytesRead = await stream.ReadAsync(header, 0, 8);
            stream.Position = 0;
            if (!IsValidExcelFile(header, bytesRead))
                return BadRequest(new { Error = "Il file non è un file Excel valido" });

            var count = await _excelImportService.ImportFromExcelAsync(stream);
            return Ok(new { ImportedCount = count, Message = $"{count} anime importati da Excel" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante importazione Excel");
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

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifica la firma (magic bytes) del file per determinare se è un file Excel valido.
    /// .xlsx = ZIP (50 4B 03 04), .xls = OLE2 Compound Document (D0 CF 11 E0)
    /// </summary>
    private static bool IsValidExcelFile(byte[] header, int bytesRead)
    {
        if (bytesRead < 4) return false;
        // .xlsx: ZIP signature PK\x03\x04
        if (header[0] == 0x50 && header[1] == 0x4B && header[2] == 0x03 && header[3] == 0x04)
            return true;
        // .xls: OLE2 Compound Document signature D0 CF 11 E0
        if (bytesRead >= 4 && header[0] == 0xD0 && header[1] == 0xCF && header[2] == 0x11 && header[3] == 0xE0)
            return true;
        return false;
    }
}
