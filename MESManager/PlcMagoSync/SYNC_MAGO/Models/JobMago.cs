namespace PlcMagoSync.SYNC_MAGO.Models
{
    public class JobMago
    {
        public string Commessa { get; set; } = "";
        public string Descrizione { get; set; } = "";
        public string Cliente { get; set; } = "";
        public string DataApertura { get; set; } = "";
        public string DataChiusuraPrevista { get; set; } = "";
        public string Stato { get; set; } = "";
        public string Quantita { get; set; } = "";
        public string QuantitaEvasa { get; set; } = "";
        public string QuantitaRimanente { get; set; } = "";
        public string UltimaModifica { get; set; } = "";
    }
}
