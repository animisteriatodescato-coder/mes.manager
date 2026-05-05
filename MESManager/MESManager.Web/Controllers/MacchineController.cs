using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MESManager.Application.DTOs;
using MESManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MESManager.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MacchineController : ControllerBase
{
    private readonly MesManagerDbContext _context;

    public MacchineController(MesManagerDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<MacchinaDto>>> Get([FromQuery] bool? hasPlc = null)
    {
        var query = _context.Macchine.AsQueryable();

        // hasPlc=true → solo macchine con IndirizzoPLC configurato (usate da Dashboard, Storico PLC, etc.)
        // hasPlc=false → solo macchine senza PLC
        // hasPlc assente → tutte le macchine
        if (hasPlc == true)
            query = query.Where(m => !string.IsNullOrWhiteSpace(m.IndirizzoPLC));
        else if (hasPlc == false)
            query = query.Where(m => string.IsNullOrWhiteSpace(m.IndirizzoPLC));

        var macchine = await query
            .OrderBy(m => m.Codice)
            .Select(m => new MacchinaDto
            {
                Id = m.Id,
                Codice = m.Codice,
                Nome = m.Nome,
                Stato = (int)m.Stato,
                AttivaInGantt = m.AttivaInGantt,
                OrdineVisualizazione = m.OrdineVisualizazione,
                IndirizzoPLC = m.IndirizzoPLC
            })
            .ToListAsync();

        return Ok(macchine);
    }
}
