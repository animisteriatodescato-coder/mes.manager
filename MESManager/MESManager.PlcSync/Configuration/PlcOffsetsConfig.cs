namespace MESManager.PlcSync.Configuration;

public class PlcOffsetsConfig
{
    public int InizioSetup { get; set; } = 8;
    public int FineSetup { get; set; } = 10;
    public int NuovaProduzione { get; set; } = 12;
    public int FineProduzione { get; set; } = 14;
    public int QuantitaRaggiunta { get; set; } = 16;
    public int CicliFatti { get; set; } = 18;
    public int CicliScarti { get; set; } = 20;
    public int NumeroOperatore { get; set; } = 22;
    public int TempoMedioRil { get; set; } = 24;
    public int StatoEmergenza { get; set; } = 34;
    public int StatoManuale { get; set; } = 36;
    public int StatoAutomatico { get; set; } = 38;
    public int StatoCiclo { get; set; } = 40;
    public int StatoPezziRagg { get; set; } = 42;
    public int StatoAllarme { get; set; } = 44;
    public int BarcodeLavorazione { get; set; } = 46;
    public int QuantitaDaProd { get; set; } = 162;
    public int TempoMedio { get; set; } = 164;
    public int Figure { get; set; } = 170;
}
