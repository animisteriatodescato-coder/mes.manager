using MESManager.Domain.Entities;
using MESManager.Domain.Enums;

namespace MESManager.Application.Interfaces;

/// <summary>
/// Interfaccia per la gestione dei TechnicalIssue (Archivio Bug & Errori)
/// </summary>
public interface ITechnicalIssueService
{
    /// <summary>
    /// Ottiene tutti gli issue con filtri opzionali
    /// </summary>
    Task<List<TechnicalIssue>> GetAllAsync(
        IssueStatus? status = null,
        IssueArea? area = null,
        IssueSeverity? severity = null,
        IssueEnvironment? environment = null,
        string? searchTitle = null);

    /// <summary>
    /// Ottiene un issue per ID
    /// </summary>
    Task<TechnicalIssue?> GetByIdAsync(int id);

    /// <summary>
    /// Crea un nuovo issue
    /// </summary>
    Task<TechnicalIssue> CreateAsync(TechnicalIssue issue);

    /// <summary>
    /// Aggiorna un issue esistente
    /// </summary>
    Task<TechnicalIssue?> UpdateAsync(TechnicalIssue issue);

    /// <summary>
    /// Elimina un issue
    /// </summary>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Segna un issue come risolto
    /// </summary>
    Task<TechnicalIssue?> MarkAsResolvedAsync(int id, string? solution = null);

    /// <summary>
    /// Segna un issue come documentato
    /// </summary>
    Task<TechnicalIssue?> MarkAsDocumentedAsync(int id, string? docsReferencePath = null);

    /// <summary>
    /// Genera il testo markdown per export AI
    /// </summary>
    string GenerateAIExportMarkdown(TechnicalIssue issue);
}
