using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;

namespace MESManager.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize] // Da riabilitare in produzione
public class PriceListsController : ControllerBase
{
    private readonly IPriceListService _priceListService;
    private readonly IExcelImportService _excelImportService;
    private readonly ILogger<PriceListsController> _logger;

    public PriceListsController(
        IPriceListService priceListService,
        IExcelImportService excelImportService,
        ILogger<PriceListsController> logger)
    {
        _priceListService = priceListService;
        _excelImportService = excelImportService;
        _logger = logger;
    }

    /// <summary>
    /// Lista tutti i listini
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<PriceListDto>>> GetAll([FromQuery] bool includeArchived = false)
    {
        var result = await _priceListService.GetAllAsync(includeArchived);
        return Ok(result);
    }

    /// <summary>
    /// Lista listini per dropdown selezione
    /// </summary>
    [HttpGet("select")]
    public async Task<ActionResult<List<PriceListSelectDto>>> GetForSelection()
    {
        var result = await _priceListService.GetForSelectionAsync();
        return Ok(result);
    }

    /// <summary>
    /// Ottiene il listino default
    /// </summary>
    [HttpGet("default")]
    public async Task<ActionResult<PriceListSelectDto?>> GetDefault()
    {
        var result = await _priceListService.GetDefaultAsync();
        return Ok(result);
    }

    /// <summary>
    /// Dettaglio singolo listino
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PriceListDto>> GetById(Guid id)
    {
        var priceList = await _priceListService.GetByIdAsync(id);
        if (priceList == null)
        {
            return NotFound();
        }
        return Ok(priceList);
    }

    /// <summary>
    /// Ottiene items di un listino
    /// </summary>
    [HttpGet("{id:guid}/items")]
    public async Task<ActionResult<List<PriceListItemDto>>> GetItems(Guid id)
    {
        var items = await _priceListService.GetItemsAsync(id);
        return Ok(items);
    }

    /// <summary>
    /// Cerca items nel listino (per autocomplete)
    /// </summary>
    [HttpGet("{id:guid}/items/search")]
    public async Task<ActionResult<List<PriceListItemSelectDto>>> SearchItems(
        Guid id, 
        [FromQuery] string? q, 
        [FromQuery] int max = 20)
    {
        var items = await _priceListService.SearchItemsAsync(id, q ?? "", max);
        return Ok(items);
    }

    /// <summary>
    /// Dettaglio singolo item
    /// </summary>
    [HttpGet("items/{itemId:guid}")]
    public async Task<ActionResult<PriceListItemDto>> GetItemById(Guid itemId)
    {
        var item = await _priceListService.GetItemByIdAsync(itemId);
        if (item == null)
        {
            return NotFound();
        }
        return Ok(item);
    }

    /// <summary>
    /// Crea nuovo listino vuoto
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PriceListDto>> Create([FromBody] PriceListCreateDto dto)
    {
        try
        {
            var userId = User.Identity?.Name;
            var priceList = await _priceListService.CreateAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { id = priceList.Id }, priceList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore creazione listino");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Imposta listino come default
    /// </summary>
    [HttpPost("{id:guid}/set-default")]
    public async Task<ActionResult> SetDefault(Guid id)
    {
        var success = await _priceListService.SetDefaultAsync(id);
        if (!success)
        {
            return NotFound(new { error = "Listino non trovato o archiviato" });
        }
        return Ok(new { message = "Listino impostato come default" });
    }

    /// <summary>
    /// Archivia listino
    /// </summary>
    [HttpPost("{id:guid}/archive")]
    public async Task<ActionResult> Archive(Guid id)
    {
        var success = await _priceListService.ArchiveAsync(id);
        if (!success)
        {
            return NotFound();
        }
        return Ok(new { message = "Listino archiviato" });
    }

    /// <summary>
    /// Import listino da Excel (upload)
    /// </summary>
    [HttpPost("import")]
    [RequestSizeLimit(52_428_800)] // 50 MB
    public async Task<ActionResult<ExcelImportResultDto>> ImportFromExcel(
        IFormFile file,
        [FromForm] string? name,
        [FromForm] string? version)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "File non valido" });
        }

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) &&
            !file.FileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Solo file Excel (.xlsx, .xls) sono supportati" });
        }

        var userId = User.Identity?.Name;
        
        using var stream = file.OpenReadStream();
        var result = await _excelImportService.ImportPriceListAsync(
            stream,
            file.FileName,
            name,
            version,
            userId);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Valida file Excel senza importare (preview)
    /// </summary>
    [HttpPost("validate")]
    [RequestSizeLimit(52_428_800)] // 50 MB
    public async Task<ActionResult<ExcelValidationResultDto>> ValidateExcel(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "File non valido" });
        }

        using var stream = file.OpenReadStream();
        var result = await _excelImportService.ValidateFileAsync(stream, file.FileName);
        
        return Ok(result);
    }

    /// <summary>
    /// Import listino da path su server (per import schedulati o batch)
    /// </summary>
    [HttpPost("import-from-path")]
    public async Task<ActionResult<ExcelImportResultDto>> ImportFromPath([FromBody] ImportFromPathRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FilePath))
        {
            return BadRequest(new { error = "Path file non specificato" });
        }

        var userId = User.Identity?.Name;
        
        var result = await _excelImportService.ImportPriceListFromFileAsync(
            request.FilePath,
            request.Name,
            request.Version,
            userId);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}

/// <summary>
/// Request per import da path server
/// </summary>
public class ImportFromPathRequest
{
    public string FilePath { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Version { get; set; }
}
