using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MESManager.Application.Interfaces;
using MESManager.Application.DTOs;

namespace MESManager.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize] // Temporaneamente disabilitato per sviluppo - riabilitare in produzione
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

    [HttpPatch("{id}/numero-macchina")]
    public async Task<ActionResult> UpdateNumeroMacchina(Guid id, [FromBody] UpdateNumeroMacchinaRequest request)
    {
        await _service.AggiornaNumeroMacchinaAsync(id, request.NumeroMacchina);
        return Ok();
    }

    [HttpPatch("{id}/stato-programma")]
    public async Task<ActionResult> UpdateStatoProgramma(Guid id, [FromBody] UpdateStatoProgrammaRequest request)
    {
        await _service.AggiornaStatoProgrammaAsync(id, request.StatoProgramma, request.Note, request.Utente);
        return Ok();
    }

    [HttpGet("{id}/storico-programmazione")]
    public async Task<ActionResult<List<StoricoProgrammazioneDto>>> GetStoricoProgrammazione(Guid id)
    {
        var result = await _service.GetStoricoProgrammazioneAsync(id);
        return Ok(result);
    }
}

public class UpdateStatoRequest
{
    public string Stato { get; set; } = string.Empty;
}

public class UpdateNumeroMacchinaRequest
{
    public string? NumeroMacchina { get; set; }
}

public class UpdateStatoProgrammaRequest
{
    public string StatoProgramma { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string? Utente { get; set; }
}
