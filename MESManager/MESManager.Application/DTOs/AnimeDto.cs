namespace MESManager.Application.DTOs
{
    public class AnimeDto
    {
        public int Id { get; set; }
        public string CodiceArticolo { get; set; } = string.Empty;
        public string DescrizioneArticolo { get; set; } = string.Empty;
        public DateTime? DataModificaRecord { get; set; }
        public string? UtenteModificaRecord { get; set; }
        public string? UnitaMisura { get; set; }
        public int? Larghezza { get; set; }
        public int? Altezza { get; set; }
        public int? Profondita { get; set; }
        public int? Imballo { get; set; }
        public string? Note { get; set; }
        public string? Allegato { get; set; }
        public string? Peso { get; set; }
        public string? Ubicazione { get; set; }
        public string? Ciclo { get; set; }
        public string? CodiceCassa { get; set; }
        public string? CodiceAnime { get; set; }
        public int? IdArticolo { get; set; }
        public string? MacchineSuDisponibili { get; set; }
        public bool TrasmettiTutto { get; set; }
        public string? Colla { get; set; }
        public string? Sabbia { get; set; }
        public string? Vernice { get; set; }
        public string? Cliente { get; set; }
        public string? TogliereSparo { get; set; }
        public int? QuantitaPiano { get; set; }
        public int? NumeroPiani { get; set; }
        public string? Figure { get; set; }
        public string? Maschere { get; set; }
        public string? Incollata { get; set; }
        public string? Assemblata { get; set; }
        public string? ArmataL { get; set; }
        public DateTime DataImportazione { get; set; }
        
        // Tracking modifiche locali
        public bool ModificatoLocalmente { get; set; }
        public DateTime? DataUltimaModificaLocale { get; set; }
        public string? UtenteUltimaModificaLocale { get; set; }
    }
}
