namespace MESManager.Application.DTOs;

/// <summary>
/// DTO per un singolo parametro di ricetta articolo
/// Mappato dalla tabella [Gantt].[dbo].[ArticoliRicetta]
/// </summary>
public class ParametroRicettaArticoloDto
{
    public int IdRigaRicetta { get; set; }
    public Guid ParametroId { get; set; }
    public string CodiceArticolo { get; set; } = string.Empty;
    public int CodiceParametro { get; set; }
    public string DescrizioneParametro { get; set; } = string.Empty;
    public int Indirizzo { get; set; }
    public string Area { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string? UM { get; set; }
    public int Valore { get; set; }
}

/// <summary>
/// DTO per la ricetta completa di un articolo (tutti i parametri)
/// </summary>
public class RicettaArticoloDto
{
    public string CodiceArticolo { get; set; } = string.Empty;
    public string? DescrizioneArticolo { get; set; }
    public List<ParametroRicettaArticoloDto> Parametri { get; set; } = new();
    public int TotaleParametri => Parametri.Count;
}

/// <summary>
/// Response per la ricerca ricette
/// </summary>
public class RicetteSearchResponse
{
    public List<RicettaArticoloDto> Ricette { get; set; } = new();
    public int TotaleRicette { get; set; }
    public int TotaleParametri { get; set; }
}
