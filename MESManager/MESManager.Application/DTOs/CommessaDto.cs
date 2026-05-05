namespace MESManager.Application.DTOs;

public class CommessaDto
{
    public Guid Id { get; set; }
    public string Codice { get; set; } = string.Empty;
    
    // Riferimenti Mago
    public string? SaleOrdId { get; set; }
    public string? InternalOrdNo { get; set; }
    public string? ExternalOrdNo { get; set; }
    public string? Line { get; set; }
    
    // Relazioni
    public Guid? ArticoloId { get; set; }
    public Guid? ClienteId { get; set; }
    
    // Campi denormalizzati per visualizzazione
    public string? ClienteRagioneSociale { get; set; }
    public string? CompanyName { get; set; } // Da Mago (fallback se ClienteId mancante)
    
    /// <summary>
    /// Cliente da visualizzare: priorità a CompanyName (dati Mago - fonte corretta),
    /// fallback a ClienteRagioneSociale (tabella Clienti - può contenere dati errati).
    /// I suffissi societari italiani (S.R.L., S.P.A., S.N.C., ecc.) vengono rimossi
    /// per compattare la visualizzazione nelle griglie e stampe.
    /// </summary>
    public string ClienteDisplay
    {
        get
        {
            var name = CompanyName ?? ClienteRagioneSociale ?? "N/D";
            return StripBusinessSuffix(name);
        }
    }

    private static readonly System.Text.RegularExpressions.Regex _businessSuffixRegex =
        new(@"\s+(?:s\.?r\.?l|s\.?p\.?a|s\.?n\.?c|s\.?a\.?s|s\.?c\.?r\.?l|s\.?a\.?p\.?a|s\.?s|s\.?c)\.?\s*$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase |
            System.Text.RegularExpressions.RegexOptions.Compiled);

    private static string StripBusinessSuffix(string name)
    {
        if (string.IsNullOrEmpty(name) || name == "N/D") return name;
        return _businessSuffixRegex.Replace(name, string.Empty).Trim();
    }
    public string? ArticoloCodice { get; set; }
    public string? ArticoloDescrizione { get; set; }
    public decimal? ArticoloPrezzo { get; set; }
    
    // Dati commessa
    public string? Description { get; set; }
    public decimal QuantitaRichiesta { get; set; }
    public string? UoM { get; set; }
    public DateTime? DataConsegna { get; set; }
    public string? Stato { get; set; }
    
    // Stato programma interno (gestito localmente)
    public string? StatoProgramma { get; set; }
    public DateTime? DataCambioStatoProgramma { get; set; }
    
    // Riferimenti
    public string? RiferimentoOrdineCliente { get; set; }
    public string? OurReference { get; set; }
    
    // Programmazione Macchine
    public int? NumeroMacchina { get; set; }
    public int OrdineSequenza { get; set; } // Ordine di esecuzione sulla macchina
    
    // Pianificazione produzione (per diagramma Gantt)
    public DateTime? DataInizioPrevisione { get; set; } // Data/ora inizio prevista
    public DateTime? DataFinePrevisione { get; set; } // Data/ora fine prevista (calcolata)
    public DateTime? DataInizioProduzione { get; set; } // Data/ora inizio effettivo
    public DateTime? DataFineProduzione { get; set; } // Data/ora fine effettivo
    
    // Audit
    public DateTime UltimaModifica { get; set; }
    public DateTime TimestampSync { get; set; }
    
    // Anime properties (from Anime table via ArticoloCodice)
    public string? UnitaMisura { get; set; }
    public int? Larghezza { get; set; }
    public int? Altezza { get; set; }
    public int? Profondita { get; set; }
    public int? Imballo { get; set; }
    public string? ImballoDescrizione { get; set; }  // Descrizione lookup
    public string? NoteAnime { get; set; }
    public string? Allegato { get; set; }
    public string? Peso { get; set; }
    public string? Ubicazione { get; set; }
    public string? Ciclo { get; set; }
    public string? CodiceCassa { get; set; }
    public string? CodiceAnime { get; set; }
    public string? MacchineSuDisponibili { get; set; }
    public string? MacchineSuDisponibiliDescrizione { get; set; }  // Descrizione lookup (nomi macchine)
    public bool? TrasmettiTutto { get; set; }
    
    // Campi aggiuntivi per etichetta
    public string? Sabbia { get; set; }
    public string? SabbiaDescrizione { get; set; }
    public string? Vernice { get; set; }
    public string? VerniceDescrizione { get; set; }
    public string? Colla { get; set; }
    public string? CollaDescrizione { get; set; }
    public int? QuantitaPiano { get; set; }
    public int? NumeroPiani { get; set; }
    public string? ClienteAnime { get; set; }
    
    // Campi anime aggiuntivi
    public string? TogliereSparo { get; set; }
    public string? Figure { get; set; }
    public string? Maschere { get; set; }
    public string? Assemblata { get; set; }
    public string? ArmataL { get; set; }
    
    // Quantità calcolata per etichetta (QuantitaPiano * NumeroPiani)
    public int? QuantitaEtichetta => (QuantitaPiano ?? 0) * (NumeroPiani ?? 0);
    
    // Flag per verificare se i dati etichetta sono completi
    public bool DatiEtichettaCompleti => 
        !string.IsNullOrEmpty(CodiceAnime) && 
        !string.IsNullOrEmpty(ClienteDisplay); // Usa fallback intelligente
    
    // Flag per verificare se l'articolo ha una ricetta configurata
    public bool HasRicetta { get; set; }
    public int NumeroParametri { get; set; }
    public DateTime? RicettaUltimaModifica { get; set; }

    /// <summary>
    /// NC aperte per l'articolo — campo calcolato lato Blazor, NON persistito su DB.
    /// Popolato dopo il caricamento commesse via /api/NonConformita/count-per-articolo.
    /// </summary>
    public int NcAperteCount { get; set; }
}
