using Microsoft.AspNetCore.Mvc;
using MESManager.Application.DTOs;
using MESManager.Application.Services;

namespace MESManager.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RicetteArticoliController : ControllerBase
{
    private readonly RicettaGanttService _ricettaService;
    private readonly ILogger<RicetteArticoliController> _logger;

    public RicetteArticoliController(
        RicettaGanttService ricettaService,
        ILogger<RicetteArticoliController> logger)
    {
        _ricettaService = ricettaService;
        _logger = logger;
    }

    /// <summary>
    /// Ottiene la ricetta completa per un codice articolo
    /// </summary>
    [HttpGet("{codiceArticolo}")]
    public async Task<ActionResult<RicettaArticoloDto>> GetByCodiceArticolo(string codiceArticolo)
    {
        try
        {
            var ricetta = await _ricettaService.GetRicettaByCodiceArticoloAsync(codiceArticolo);
            if (ricetta == null)
                return NotFound(new { message = $"Nessuna ricetta trovata per articolo {codiceArticolo}" });

            return Ok(ricetta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante lettura ricetta per {CodiceArticolo}", codiceArticolo);
            return StatusCode(500, new { message = "Errore durante lettura ricetta", error = ex.Message });
        }
    }

    /// <summary>
    /// Cerca ricette per codice articolo (ricerca parziale)
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<RicetteSearchResponse>> Search(
        [FromQuery] string? q = null,
        [FromQuery] int max = 50)
    {
        try
        {
            var result = await _ricettaService.SearchRicetteAsync(q, max);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante ricerca ricette");
            return StatusCode(500, new { message = "Errore durante ricerca ricette", error = ex.Message });
        }
    }

    /// <summary>
    /// Ottiene la lista di tutti i codici articolo che hanno ricette
    /// </summary>
    [HttpGet("codici")]
    public async Task<ActionResult<List<string>>> GetCodiciArticolo()
    {
        try
        {
            var codici = await _ricettaService.GetCodiciArticoloConRicetteAsync();
            return Ok(codici);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante lettura codici articolo");
            return StatusCode(500, new { message = "Errore durante lettura codici", error = ex.Message });
        }
    }

    /// <summary>
    /// Conta il totale delle ricette disponibili
    /// </summary>
    [HttpGet("count")]
    public async Task<ActionResult<int>> Count()
    {
        try
        {
            var count = await _ricettaService.CountRicetteAsync();
            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante conteggio ricette");
            return StatusCode(500, new { message = "Errore durante conteggio", error = ex.Message });
        }
    }

    /// <summary>
    /// Aggiorna il valore di un singolo parametro ricetta
    /// </summary>
    [HttpPut("parametro/{parametroId:guid}/valore")]
    public async Task<IActionResult> UpdateValoreParametro(Guid parametroId, [FromBody] UpdateValoreRequest request)
    {
        try
        {
            var updated = await _ricettaService.UpdateValoreParametroAsync(parametroId, request.Valore);
            if (!updated)
                return NotFound(new { message = $"Parametro {parametroId} non trovato" });

            return Ok(new { message = "Valore aggiornato", parametroId, valore = request.Valore });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante aggiornamento valore parametro {ParametroId}", parametroId);
            return StatusCode(500, new { message = "Errore durante aggiornamento", error = ex.Message });
        }
    }
}

public record UpdateValoreRequest(int Valore);
