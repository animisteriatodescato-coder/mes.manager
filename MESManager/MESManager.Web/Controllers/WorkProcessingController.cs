using Microsoft.AspNetCore.Mvc;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;

namespace MESManager.Web.Controllers;

/// <summary>
/// API per gestione tipi lavorazione anime e pricing automatico
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class WorkProcessingController : ControllerBase
{
    private readonly IWorkProcessingService _service;
    private readonly ILogger<WorkProcessingController> _logger;

    public WorkProcessingController(
        IWorkProcessingService service,
        ILogger<WorkProcessingController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════
    // TIPI LAVORAZIONE - CRUD
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// GET api/workprocessing/types - Lista tipi lavorazione
    /// </summary>
    [HttpGet("types")]
    [ProducesResponseType(typeof(List<WorkProcessingTypeDto>), 200)]
    public async Task<IActionResult> GetAllTypes([FromQuery] bool onlyActive = true)
    {
        try
        {
            var types = await _service.GetAllTypesAsync(onlyActive);
            return Ok(types);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WorkProcessing] Errore recupero tipi lavorazione");
            return StatusCode(500, new { error = "Errore interno server" });
        }
    }

    /// <summary>
    /// GET api/workprocessing/types/{id} - Dettaglio tipo lavorazione
    /// </summary>
    [HttpGet("types/{id:guid}")]
    [ProducesResponseType(typeof(WorkProcessingTypeDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTypeById(Guid id)
    {
        try
        {
            var type = await _service.GetTypeByIdAsync(id);
            
            if (type == null)
                return NotFound(new { error = $"Tipo lavorazione {id} non trovato" });

            return Ok(type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WorkProcessing] Errore lettura tipo {TypeId}", id);
            return StatusCode(500, new { error = "Errore interno server" });
        }
    }

    /// <summary>
    /// GET api/workprocessing/types/code/{codice} - Dettaglio per codice
    /// </summary>
    [HttpGet("types/code/{codice}")]
    [ProducesResponseType(typeof(WorkProcessingTypeDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTypeByCode(string codice)
    {
        try
        {
            var type = await _service.GetTypeByCodeAsync(codice);
            
            if (type == null)
                return NotFound(new { error = $"Tipo lavorazione '{codice}' non trovato" });

            return Ok(type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WorkProcessing] Errore lettura tipo codice {Code}", codice);
            return StatusCode(500, new { error = "Errore interno server" });
        }
    }

    /// <summary>
    /// POST api/workprocessing/types - Crea nuovo tipo lavorazione
    /// </summary>
    [HttpPost("types")]
    [ProducesResponseType(typeof(WorkProcessingTypeDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateType([FromBody] WorkProcessingTypeSaveDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.CreateTypeAsync(dto, User?.Identity?.Name);
            
            _logger.LogInformation("[WorkProcessing] Tipo lavorazione creato: {Code} - {Name}", result.Codice, result.Nome);
            
            return CreatedAtAction(nameof(GetTypeById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WorkProcessing] Errore creazione tipo lavorazione");
            return StatusCode(500, new { error = "Errore interno server" });
        }
    }

    /// <summary>
    /// PUT api/workprocessing/types/{id} - Aggiorna tipo lavorazione
    /// </summary>
    [HttpPut("types/{id:guid}")]
    [ProducesResponseType(typeof(WorkProcessingTypeDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateType(Guid id, [FromBody] WorkProcessingTypeSaveDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto.Id.HasValue && dto.Id.Value != id)
                return BadRequest(new { error = "ID in URL e body non corrispondono" });

            dto.Id = id;

            var result = await _service.UpdateTypeAsync(dto, User?.Identity?.Name);
            
            _logger.LogInformation(" [WorkProcessing] Tipo lavorazione aggiornato: {Code} - {Name}", result.Codice, result.Nome);
            
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WorkProcessing] Errore aggiornamento tipo {TypeId}", id);
            return StatusCode(500, new { error = "Errore interno server" });
        }
    }

    /// <summary>
    /// DELETE api/workprocessing/types/{id} - Archivia tipo lavorazione (soft delete)
    /// </summary>
    [HttpDelete("types/{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ArchiveType(Guid id)
    {
        try
        {
            var success = await _service.ArchiveTypeAsync(id, User?.Identity?.Name);
            
            if (!success)
                return NotFound(new { error = $"Tipo lavorazione {id} non trovato" });

            _logger.LogInformation("[WorkProcessing] Tipo lavorazione archiviato: {TypeId}", id);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WorkProcessing] Errore archiviazione tipo {TypeId}", id);
            return StatusCode(500, new { error = "Errore interno server" });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // PARAMETRI ECONOMICI - GESTIONE
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// GET api/workprocessing/types/{id}/parameters - Parametri correnti tipo lavorazione
    /// </summary>
    [HttpGet("types/{id:guid}/parameters")]
    [ProducesResponseType(typeof(WorkProcessingParameterDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetCurrentParameters(Guid id)
    {
        try
        {
            var parameters = await _service.GetCurrentParametersAsync(id);
            
            if (parameters == null)
                return NotFound(new { error = $"Parametri non configurati per tipo lavorazione {id}" });

            return Ok(parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WorkProcessing] Errore lettura parametri tipo {TypeId}", id);
            return StatusCode(500, new { error = "Errore interno server" });
        }
    }

    /// <summary>
    /// GET api/workprocessing/types/{id}/parameters/history - Storico parametri
    /// </summary>
    [HttpGet("types/{id:guid}/parameters/history")]
    [ProducesResponseType(typeof(List<WorkProcessingParameterDto>), 200)]
    public async Task<IActionResult> GetParametersHistory(Guid id)
    {
        try
        {
            var history = await _service.GetParametersHistoryAsync(id);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WorkProcessing] Errore lettura storico parametri tipo {TypeId}", id);
            return StatusCode(500, new { error = "Errore interno server" });
        }
    }

    /// <summary>
    /// POST api/workprocessing/parameters - Salva/Aggiorna parametri economici
    /// </summary>
    [HttpPost("parameters")]
    [ProducesResponseType(typeof(WorkProcessingParameterDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SaveParameters([FromBody] WorkProcessingParameterSaveDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.SaveParametersAsync(dto, User?.Identity?.Name);
            
            _logger.LogInformation(
                "[WorkProcessing] Parametri salvati per tipo {TypeId} - Versione: {IsNew}",
                dto.WorkProcessingTypeId,
                dto.ImposeNewVersion ? "NUOVA" : "AGGIORNAMENTO"
            );
            
            return CreatedAtAction(
                nameof(GetCurrentParameters),
                new { id = dto.WorkProcessingTypeId },
                result
            );
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WorkProcessing] Errore salvataggio parametri");
            return StatusCode(500, new { error = "Errore interno server" });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // PRICING ENGINE - CALCOLO AUTOMATICO
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// POST api/workprocessing/calculate - Calcola prezzo lavorazione anime
    /// </summary>
    /// <remarks>
    /// Calcola automaticamente il prezzo di vendita basato su parametri tecnici articolo.
    /// 
    /// Output include breakdown dettagliato:
    /// - Costo sabbiatura (€/Ora / Spari)
    /// - Costo sabbia (kg * figure)    /// - Costo attrezzatura
    /// - Costi fuori macchina (vernice, verniciatura, incollaggio, imballo)
    /// - Prezzo finale con margine applicato
    /// </remarks>
    [HttpPost("calculate")]
    [ProducesResponseType(typeof(WorkProcessingCalculationResult), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CalculatePrice([FromBody] WorkProcessingCalculationInput input)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.CalculatePriceAsync(input);
            
            if (!result.Success)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            _logger.LogInformation(
                "[WorkProcessing] Calcolo prezzo: Tipo {TypeId}, Prezzo {Price:F4}€",
                input.WorkProcessingTypeId,
                result.PrezzoVenditaPezzo
            );

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WorkProcessing] Errore calcolo prezzo");
            return StatusCode(500, new { error = "Errore interno server" });
        }
    }

    /// <summary>
    /// POST api/workprocessing/validate - Valida input calcolo prezzo
    /// </summary>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ValidateCalculationInput([FromBody] WorkProcessingCalculationInput input)
    {
        try
        {
            var (isValid, errorMessage) = await _service.ValidateCalculationInputAsync(input);
            
            if (!isValid)
            {
                return BadRequest(new { isValid = false, error = errorMessage });
            }

            return Ok(new { isValid = true, message = "Input valido" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WorkProcessing] Errore validazione input");
            return StatusCode(500, new { error = "Errore interno server" });
        }
    }
}
