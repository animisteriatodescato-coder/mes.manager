namespace MESManager.Domain.Enums;

/// <summary>
/// Ambiente in cui si è verificato il problema
/// </summary>
public enum IssueEnvironment
{
    Dev = 0,
    Prod = 1
}

/// <summary>
/// Area funzionale del problema
/// </summary>
public enum IssueArea
{
    DB = 0,
    Deploy = 1,
    AGGrid = 2,
    UX = 3,
    Security = 4,
    Performance = 5,
    PLC = 6,
    Sync = 7,
    Other = 99
}

/// <summary>
/// Gravità del problema
/// </summary>
public enum IssueSeverity
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// Stato del workflow dell'issue
/// </summary>
public enum IssueStatus
{
    Open = 0,
    Investigating = 1,
    Resolved = 2,
    Documented = 3
}
