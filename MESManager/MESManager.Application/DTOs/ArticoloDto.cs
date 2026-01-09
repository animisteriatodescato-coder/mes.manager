namespace MESManager.Application.DTOs;

public class ArticoloDto
{
    public Guid Id { get; set; }
    public string Codice { get; set; } = string.Empty;
    public string Descrizione { get; set; } = string.Empty;
    public decimal Prezzo { get; set; }
    public bool Attivo { get; set; }
    public DateTime UltimaModifica { get; set; }
    public DateTime TimestampSync { get; set; }
}
