namespace MESManager.Domain.Entities;

public class EventoPLC
{
    public Guid Id { get; set; }
    public Guid MacchinaId { get; set; }
    public DateTime DataOra { get; set; }
    public string TipoEvento { get; set; } = string.Empty;
    
    // Navigazioni
    public Macchina Macchina { get; set; } = null!;
}
