using Microsoft.AspNetCore.Mvc;
using MESManager.Application.Interfaces;
using MESManager.Application.DTOs;
using MESManager.Infrastructure.Services;

namespace MESManager.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlcController : ControllerBase
{
    private readonly IPlcAppService _service;
    private readonly IPlcSyncCoordinator _syncCoordinator;

    public PlcController(IPlcAppService service, IPlcSyncCoordinator syncCoordinator)
    {
        _service = service;
        _syncCoordinator = syncCoordinator;
    }

    [HttpGet("realtime")]
    public async Task<ActionResult<List<PlcRealtimeDto>>> GetRealtime()
    {
        var result = await _service.GetRealtimeDataAsync();
        return Ok(result);
    }

    [HttpGet("storico/{macchinaId}")]
    public async Task<ActionResult<List<PlcStoricoDto>>> GetStorico(
        Guid macchinaId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var result = await _service.GetStoricoAsync(macchinaId, from, to);
        return Ok(result);
    }

    [HttpGet("eventi/{macchinaId}")]
    public async Task<ActionResult<List<EventoPLCDto>>> GetEventi(
        Guid macchinaId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var result = await _service.GetEventiAsync(macchinaId, from, to);
        return Ok(result);
    }

    [HttpPost("sync/{macchinaId}")]
    public async Task<ActionResult<PlcSyncResultDto>> SyncMacchina(Guid macchinaId)
    {
        var result = await _syncCoordinator.SyncMacchinaAsync(macchinaId);
        
        var dto = new PlcSyncResultDto
        {
            MacchinaId = result.MacchinaId,
            MacchinaCodiceMacchina = result.MacchinaCodiceMacchina,
            Successo = result.Successo,
            RecordAggiornati = result.RecordAggiornati,
            MessaggioErrore = result.MessaggioErrore,
            DataOra = result.DataOra
        };
        
        if (!result.Successo)
        {
            return BadRequest(dto);
        }
        
        return Ok(dto);
    }

    [HttpPost("sync/all")]
    public async Task<ActionResult<List<PlcSyncResultDto>>> SyncAll()
    {
        var results = await _syncCoordinator.SyncTutteMacchineAsync();
        
        var dtos = results.Select(r => new PlcSyncResultDto
        {
            MacchinaId = r.MacchinaId,
            MacchinaCodiceMacchina = r.MacchinaCodiceMacchina,
            Successo = r.Successo,
            RecordAggiornati = r.RecordAggiornati,
            MessaggioErrore = r.MessaggioErrore,
            DataOra = r.DataOra
        }).ToList();
        
        return Ok(dtos);
    }
}
