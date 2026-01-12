namespace MESManager.Domain.Entities;

public class PLCStorico
{
    public Guid Id { get; set; }
    public Guid MacchinaId { get; set; }
    public DateTime DataOra { get; set; }
    public string Dati { get; set; } = string.Empty; // JSON completo snapshot
    
    // Campi denormalizzati per query veloci
    public string? StatoMacchina { get; set; }
    public Guid? OperatoreId { get; set; }
    
    // Navigazioni
    public Macchina Macchina { get; set; } = null!;
    public Operatore? Operatore { get; set; }
}
