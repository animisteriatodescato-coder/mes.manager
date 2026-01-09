namespace MESManager.Domain.Entities;

public class LogSyncEntry
{
    public Guid Id { get; set; }
    public DateTime DataOra { get; set; }
    
    /// <summary>Modulo sincronizzato: "Clienti", "Articoli", "Commesse", "Tutto".</summary>
    public string Modulo { get; set; } = string.Empty;
    
    public int Nuovi { get; set; }
    public int Aggiornati { get; set; }
    public int Ignorati { get; set; }
    public int Errori { get; set; }
    
    public string? MessaggioErrore { get; set; }
    public string? FileBackupPath { get; set; }
}
