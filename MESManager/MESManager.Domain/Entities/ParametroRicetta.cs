namespace MESManager.Domain.Entities;

public class ParametroRicetta
{
    public Guid Id { get; set; }
    public Guid RicettaId { get; set; }
    public string NomeParametro { get; set; } = string.Empty;
    public string Valore { get; set; } = string.Empty;
    public string UnitaMisura { get; set; } = string.Empty;
    
    // Campi importati da Gantt.ArticoliRicetta
    public int? CodiceParametro { get; set; }
    public int? Indirizzo { get; set; }
    public string? Area { get; set; }
    public string? Tipo { get; set; }
    
    // Navigazioni
    public Ricetta Ricetta { get; set; } = null!;
}
