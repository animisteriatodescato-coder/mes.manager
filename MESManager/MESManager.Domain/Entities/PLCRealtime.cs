namespace MESManager.Domain.Entities;

public class PLCRealtime
{
    public Guid Id { get; set; }
    public Guid MacchinaId { get; set; }
    public DateTime DataUltimoAggiornamento { get; set; }
    public int ValoreGenerico { get; set; }
    
    // Navigazioni
    public Macchina Macchina { get; set; } = null!;
}
