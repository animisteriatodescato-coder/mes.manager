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
