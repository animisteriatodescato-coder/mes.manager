using Microsoft.AspNetCore.Identity;

namespace MESManager.Infrastructure.Entities;

/// <summary>
/// Utente unificato: estende IdentityUser con il profilo applicativo.
/// Sostituisce il doppio sistema IdentityUser (auth) + UtenteApp (preferenze UI).
/// Unica fonte di verità per autenticazione, ruoli E profilo visivo.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>Display name visibile nell'app (es. IRENE, FABIO) — distinto da UserName</summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>Colore personalizzato hex (es. #FF5733) per indicatori visivi</summary>
    public string? Colore { get; set; }

    /// <summary>Ordine di visualizzazione nei selettori utente</summary>
    public int Ordine { get; set; }

    /// <summary>Utente abilitato all'accesso applicativo</summary>
    public bool Attivo { get; set; } = true;

    public DateTime DataCreazione { get; set; } = DateTime.UtcNow;
}
