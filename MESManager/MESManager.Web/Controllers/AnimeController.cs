using Microsoft.AspNetCore.Mvc;
using MESManager.Application.Interfaces;
using MESManager.Application.DTOs;
using MESManager.Application.Services;
using MESManager.Infrastructure.Services;

namespace MESManager.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnimeController : ControllerBase
{
    private readonly IAnimeService _service;
    private readonly AnimeImportService _importService;
    private readonly AnimeExcelImportService _excelImportService;
    
    public AnimeController(IAnimeService service, AnimeImportService importService, AnimeExcelImportService excelImportService)
    {
        _service = service;
        _importService = importService;
        _excelImportService = excelImportService;
    }

    [HttpGet]
    public async Task<ActionResult<List<AnimeDto>>> Get()
    {
        var result = await _service.GetAllAsync();
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

    [HttpPost("import")]
    public async Task<ActionResult<int>> ImportFromGantt()
    {
        try
        {
            var count = await _importService.ImportFromGanttAsync();
            return Ok(new { ImportedCount = count, Message = $"{count} anime importati da Gantt" });
        }
        catch (Exception ex)
        {
            // Log completo dell'errore
            Console.WriteLine($"Errore durante importazione: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
            
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
        var result = await _service.UpdateAsync(id, dto);
        if (result == null)
            return NotFound();
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
