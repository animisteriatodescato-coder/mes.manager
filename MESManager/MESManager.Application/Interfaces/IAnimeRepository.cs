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
    }
}