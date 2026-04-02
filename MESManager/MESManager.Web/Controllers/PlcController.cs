using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MESManager.Web.Constants;
using Microsoft.EntityFrameworkCore;
using MESManager.Application.Interfaces;
using MESManager.Application.DTOs;
using MESManager.Domain.Entities;
using MESManager.Domain.Constants;
using MESManager.Infrastructure.Data;
using MESManager.Infrastructure.Services;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MESManager.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize] // Temporaneamente disabilitato per sviluppo - riabilitare in produzione
public class PlcController : ControllerBase
{
    private readonly IPlcAppService _service;
    private readonly IPlcSyncCoordinator _syncCoordinator;
    private readonly IPlcRecipeWriterService _recipeWriter;
    private readonly IRecipeAutoLoaderService _autoLoader;
    private readonly IRicettaGanttService _ricettaService;
    private readonly IAnimeFtpService _ftpService;
    private readonly MesManagerDbContext _context;
    private readonly ILogger<PlcController> _logger;

    public PlcController(
        IPlcAppService service, 
        IPlcSyncCoordinator syncCoordinator,
        IPlcRecipeWriterService recipeWriter,
        IRecipeAutoLoaderService autoLoader,
        IRicettaGanttService ricettaService,
        IAnimeFtpService ftpService,
        MesManagerDbContext context,
        ILogger<PlcController> logger)
    {
        _service = service;
        _syncCoordinator = syncCoordinator;
        _recipeWriter = recipeWriter;
        _autoLoader = autoLoader;
        _ricettaService = ricettaService;
        _ftpService = ftpService;
        _context = context;
        _logger = logger;
    }

    [HttpGet("realtime")]
    public async Task<ActionResult<List<PlcRealtimeDto>>> GetRealtime()
    {
        var result = await _service.GetRealtimeDataAsync();
        return Ok(result);
    }

    [HttpGet("storico/{macchinaId}")]
    public async Task<ActionResult<List<PlcStoricoDto>>> GetStorico(
        Guid macchinaId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var result = await _service.GetStoricoAsync(macchinaId, from, to);
        return Ok(result);
    }

    [HttpGet("storico")]
    public async Task<ActionResult<List<PlcStoricoDto>>> GetStoricoAll(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var result = await _service.GetAllStoricoAsync(from, to);
        return Ok(result);
    }

    [HttpGet("eventi/{macchinaId}")]
    public async Task<ActionResult<List<EventoPLCDto>>> GetEventi(
        Guid macchinaId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var result = await _service.GetEventiAsync(macchinaId, from, to);
        return Ok(result);
    }

    [HttpPost("sync/{macchinaId}")]
    public async Task<ActionResult<PlcSyncResultDto>> SyncMacchina(Guid macchinaId)
    {
        var result = await _syncCoordinator.SyncMacchinaAsync(macchinaId);
        
        var dto = new PlcSyncResultDto
        {
            MacchinaId = result.MacchinaId,
            MacchinaCodiceMacchina = result.MacchinaCodiceMacchina,
            Successo = result.Successo,
            RecordAggiornati = result.RecordAggiornati,
            MessaggioErrore = result.MessaggioErrore,
            DataOra = result.DataOra
        };
        
        if (!result.Successo)
        {
            return BadRequest(dto);
        }
        
        return Ok(dto);
    }

    [HttpPost("sync/all")]
    public async Task<ActionResult<List<PlcSyncResultDto>>> SyncAll()
    {
        var results = await _syncCoordinator.SyncTutteMacchineAsync();
        
        var dtos = results.Select(r => new PlcSyncResultDto
        {
            MacchinaId = r.MacchinaId,
            MacchinaCodiceMacchina = r.MacchinaCodiceMacchina,
            Successo = r.Successo,
            RecordAggiornati = r.RecordAggiornati,
            MessaggioErrore = r.MessaggioErrore,
            DataOra = r.DataOra
        }).ToList();
        
        return Ok(dtos);
    }

    /// <summary>
    /// Avvia il servizio PlcSync se non è già in esecuzione
    /// </summary>
    [HttpPost("service/start")]
    public async Task<ActionResult<ServiceControlResult>> StartPlcSyncService()
    {
        var result = new ServiceControlResult();

        try
        {
            // Verifica se è già in esecuzione
            var existingProcesses = Process.GetProcessesByName("MESManager.PlcSync");
            if (existingProcesses.Length > 0)
            {
                result.Success = true;
                result.Message = "Il servizio PlcSync è già in esecuzione";
                result.IsRunning = true;
                result.ProcessId = existingProcesses[0].Id;
                return Ok(result);
            }

            // Trova il percorso dell'eseguibile
            var basePath = AppContext.BaseDirectory;
            var plcSyncPath = Path.Combine(basePath, "PlcSync", "MESManager.PlcSync.exe");
            
            // Fallback: prova nella stessa cartella
            if (!System.IO.File.Exists(plcSyncPath))
            {
                plcSyncPath = Path.Combine(basePath, "..", "PlcSync", "MESManager.PlcSync.exe");
            }
            
            // Fallback 2: percorso produzione
            if (!System.IO.File.Exists(plcSyncPath))
            {
                plcSyncPath = @"C:\MESManager\PlcSync\MESManager.PlcSync.exe";
            }

            if (!System.IO.File.Exists(plcSyncPath))
            {
                result.Success = false;
                result.Message = $"Eseguibile PlcSync non trovato. Cercato in: {plcSyncPath}";
                return NotFound(result);
            }

            // Avvia il processo
            var startInfo = new ProcessStartInfo
            {
                FileName = plcSyncPath,
                WorkingDirectory = Path.GetDirectoryName(plcSyncPath),
                UseShellExecute = true,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Minimized
            };

            // Imposta variabile ambiente Production
            startInfo.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Production";

            var process = Process.Start(startInfo);
            
            if (process != null)
            {
                // Aspetta un attimo per verificare che sia partito
                await Task.Delay(2000);
                
                if (!process.HasExited)
                {
                    result.Success = true;
                    result.Message = "Servizio PlcSync avviato con successo";
                    result.IsRunning = true;
                    result.ProcessId = process.Id;
                }
                else
                {
                    result.Success = false;
                    result.Message = $"Il servizio PlcSync si è chiuso subito dopo l'avvio (exit code: {process.ExitCode})";
                }
            }
            else
            {
                result.Success = false;
                result.Message = "Impossibile avviare il processo PlcSync";
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Errore durante l'avvio del servizio: {ex.Message}";
        }

        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Ferma il servizio PlcSync
    /// </summary>
    [HttpPost("service/stop")]
    public ActionResult<ServiceControlResult> StopPlcSyncService()
    {
        var result = new ServiceControlResult();

        try
        {
            var processes = Process.GetProcessesByName("MESManager.PlcSync");
            
            if (processes.Length == 0)
            {
                result.Success = true;
                result.Message = "Il servizio PlcSync non era in esecuzione";
                result.IsRunning = false;
                return Ok(result);
            }

            foreach (var process in processes)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(5000);
                }
                catch
                {
                    // Ignora errori per singoli processi
                }
            }

            result.Success = true;
            result.Message = $"Servizio PlcSync fermato ({processes.Length} processo/i terminato/i)";
            result.IsRunning = false;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Errore durante l'arresto del servizio: {ex.Message}";
        }

        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Verifica lo stato del servizio PlcSync
    /// </summary>
    [HttpGet("service/status")]
    public ActionResult<ServiceControlResult> GetPlcSyncServiceStatus()
    {
        var result = new ServiceControlResult();

        try
        {
            var processes = Process.GetProcessesByName("MESManager.PlcSync");
            
            result.Success = true;
            result.IsRunning = processes.Length > 0;
            result.Message = result.IsRunning 
                ? $"Servizio PlcSync in esecuzione (PID: {processes[0].Id})"
                : "Servizio PlcSync non in esecuzione";
            
            if (processes.Length > 0)
            {
                result.ProcessId = processes[0].Id;
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Errore verifica stato: {ex.Message}";
        }

        return Ok(result);
    }
    
    // ===== RECIPE TRANSMISSION ENDPOINTS (v1.33.0) =====
    
    /// <summary>
    /// Carica manualmente la prossima ricetta dal Gantt su DB55 (offset 100+)
    /// </summary>
    [HttpPost("load-next-recipe-manual/{macchinaId}")]
    public async Task<ActionResult<RecipeWriteResult>> LoadNextRecipeManual(Guid macchinaId)
    {
        try
        {
            var result = await _autoLoader.LoadNextRecipeManualAsync(macchinaId, HttpContext.RequestAborted);
            
            if (result.Success)
                return Ok(result);
            
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new RecipeWriteResult 
            { 
                Success = false, 
                ErrorMessage = ex.Message 
            });
        }
    }
    
    /// <summary>
    /// Carica ricetta specifica per codice articolo su DB55 (offset 100+)
    /// </summary>
    [HttpPost("load-recipe-by-article")]
    public async Task<ActionResult<RecipeWriteResult>> LoadRecipeByArticle([FromBody] LoadRecipeRequest request)
    {
        try
        {
            var ricetta = await _ricettaService.GetRicettaByCodiceArticoloAsync(request.CodiceArticolo);
            
            if (ricetta == null)
            {
                return NotFound(new RecipeWriteResult
                {
                    Success = false,
                    ErrorMessage = $"Ricetta non trovata per articolo {request.CodiceArticolo}"
                });
            }
            
            // Ottieni SaleOrdId (ID Mago) dalla commessa per l'articolo trasmesso
            // Cerca per articolo senza filtro macchina: le commesse potrebbero non avere NumeroMacchina
            // assegnato (NULL) anche se visibili in ProgrammaMacchine
            int codicePdf = 0;
            var macchina = await _context.Macchine.FindAsync(request.MacchinaId);
            {
                var prossima = await _context.Commesse
                    .Include(c => c.Articolo)
                    .Where(c => c.Articolo != null && c.Articolo.Codice == request.CodiceArticolo)
                    .OrderByDescending(c => c.OrdineSequenza)
                    .ThenByDescending(c => c.DataInizioPrevisione)
                    .FirstOrDefaultAsync(CancellationToken.None);

                if (prossima == null)
                {
                    _logger.LogWarning("⚠️ [FTP] Nessuna commessa in DB per articolo={Art} — codice PDF non impostato", 
                        request.CodiceArticolo);
                }
                else if (int.TryParse(prossima.SaleOrdId, out var sid) && sid > 0)
                {
                    codicePdf = sid;
                    _logger.LogInformation("📄 [FTP] Commessa trovata: {Codice} SaleOrdId={SaleOrdId} Stato={Stato}", 
                        prossima.Codice, prossima.SaleOrdId, prossima.StatoProgramma);

                    // 1. Aggiorna in-memory la ricetta per scriverselo al PLC
                    ricetta.Parametri.RemoveAll(p => p.Indirizzo == 160);
                    ricetta.Parametri.Add(new Application.DTOs.ParametroRicettaArticoloDto
                    {
                        CodiceArticolo = request.CodiceArticolo,
                        DescrizioneParametro = "CodicePDF",
                        Indirizzo = 160,
                        Valore = codicePdf,
                        Tipo = "INT",
                        Area = "Ricetta"
                    });

                    // 2. Persiste il nuovo valore nel DB (upsert su ParametriRicetta offset 160)
                    //    così la ricetta è aggiornata anche per future visualizzazioni
                    await UpsertCodicePdfNelDbAsync(request.CodiceArticolo, codicePdf, HttpContext.RequestAborted);
                }
                else
                {
                    _logger.LogWarning("⚠️ [FTP] SaleOrdId non valido: '{SaleOrdId}' per commessa {Codice}",
                        prossima.SaleOrdId, prossima.Codice);
                }
            }

            var result = await _recipeWriter.WriteRecipeToDb56Async(
                request.MacchinaId,
                ricetta,
                HttpContext.RequestAborted);

            if (result.Success && codicePdf > 0)
            {
                // Invia scheda produttiva via FTP — await con CancellationToken.None
                // (HttpContext.RequestAborted verrebbe cancellato dopo Ok(result), blocando l'upload)
                await _ftpService.SendSchedaToMacchinaAsync(
                    request.CodiceArticolo, request.MacchinaId, codicePdf, CancellationToken.None);
            }

            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new RecipeWriteResult 
            { 
                Success = false, 
                ErrorMessage = ex.Message 
            });
        }
    }
    
    /// <summary>
    /// Aggiorna (o crea) il parametro CodicePDF (offset 160) nella tabella ParametriRicetta.
    /// Garantisce che il valore visualizzato nella ricetta corrisponda all'ultimo SaleOrdId trasmesso.
    /// </summary>
    private async Task UpsertCodicePdfNelDbAsync(string codiceArticolo, int codicePdf, CancellationToken ct)
    {
        try
        {
            // Trova la Ricetta dell'articolo con il suo ParametroRicetta a offset 160
            var ricettaDb = await _context.Set<Domain.Entities.Ricetta>()
                .Include(r => r.Articolo)
                .Include(r => r.Parametri)
                .Where(r => r.Articolo.Codice == codiceArticolo)
                .FirstOrDefaultAsync(ct);

            if (ricettaDb == null)
            {
                _logger.LogWarning("⚠️ [DB-UPSERT] Ricetta non trovata per articolo={Art}", codiceArticolo);
                return;
            }

            var param = ricettaDb.Parametri.FirstOrDefault(p => p.Indirizzo == 160);
            if (param != null)
            {
                // Aggiorna il valore esistente
                param.Valore = codicePdf.ToString();
                _context.Set<Domain.Entities.ParametroRicetta>().Update(param);
            }
            else
            {
                // Crea il parametro se non esiste
                var nuovoParam = new Domain.Entities.ParametroRicetta
                {
                    Id = Guid.NewGuid(),
                    RicettaId = ricettaDb.Id,
                    NomeParametro = "CodicePDF",
                    Valore = codicePdf.ToString(),
                    Indirizzo = 160,
                    Tipo = "INT",
                    Area = "Ricetta",
                    UnitaMisura = string.Empty
                };
                await _context.Set<Domain.Entities.ParametroRicetta>().AddAsync(nuovoParam, ct);
            }

            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("💾 [DB-UPSERT] CodicePDF={Valore} salvato nel DB per articolo={Art}", codicePdf, codiceArticolo);
        }
        catch (Exception ex)
        {
            // Non-blocking: l'upsert DB è best-effort, non ferma la trasmissione
            _logger.LogError(ex, "❌ [DB-UPSERT] Errore salvataggio CodicePDF per articolo={Art}", codiceArticolo);
        }
    }

    /// <summary>
    /// Legge contenuto DB55 (stato/lettura PLC)
    /// </summary>
    [HttpGet("db55/{macchinaId}")]
    public async Task<ActionResult<List<PlcDbEntryDto>>> ReadDb55(Guid macchinaId)
    {
        try
        {
            var entries = await _recipeWriter.ReadDb55Async(macchinaId, HttpContext.RequestAborted);
            return Ok(entries);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
    
    /// <summary>
    /// Legge contenuto DB56 (tempi/valori di esecuzione PLC)
    /// </summary>
    [HttpGet("db56/{macchinaId}")]
    public async Task<ActionResult<List<PlcDbEntryDto>>> ReadDb56(Guid macchinaId)
    {
        try
        {
            var entries = await _recipeWriter.ReadDb56Async(macchinaId, HttpContext.RequestAborted);
            return Ok(entries);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
    
    /// <summary>
    /// Sincronizza contenuto DB56 (esecuzione) → DB55 (area scrivibile offset 100+)
    /// </summary>
    [HttpPost("copy-db56-to-db55/{macchinaId}")]
    [HttpPost("copy-db55-to-db56/{macchinaId}")]
    public async Task<ActionResult<RecipeWriteResult>> CopyDb55ToDb56(Guid macchinaId)
    {
        try
        {
            var result = await _recipeWriter.CopyDb55ToDb56Async(macchinaId, HttpContext.RequestAborted);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new RecipeWriteResult 
            {
                Success = false, 
                ErrorMessage = ex.Message 
            });
        }
    }
    
    /// <summary>
    /// Scansiona tutti i DB disponibili su un PLC (DB1..DB100)
    /// </summary>
    [HttpGet("scan-db/{macchinaId}")]
    public async Task<ActionResult<List<PlcDbScanResultDto>>> ScanAvailableDbs(Guid macchinaId, [FromQuery] int maxDb = 100)
    {
        try
        {
            var results = await _recipeWriter.ScanAvailableDbsAsync(macchinaId, maxDb, HttpContext.RequestAborted);
            return Ok(results);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
    
    /// <summary>
    /// Salva parametri runtime DB56 (offset 100-196) come ricetta master per un articolo.
    /// FONTE DATI: DB56 offset 100-196 (parametri esecuzione macchina scritti dal PLC)
    /// </summary>
    [HttpPost("save-recipe-from-plc")]
    public async Task<ActionResult<SaveRecipeFromPlcResult>> SaveRecipeFromPlc([FromBody] SaveRecipeFromPlcRequest request)
    {
        var result = new SaveRecipeFromPlcResult();
        
        try
        {
            // 1. Trova articolo dal codice articolo (catalogo Anime)
            var articolo = await _context.Articoli
                .FirstOrDefaultAsync(a => a.Codice == request.CodiceArticolo, HttpContext.RequestAborted);
            
            if (articolo == null)
            {
                result.Success = false;
                result.ErrorMessage = $"Articolo con codice '{request.CodiceArticolo}' non trovato";
                return NotFound(result);
            }
            
            // 2. Leggi DB56 (parametri esecuzione runtime) se entries non fornite
            var entries = request.Entries?.ToList();
            if (entries == null || entries.Count == 0)
            {
                entries = await _recipeWriter.ReadDb56Async(request.MacchinaId, HttpContext.RequestAborted);
            }
            
            // 3. Filtra SOLO parametri DB56 offset 100-196 (range esecuzione macchina)
            var parametriDaSalvare = entries
                .Where(e => 
                    e.Offset >= PlcConstants.OFFSET_DB56_EXECUTION_START && 
                    e.Offset <= PlcConstants.OFFSET_DB56_EXECUTION_END)
                .ToList();
            
            if (parametriDaSalvare.Count == 0)
            {
                result.Success = false;
                result.ErrorMessage = $"Nessun parametro trovato in DB56 range {PlcConstants.OFFSET_DB56_EXECUTION_START}-{PlcConstants.OFFSET_DB56_EXECUTION_END}";
                return BadRequest(result);
            }
            
            // 4. Trova o crea Ricetta per questo articolo
            var ricetta = await _context.Ricette
                .Include(r => r.Parametri)
                .FirstOrDefaultAsync(r => r.ArticoloId == articolo.Id, HttpContext.RequestAborted);
            
            if (ricetta == null)
            {
                // Crea nuova ricetta
                ricetta = new Ricetta
                {
                    Id = Guid.NewGuid(),
                    ArticoloId = articolo.Id
                };
                _context.Ricette.Add(ricetta);
            }
            else
            {
                // Cancella vecchi parametri
                _context.ParametriRicetta.RemoveRange(ricetta.Parametri);
            }
            
            // 5. Crea nuovi ParametroRicetta da parametri DB56 (100-196)
            // Ordina per Offset e assegna CodiceParametro sequenziale
            var parametriOrdinati = parametriDaSalvare.OrderBy(e => e.Offset).ToList();
            int codice = 1;
            
            foreach (var entry in parametriOrdinati)
            {
                var parametro = new ParametroRicetta
                {
                    Id = Guid.NewGuid(),
                    RicettaId = ricetta.Id,
                    NomeParametro = entry.Nome,
                    Valore = entry.Valore,
                    UnitaMisura = entry.UnitaMisura ?? string.Empty,
                    CodiceParametro = codice++,   // Sequenziale: 1, 2, 3...
                    Indirizzo = entry.Offset,      // Offset PLC: 0, 2, 4, 6...
                    Area = "DB",                   // Area DB standard
                    Tipo = entry.Tipo              // Tipo dato: INT, REAL, etc.
                };
                
                _context.ParametriRicetta.Add(parametro);
            }
            
            // 6. Salva tutto
            await _context.SaveChangesAsync(HttpContext.RequestAborted);
            
            result.Success = true;
            result.RicettaId = ricetta.Id;
            result.NumeroParametriSalvati = parametriDaSalvare.Count;
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Errore durante il salvataggio: {ex.Message}";
            return StatusCode(500, result);
        }
    }

    /// <summary>
    /// Restituisce i segmenti temporali a stato costante di ogni macchina nel periodo indicato.
    /// Il campo Colore di ogni segmento è calcolato via MesDesignTokens.PlcStatoColore().
    /// </summary>
    [HttpGet("gantt-storico")]
    public async Task<ActionResult<List<PlcGanttSegmentoDto>>> GetGanttStorico(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? macchinaId)
    {
        var fromDate = from ?? DateTime.Today.AddDays(-1);
        var toDate   = to   ?? DateTime.Now;
        var segmenti = await _service.GetGanttStoricoAsync(fromDate, toDate, macchinaId);

        // Arricchisce Colore nel layer Web (MesDesignTokens — fonte di verità colori stato PLC)
        foreach (var s in segmenti)
            s.Colore = MesDesignTokens.PlcStatoColore(s.StatoMacchina);

        return Ok(segmenti);
    }

    /// <summary>
    /// Restituisce i KPI aggregati (% AUTOMATICO, ALLARME, EMERGENZA, MANUALE, SETUP) per macchina.
    /// </summary>
    [HttpGet("kpi-storico")]
    public async Task<ActionResult<List<PlcKpiStoricoDto>>> GetKpiStorico(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? macchinaId)
    {
        var fromDate = from ?? DateTime.Today.AddDays(-1);
        var toDate   = to   ?? DateTime.Now;
        var result = await _service.GetKpiStoricoAsync(fromDate, toDate, macchinaId);
        return Ok(result);
    }
}

public class ServiceControlResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public bool IsRunning { get; set; }
    public int? ProcessId { get; set; }
}
