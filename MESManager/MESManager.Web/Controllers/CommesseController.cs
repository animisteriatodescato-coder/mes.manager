using Microsoft.AspNetCore.Mvc;
using MESManager.Application.Interfaces;
using MESManager.Application.DTOs;

namespace MESManager.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommesseController : ControllerBase
{
    private readonly ICommessaAppService _service;
    public CommesseController(ICommessaAppService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<CommessaDto>>> Get()
    {
        var result = await _service.GetListaAsync();
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CommessaDto>> Update(Guid id, [FromBody] CommessaDto dto)
    {
        var result = await _service.AggiornaAsync(id, dto);
        return Ok(result);
    }

    [HttpPatch("{id}/stato")]
    public async Task<ActionResult> UpdateStato(Guid id, [FromBody] UpdateStatoRequest request)
    {
        await _service.AggiornaStatoAsync(id, request.Stato);
        return Ok();
    }
}

public class UpdateStatoRequest
{
    public string Stato { get; set; } = string.Empty;
}
