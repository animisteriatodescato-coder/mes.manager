namespace MESManager.Domain.Entities;

public class WorkProcessingType
{
    public Guid Id { get; set; }
    public string Codice { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? Descrizione { get; set; }
    public string? Categoria { get; set; }
    public bool Attivo { get; set; }
    public bool Archiviato { get; set; }
    public int Ordinamento { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public ICollection<WorkProcessingParameter> Parametri { get; set; } = new List<WorkProcessingParameter>();
}
