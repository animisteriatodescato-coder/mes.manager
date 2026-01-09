namespace PlcMagoSync.SYNC_MAGO.Models
{
    public class CommessaMago
    {
        public string SaleOrdId { get; set; } = "";           // ID tecnico Mago (PK join)
        public string InternalOrdNo { get; set; } = "";       // Numero commessa interno
        public string ExternalOrdNo { get; set; } = "";       // Numero ordine cliente
        public string Customer { get; set; } = "";            // Codice cliente
        public string CompanyName { get; set; } = "";         // Nome cliente (join MA_CustSupp)
        public string Delivered { get; set; } = "";           // 0=aperto, 1=chiuso
        public string ExpectedDeliveryDate { get; set; } = ""; // Data consegna attesa
        public string OurReference { get; set; } = "";        // Nostro riferimento
        public string YourReference { get; set; } = "";       // Loro riferimento
        public string Item { get; set; } = "";                // Codice articolo (da Details)
        public string Line { get; set; } = "";                // Numero linea (da Details)
        public string Description { get; set; } = "";         // Descrizione (da Details)
        public string Qty { get; set; } = "";                 // Quantità (da Details)
        public string UoM { get; set; } = "";                 // Unità di misura (da Details)
        public string TBModified { get; set; } = "";          // Ultima modifica
    }
}
