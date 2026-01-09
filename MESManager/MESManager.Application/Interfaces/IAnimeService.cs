using System.Collections.Generic;
using System.Threading.Tasks;
using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces
{
    public interface IAnimeService
    {
        Task<List<AnimeDto>> GetAllAsync();
        Task<AnimeDto?> GetByIdAsync(int id);
        Task<AnimeDto> AddAsync(AnimeDto dto);
        Task<AnimeDto?> UpdateAsync(int id, AnimeDto dto);
        Task<bool> DeleteAsync(int id);
    }
}