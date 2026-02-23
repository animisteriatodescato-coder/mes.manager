namespace MESManager.Domain.Entities;

public class Ricetta
{
    public Guid Id { get; set; }
    public Guid ArticoloId { get; set; }
    
    // Audit
    public DateTime DataCreazione { get; set; } = DateTime.Now;
    public DateTime DataUltimoAggiornamento { get; set; } = DateTime.Now;
    
    // Navigazioni
    public Articolo Articolo { get; set; } = null!;
    public ICollection<ParametroRicetta> Parametri { get; set; } = new List<ParametroRicetta>();
}
