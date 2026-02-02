using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;
using MESManager.Domain.Enums;

namespace MESManager.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize] // Da riabilitare in produzione
public class QuotesController : ControllerBase
{
    private readonly IQuoteService _quoteService;
    private readonly IQuotePricingEngine _pricingEngine;
    private readonly IQuoteAttachmentService _attachmentService;
    private readonly IQuotePdfGenerator _pdfGenerator;
    private readonly ILogger<QuotesController> _logger;

    public QuotesController(
        IQuoteService quoteService,
        IQuotePricingEngine pricingEngine,
        IQuoteAttachmentService attachmentService,
        IQuotePdfGenerator pdfGenerator,
        ILogger<QuotesController> logger)
    {
        _quoteService = quoteService;
        _pricingEngine = pricingEngine;
        _attachmentService = attachmentService;
        _pdfGenerator = pdfGenerator;
        _logger = logger;
    }

    /// <summary>
    /// Lista preventivi con filtri e paginazione
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<QuoteListResult>> GetList([FromQuery] QuoteListFilter filter)
    {
        var result = await _quoteService.GetListAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Dettaglio singolo preventivo
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<QuoteDto>> GetById(Guid id)
    {
        var quote = await _quoteService.GetByIdAsync(id);
        if (quote == null)
        {
            return NotFound();
        }
        return Ok(quote);
    }

    /// <summary>
    /// Crea nuovo preventivo
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<QuoteDto>> Create([FromBody] QuoteSaveDto dto)
    {
        try
        {
            var userId = User.Identity?.Name;
            var quote = await _quoteService.CreateAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { id = quote.Id }, quote);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore creazione preventivo");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Aggiorna preventivo esistente
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<QuoteDto>> Update(Guid id, [FromBody] QuoteSaveDto dto)
    {
        try
        {
            dto.Id = id;
            var userId = User.Identity?.Name;
            var quote = await _quoteService.UpdateAsync(dto, userId);
            return Ok(quote);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore aggiornamento preventivo {Id}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Elimina preventivo
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var success = await _quoteService.DeleteAsync(id);
        if (!success)
        {
            return NotFound();
        }
        return NoContent();
    }

    /// <summary>
    /// Duplica preventivo esistente
    /// </summary>
    [HttpPost("{id:guid}/duplicate")]
    public async Task<ActionResult<QuoteDto>> Duplicate(Guid id)
    {
        try
        {
            var userId = User.Identity?.Name;
            var quote = await _quoteService.DuplicateAsync(id, userId);
            return CreatedAtAction(nameof(GetById), new { id = quote.Id }, quote);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore duplicazione preventivo {Id}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Cambia stato preventivo
    /// </summary>
    [HttpPost("{id:guid}/status")]
    public async Task<ActionResult<QuoteDto>> ChangeStatus(Guid id, [FromBody] QuoteStatusChangeRequest request)
    {
        try
        {
            var userId = User.Identity?.Name;
            var dto = new QuoteStatusChangeDto
            {
                QuoteId = id,
                NewStatus = request.NewStatus,
                Notes = request.Notes
            };
            var quote = await _quoteService.ChangeStatusAsync(dto, userId);
            return Ok(quote);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore cambio stato preventivo {Id}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Calcola totali preventivo (senza salvare)
    /// </summary>
    [HttpPost("calculate")]
    public ActionResult<QuoteCalculationResult> Calculate([FromBody] List<QuoteRowCalculationInput> rows)
    {
        var result = _pricingEngine.CalculateQuote(rows);
        return Ok(result);
    }

    /// <summary>
    /// Genera prossimo numero preventivo
    /// </summary>
    [HttpGet("next-number")]
    public async Task<ActionResult<string>> GetNextNumber()
    {
        var number = await _quoteService.GenerateNextNumberAsync();
        return Ok(new { number });
    }

    // ==================== ALLEGATI ====================

    /// <summary>
    /// Lista allegati di un preventivo
    /// </summary>
    [HttpGet("{id:guid}/attachments")]
    public async Task<ActionResult<List<QuoteAttachmentDto>>> GetAttachments(Guid id)
    {
        var attachments = await _attachmentService.GetByQuoteIdAsync(id);
        return Ok(attachments);
    }

    /// <summary>
    /// Upload allegato
    /// </summary>
    [HttpPost("{id:guid}/attachments")]
    [RequestSizeLimit(26_214_400)] // 25 MB
    public async Task<ActionResult<QuoteAttachmentDto>> UploadAttachment(Guid id, IFormFile file, [FromForm] string? description)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "File non valido" });
        }

        var userId = User.Identity?.Name;
        
        using var stream = file.OpenReadStream();
        var result = await _attachmentService.UploadAsync(
            id,
            stream,
            file.FileName,
            file.ContentType,
            description,
            userId);

        if (!result.Success)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(result.Attachment);
    }

    /// <summary>
    /// Download allegato
    /// </summary>
    [HttpGet("attachments/{attachmentId:guid}")]
    public async Task<ActionResult> DownloadAttachment(Guid attachmentId)
    {
        var (stream, contentType, fileName) = await _attachmentService.DownloadAsync(attachmentId);
        
        if (stream == null)
        {
            return NotFound();
        }

        return File(stream, contentType ?? "application/octet-stream", fileName);
    }

    /// <summary>
    /// Elimina allegato
    /// </summary>
    [HttpDelete("attachments/{attachmentId:guid}")]
    public async Task<ActionResult> DeleteAttachment(Guid attachmentId)
    {
        var success = await _attachmentService.DeleteAsync(attachmentId);
        if (!success)
        {
            return NotFound();
        }
        return NoContent();
    }

    // ==================== PDF ====================

    /// <summary>
    /// Genera e scarica PDF del preventivo
    /// </summary>
    [HttpGet("{id:guid}/pdf")]
    public async Task<ActionResult> GeneratePdf(Guid id)
    {
        var quote = await _quoteService.GetByIdAsync(id);
        if (quote == null)
        {
            return NotFound();
        }

        try
        {
            var pdfStream = await _pdfGenerator.GenerateAsync(quote);
            return File(pdfStream, "application/pdf", $"Preventivo_{quote.Number}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore generazione PDF preventivo {Id}", id);
            return BadRequest(new { error = "Errore generazione PDF: " + ex.Message });
        }
    }
}

/// <summary>
/// Request per cambio stato
/// </summary>
public class QuoteStatusChangeRequest
{
    public QuoteStatus NewStatus { get; set; }
    public string? Notes { get; set; }
}
