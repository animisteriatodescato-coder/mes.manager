namespace MESManager.Application.DTOs;

/// <summary>
/// Request per spostare una commessa nel Gantt
/// </summary>
public class SpostaCommessaRequest
{
    /// <summary>
    /// ID della commessa da spostare
    /// </summary>
    public Guid CommessaId { get; set; }
    
    /// <summary>
    /// Numero macchina di destinazione (es. 1, 2, 3...)
    /// </summary>
    public int TargetMacchina { get; set; }
    
    /// <summary>
    /// Data/ora inizio desiderata. Se null, viene accodata in fondo alla macchina.
    /// </summary>
    public DateTime? TargetDataInizio { get; set; }
    
    /// <summary>
    /// ID della commessa PRIMA della quale inserire. Se null, accoda in fondo.
    /// </summary>
    public Guid? InsertBeforeCommessaId { get; set; }
}

/// <summary>
/// Response per lo spostamento commessa
/// </summary>
public class SpostaCommessaResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Lista delle commesse aggiornate (tutte quelle della macchina ricalcolate)
    /// </summary>
    public List<CommessaGanttDto> CommesseAggiornate { get; set; } = new();
    
    /// <summary>
    /// Se la macchina di origine era diversa, contiene le commesse ricalcolate della macchina origine
    /// </summary>
    public List<CommessaGanttDto>? CommesseMacchinaOrigine { get; set; }
    
    /// <summary>
    /// Versione aggiornamento (timestamp ticks) per evitare loop e stale updates
    /// </summary>
    public long UpdateVersion { get; set; }
    
    /// <summary>
    /// Lista macchine coinvolte (per SignalR mirato)
    /// </summary>
    public List<string> MacchineCoinvolte { get; set; } = new();
}

/// <summary>
/// DTO per un giorno festivo
/// </summary>
public class FestivoDto
{
    public Guid Id { get; set; }
    public DateOnly Data { get; set; }
    public string Descrizione { get; set; } = string.Empty;
    public bool Ricorrente { get; set; }
    public int? Anno { get; set; }
}

/// <summary>
/// Request per creare/aggiornare un festivo
/// </summary>
public class CreateFestivoRequest
{
    public DateOnly Data { get; set; }
    public string Descrizione { get; set; } = string.Empty;
    public bool Ricorrente { get; set; }
}

/// <summary>
/// Request per caricamento commessa su Gantt (opzionale: con macchina specifica)
/// </summary>
public class CaricaSuGanttRequest
{
    /// <summary>
    /// Numero macchina manuale (se null, usa auto-scheduler)
    /// </summary>
    public int? NumeroMacchina { get; set; }
}

/// <summary>
/// Response per caricamento automatico sul Gantt
/// </summary>
public class CaricaSuGanttResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Numero macchina assegnata dall'algoritmo
    /// </summary>
    public int? MacchinaAssegnata { get; set; }
    
    /// <summary>
    /// Data inizio calcolata
    /// </summary>
    public DateTime? DataInizioCalcolata { get; set; }
    
    /// <summary>
    /// Data fine calcolata
    /// </summary>
    public DateTime? DataFineCalcolata { get; set; }
    
    /// <summary>
    /// Ore necessarie stimate
    /// </summary>
    public decimal OreNecessarie { get; set; }
    
    /// <summary>
    /// Motivazione scelta della macchina
    /// </summary>
    public string Motivazione { get; set; } = string.Empty;
    
    /// <summary>
    /// Commesse aggiornate sulla macchina
    /// </summary>
    public List<CommessaGanttDto> CommesseAggiornate { get; set; } = new();
    
    public long UpdateVersion { get; set; }
}

/// <summary>
/// DTO per macchina disponibile (da ricetta/anima)
/// </summary>
public class MacchinaDisponibileDto
{
    public string Codice { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public int? NumeroMacchina { get; set; }
}
