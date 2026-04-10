using Microsoft.AspNetCore.Mvc;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;

namespace MESManager.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize] // Temporaneamente disabilitato come tutti gli altri controller
public class PreventiviController : ControllerBase
{
    private readonly IPreventivoService _service;

    public PreventiviController(IPreventivoService service)
    {
        _service = service;
    }

    // ── Tipi Sabbia ────────────────────────────────────────────────────

    [HttpGet("tipi-sabbia")]
    public async Task<ActionResult<List<PreventivoTipoSabbiaDto>>> GetTipiSabbia()
        => Ok(await _service.GetTipiSabbiaAsync());

    [HttpPost("tipi-sabbia")]
    public async Task<ActionResult<PreventivoTipoSabbiaDto>> CreateTipoSabbia([FromBody] PreventivoTipoSabbiaDto dto)
        => Ok(await _service.CreateTipoSabbiaAsync(dto));

    [HttpPut("tipi-sabbia/{id:guid}")]
    public async Task<ActionResult<PreventivoTipoSabbiaDto>> UpdateTipoSabbia(Guid id, [FromBody] PreventivoTipoSabbiaDto dto)
    {
        dto.Id = id;
        var result = await _service.UpdateTipoSabbiaAsync(dto);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpDelete("tipi-sabbia/{id:guid}")]
    public async Task<IActionResult> DeleteTipoSabbia(Guid id)
        => await _service.DeleteTipoSabbiaAsync(id) ? NoContent() : NotFound();

    // ── Tipi Vernice ───────────────────────────────────────────────────

    [HttpGet("tipi-vernice")]
    public async Task<ActionResult<List<PreventivoTipoVerniceDto>>> GetTipiVernice()
        => Ok(await _service.GetTipiVerniceAsync());

    [HttpPost("tipi-vernice")]
    public async Task<ActionResult<PreventivoTipoVerniceDto>> CreateTipoVernice([FromBody] PreventivoTipoVerniceDto dto)
        => Ok(await _service.CreateTipoVerniceAsync(dto));

    [HttpPut("tipi-vernice/{id:guid}")]
    public async Task<ActionResult<PreventivoTipoVerniceDto>> UpdateTipoVernice(Guid id, [FromBody] PreventivoTipoVerniceDto dto)
    {
        dto.Id = id;
        var result = await _service.UpdateTipoVerniceAsync(dto);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpDelete("tipi-vernice/{id:guid}")]
    public async Task<IActionResult> DeleteTipoVernice(Guid id)
        => await _service.DeleteTipoVerniceAsync(id) ? NoContent() : NotFound();

    // ── Preventivi ─────────────────────────────────────────────────────

    [HttpGet]
    public async Task<ActionResult<List<PreventivoDto>>> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PreventivoDto>> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<PreventivoDto>> Create([FromBody] PreventivoDto dto)
        => Ok(await _service.CreateAsync(dto));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PreventivoDto>> Update(Guid id, [FromBody] PreventivoDto dto)
    {
        dto.Id = id;
        var result = await _service.UpdateAsync(dto);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
        => await _service.DeleteAsync(id) ? NoContent() : NotFound();

    [HttpPatch("{id:guid}/stato")]
    public async Task<ActionResult<PreventivoDto>> UpdateStato(Guid id, [FromBody] string stato)
    {
        var result = await _service.UpdateStatoAsync(id, stato);
        return result == null ? NotFound() : Ok(result);
    }
}
