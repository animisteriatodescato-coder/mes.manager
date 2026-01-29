using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MESManager.Application.Interfaces;
using MESManager.Application.DTOs;
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

    public PlcController(IPlcAppService service, IPlcSyncCoordinator syncCoordinator)
    {
        _service = service;
        _syncCoordinator = syncCoordinator;
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
}

public class ServiceControlResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public bool IsRunning { get; set; }
    public int? ProcessId { get; set; }
}
