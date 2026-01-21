using Microsoft.AspNetCore.Mvc;
using MESManager.Application.Services;
using MESManager.Application.DTOs;

namespace MESManager.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AllegatiAnimaController : ControllerBase
    {
        private readonly AllegatiAnimaService _allegatiService;
        private readonly ILogger<AllegatiAnimaController> _logger;

        public AllegatiAnimaController(AllegatiAnimaService allegatiService, ILogger<AllegatiAnimaController> logger)
        {
            _allegatiService = allegatiService;
            _logger = logger;
        }

        /// <summary>
        /// Recupera tutti gli allegati per un'anima (IdArchivio)
        /// </summary>
        [HttpGet("{idArchivio:int}")]
        public async Task<ActionResult<AllegatiAnimaResponse>> GetAllegati(int idArchivio)
        {
            _logger.LogInformation("GET /api/AllegatiAnima/{IdArchivio} - START", idArchivio);
            
            try
            {
                var result = await _allegatiService.GetAllegatiByIdArchivioAsync(idArchivio);
                
                _logger.LogInformation(
                    "GET /api/AllegatiAnima/{IdArchivio} - OK: Foto={FotoCount}, Documenti={DocCount}",
                    idArchivio, result.TotaleFoto, result.TotaleDocumenti);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET /api/AllegatiAnima/{IdArchivio} - ERROR", idArchivio);
                return StatusCode(500, "Errore nel recupero degli allegati");
            }
        }

        /// <summary>
        /// Proxy per servire i file allegati
        /// </summary>
        [HttpGet("file/{id:int}")]
        public async Task<IActionResult> GetFile(int id)
        {
            _logger.LogDebug("GET /api/AllegatiAnima/file/{Id} - START", id);
            
            try
            {
                var allegato = await _allegatiService.GetAllegatoByIdAsync(id);
                if (allegato == null)
                {
                    _logger.LogWarning("GET /api/AllegatiAnima/file/{Id} - Allegato not found", id);
                    return NotFound("Allegato non trovato");
                }

                var content = await _allegatiService.GetFileContentAsync(allegato.PathCompleto);
                if (content == null)
                {
                    _logger.LogWarning("GET /api/AllegatiAnima/file/{Id} - File not found: {Path}", id, allegato.PathCompleto);
                    return NotFound("File non trovato sul server");
                }

                var mimeType = await _allegatiService.GetFileMimeTypeAsync(allegato.PathCompleto);
                
                _logger.LogDebug("GET /api/AllegatiAnima/file/{Id} - OK, size={Size}bytes", id, content.Length);
                
                return File(content, mimeType ?? "application/octet-stream", allegato.NomeFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET /api/AllegatiAnima/file/{Id} - ERROR", id);
                return StatusCode(500, "Errore nel recupero del file");
            }
        }
    }
}
