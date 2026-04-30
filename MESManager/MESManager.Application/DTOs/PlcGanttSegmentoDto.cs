namespace MESManager.Application.DTOs;

/// <summary>
/// Rappresenta un segmento temporale in cui una macchina ha mantenuto uno stato stabile.
/// Generato da <see cref="IPlcAppService.GetGanttStoricoAsync"/> segmentando i record PLCStorico.
/// Il campo <see cref="Colore"/> viene popolato dal controller (MesDesignTokens.PlcStatoColore).
/// </summary>
public class PlcGanttSegmentoDto
{
    public Guid MacchinaId { get; set; }
    public string MacchianaNome { get; set; } = string.Empty;

    public DateTime Inizio { get; set; }
    public DateTime Fine { get; set; }

    public string StatoMacchina { get; set; } = string.Empty;

    /// <summary>Colore esadecimale (#RRGGBB) calcolato dal controller via MesDesignTokens.PlcStatoColore().</summary>
    public string Colore { get; set; } = "#9E9E9E";

    public string? NomeOperatore { get; set; }
    public int? NumeroOperatore { get; set; }

    public int CicliFatti { get; set; }
    public int BarcodeLavorazione { get; set; }

    /// <summary>Tempo ciclo medio rilevato dal PLC (secondi). 0 se non disponibile.</summary>
    public int TempoMedioRilevato { get; set; }

    /// <summary>Pezzi prodotti SOLO in questo segmento (Δ rispetto al record precedente). Usato per calcolo tempo ciclo preciso nel tooltip.</summary>
    public int CicliFattiDelta { get; set; }

    /// <summary>Pezzi per ciclo macchina (da DB56 runtime). Divide CicliFattiDelta per ottenere i cicli macchina reali.</summary>
    public int Figure { get; set; }

    /// <summary>Durata in minuti — calcolato da Inizio/Fine, serializzato nel JSON per il client JS.</summary>
    public double DurataMinuti => (Fine - Inizio).TotalMinutes;
}
