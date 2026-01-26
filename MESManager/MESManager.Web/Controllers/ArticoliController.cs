using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MESManager.Application.Interfaces;
using MESManager.Application.DTOs;

namespace MESManager.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize] // Temporaneamente disabilitato per sviluppo - riabilitare in produzione
public class ArticoliController : ControllerBase
{
    private readonly IArticoloAppService _service;
    public ArticoliController(IArticoloAppService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<ArticoloDto>>> Get()
    {
        var result = await _service.GetListaAsync();
        return Ok(result);
    }
}
