using System.ComponentModel.DataAnnotations;

namespace MESManager.Domain.Entities
{
    /// <summary>
    /// Allegato associato a un articolo (foto o documento).
    /// Importato da Gantt.dbo.Allegati e/o creato localmente.
    /// </summary>
    public class AllegatoArticolo
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Tipo archivio (es: "ARTICO" per articoli)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Archivio { get; set; } = "ARTICO";
        
        /// <summary>
        /// ID dell'articolo nel sistema (corrisponde a IdArticolo dell'anima)
        /// </summary>
        public int? IdArchivio { get; set; }
        
        /// <summary>
        /// Codice articolo per ricerca rapida (es: "302558", "0616AB")
        /// </summary>
        [MaxLength(50)]
        public string? CodiceArticolo { get; set; }
        
        /// <summary>
        /// Path completo del file su disco
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string PathFile { get; set; } = string.Empty;
        
        /// <summary>
        /// Nome file originale
        /// </summary>
        [MaxLength(255)]
        public string? NomeFile { get; set; }
        
        /// <summary>
        /// Descrizione dell'allegato
        /// </summary>
        [MaxLength(500)]
        public string? Descrizione { get; set; }
        
        /// <summary>
        /// Priorità per ordinamento (1 = primo)
        /// </summary>
        public int Priorita { get; set; } = 1;
        
        /// <summary>
        /// Tipo file: "FOTO" o "DOCUMENTO"
        /// </summary>
        [MaxLength(20)]
        public string TipoFile { get; set; } = "FOTO";
        
        /// <summary>
        /// Estensione file (es: ".jpg", ".pdf")
        /// </summary>
        [MaxLength(10)]
        public string? Estensione { get; set; }
        
        /// <summary>
        /// Dimensione file in bytes
        /// </summary>
        public long? DimensioneBytes { get; set; }
        
        /// <summary>
        /// Data importazione da Gantt (null se creato localmente)
        /// </summary>
        public DateTime? DataImportazione { get; set; }
        
        /// <summary>
        /// Data creazione record
        /// </summary>
        public DateTime DataCreazione { get; set; } = DateTime.Now;
        
        /// <summary>
        /// True se importato da Gantt, false se creato localmente
        /// </summary>
        public bool ImportatoDaGantt { get; set; }
        
        /// <summary>
        /// ID originale in Gantt.Allegati (per evitare duplicati in reimport)
        /// </summary>
        public int? IdGanttOriginale { get; set; }
    }
}
