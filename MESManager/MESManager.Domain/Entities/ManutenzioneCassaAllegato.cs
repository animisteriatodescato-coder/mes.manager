namespace MESManager.Domain.Entities;

/// <summary>
/// Allegato (foto o PDF) associato a una scheda di manutenzione cassa d'anima.
/// </summary>
public class ManutenzioneCassaAllegato
{
    public int Id { get; set; }

    /// <summary>FK verso ManutenzioneCasseSchede</summary>
    public Guid SchedaId { get; set; }

    public string NomeFile { get; set; } = string.Empty;

    /// <summary>Path assoluto su disco</summary>
    public string PathFile { get; set; } = string.Empty;

    /// <summary>"Foto" | "Documento"</summary>
    public string TipoFile { get; set; } = "Documento";

    public string Estensione { get; set; } = string.Empty;

    public string? Descrizione { get; set; }

    public long DimensioneBytes { get; set; }

    public DateTime DataCaricamento { get; set; } = DateTime.UtcNow;

    // Nav
    public ManutenzioneCassaScheda? Scheda { get; set; }
}
