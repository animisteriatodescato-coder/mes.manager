namespace MESManager.Domain.Entities;

/// <summary>
/// Tipo di sabbia usato nel calcolo preventivi.
/// Gestito da ImpostazioniTabelle → tab "Sabbia Preventivi".
/// </summary>
public class PreventivoTipoSabbia
{
    public Guid Id { get; set; }
    public string Codice { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    /// <summary>distribut | cerabeads | normale</summary>
    public string Famiglia { get; set; } = string.Empty;
    public decimal EuroOra { get; set; }
    public decimal PrezzoKg { get; set; }
    public int SpariDefault { get; set; }
    public bool Attivo { get; set; } = true;
    public int Ordine { get; set; }
}
