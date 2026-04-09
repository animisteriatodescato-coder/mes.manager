namespace MESManager.Domain.Entities;

public class WorkProcessingParameter
{
    public Guid Id { get; set; }
    public Guid WorkProcessingTypeId { get; set; }
    public bool IsCurrent { get; set; }
    public decimal EuroOra { get; set; }
    public decimal CostoAttrezzatura { get; set; }
    public decimal ImballaggioOra { get; set; }
    public decimal IncollaggioCostoOra { get; set; }
    public decimal SabbiaCostoKg { get; set; }
    public decimal VerniceCostoPezzo { get; set; }
    public decimal VerniciaturaCostoOra { get; set; }
    public decimal MargineDefaultPercent { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public string? VersionNotes { get; set; }

    public WorkProcessingType WorkProcessingType { get; set; } = null!;
}
