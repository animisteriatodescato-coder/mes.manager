namespace MESManager.Application.DTOs;

/// <summary>
/// KPI aggregati per una macchina in un intervallo di tempo.
/// Generato da <see cref="IPlcAppService.GetKpiStoricoAsync"/> aggregando i segmenti PLCStorico.
/// Le proprietà Perc* sono calcolate e serializzate nel JSON per il client.
/// </summary>
public class PlcKpiStoricoDto
{
    public Guid MacchinaId { get; set; }
    public string MacchianaNome { get; set; } = string.Empty;

    /// <summary>Minuti totali coperti dai segmenti nel periodo richiesto.</summary>
    public double TotaleMinuti { get; set; }

    public double MinutiAutomatico { get; set; }
    public double MinutiAllarme    { get; set; }
    public double MinutiEmergenza  { get; set; }
    public double MinutiManuale    { get; set; }
    public double MinutiSetup      { get; set; }
    public double MinutiAltro      { get; set; }

    public double PercAutomatico => TotaleMinuti > 0 ? Math.Round(MinutiAutomatico / TotaleMinuti * 100, 1) : 0;
    public double PercAllarme    => TotaleMinuti > 0 ? Math.Round(MinutiAllarme    / TotaleMinuti * 100, 1) : 0;
    public double PercEmergenza  => TotaleMinuti > 0 ? Math.Round(MinutiEmergenza  / TotaleMinuti * 100, 1) : 0;
    public double PercManuale    => TotaleMinuti > 0 ? Math.Round(MinutiManuale    / TotaleMinuti * 100, 1) : 0;
    public double PercSetup      => TotaleMinuti > 0 ? Math.Round(MinutiSetup      / TotaleMinuti * 100, 1) : 0;
    public double PercAltro      => TotaleMinuti > 0 ? Math.Round(MinutiAltro      / TotaleMinuti * 100, 1) : 0;
}
