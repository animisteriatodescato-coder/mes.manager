namespace MESManager.Domain.Entities;

/// <summary>
/// Utente dell'applicazione per gestione preferenze individuali.
/// Non usato per autenticazione (dropdown senza password).
/// </summary>
public class UtenteApp
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Nome visualizzato nel dropdown (es: IRENE, FABIO, GIULIA)
    /// </summary>
    public string Nome { get; set; } = string.Empty;
    
    /// <summary>
    /// Se l'utente è attivo e visibile nel dropdown
    /// </summary>
    public bool Attivo { get; set; } = true;
    
    /// <summary>
    /// Ordine di visualizzazione nel dropdown
    /// </summary>
    public int Ordine { get; set; }
    
    /// <summary>
    /// Data di creazione
    /// </summary>
    public DateTime DataCreazione { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Ultima modifica
    /// </summary>
    public DateTime UltimaModifica { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Preferenze associate a questo utente
    /// </summary>
    public ICollection<PreferenzaUtente> Preferenze { get; set; } = new List<PreferenzaUtente>();
}
