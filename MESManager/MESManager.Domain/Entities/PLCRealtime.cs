namespace MESManager.Domain.Entities;

public class PLCRealtime
{
    public Guid Id { get; set; }
    public Guid MacchinaId { get; set; }
    public DateTime DataUltimoAggiornamento { get; set; }
    
    // === PRODUZIONE ===
    public int CicliFatti { get; set; }
    public int QuantitaDaProdurre { get; set; }
    public int CicliScarti { get; set; }
    public int BarcodeLavorazione { get; set; }
    
    // === OPERATORE ===
    public Guid? OperatoreId { get; set; }
    public int NumeroOperatore { get; set; }  // Numero raw dal PLC
    
    // === TEMPI ===
    public int TempoMedioRilevato { get; set; }
    public int TempoMedio { get; set; }
    public int Figure { get; set; }
    
    // === STATI ===
    public string StatoMacchina { get; set; } = string.Empty;
    public bool QuantitaRaggiunta { get; set; }

    // === EVENTI (ultimo timestamp rilevato per tipo) ===
    /// <summary>Ultimo cambio commessa/barcode rilevato.</summary>
    public DateTime? UltimaNuovaProduzione { get; set; }
    /// <summary>Ultimo inizio setup rilevato dal PLC.</summary>
    public DateTime? UltimoInizioSetup { get; set; }
    /// <summary>Ultimo fine setup rilevato dal PLC.</summary>
    public DateTime? UltimoFineSetup { get; set; }

    // Navigazioni
    public Macchina Macchina { get; set; } = null!;
    public Operatore? Operatore { get; set; }
}
