using MESManager.Domain.Constants;

namespace MESManager.PlcSync.Configuration;

public class PlcOffsetsConfig
{
    // === CAMPI PRODUZIONE (uso corrente) ===
    public int NumeroMacchina { get; set; } = 0;
    public int ComunicazioneAbilitata { get; set; } = 2;
    public int ProntoRicevereNuoviDati { get; set; } = 4;
    public int DatiRicevuti { get; set; } = 6;
    public int InizioSetup { get; set; } = PlcConstants.Offsets.Fields.InizioSetup;
    public int FineSetup { get; set; } = PlcConstants.Offsets.Fields.FineSetup;
    public int NuovaProduzione { get; set; } = PlcConstants.Offsets.Fields.NuovaProduzione;
    public int FineProduzione { get; set; } = PlcConstants.Offsets.Fields.FineProduzione;
    public int QuantitaRaggiunta { get; set; } = PlcConstants.Offsets.Fields.QuantitaRaggiunta;
    public int CicliFatti { get; set; } = PlcConstants.Offsets.Fields.CicliFatti;
    public int CicliScarti { get; set; } = PlcConstants.Offsets.Fields.CicliScarti;
    public int NumeroOperatore { get; set; } = PlcConstants.Offsets.Fields.NumeroOperatore;
    public int TempoMedioRil { get; set; } = PlcConstants.Offsets.Fields.TempoMedioRilevato;
    public int ProduzioneInRitardo { get; set; } = 26;
    public int ProduzioneInAnticipo { get; set; } = 28;
    public int ProduzioneInLineaConTempi { get; set; } = 30;
    public int RegistroWatchDog { get; set; } = 32;
    public int StatoEmergenza { get; set; } = PlcConstants.Offsets.Fields.StatoEmergenza;
    public int StatoManuale { get; set; } = PlcConstants.Offsets.Fields.StatoManuale;
    public int StatoAutomatico { get; set; } = PlcConstants.Offsets.Fields.StatoAutomatico;
    public int StatoCiclo { get; set; } = PlcConstants.Offsets.Fields.StatoCiclo;
    public int StatoPezziRagg { get; set; } = PlcConstants.Offsets.Fields.StatoPezziRaggiunti;
    public int StatoAllarme { get; set; } = PlcConstants.Offsets.Fields.StatoAllarme;
    public int BarcodeLavorazione { get; set; } = PlcConstants.Offsets.Fields.BarcodeLavorazione;
    
    // === CAMPI RICETTE (uso futuro) ===
    public int StatoProduzione { get; set; } = 98;
    public int NumeroRicetta { get; set; } = 100;
    public int AbilitazionePrimaPulitura { get; set; } = 102;
    public int AbilitazioneSecondaPulitura { get; set; } = 104;
    public int TempoPulitoreAvanti { get; set; } = 106;
    public int TempoRitardoSecondaPulitura { get; set; } = 108;
    public int TempoSecondaPulitura { get; set; } = 110;
    public int AbilitazioneNastroSalitaDiscesa { get; set; } = 112;
    public int AbilitazioneNastroIndietro { get; set; } = 114;
    public int TempoNastroAvanti { get; set; } = 116;
    public int TempoRitardoNastroIndietro { get; set; } = 118;
    public int TempoNastroIndietro { get; set; } = 120;
    public int TempoRitardoSparo { get; set; } = 122;
    public int TempoSparo { get; set; } = 124;
    public int TempoInvestimento { get; set; } = 126;
    public int TempoCottura { get; set; } = 128;
    public int FrequenzaCariche { get; set; } = 130;
    public int TempoMandata { get; set; } = 132;
    public int TempoScaricoMandata { get; set; } = 134;
    public int TempoSerbatoioChiudi { get; set; } = 136;
    public int TempoRitardoDiscesaSerbatoio { get; set; } = 138;
    public int RitardoEstrattoreLatoMobile { get; set; } = 140;
    public int TempoEstrattoreLatoMobile { get; set; } = 142;
    public int RitardoEstrattoreLatoFisso { get; set; } = 144;
    public int TempoEstrattoreLatoFisso { get; set; } = 146;
    public int TempoChiusuraPannello { get; set; } = 148;
    public int AbilitazioneMaschio { get; set; } = 150;
    public int TempoMaschio { get; set; } = 152;
    public int RitardoChiusuraMaschio { get; set; } = 154;
    public int RitardoAperturaMaschio { get; set; } = 156;
    public int RitardoRestartCiclo { get; set; } = 158;
    public int SaleOrdId { get; set; } = 160;
    public int QuantitaDaProd { get; set; } = PlcConstants.Offsets.Fields.QuantitaDaProdurre;
    public int TempoMedio { get; set; } = PlcConstants.Offsets.Fields.TempoMedio;
    public int TempoRallentamentoChiusuraPannello { get; set; } = 166;
    public int AbilitazioneSparoLaterale { get; set; } = 168;
    public int Figure { get; set; } = PlcConstants.Offsets.Fields.Figure;
    public int RitardoCaricoSabbia { get; set; } = 172;
    public int NastroAlto { get; set; } = 174;
    public int NastroBasso { get; set; } = 176;
    public int QuotaPannelloChiuso { get; set; } = 178;
    public int QuotaRallentamentoChiusura { get; set; } = 180;
    public int QuotaDisaccoppiamento { get; set; } = 182;
    public int QuotaRallentamentoApertura { get; set; } = 184;
    public int QuotaPannelloAperto { get; set; } = 186;
    public int PressioneSparo { get; set; } = 188;
    public int TempoCaricoSabbiaSuperiore { get; set; } = 190;
    public int AbilitaPulitoreSuperiore { get; set; } = 192;
    public int AbilitaSparoSuperiore { get; set; } = 194;
    public int TempoDiscesaTesta { get; set; } = 196;
}
