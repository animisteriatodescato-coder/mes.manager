using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MESManager.Application.Services;
using MESManager.Application.DTOs;

namespace MESManager.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Richiede autenticazione per tutti gli endpoint
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
        /// Recupera tutti gli allegati per un'anima per CodiceArticolo
        /// </summary>
        [HttpGet("codice/{codiceArticolo}")]
        public async Task<ActionResult<AllegatiAnimaResponse>> GetAllegatiByCodice(string codiceArticolo)
        {
            _logger.LogInformation("GET /api/AllegatiAnima/codice/{CodiceArticolo} - START", codiceArticolo);
            
            try
            {
                var result = await _allegatiService.GetAllegatiByCodiceArticoloAsync(codiceArticolo);
                
                _logger.LogInformation(
                    "GET /api/AllegatiAnima/codice/{CodiceArticolo} - OK: Foto={FotoCount}, Documenti={DocCount}",
                    codiceArticolo, result.TotaleFoto, result.TotaleDocumenti);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET /api/AllegatiAnima/codice/{CodiceArticolo} - ERROR", codiceArticolo);
                return StatusCode(500, "Errore nel recupero degli allegati");
            }
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

        /// <summary>
        /// Upload di un nuovo allegato
        /// </summary>
        [HttpPost("upload/{codiceArticolo}")]
        public async Task<ActionResult<AllegatoAnimaDto>> UploadAllegato(
            string codiceArticolo, 
            IFormFile file, 
            [FromQuery] string? descrizione = null)
        {
            _logger.LogInformation("POST /api/AllegatiAnima/upload/{CodiceArticolo} - START, File={FileName}, Size={Size}", 
                codiceArticolo, file?.FileName, file?.Length);
            
            if (file == null || file.Length == 0)
            {
                return BadRequest("Nessun file caricato");
            }

            try
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                var contenuto = ms.ToArray();

                var estensione = Path.GetExtension(file.FileName).ToLowerInvariant();
                var isFoto = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" }.Contains(estensione);

                var result = await _allegatiService.UploadAllegatoAsync(
                    codiceArticolo, 
                    file.FileName, 
                    contenuto, 
                    descrizione, 
                    isFoto);

                if (result == null)
                {
                    return StatusCode(500, "Errore nel salvataggio dell'allegato");
                }

                _logger.LogInformation("POST /api/AllegatiAnima/upload/{CodiceArticolo} - OK, NewId={Id}", 
                    codiceArticolo, result.Id);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "POST /api/AllegatiAnima/upload/{CodiceArticolo} - ERROR", codiceArticolo);
                return StatusCode(500, "Errore nel caricamento del file");
            }
        }

        /// <summary>
        /// Elimina un allegato
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteAllegato(int id)
        {
            _logger.LogInformation("DELETE /api/AllegatiAnima/{Id} - START", id);
            
            try
            {
                var result = await _allegatiService.DeleteAllegatoAsync(id);
                
                if (!result)
                {
                    return NotFound("Allegato non trovato");
                }

                _logger.LogInformation("DELETE /api/AllegatiAnima/{Id} - OK", id);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DELETE /api/AllegatiAnima/{Id} - ERROR", id);
                return StatusCode(500, "Errore nell'eliminazione dell'allegato");
            }
        }
    }
}
