using MESManager.Domain.Enums;

namespace MESManager.Domain.Entities;

public class Commessa
{
    public Guid Id { get; set; }
    public string Codice { get; set; } = string.Empty;
    public Guid? ArticoloId { get; set; }
    public Guid? ClienteId { get; set; }
    public decimal QuantitaRichiesta { get; set; }
    public DateTime? DataConsegna { get; set; }
    public StatoCommessa Stato { get; set; }
    public string? RiferimentoOrdineCliente { get; set; }
    public DateTime UltimaModifica { get; set; }
    public DateTime TimestampSync { get; set; }

    // Navigazioni
    public Articolo? Articolo { get; set; }
    public Cliente? Cliente { get; set; }
}
