namespace MESManager.Sync.DTO;

public class ArticoloMago
{
    public string Codice { get; set; } = string.Empty;
    public string Descrizione { get; set; } = string.Empty;
    public decimal Prezzo { get; set; }
    public bool Attivo { get; set; }
    public string UltimaModifica { get; set; } = string.Empty;
}
