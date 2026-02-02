namespace MESManager.Domain.Entities;

/// <summary>
/// Rappresenta un giorno festivo da escludere dal calcolo delle date di produzione.
/// </summary>
public class Festivo
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Data del giorno festivo
    /// </summary>
    public DateOnly Data { get; set; }
    
    /// <summary>
    /// Descrizione del festivo (es. "Natale", "Ferragosto", "Chiusura aziendale")
    /// </summary>
    public string Descrizione { get; set; } = string.Empty;
    
    /// <summary>
    /// Se true, il festivo si ripete ogni anno (es. Natale, 1 Maggio)
    /// </summary>
    public bool Ricorrente { get; set; }
    
    /// <summary>
    /// Anno di riferimento (usato solo se Ricorrente = false)
    /// </summary>
    public int? Anno { get; set; }
    
    public DateTime DataCreazione { get; set; } = DateTime.UtcNow;
}
