using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MESManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MESManager.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize] // Temporaneamente disabilitato per sviluppo - riabilitare in produzione
public class MacchineController : ControllerBase
{
    private readonly MesManagerDbContext _context;

    public MacchineController(MesManagerDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<MacchinaDto>>> Get()
    {
        var macchine = await _context.Macchine
            .OrderBy(m => m.Codice)
            .Select(m => new MacchinaDto
            {
                Id = m.Id,
                Codice = m.Codice,
                Nome = m.Nome
            })
            .ToListAsync();

        return Ok(macchine);
    }
}

public class MacchinaDto
{
    public Guid Id { get; set; }
    public string Codice { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
}
