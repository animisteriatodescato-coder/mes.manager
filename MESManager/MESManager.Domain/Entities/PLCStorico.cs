namespace MESManager.Domain.Entities;

public class PLCStorico
{
    public Guid Id { get; set; }
    public Guid MacchinaId { get; set; }
    public DateTime DataOra { get; set; }
    public string Dati { get; set; } = string.Empty; // JSON placeholder
    
    // Navigazioni
    public Macchina Macchina { get; set; } = null!;
}
