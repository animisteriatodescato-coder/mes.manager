using Microsoft.AspNetCore.Mvc;
using MESManager.Application.Interfaces;
using MESManager.Application.DTOs;

namespace MESManager.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientiController : ControllerBase
{
    private readonly IClienteAppService _service;
    public ClientiController(IClienteAppService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<ClienteDto>>> Get()
    {
        var result = await _service.GetListaAsync();
        return Ok(result);
    }
}
