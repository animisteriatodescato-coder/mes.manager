namespace MESManager.Application.DTOs;

/// <summary>
/// DTO per allegato articolo
/// </summary>
public class AllegatoArticoloDto
{
    public int Id { get; set; }
    public string Archivio { get; set; } = string.Empty;
    public int? IdArchivio { get; set; }
    public string CodiceArticolo { get; set; } = string.Empty;
    public string PathFile { get; set; } = string.Empty;
    public string NomeFile { get; set; } = string.Empty;
    public string? Descrizione { get; set; }
    public int Priorita { get; set; }
    public string TipoFile { get; set; } = string.Empty; // "Foto" o "Documento"
    public string Estensione { get; set; } = string.Empty;
    public long? DimensioneBytes { get; set; }
    public DateTime DataCreazione { get; set; }
    public bool ImportatoDaGantt { get; set; }
    
    /// <summary>
    /// URL per accedere al file (generato dal service)
    /// </summary>
    public string? FileUrl { get; set; }
    
    /// <summary>
    /// Se il file è una foto
    /// </summary>
    public bool IsFoto => TipoFile == "Foto";
}

/// <summary>
/// Response per lista allegati separati per tipo
/// </summary>
public class AllegatiArticoloResponse
{
    public List<AllegatoArticoloDto> Foto { get; set; } = new();
    public List<AllegatoArticoloDto> Documenti { get; set; } = new();
    public int TotaleAllegati => Foto.Count + Documenti.Count;
}

/// <summary>
/// DTO per upload di un allegato
/// </summary>
public class UploadAllegatoRequest
{
    public string CodiceArticolo { get; set; } = string.Empty;
    public int? IdArchivio { get; set; }
    public string? Descrizione { get; set; }
    public int Priorita { get; set; } = 0;
}

/// <summary>
/// Risultato dell'importazione da Gantt
/// </summary>
public class ImportAllegatiResult
{
    public int TotaleImportati { get; set; }
    public int TotaleSaltati { get; set; }
    public int TotaleErrori { get; set; }
    public List<string> Errori { get; set; } = new();
    public bool Success => TotaleErrori == 0;
}
