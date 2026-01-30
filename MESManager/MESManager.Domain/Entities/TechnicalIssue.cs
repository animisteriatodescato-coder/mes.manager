using MESManager.Domain.Enums;

namespace MESManager.Domain.Entities;

/// <summary>
/// Entità per tracciare bug, errori e problemi tecnici.
/// Permette di documentare issues e generare output "pronto per AI".
/// </summary>
public class TechnicalIssue
{
    public int Id { get; set; }

    /// <summary>
    /// Data creazione dell'issue
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Data ultima modifica
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Ambiente (Dev/Prod)
    /// </summary>
    public IssueEnvironment Environment { get; set; } = IssueEnvironment.Dev;

    /// <summary>
    /// Area funzionale (DB, Deploy, AGGrid, UX, Security, Performance, PLC, Sync, Other)
    /// </summary>
    public IssueArea Area { get; set; } = IssueArea.Other;

    /// <summary>
    /// Gravità (Low, Medium, High, Critical)
    /// </summary>
    public IssueSeverity Severity { get; set; } = IssueSeverity.Medium;

    /// <summary>
    /// Stato workflow (Open, Investigating, Resolved, Documented)
    /// </summary>
    public IssueStatus Status { get; set; } = IssueStatus.Open;

    /// <summary>
    /// Titolo breve dell'issue (max 200 caratteri)
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Descrizione dettagliata del problema
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Passi per riprodurre il problema
    /// </summary>
    public string ReproSteps { get; set; } = string.Empty;

    /// <summary>
    /// Log e messaggi di errore (nullable)
    /// </summary>
    public string? Logs { get; set; }

    /// <summary>
    /// Versione dell'app in cui si è verificato il problema
    /// </summary>
    public string? AffectedVersion { get; set; }

    /// <summary>
    /// Soluzione implementata (nullable, da compilare quando risolto)
    /// </summary>
    public string? Solution { get; set; }

    /// <summary>
    /// Regole/lezioni apprese (da documentare in /docs)
    /// </summary>
    public string? RulesLearned { get; set; }

    /// <summary>
    /// Se la documentazione è stata aggiornata
    /// </summary>
    public bool DocsUpdated { get; set; } = false;

    /// <summary>
    /// Path del file docs aggiornato (es: docs/storico/FIX-XYZ.md)
    /// </summary>
    public string? DocsReferencePath { get; set; }

    /// <summary>
    /// Utente che ha creato l'issue
    /// </summary>
    public string? CreatedBy { get; set; }
}
