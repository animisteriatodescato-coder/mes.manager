using MESManager.Domain.Enums;

namespace MESManager.Domain.Entities;

/// <summary>
/// Traccia le modifiche di stato programma delle commesse.
/// Ogni cambio di StatoProgramma viene registrato per audit e storico.
/// </summary>
public class StoricoProgrammazione
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Riferimento alla commessa
    /// </summary>
    public Guid CommessaId { get; set; }
    
    /// <summary>
    /// Stato precedente (null se prima assegnazione)
    /// </summary>
    public StatoProgramma? StatoPrecedente { get; set; }
    
    /// <summary>
    /// Nuovo stato assegnato
    /// </summary>
    public StatoProgramma StatoNuovo { get; set; }
    
    /// <summary>
    /// Data e ora della modifica
    /// </summary>
    public DateTime DataModifica { get; set; }
    
    /// <summary>
    /// Utente che ha effettuato la modifica (opzionale)
    /// </summary>
    public string? UtenteModifica { get; set; }
    
    /// <summary>
    /// Note aggiuntive sulla modifica
    /// </summary>
    public string? Note { get; set; }
    
    // Navigazione
    public Commessa? Commessa { get; set; }
}
