namespace MESManager.Domain.Entities;

public class Articolo
{
    public Guid Id { get; set; }
    public string Codice { get; set; } = string.Empty;
    public string Descrizione { get; set; } = string.Empty;
    public decimal Prezzo { get; set; }
    public bool Attivo { get; set; } = true;
    public DateTime UltimaModifica { get; set; }
    public DateTime TimestampSync { get; set; }

    // Navigazioni
    public Ricetta? Ricetta { get; set; }
    public ICollection<Commessa> Commesse { get; set; } = new List<Commessa>();
}
