namespace MESManager.Domain.Entities;

public class Cliente
{
    public Guid Id { get; set; }
    public string Codice { get; set; } = string.Empty;
    public string RagioneSociale { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Note { get; set; }
    public bool Attivo { get; set; } = true;
    public DateTime UltimaModifica { get; set; }
    public DateTime TimestampSync { get; set; }

    // Navigazioni
    public ICollection<Commessa> Commesse { get; set; } = new List<Commessa>();
}
