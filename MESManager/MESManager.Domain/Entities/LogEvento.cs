namespace MESManager.Domain.Entities;

public class LogEvento
{
    public Guid Id { get; set; }
    public DateTime DataOra { get; set; }
    public string Utente { get; set; } = string.Empty;
    public string Azione { get; set; } = string.Empty; // Create/Update/Delete
    public string Entita { get; set; } = string.Empty;
    public Guid? IdEntita { get; set; }
    public string? ValorePrecedenteJson { get; set; }
    public string? ValoreSuccessivoJson { get; set; }
}
