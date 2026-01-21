namespace MESManager.Application.DTOs
{
    public class AllegatoAnimaDto
    {
        public int Id { get; set; }
        public string Archivio { get; set; } = string.Empty;
        public int IdArchivio { get; set; }
        public string PathCompleto { get; set; } = string.Empty;
        public string NomeFile { get; set; } = string.Empty;
        public string? Descrizione { get; set; }
        public int Priorita { get; set; }
        public string Estensione { get; set; } = string.Empty;
        public bool IsFoto { get; set; }
        public string UrlProxy { get; set; } = string.Empty;
    }

    public class AllegatiAnimaResponse
    {
        public List<AllegatoAnimaDto> Foto { get; set; } = new();
        public List<AllegatoAnimaDto> Documenti { get; set; } = new();
        public int TotaleFoto => Foto.Count;
        public int TotaleDocumenti => Documenti.Count;
    }
}
