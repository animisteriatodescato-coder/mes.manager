
using System.Collections.Generic;
using System.Threading.Tasks;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;

namespace MESManager.Application.Services
{
    public class AnimeService : IAnimeService
    {
        private readonly IAnimeRepository _repo;
        public AnimeService(IAnimeRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<AnimeDto>> GetAllAsync()
        {
            var entities = await _repo.GetAllAsync();
            return entities.Select(x => new AnimeDto { Codice = x.Codice, Descrizione = x.Descrizione }).ToList();
        }

        public async Task<AnimeDto?> GetByIdAsync(int id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return entity == null ? null : new AnimeDto { Codice = entity.Codice, Descrizione = entity.Descrizione };
        }

        public async Task<AnimeDto> AddAsync(AnimeDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Codice))
                throw new ArgumentException("Codice obbligatorio");
            var entity = new Anime { Codice = dto.Codice, Descrizione = dto.Descrizione };
            await _repo.AddAsync(entity);
            return new AnimeDto { Codice = entity.Codice, Descrizione = entity.Descrizione };
        }

        public async Task<AnimeDto?> UpdateAsync(int id, AnimeDto dto)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return null;
            if (string.IsNullOrWhiteSpace(dto.Codice))
                throw new ArgumentException("Codice obbligatorio");
            entity.Codice = dto.Codice;
            entity.Descrizione = dto.Descrizione;
            await _repo.UpdateAsync(entity);
            return new AnimeDto { Codice = entity.Codice, Descrizione = entity.Descrizione };
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repo.DeleteAsync(id);
        }
    }
}