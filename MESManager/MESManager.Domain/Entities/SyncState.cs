namespace MESManager.Domain.Entities;

public class SyncState
{
    public Guid Id { get; set; }
    
    /// <summary>Nome del modulo: "Clienti", "Articoli", "Commesse".</summary>
    public string Modulo { get; set; } = string.Empty;
    
    public DateTime? UltimaSyncRiuscita { get; set; }
}
