namespace MESManager.Domain.Entities;

public class ParametroRicetta
{
    public Guid Id { get; set; }
    public Guid RicettaId { get; set; }
    public string NomeParametro { get; set; } = string.Empty;
    public string Valore { get; set; } = string.Empty;
    public string UnitaMisura { get; set; } = string.Empty;
    
    // Navigazioni
    public Ricetta Ricetta { get; set; } = null!;
}
