namespace MESManager.Application.DTOs;

public class PlcRealtimeDto
{
    public Guid MacchinaId { get; set; }
    public string MacchinaNumero { get; set; } = string.Empty;
    public string MacchianaNome { get; set; } = string.Empty;
    
    /// <summary>
    /// Indirizzo IP del PLC associato alla macchina.
    /// </summary>
    public string? IndirizzoPLC { get; set; }
    
    /// <summary>
    /// Indica se la macchina è connessa (ha IP e dati recenti).
    /// </summary>
    public bool IsConnessa { get; set; } = true;
    
    public int CicliFatti { get; set; }
    public int QuantitaDaProdurre { get; set; }
    public int CicliScarti { get; set; }
    public int BarcodeLavorazione { get; set; }
    
    public int? NumeroOperatore { get; set; }
    public string? NomeOperatore { get; set; }
    
    public int TempoMedioRilevato { get; set; }
    public int TempoMedio { get; set; }
    public int Figure { get; set; }
    
    public string StatoMacchina { get; set; } = string.Empty;
    public bool QuantitaRaggiunta { get; set; }
    
    public DateTime UltimoAggiornamento { get; set; }

    // === EVENTI ===
    /// <summary>Ultimo cambio commessa/barcode rilevato.</summary>
    public DateTime? UltimaNuovaProduzione { get; set; }
    /// <summary>Ultimo inizio setup rilevato dal PLC.</summary>
    public DateTime? UltimoInizioSetup { get; set; }
    /// <summary>Ultimo fine setup rilevato dal PLC.</summary>
    public DateTime? UltimoFineSetup { get; set; }

    /// <summary>Durata dell'ultimo setup in minuti (null se InizioSetup o FineSetup mancante, o FineSetup precedente a InizioSetup).</summary>
    public double? DurataUltimoSetupMinuti =>
        UltimoInizioSetup.HasValue && UltimoFineSetup.HasValue && UltimoFineSetup > UltimoInizioSetup
            ? (UltimoFineSetup.Value - UltimoInizioSetup.Value).TotalMinutes
            : null;

    /// <summary>True se la macchina è attualmente in setup (InizioSetup rilevato ma FineSetup non ancora arrivato o precedente).</summary>
    public bool InSetupOra =>
        UltimoInizioSetup.HasValue &&
        (!UltimoFineSetup.HasValue || UltimoFineSetup < UltimoInizioSetup);

    /// <summary>
    /// Codice articolo della prossima commessa programmata nel Gantt per questa macchina.
    /// </summary>
    public string? ProssimoArticoloCodice { get; set; }

    /// <summary>
    /// Alert produzione attivi (NC aperte) sull'articolo prossimo in produzione.
    /// Popolato da PlcAppService via IAlertProduzioneService — zero chiamate HTTP extra dal client.
    /// </summary>
    public List<AlertProduzioneDto> AlertProduzione { get; set; } = new();

    /// <summary>Numero totale alert attivi — shortcut per badge UI.</summary>
    public int AlertCount => AlertProduzione.Count;

    /// <summary>True se ci sono alert attivi sull'articolo prossimo.</summary>
    public bool HasAlert => AlertProduzione.Count > 0;
    
    // Percentuale completamento
    public decimal PercentualeCompletamento => 
        QuantitaDaProdurre > 0 ? (decimal)CicliFatti / QuantitaDaProdurre * 100 : 0;

    // Scarti effettivi: include cicli extra oltre l'obiettivo (operatore che recupera scarti dimenticati)
    public int ScartiExtra => QuantitaDaProdurre > 0 ? Math.Max(0, CicliFatti - QuantitaDaProdurre) : 0;
    public int ScartiEffettivi => CicliScarti + ScartiExtra;
    public decimal PercentualeScartiEffettiva => CicliFatti > 0 ? Math.Round((decimal)ScartiEffettivi / CicliFatti * 100, 1) : 0;
}
