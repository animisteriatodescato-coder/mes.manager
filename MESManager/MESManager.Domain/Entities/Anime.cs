using System.ComponentModel.DataAnnotations;

namespace MESManager.Domain.Entities
{
    public class Anime
    {
        public int Id { get; set; }
        [Required]
        public string Codice { get; set; } = string.Empty;
        public string Descrizione { get; set; } = string.Empty;
    }
}