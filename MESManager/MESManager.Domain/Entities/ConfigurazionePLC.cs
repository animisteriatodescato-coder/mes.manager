namespace MESManager.Domain.Entities;

public class ConfigurazionePLC
{
    public Guid Id { get; set; }
    public Guid MacchinaId { get; set; }
    public string NomeParametro { get; set; } = string.Empty;
    public string Indirizzo { get; set; } = string.Empty;
    public string TipoDato { get; set; } = string.Empty;
    
    // Navigazioni
    public Macchina Macchina { get; set; } = null!;
}
