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
}
