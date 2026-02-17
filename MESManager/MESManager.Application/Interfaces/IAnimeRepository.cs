using System.Collections.Generic;
using System.Threading.Tasks;
using MESManager.Domain.Entities;

namespace MESManager.Application.Interfaces
{
    public interface IAnimeRepository
    {
        Task<List<Anime>> GetAllAsync();
        Task<Anime?> GetByIdAsync(int id);
        Task<Anime?> GetByCodiceArticoloAsync(string codiceArticolo);
        Task AddAsync(Anime entity);
        Task UpdateAsync(Anime entity);
        Task<bool> DeleteAsync(int id);
        
        // Nuovo metodo per informazioni ricetta aggregate
        Task<Dictionary<string, RicettaInfo>> GetRicetteInfoByCodiceArticoloAsync(List<string> codiciArticolo);
    }
    
    // DTO per info ricetta
    public class RicettaInfo
    {
        public int NumeroParametri { get; set; }
        public DateTime? UltimaModifica { get; set; }
    }
}