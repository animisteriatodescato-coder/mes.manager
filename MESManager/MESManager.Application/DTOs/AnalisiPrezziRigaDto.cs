namespace MESManager.Application.DTOs;

public class AnalisiPrezziRigaDto
{
    public string CodiceArticolo { get; set; } = "";
    public string? Descrizione { get; set; }
    public string? Cliente { get; set; }
    public DateTime? DataUltimoPreventivo { get; set; }
    public decimal PrezzoUltimoPreventivo { get; set; }
    public decimal PrezzoCatalogoAttuale { get; set; }
    /// <summary>Variazione percentuale: (PreventivoUltimo - CatalogoAttuale) / CatalogoAttuale * 100</summary>
    public decimal DeltaPercentuale { get; set; }
    public int NumeroPreventiviTotali { get; set; }
    public bool AlertSoglia { get; set; }
    public string? TipoUltimoDocumento { get; set; }
}
