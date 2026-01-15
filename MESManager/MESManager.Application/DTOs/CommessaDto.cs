namespace MESManager.Application.DTOs;

public class CommessaDto
{
    public Guid Id { get; set; }
    public string Codice { get; set; } = string.Empty;
    
    // Riferimenti Mago
    public string? SaleOrdId { get; set; }
    public string? InternalOrdNo { get; set; }
    public string? ExternalOrdNo { get; set; }
    public string? Line { get; set; }
    
    // Relazioni
    public Guid? ArticoloId { get; set; }
    public Guid? ClienteId { get; set; }
    
    // Campi denormalizzati per visualizzazione
    public string? ClienteRagioneSociale { get; set; }
    public string? ArticoloCodice { get; set; }
    public string? ArticoloDescrizione { get; set; }
    
    // Dati commessa
    public string? Description { get; set; }
    public decimal QuantitaRichiesta { get; set; }
    public string? UoM { get; set; }
    public DateTime? DataConsegna { get; set; }
    public string? Stato { get; set; }
    
    // Riferimenti
    public string? RiferimentoOrdineCliente { get; set; }
    public string? OurReference { get; set; }
    
    // Audit
    public DateTime UltimaModifica { get; set; }
    public DateTime TimestampSync { get; set; }
}
