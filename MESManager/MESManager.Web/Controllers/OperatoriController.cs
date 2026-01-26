using Microsoft.AspNetCore.Authorization;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MESManager.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
// [Authorize] // Temporaneamente disabilitato per sviluppo - riabilitare in produzione
public class OperatoriController : ControllerBase
{
    private readonly IOperatoreAppService _operatoreService;

    public OperatoriController(IOperatoreAppService operatoreService)
    {
        _operatoreService = operatoreService;
    }

    [HttpGet]
    public async Task<ActionResult<List<OperatoreDto>>> GetAll()
    {
        var operatori = await _operatoreService.GetAllAsync();
        return Ok(operatori);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OperatoreDto>> GetById(Guid id)
    {
        var operatore = await _operatoreService.GetByIdAsync(id);
        if (operatore == null)
            return NotFound();
        return Ok(operatore);
    }

    [HttpPost]
    public async Task<ActionResult<OperatoreDto>> Create([FromBody] OperatoreDto dto)
    {
        try
        {
            var result = await _operatoreService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<OperatoreDto>> Update(Guid id, [FromBody] OperatoreDto dto)
    {
        if (id != dto.Id)
            return BadRequest("ID mismatch");

        try
        {
            var result = await _operatoreService.UpdateAsync(dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _operatoreService.DeleteAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
