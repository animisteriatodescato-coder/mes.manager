using MESManager.Domain.Enums;

namespace MESManager.Domain.Entities;

public class Commessa
{
    public Guid Id { get; set; }
    public string Codice { get; set; } = string.Empty; // InternalOrdNo-Item formato
    
    // Riferimenti Mago
    public string? SaleOrdId { get; set; } // ID tecnico Mago
    public string? InternalOrdNo { get; set; } // Numero ordine interno Mago
    public string? ExternalOrdNo { get; set; } // Numero ordine esterno/cliente
    public string? Line { get; set; } // Numero linea dettaglio
    
    // Relazioni
    public Guid? ArticoloId { get; set; }
    public Guid? ClienteId { get; set; }
    public string? CompanyName { get; set; } // Nome cliente da Mago (denormalizzato)
    
    // Dati commessa
    public string? Description { get; set; } // Descrizione dalla linea
    public decimal QuantitaRichiesta { get; set; }
    public string? UoM { get; set; } // Unità di misura
    public DateTime? DataConsegna { get; set; }
    public StatoCommessa Stato { get; set; }
    
    // Riferimenti
    public string? RiferimentoOrdineCliente { get; set; } // YourReference
    public string? OurReference { get; set; } // Nostro riferimento
    
    // Programmazione Macchine
    public int? NumeroMacchina { get; set; } // Numero macchina assegnata
    
    // Pianificazione produzione (per diagramma Gantt)
    public DateTime? DataInizioPrevisione { get; set; } // Data/ora inizio prevista
    public DateTime? DataFinePrevisione { get; set; } // Data/ora fine prevista (calcolata)
    public DateTime? DataInizioProduzione { get; set; } // Data/ora inizio effettivo
    public DateTime? DataFineProduzione { get; set; } // Data/ora fine effettivo
    
    // Audit
    public DateTime UltimaModifica { get; set; }
    public DateTime TimestampSync { get; set; }

    // Navigazioni
    public Articolo? Articolo { get; set; }
    public Cliente? Cliente { get; set; }
}
