using MESManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using MESManager.Application.Interfaces;
using MESManager.Infrastructure.Data;

namespace MESManager.Infrastructure.Repositories
{
    public class AnimeRepository : IAnimeRepository
    {
        private readonly MesManagerDbContext _context;
        public AnimeRepository(MesManagerDbContext context)
        {
            _context = context;
        }

        public async Task<List<Anime>> GetAllAsync()
        {
            return await _context.Anime.ToListAsync();
        }

        public async Task<Anime?> GetByIdAsync(int id)
        {
            return await _context.Anime.FindAsync(id);
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