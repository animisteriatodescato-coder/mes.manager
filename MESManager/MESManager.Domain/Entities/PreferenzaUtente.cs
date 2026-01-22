namespace MESManager.Domain.Entities;

/// <summary>
/// Preferenza individuale di un utente (stato griglia, colonne stampa, tema, ecc.)
/// </summary>
public class PreferenzaUtente
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// FK all'utente proprietario della preferenza
    /// </summary>
    public Guid UtenteAppId { get; set; }
    
    /// <summary>
    /// Utente proprietario
    /// </summary>
    public UtenteApp UtenteApp { get; set; } = null!;
    
    /// <summary>
    /// Chiave della preferenza (es: "grid-state-commesse-aperte", "print-columns-programma", "fix-state-anime")
    /// </summary>
    public string Chiave { get; set; } = string.Empty;
    
    /// <summary>
    /// Valore della preferenza in formato JSON
    /// </summary>
    public string ValoreJson { get; set; } = string.Empty;
    
    /// <summary>
    /// Data di creazione
    /// </summary>
    public DateTime DataCreazione { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Ultima modifica
    /// </summary>
    public DateTime UltimaModifica { get; set; } = DateTime.Now;
}
