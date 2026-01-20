using MESManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using MESManager.Application.Interfaces;
using MESManager.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace MESManager.Infrastructure.Repositories
{
    public class AnimeRepository : IAnimeRepository
    {
        private readonly MesManagerDbContext _context;
        private readonly ILogger<AnimeRepository> _logger;
        
        public AnimeRepository(MesManagerDbContext context, ILogger<AnimeRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Anime>> GetAllAsync()
        {
            var result = await _context.Anime
                .AsNoTracking()
                .ToListAsync();
            
            _logger.LogInformation("AnimeRepository.GetAllAsync returned {Count} records", result.Count);
            return result;
        }

        public async Task<Anime?> GetByIdAsync(int id)
        {
            var result = await _context.Anime.FindAsync(id);
            _logger.LogDebug("AnimeRepository.GetByIdAsync({Id}) returned {Found}", id, result != null ? "found" : "NOT FOUND");
            return result;
        }

        public async Task<Anime?> GetByCodiceArticoloAsync(string codiceArticolo)
        {
            var result = await _context.Anime
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.CodiceArticolo == codiceArticolo);
            
            _logger.LogDebug("AnimeRepository.GetByCodiceArticoloAsync({Codice}) returned {Found}", 
                codiceArticolo, result != null ? "found" : "NOT FOUND");
            return result;
        }

        public async Task AddAsync(Anime entity)
        {
            _context.Anime.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Anime entity)
        {
            _context.Anime.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.Anime.FindAsync(id);
            if (entity == null) return false;
            _context.Anime.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}