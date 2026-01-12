namespace MESManager.Domain.Entities;

public class EventoPLC
{
    public Guid Id { get; set; }
    public Guid MacchinaId { get; set; }
    public DateTime DataOra { get; set; }
    public string TipoEvento { get; set; } = string.Empty;
    // "NuovaProduzione", "InizioSetup", "FineSetup", "InProduzione", "QuantitaRaggiunta"
    
    public string? Dettagli { get; set; }
    
    // Navigazioni
    public Macchina Macchina { get; set; } = null!;
}
