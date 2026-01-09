namespace MESManager.Domain.Entities;

public class Manutenzione
{
    public Guid Id { get; set; }
    public Guid MacchinaId { get; set; }
    public string Descrizione { get; set; } = string.Empty;
    public DateTime DataPrevista { get; set; }
    public DateTime? DataEsecuzione { get; set; }
    
    // Navigazioni
    public Macchina Macchina { get; set; } = null!;
}
