using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MESManager.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MESManager.Web.Controllers;

/// <summary>
/// Controller REST per generazione PDF scheda anima.
/// GET /api/anime/{id}/pdf → FileStreamResult (application/pdf)
/// </summary>
[ApiController]
[Route("api/anime")]
[Authorize]
public class AnimePdfController : ControllerBase
{
    private readonly IAnimePdfService _pdfService;
    private readonly ILogger<AnimePdfController> _logger;

    public AnimePdfController(IAnimePdfService pdfService, ILogger<AnimePdfController> logger)
    {
        _pdfService = pdfService;
        _logger = logger;
    }

    /// <summary>
    /// Genera e scarica il PDF scheda anima per l'ID specificato.
    /// Aperto direttamente nel browser: window.open('/api/anime/{id}/pdf')
    /// </summary>
    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> GetPdf(int id)
    {
        _logger.LogInformation("GET /api/anime/{Id}/pdf - richiesta PDF scheda anima", id);

        var stream = await _pdfService.GenerateSchedaAsync(id);
        if (stream is null)
        {
            _logger.LogWarning("GET /api/anime/{Id}/pdf - anima non trovata", id);
            return NotFound($"Anima con ID {id} non trovata.");
        }

        return File(stream, "application/pdf", $"scheda-anima-{id}.pdf");
    }
}
