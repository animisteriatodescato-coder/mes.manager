namespace MESManager.Domain.Enums;

/// <summary>
/// Stati interni di programmazione della commessa.
/// Questi stati sono gestiti internamente e NON vengono sovrascritti dal sync con Mago.
/// </summary>
public enum StatoProgramma
{
    /// <summary>
    /// Commessa non ancora programmata
    /// </summary>
    NonProgrammata = 0,
    
    /// <summary>
    /// Commessa inserita nel programma di produzione
    /// </summary>
    Programmata = 1,
    
    /// <summary>
    /// Commessa in corso di produzione
    /// </summary>
    InProduzione = 2,
    
    /// <summary>
    /// Produzione completata internamente
    /// </summary>
    Completata = 3,
    
    /// <summary>
    /// Commessa archiviata (nascosta dal programma)
    /// </summary>
    Archiviata = 4
}
