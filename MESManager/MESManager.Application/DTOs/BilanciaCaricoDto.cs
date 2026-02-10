namespace MESManager.Application.DTOs;

/// <summary>
/// Request per suggerire la macchina migliore per una commessa
/// </summary>
public class SuggerisciMacchinaRequest
{
    /// <summary>
    /// ID della commessa da assegnare
    /// </summary>
    public Guid CommessaId { get; set; }
    
    /// <summary>
    /// Elenco macchine candidate (opzionale). Se null, valuta tutte le macchine attive.
    /// </summary>
    public List<string>? NumeriMacchineCandidate { get; set; }
}

/// <summary>
/// Response con suggerimento macchina migliore
/// </summary>
public class SuggerisciMacchinaResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Numero macchina suggerita (quella con completion time minore)
    /// </summary>
    public string? MacchinaSuggerita { get; set; }
    
    /// <summary>
    /// Nome macchina suggerita
    /// </summary>
    public string? NomeMacchina { get; set; }
    
    /// <summary>
    /// Data/ora prevista di fine ultima commessa su questa macchina (senza la nuova)
    /// </summary>
    public DateTime? DataFineUltimaCommessa { get; set; }
    
    /// <summary>
    /// Data inizio prevista per la nuova commessa su questa macchina
    /// </summary>
    public DateTime? DataInizioPrevista { get; set; }
    
    /// <summary>
    /// Data fine prevista per la nuova commessa su questa macchina
    /// </summary>
    public DateTime? DataFinePrevista { get; set; }
    
    /// <summary>
    /// Valutazioni alternative (per tutte le macchine candidate)
    /// </summary>
    public List<ValutazioneMacchina> Valutazioni { get; set; } = new();
}

/// <summary>
/// Valutazione di una singola macchina per la commessa
/// </summary>
public class ValutazioneMacchina
{
    public string NumeroMacchina { get; set; } = string.Empty;
    public string NomeMacchina { get; set; } = string.Empty;
    public DateTime? DataFineUltimaCommessa { get; set; }
    public DateTime? DataInizioPrevista { get; set; }
    public DateTime? DataFinePrevista { get; set; }
    public int NumeroCommesseInCoda { get; set; }
    public int CaricoPrevisto { get; set; } // Minuti totali
}
