using Microsoft.AspNetCore.Mvc;
using MESManager.Sync.Services;

namespace MESManager.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SyncController : ControllerBase
{
    private readonly ISyncCoordinator _syncCoordinator;
    private readonly ILogger<SyncController> _logger;

    public SyncController(ISyncCoordinator syncCoordinator, ILogger<SyncController> logger)
    {
        _syncCoordinator = syncCoordinator;
        _logger = logger;
    }

    [HttpPost("all")]
    public async Task<IActionResult> SyncAll()
    {
        try
        {
            _logger.LogInformation("Sincronizzazione manuale avviata");
            var logs = await _syncCoordinator.SyncTuttoAsync();
            _logger.LogInformation("Sincronizzazione completata");
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la sincronizzazione");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("commesse")]
    public async Task<IActionResult> SyncCommesse()
    {
        try
        {
            _logger.LogInformation("Sincronizzazione commesse avviata");
            var log = await _syncCoordinator.SyncCommesseAsync();
            _logger.LogInformation("Sincronizzazione commesse completata: Nuovi={nuovi}, Aggiornati={aggiornati}, Ignorati={ignorati}, Errori={errori}",
                log.Nuovi, log.Aggiornati, log.Ignorati, log.Errori);
            return Ok(log);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la sincronizzazione commesse");
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    [HttpPost("clienti")]
    public async Task<IActionResult> SyncClienti()
    {
        try
        {
            var log = await _syncCoordinator.SyncClientiAsync();
            return Ok(log);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la sincronizzazione clienti");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("articoli")]
    public async Task<IActionResult> SyncArticoli()
    {
        try
        {
            var log = await _syncCoordinator.SyncArticoliAsync();
            return Ok(log);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la sincronizzazione articoli");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
