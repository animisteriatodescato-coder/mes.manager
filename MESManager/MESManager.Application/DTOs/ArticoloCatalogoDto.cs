namespace MESManager.Application.DTOs
{
    public class ArticoloCatalogoDto
    {
        public int IdArticolo { get; set; }
        public string CodiceArticolo { get; set; }
        public string DescrizioneArticolo { get; set; }
        public DateTime? DataModificaRecord { get; set; }
        public string UnitaMisura { get; set; }
        public int? Larghezza { get; set; }
        public int? Altezza { get; set; }
        public int? Profondita { get; set; }
        public int? Imballo { get; set; }
        // Aggiungi altre proprietà se necessario
    }
}
