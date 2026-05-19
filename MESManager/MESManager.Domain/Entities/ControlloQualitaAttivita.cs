namespace MESManager.Domain.Entities;

/// <summary>
/// Catalogo dei controlli qualita in-process eseguibili sulle macchine.
/// </summary>
public class ControlloQualitaAttivita
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Dettaglio { get; set; }
    public int Ordine { get; set; }
    public bool Attiva { get; set; } = true;
    public int FontSize { get; set; } = 11;
    public bool QuandoNecessario { get; set; }
    public bool RichiedeNotaSeProblema { get; set; } = true;
    public string? MacchinaCodiceFiltro { get; set; }

    public ICollection<ControlloQualitaRiga> Righe { get; set; } = new List<ControlloQualitaRiga>();
}
