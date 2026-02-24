using Microsoft.AspNetCore.Mvc;
using MESManager.Application.Interfaces;
using MESManager.Domain.Enums;
using MESManager.Web.Constants;

namespace MESManager.Web.Controllers;

/// <summary>
/// Endpoint per l'auto-cattura di errori dal browser (JS errors, fetch failures, console.error).
/// Riceve segnalazioni dal JavaScript error-interceptor e le salva come TechnicalIssue.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class IssueLogController : ControllerBase
{
    private readonly ITechnicalIssueService _issueService;
    private readonly ILogger<IssueLogController> _logger;
    private readonly IWebHostEnvironment _env;

    public IssueLogController(
        ITechnicalIssueService issueService,
        ILogger<IssueLogController> logger,
        IWebHostEnvironment env)
    {
        _issueService = issueService;
        _logger = logger;
        _env = env;
    }

    /// <summary>
    /// Riceve un errore catturato dal browser e lo salva come Issue automatico.
    /// In caso di duplicato nelle ultime 2 ore, non crea un nuovo record.
    /// </summary>
    [HttpPost("error")]
    public async Task<IActionResult> ReportError([FromBody] BrowserErrorDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Message))
            return BadRequest("Messaggio errore mancante");

        // Limita la lunghezza per sicurezza
        var message = dto.Message.Length > 300 ? dto.Message[..300] : dto.Message;
        var stack = dto.Stack?.Length > 2000 ? dto.Stack[..2000] : dto.Stack;
        var sourceUrl = dto.SourceUrl?.Length > 500 ? dto.SourceUrl[..500] : dto.SourceUrl;

        var (area, severity) = ClassifyError(dto);
        var environment = _env.IsProduction() ? IssueEnvironment.Prod : IssueEnvironment.Dev;

        var title = BuildTitle(dto.ErrorType, message, dto.StatusCode);

        var description = BuildDescription(dto);

        var created = await _issueService.CreateAutoCaptureAsync(
            title: title,
            description: description,
            logs: stack,
            sourceUrl: sourceUrl,
            area: area,
            severity: severity,
            environment: environment,
            affectedVersion: AppVersion.Current);

        if (created == null)
        {
            // Duplicato silenzioso: non creare rumore
            return Ok(new { deduplicated = true });
        }

        _logger.LogWarning("[AUTO-CAPTURE] Nuovo issue #{Id} — {Title}", created.Id, created.Title);
        return Ok(new { id = created.Id, deduplicated = false });
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static string BuildTitle(string? errorType, string message, int? statusCode)
    {
        var prefix = errorType switch
        {
            "fetch_error" when statusCode.HasValue => $"[HTTP {statusCode}]",
            "fetch_error"                          => "[HTTP Error]",
            "js_error"                             => "[JS Error]",
            "promise_rejection"                    => "[Promise Rejection]",
            "console_error"                        => "[Console Error]",
            _                                      => "[Error]"
        };

        // Tronca a 200 char per il title
        var body = message.Length > 190 ? message[..190] : message;
        return $"{prefix} {body}";
    }

    private static string BuildDescription(BrowserErrorDto dto)
    {
        var lines = new List<string>
        {
            $"**Tipo**: {dto.ErrorType ?? "unknown"}",
            $"**Messaggio**: {dto.Message}",
        };

        if (dto.StatusCode.HasValue)
            lines.Add($"**HTTP Status**: {dto.StatusCode}");

        if (!string.IsNullOrWhiteSpace(dto.SourceUrl))
            lines.Add($"**URL**: {dto.SourceUrl}");

        lines.Add($"**Catturato il**: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
        lines.Add("");
        lines.Add("*Issue catturato automaticamente dall'error interceptor del browser.*");

        return string.Join("\n", lines);
    }

    private static (IssueArea area, IssueSeverity severity) ClassifyError(BrowserErrorDto dto)
    {
        // Classificazione area
        IssueArea area;

        if (dto.ErrorType == "fetch_error")
        {
            // Errori HTTP → dipende dall'URL
            var url = dto.SourceUrl ?? dto.Message ?? "";
            if (url.Contains("/api/Plc") || url.Contains("plc", StringComparison.OrdinalIgnoreCase))
                area = IssueArea.PLC;
            else if (url.Contains("/api/Allegati") || url.Contains("allegat", StringComparison.OrdinalIgnoreCase))
                area = IssueArea.UX;
            else
                area = IssueArea.UX;
        }
        else
        {
            area = IssueArea.UX;
        }

        // Classificazione severity
        IssueSeverity severity = dto.ErrorType switch
        {
            "fetch_error" when dto.StatusCode >= 500 => IssueSeverity.High,
            "fetch_error" when dto.StatusCode >= 400 => IssueSeverity.Low,
            "js_error"                               => IssueSeverity.Medium,
            "promise_rejection"                      => IssueSeverity.Medium,
            _                                        => IssueSeverity.Low
        };

        return (area, severity);
    }
}

/// <summary>
/// DTO per gli errori segnalati dal browser
/// </summary>
public record BrowserErrorDto(
    string? ErrorType,
    string Message,
    string? Stack,
    string? SourceUrl,
    int? StatusCode
);
