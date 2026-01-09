namespace MESManager.Domain.Entities;

public class Ricetta
{
    public Guid Id { get; set; }
    public Guid ArticoloId { get; set; }
    
    // Navigazioni
    public Articolo Articolo { get; set; } = null!;
    public ICollection<ParametroRicetta> Parametri { get; set; } = new List<ParametroRicetta>();
}
