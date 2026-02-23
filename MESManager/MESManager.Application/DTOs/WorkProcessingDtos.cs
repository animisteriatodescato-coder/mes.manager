namespace MESManager.Application.DTOs;

/// <summary>
/// DTO Tipo Lavorazione (visualizzazione lista master)
/// </summary>
public class WorkProcessingTypeDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Codice { get; set; } = string.Empty;
    public string? Descrizione { get; set; }
    public string? Categoria { get; set; }
    public int Ordinamento { get; set; }
    public bool Attivo { get; set; }
    public bool Archiviato { get; set; }
    
    // Parametri correnti (computed)
    public WorkProcessingParameterDto? ParametriCorrenti { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// DTO Parametri Economici Lavorazione
/// </summary>
public class WorkProcessingParameterDto
{
    public Guid Id { get; set; }
    public Guid WorkProcessingTypeId { get; set; }
    
    // Parametri economici
    public decimal EuroOra { get; set; }
    public decimal SabbiaCostoKg { get; set; }
    public decimal CostoAttrezzatura { get; set; }
    public decimal VerniceCostoPezzo { get; set; }
    public decimal VerniciaturaCostoOra { get; set; }
    public decimal IncollaggioCostoOra { get; set; }
    public decimal ImballaggioOra { get; set; }
    public decimal MargineDefaultPercent { get; set; }
    
    // Storicità
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool IsCurrent { get; set; }
    public string? VersionNotes { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}

/// <summary>
/// DTO Dati Tecnici Articolo per Calcolo
/// </summary>
public class WorkProcessingTechnicalDataDto
{
    public Guid Id { get; set; }
    public Guid QuoteRowId { get; set; }
    
    public decimal PesoKg { get; set; }
    public int Figure { get; set; }
    public int Lotto { get; set; }
    public decimal SpariOrari { get; set; }
    public decimal VerniciaturaPezziOra { get; set; }
    public decimal IncollaggioOre { get; set; }
    public decimal ImballaggioOre { get; set; }
    public decimal VernicePesoKg { get; set; }
    
    // Risultati calcolo
    public decimal CostoAnimaCalcolato { get; set; }
    public decimal CostoFuoriMacchina { get; set; }
    public decimal CostoTotalePezzo { get; set; }
    public decimal MargineApplicatoPercent { get; set; }
    public decimal PrezzoVenditaPezzo { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO per creazione/modifica Tipo Lavorazione
/// </summary>
public class WorkProcessingTypeSaveDto
{
    public Guid? Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Codice { get; set; } = string.Empty;
    public string? Descrizione { get; set; }
    public string? Categoria { get; set; }
    public int Ordinamento { get; set; }
    public bool Attivo { get; set; } = true;
}

/// <summary>
/// DTO per  creazione/modifica Parametri Economici
/// </summary>
public class WorkProcessingParameterSaveDto
{
    public Guid? Id { get; set; }
    public Guid WorkProcessingTypeId { get; set; }
    
    public decimal EuroOra { get; set; }
    public decimal SabbiaCostoKg { get; set; }
    public decimal CostoAttrezzatura { get; set; }
    public decimal VerniceCostoPezzo { get; set; } = 3m;
    public decimal VerniciaturaCostoOra { get; set; } = 40m;
    public decimal IncollaggioCostoOra { get; set; } = 40m;
    public decimal ImballaggioOra { get; set; } = 40m;
    public decimal MargineDefaultPercent { get; set; } = 30m;
    
    public string? VersionNotes { get; set; }
    public bool ImposeNewVersion { get; set; } = false; // Se true, invalida versione precedente
}

/// <summary>
/// Input per calcolo prezzo lavorazione anime
/// </summary>
public class WorkProcessingCalculationInput
{
    public Guid WorkProcessingTypeId { get; set; }
    
    // Dati tecnici articolo
    public decimal PesoKg { get; set; }
    public int Figure { get; set; } = 1;
    public int Lotto { get; set; } = 1;
    public decimal SpariOrari { get; set; }
    public decimal VerniciaturaPezziOra { get; set; } = 0;
    public decimal IncollaggioOre { get; set; } = 0;
    public decimal ImballaggioOre { get; set; } = 0;
    public decimal VernicePesoKg { get; set; } = 0.053m;
    
    // Margine personalizzato (se null, usa default parametri)
    public decimal? MarginePercent { get; set; }
}

/// <summary>
/// Risultato calcolo prezzo lavorazione
/// </summary>
public class WorkProcessingCalculationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    
    // Breakdown costi
    public decimal CostoSabbiatura { get; set; }      // (EuroOra / SpariOrari)
    public decimal CostoSabbia { get; set; }           // (SabbiaCostoKg * Peso * Figure)
    public decimal CostoAttrezzatura { get; set; }     // (CostoAtt / Lotto)
    public decimal CostoAnimaTotale { get; set; }      // Sabbiatura + Sabbia + Attrezzatura
    
    public decimal CostoVernice { get; set; }          // (VerniceCosto * VernicePeso)
    public decimal CostoVerniciatura { get; set; }     // (VernCostoOra / (Lotto / VernPezziOra))
    public decimal CostoIncollaggio { get; set; }      // (IncCostoOra * IncOre / Lotto)
    public decimal CostoImballo { get; set; }          // (ImbOra * ImbOre / Lotto)
    public decimal CostoFuoriMacchina { get; set; }    // Somma costi fuori macchina
    
    public decimal CostoTotalePezzo { get; set; }      // Anima + Fuori Macchina
    public decimal MargineApplicato { get; set; }      // % margine usato
    public decimal PrezzoVenditaPezzo { get; set; }    // Costo * (1 + Margine%)
    
    // Dettagli parametri usati (per tracciabilità)
    public WorkProcessingParameterDto? ParametriUsati { get; set; }
}
