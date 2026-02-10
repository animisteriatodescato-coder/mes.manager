namespace MESManager.Application.DTOs;

public class CommessaGanttDto
{
    public Guid Id { get; set; }
    public string Codice { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? NumeroMacchina { get; set; }
    public string? NomeMacchina { get; set; }
    public int OrdineSequenza { get; set; }
    
    // Date pianificazione
    public DateTime? DataInizioPrevisione { get; set; }
    public DateTime? DataFinePrevisione { get; set; }
    public DateTime? DataInizioProduzione { get; set; }
    public DateTime? DataFineProduzione { get; set; }
    
    // Dati produttivi
    public decimal QuantitaRichiesta { get; set; }
    public string? UoM { get; set; }
    public DateTime? DataConsegna { get; set; }
    
    // Dati calcolo tempi
    public int TempoCicloSecondi { get; set; } // Dal catalogo
    public int NumeroFigure { get; set; } // Dal catalogo
    public int TempoSetupMinuti { get; set; } // Dalle impostazioni o override
    public int DurataPrevistaMinuti { get; set; } // Calcolato
    
    // Stato e colore
    public string Stato { get; set; } = string.Empty;
    public string ColoreStato { get; set; } = string.Empty; // Per il Gantt
    public string StatoProgramma { get; set; } = string.Empty;
    
    // Percentuale completamento
    public decimal PercentualeCompletamento { get; set; }
    
    // Indicatore dati incompleti (per triangolino avviso nel Gantt)
    public bool DatiIncompleti { get; set; }
    
    // ===== NUOVI CAMPI - SCHEDULING AVANZATO =====
    
    // Priorità e Lock
    public int Priorita { get; set; } = 100; // Più basso = più urgente
    public bool Bloccata { get; set; } = false; // Se true, non trascinabile e non ricalcolabile
    
    // Vincoli temporali
    public DateTime? VincoloDataInizio { get; set; } // Non può iniziare prima
    public DateTime? VincoloDataFine { get; set; } // Deve finire entro
    public bool VincoloDataFineSuperato { get; set; } // Warning se true (calcolato lato server)
    
    // Classe lavorazione
    public string? ClasseLavorazione { get; set; }
    
    // Codice Cassa (dalla tabella Anime)
    public string? CodiceCassa { get; set; }
}
