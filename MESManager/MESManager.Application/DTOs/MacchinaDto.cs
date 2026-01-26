namespace MESManager.Application.DTOs;

public class MacchinaDto
{
    public Guid Id { get; set; }
    public string Codice { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public int Stato { get; set; }
    public bool AttivaInGantt { get; set; } = true;
    public int OrdineVisualizazione { get; set; } = 0;
    
    /// <summary>
    /// Indirizzo IP del PLC associato alla macchina.
    /// Se null o vuoto, la macchina non è connessa al PLC.
    /// </summary>
    public string? IndirizzoPLC { get; set; }
}
