using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MESManager.Infrastructure.Data;

namespace MESManager.Web.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/[controller]")]
public class DiagnosticsController : ControllerBase
{
    private readonly MesManagerDbContext _context;
    private readonly ILogger<DiagnosticsController> _logger;

    public DiagnosticsController(MesManagerDbContext context, ILogger<DiagnosticsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Diagnostica completa cataloghi (Anime, Operatori, Macchine)
    /// </summary>
    [HttpGet("catalogs")]
    public async Task<ActionResult<CatalogDiagnostics>> GetCatalogDiagnostics()
    {
        try
        {
            _logger.LogInformation("[DIAGNOSTICS] Avvio diagnostica cataloghi");

            var result = new CatalogDiagnostics
            {
                Timestamp = DateTime.UtcNow,
                DatabaseName = _context.Database.GetDbConnection().Database
            };

            // Conteggi base
            result.AnimeCount = await _context.Anime.CountAsync();
            result.OperatoriCount = await _context.Operatori.CountAsync();
            result.MacchineCount = await _context.Macchine.CountAsync();
            result.ArticoliCount = await _context.Articoli.CountAsync();
            result.RicetteCount = await _context.Ricette.CountAsync();
            result.ClientiCount = await _context.Clienti.CountAsync();
            result.CommesseCount = await _context.Commesse.CountAsync();

            // Duplicati Macchine
            result.MacchineAttive = await _context.Macchine.CountAsync(m => m.AttivaInGantt);
            result.MacchineInattive = await _context.Macchine.CountAsync(m => !m.AttivaInGantt);

            var duplicatiMacchine = await _context.Macchine
                .GroupBy(m => new { m.Codice, m.Nome })
                .Where(g => g.Count() > 1)
                .Select(g => new DuplicateInfo
                {
                    Key = $"{g.Key.Codice} - {g.Key.Nome}",
                    Count = g.Count()
                })
                .ToListAsync();

            result.MacchineDeuplicate = duplicatiMacchine;
            result.HasDuplicates = duplicatiMacchine.Any();

            // Operatori attivi
            result.OperatoriAttivi = await _context.Operatori.CountAsync(o => o.Attivo);

            // Sample data
            result.SampleAnime = await _context.Anime
                .OrderBy(a => a.Id)
                .Take(5)
                .Select(a => new { a.Id, a.CodiceArticolo, a.DescrizioneArticolo })
                .ToListAsync();

            result.SampleOperatori = await _context.Operatori
                .OrderBy(o => o.NumeroOperatore)
                .Select(o => new { o.Id, o.NumeroOperatore, o.Nome, o.Cognome, o.Attivo })
                .ToListAsync();

            result.SampleMacchine = await _context.Macchine
                .OrderBy(m => m.Codice)
                .Select(m => new { m.Id, m.Codice, m.Nome, m.AttivaInGantt })
                .ToListAsync();

            // Validazione
            result.IsValid = 
                result.AnimeCount > 0 &&
                result.OperatoriCount > 0 &&
                result.MacchineCount > 0 &&
                !result.HasDuplicates;

            result.Warnings = new List<string>();
            if (result.AnimeCount == 0) result.Warnings.Add("ATTENZIONE: Tabella Anime vuota");
            if (result.OperatoriCount == 0) result.Warnings.Add("CRITICO: Tabella Operatori vuota");
            if (result.MacchineCount == 0) result.Warnings.Add("CRITICO: Tabella Macchine vuota");
            if (result.HasDuplicates) result.Warnings.Add($"ATTENZIONE: {duplicatiMacchine.Count} duplicati in Macchine");

            _logger.LogInformation("[DIAGNOSTICS] Diagnostica completata: Valid={IsValid}, Warnings={WarningCount}", 
                result.IsValid, result.Warnings.Count);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DIAGNOSTICS] Errore durante diagnostica cataloghi");
            return StatusCode(500, new { error = "Errore diagnostica", message = ex.Message });
        }
    }

    /// <summary>
    /// Health check rapido database
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult<object>> GetHealthStatus()
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync();
            
            if (!canConnect)
            {
                return StatusCode(503, new { status = "unhealthy", message = "Database non raggiungibile" });
            }

            var counts = new
            {
                anime = await _context.Anime.CountAsync(),
                operatori = await _context.Operatori.CountAsync(),
                macchine = await _context.Macchine.CountAsync()
            };

            var isHealthy = counts.anime > 0 && counts.operatori > 0 && counts.macchine > 0;

            return Ok(new
            {
                status = isHealthy ? "healthy" : "degraded",
                timestamp = DateTime.UtcNow,
                database = _context.Database.GetDbConnection().Database,
                counts
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DIAGNOSTICS] Errore health check");
            return StatusCode(503, new { status = "unhealthy", error = ex.Message });
        }
    }
}

public class CatalogDiagnostics
{
    public DateTime Timestamp { get; set; }
    public string DatabaseName { get; set; } = string.Empty;
    
    // Conteggi
    public int AnimeCount { get; set; }
    public int OperatoriCount { get; set; }
    public int MacchineCount { get; set; }
    public int ArticoliCount { get; set; }
    public int RicetteCount { get; set; }
    public int ClientiCount { get; set; }
    public int CommesseCount { get; set; }
    
    // Dettagli
    public int MacchineAttive { get; set; }
    public int MacchineInattive { get; set; }
    public int OperatoriAttivi { get; set; }
    
    // Problemi
    public bool HasDuplicates { get; set; }
    public List<DuplicateInfo> MacchineDeuplicate { get; set; } = new();
    
    // Validazione
    public bool IsValid { get; set; }
    public List<string> Warnings { get; set; } = new();
    
    // Sample data
    public object? SampleAnime { get; set; }
    public object? SampleOperatori { get; set; }
    public object? SampleMacchine { get; set; }
}

public class DuplicateInfo
{
    public string Key { get; set; } = string.Empty;
    public int Count { get; set; }
}
