namespace MESManager.Application.DTOs;

public class MacchinaDto
{
    public Guid Id { get; set; }
    public string Codice { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public int Stato { get; set; }
    public bool AttivaInGantt { get; set; } = true;
    public int OrdineVisualizazione { get; set; } = 0;
}
