namespace MESManager.Application.DTOs;

public class CommessaDto
{
    public Guid Id { get; set; }
    public string Codice { get; set; } = string.Empty;
    public Guid? ArticoloId { get; set; }
    public Guid? ClienteId { get; set; }
    
    // Campi denormalizzati per visualizzazione
    public string? ClienteRagioneSociale { get; set; }
    public string? ArticoloCodice { get; set; }
    public string? ArticoloDescrizione { get; set; }
    
    public decimal QuantitaRichiesta { get; set; }
    public DateTime? DataConsegna { get; set; }
    public string? Stato { get; set; }
    public string? RiferimentoOrdineCliente { get; set; }
    public DateTime UltimaModifica { get; set; }
    public DateTime TimestampSync { get; set; }
}
