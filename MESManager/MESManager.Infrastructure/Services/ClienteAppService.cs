using Microsoft.EntityFrameworkCore;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;

namespace MESManager.Infrastructure.Services;

public class ClienteAppService : IClienteAppService
{
    private readonly MesManagerDbContext _context;
    
    public ClienteAppService(MesManagerDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<ClienteDto>> GetListaAsync()
    {
        return await _context.Clienti
            .Select(c => new ClienteDto
            {
                Id = c.Id,
                Codice = c.Codice,
                RagioneSociale = c.RagioneSociale,
                Email = c.Email,
                Note = c.Note,
                Attivo = c.Attivo,
                UltimaModifica = c.UltimaModifica,
                TimestampSync = c.TimestampSync
            })
            .ToListAsync();
    }
    
    public async Task<ClienteDto?> GetByIdAsync(Guid id)
    {
        var cliente = await _context.Clienti.FindAsync(id);
        if (cliente == null) return null;
        
        return new ClienteDto
        {
            Id = cliente.Id,
            Codice = cliente.Codice,
            RagioneSociale = cliente.RagioneSociale
        };
    }
    
    public async Task<ClienteDto> CreaAsync(ClienteDto dto)
    {
        var cliente = new Cliente
        {
            Codice = dto.Codice,
            RagioneSociale = dto.RagioneSociale
        };
        
        _context.Clienti.Add(cliente);
        await _context.SaveChangesAsync();
        
        return new ClienteDto
        {
            Id = cliente.Id,
            Codice = cliente.Codice,
            RagioneSociale = cliente.RagioneSociale
        };
    }
    
    public async Task<ClienteDto> AggiornaAsync(Guid id, ClienteDto dto)
    {
        var cliente = await _context.Clienti.FindAsync(id);
        if (cliente == null) throw new Exception("Cliente non trovato");
        
        cliente.Codice = dto.Codice;
        cliente.RagioneSociale = dto.RagioneSociale;
        
        await _context.SaveChangesAsync();
        
        return new ClienteDto
        {
            Id = cliente.Id,
            Codice = cliente.Codice,
            RagioneSociale = cliente.RagioneSociale
        };
    }
    
    public async Task EliminaAsync(Guid id)
    {
        var cliente = await _context.Clienti.FindAsync(id);
        if (cliente == null) throw new Exception("Cliente non trovato");
        
        _context.Clienti.Remove(cliente);
        await _context.SaveChangesAsync();
    }
}
