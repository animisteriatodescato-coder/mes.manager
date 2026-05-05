using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;

namespace MESManager.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NonConformitaController : ControllerBase
{
    private readonly INonConformitaService _service;

    public NonConformitaController(INonConformitaService service)
    {
        _service = service;
    }

    /// <summary>Tutte le NC (usato dalla pagina CatalogoNonConformita via DI diretta, non HTTP).</summary>
    [HttpGet]
    public async Task<ActionResult<List<NonConformitaDto>>> GetAll()
        => Ok(await _service.GetAllAsync());

    /// <summary>Solo NC aperte (Aperta + InGestione). Usato per badge + popup.</summary>
    [HttpGet("aperte")]
    public async Task<ActionResult<List<NonConformitaDto>>> GetAperte()
        => Ok(await _service.GetAperteAsync());

    /// <summary>NC per codice articolo (tutte, non solo aperte).</summary>
    [HttpGet("articolo/{codice}")]
    public async Task<ActionResult<List<NonConformitaDto>>> GetByArticolo(string codice)
    {
        if (string.IsNullOrWhiteSpace(codice)) return BadRequest();
        return Ok(await _service.GetByCodiceArticoloAsync(Uri.UnescapeDataString(codice)));
    }

    /// <summary>NC aperte per articolo — per popup warning nel form commessa.</summary>
    [HttpGet("aperte/articolo/{codice}")]
    public async Task<ActionResult<List<NonConformitaDto>>> GetAperteByArticolo(string codice)
    {
        if (string.IsNullOrWhiteSpace(codice)) return BadRequest();
        var all = await _service.GetByCodiceArticoloAsync(Uri.UnescapeDataString(codice));
        return Ok(all.Where(nc => nc.Stato != "Chiusa").ToList());
    }

    /// <summary>
    /// Conteggio NC aperte per articolo — batch call per arricchire la griglia commesse.
    /// Ritorna Dictionary[CodiceArticolo, CountNcAperte].
    /// </summary>
    [HttpGet("count-per-articolo")]
    public async Task<ActionResult<Dictionary<string, int>>> GetCountPerArticolo()
    {
        var aperte = await _service.GetAperteAsync();
        var dict = aperte
            .GroupBy(nc => nc.CodiceArticolo)
            .ToDictionary(g => g.Key, g => g.Count());
        return Ok(dict);
    }

    /// <summary>Conteggio totale NC aperte (per badge menu sidebar).</summary>
    [HttpGet("count-aperte-totale")]
    public async Task<ActionResult<int>> GetCountAperteTotale()
    {
        var aperte = await _service.GetAperteAsync();
        return Ok(aperte.Count);
    }
}
