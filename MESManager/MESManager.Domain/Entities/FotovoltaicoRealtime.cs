namespace MESManager.Domain.Entities;

/// <summary>
/// Snapshot realtime dell'impianto fotovoltaico (Huawei SUN2000).
/// Riga singola aggiornata ogni ~30s dal FotovoltaicoWorker via Modbus TCP.
/// </summary>
public class FotovoltaicoRealtime
{
    public int Id { get; set; } = 1; // sempre Id=1 — riga singleton

    public DateTime UltimoAggiornamento { get; set; }
    public bool ConnessioneOk { get; set; }
    public string? ErroreConnessione { get; set; }

    // === PRODUZIONE ===
    public double PotenzaAttuale_kW { get; set; }       // Active power (W→kW)
    public double EnergiaOggi_kWh { get; set; }         // Daily yield
    public double EnergiaAccumulata_kWh { get; set; }   // Cumulative total energy

    // === LATO DC (stringa PV1) ===
    public double TensioneStringa_V { get; set; }       // PV1 voltage (0.1V res)
    public double CorrenteStringa_A { get; set; }       // PV1 current (0.01A res)

    // === LATO AC (rete) ===
    public double TensioneRete_V { get; set; }          // Phase A grid voltage

    // === TEMPERATURA E STATO ===
    public double TemperaturaInterna_C { get; set; }    // Internal temperature
    public string StatoInverter { get; set; } = string.Empty; // descrizione stato
    public int StatoCodice { get; set; }                // raw status code SUN2000
}
