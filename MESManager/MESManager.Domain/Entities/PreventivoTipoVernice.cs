namespace MESManager.Domain.Entities;

/// <summary>
/// Tipo di vernice usato nel calcolo preventivi.
/// Gestito da ImpostazioniTabelle → tab "Vernice Preventivi".
/// </summary>
public class PreventivoTipoVernice
{
    public Guid Id { get; set; }
    public string Codice { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    /// <summary>acqua | alcool</summary>
    public string Famiglia { get; set; } = string.Empty;
    public decimal PrezzoKg { get; set; }
    public decimal PercentualeApplicazione { get; set; } = 8;
    public bool Attivo { get; set; } = true;
    public int Ordine { get; set; }
}
