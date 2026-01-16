using MESManager.Domain.Enums;

namespace MESManager.Domain.Entities;

public class Macchina
{
    public Guid Id { get; set; }
    public string Codice { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public StatoMacchina Stato { get; set; }
    public bool AttivaInGantt { get; set; } = true;
    public int OrdineVisualizazione { get; set; } = 0;
    
    // Navigazioni
    public ICollection<EventoPLC> EventiPLC { get; set; } = new List<EventoPLC>();
    public ICollection<Manutenzione> Manutenzioni { get; set; } = new List<Manutenzione>();
    public ICollection<ConfigurazionePLC> ConfigurazioniPLC { get; set; } = new List<ConfigurazionePLC>();
}
