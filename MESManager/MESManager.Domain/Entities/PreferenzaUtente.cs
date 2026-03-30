namespace MESManager.Domain.Entities;

/// <summary>
/// Preferenza individuale di un utente (stato griglia, colonne stampa, tema, ecc.)
/// FK → AspNetUsers.Id (stringa Identity) — non più dipende da UtenteApp.
/// </summary>
public class PreferenzaUtente
{
    public Guid Id { get; set; }

    /// <summary>
    /// ID dell'utente autenticato (AspNetUsers.Id — stringa Identity)
    /// </summary>
    public string UserId { get; set; } = string.Empty;

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

    /// <summary>
    /// Se true, è il default globale per tutti gli utenti.
    /// UserId sarà "GLOBAL" — non è una FK verso AspNetUsers.
    /// </summary>
    public bool IsGlobal { get; set; } = false;
}
