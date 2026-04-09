namespace MESManager.Domain.Entities;

public class WorkProcessingTechnicalData
{
    public Guid Id { get; set; }
    public Guid QuoteRowId { get; set; }
    public int Figure { get; set; }
    public int Lotto { get; set; }
    public decimal PesoKg { get; set; }
    public decimal SpariOrari { get; set; }
    public decimal IncollaggioOre { get; set; }
    public decimal ImballaggioOre { get; set; }
    public decimal VerniciaturaPezziOra { get; set; }
    public decimal VernicePesoKg { get; set; }
    public decimal CostoAnimaCalcolato { get; set; }
    public decimal CostoFuoriMacchina { get; set; }
    public decimal CostoTotalePezzo { get; set; }
    public decimal PrezzoVenditaPezzo { get; set; }
    public decimal MargineApplicatoPercent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public QuoteRow QuoteRow { get; set; } = null!;
}
