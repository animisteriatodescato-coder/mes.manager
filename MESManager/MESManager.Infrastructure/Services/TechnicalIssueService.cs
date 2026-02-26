using System.Text;
using Microsoft.EntityFrameworkCore;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;
using MESManager.Domain.Enums;
using MESManager.Infrastructure.Data;

namespace MESManager.Infrastructure.Services;

/// <summary>
/// Servizio per la gestione dei TechnicalIssue.
/// Usa IDbContextFactory per operazioni thread-safe in Blazor Server.
/// </summary>
public class TechnicalIssueService : ITechnicalIssueService
{
    private readonly IDbContextFactory<MesManagerDbContext> _contextFactory;

    public TechnicalIssueService(IDbContextFactory<MesManagerDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<TechnicalIssue>> GetAllAsync(
        IssueStatus? status = null,
        IssueArea? area = null,
        IssueSeverity? severity = null,
        IssueEnvironment? environment = null,
        string? searchTitle = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var query = context.TechnicalIssues.AsQueryable();

        if (status.HasValue)
            query = query.Where(i => i.Status == status.Value);

        if (area.HasValue)
            query = query.Where(i => i.Area == area.Value);

        if (severity.HasValue)
            query = query.Where(i => i.Severity == severity.Value);

        if (environment.HasValue)
            query = query.Where(i => i.Environment == environment.Value);

        if (!string.IsNullOrWhiteSpace(searchTitle))
            query = query.Where(i => i.Title.Contains(searchTitle));

        return await query
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task<TechnicalIssue?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.TechnicalIssues.FindAsync(id);
    }

    public async Task<TechnicalIssue> CreateAsync(TechnicalIssue issue)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        issue.CreatedAt = DateTime.Now;
        issue.UpdatedAt = null;
        
        context.TechnicalIssues.Add(issue);
        await context.SaveChangesAsync();
        
        return issue;
    }

    public async Task<TechnicalIssue?> UpdateAsync(TechnicalIssue issueUpdate)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var issue = await context.TechnicalIssues.FindAsync(issueUpdate.Id);
        if (issue == null) return null;

        issue.Environment = issueUpdate.Environment;
        issue.Area = issueUpdate.Area;
        issue.Severity = issueUpdate.Severity;
        issue.Status = issueUpdate.Status;
        issue.Title = issueUpdate.Title;
        issue.Description = issueUpdate.Description;
        issue.ReproSteps = issueUpdate.ReproSteps;
        issue.Logs = issueUpdate.Logs;
        issue.AffectedVersion = issueUpdate.AffectedVersion;
        issue.Solution = issueUpdate.Solution;
        issue.RulesLearned = issueUpdate.RulesLearned;
        issue.DocsUpdated = issueUpdate.DocsUpdated;
        issue.DocsReferencePath = issueUpdate.DocsReferencePath;
        issue.UpdatedAt = DateTime.Now;

        await context.SaveChangesAsync();
        return issue;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var issue = await context.TechnicalIssues.FindAsync(id);
        if (issue == null) return false;

        context.TechnicalIssues.Remove(issue);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<TechnicalIssue?> MarkAsResolvedAsync(int id, string? solution = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var issue = await context.TechnicalIssues.FindAsync(id);
        if (issue == null) return null;

        issue.Status = IssueStatus.Resolved;
        if (!string.IsNullOrWhiteSpace(solution))
            issue.Solution = solution;
        issue.UpdatedAt = DateTime.Now;

        await context.SaveChangesAsync();
        return issue;
    }

    public async Task<TechnicalIssue?> MarkAsDocumentedAsync(int id, string? docsReferencePath = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var issue = await context.TechnicalIssues.FindAsync(id);
        if (issue == null) return null;

        issue.Status = IssueStatus.Documented;
        issue.DocsUpdated = true;
        if (!string.IsNullOrWhiteSpace(docsReferencePath))
            issue.DocsReferencePath = docsReferencePath;
        issue.UpdatedAt = DateTime.Now;

        await context.SaveChangesAsync();
        return issue;
    }

    public string GenerateAIExportMarkdown(TechnicalIssue issue)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("CONTESTO:");
        sb.AppendLine("- Progetto: MESManager");
        sb.AppendLine($"- Versione coinvolta: {issue.AffectedVersion ?? "Non specificata"}");
        sb.AppendLine($"- Ambiente: {issue.Environment}");
        sb.AppendLine($"- Area: {issue.Area}");
        sb.AppendLine($"- Gravità: {issue.Severity}");
        sb.AppendLine();
        
        sb.AppendLine("PROBLEMA:");
        sb.AppendLine(issue.Description);
        sb.AppendLine();
        
        sb.AppendLine("PASSAGGI PER RIPRODURRE:");
        sb.AppendLine(string.IsNullOrWhiteSpace(issue.ReproSteps) ? "(Non specificati)" : issue.ReproSteps);
        sb.AppendLine();
        
        sb.AppendLine("LOG / ERRORI:");
        sb.AppendLine(string.IsNullOrWhiteSpace(issue.Logs) ? "(Nessun log fornito)" : issue.Logs);
        sb.AppendLine();
        
        sb.AppendLine("VINCOLI:");
        sb.AppendLine("- Consultare docs in C:\\Dev\\MESManager\\docs e mantenere coerenza");
        sb.AppendLine("- Evitare duplicazioni");
        sb.AppendLine("- Separare DEV/PROD");
        sb.AppendLine("- Aggiornare docs con postmortem/regola/ADR se emerge nuova conoscenza");
        sb.AppendLine();
        
        sb.AppendLine("OBIETTIVO:");
        sb.AppendLine("- Analisi causa radice");
        sb.AppendLine("- Soluzione robusta e semplice");
        sb.AppendLine("- Lista file da modificare");
        sb.AppendLine("- Indicazione docs da aggiornare");

        return sb.ToString();
    }

    public async Task MarkAsExportedAsync(IEnumerable<int> ids)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var idList = ids.ToList();
        var issues = await context.TechnicalIssues
            .Where(i => idList.Contains(i.Id))
            .ToListAsync();
        var now = DateTime.Now;
        foreach (var issue in issues)
        {
            issue.ExportedToAI = true;
            issue.ExportedToAIAt = now;
        }
        await context.SaveChangesAsync();
    }

    public async Task<TechnicalIssue?> CreateAutoCaptureAsync(
        string title,
        string description,
        string? logs,
        string? sourceUrl,
        IssueArea area,
        IssueSeverity severity,
        IssueEnvironment environment,
        string? affectedVersion)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Deduplicazione: se esiste già un issue auto-catturato con stesso titolo (esatto) nelle ultime 2 ore, non ne crea uno nuovo
        var cutoff = DateTime.Now.AddHours(-2);
        var duplicate = await context.TechnicalIssues
            .Where(i => i.IsAutoCapture
                     && i.Title == title
                     && i.Status != IssueStatus.Resolved
                     && i.Status != IssueStatus.Documented
                     && i.CreatedAt >= cutoff)
            .FirstOrDefaultAsync();

        if (duplicate != null)
            return null; // duplicato: non creare

        var issue = new TechnicalIssue
        {
            CreatedAt = DateTime.Now,
            Title = title,
            Description = description,
            Logs = logs,
            SourceUrl = sourceUrl,
            Area = area,
            Severity = severity,
            Environment = environment,
            AffectedVersion = affectedVersion,
            Status = IssueStatus.Open,
            IsAutoCapture = true,
            CreatedBy = "AUTO-CAPTURE"
        };

        context.TechnicalIssues.Add(issue);
        await context.SaveChangesAsync();
        return issue;
    }
}
