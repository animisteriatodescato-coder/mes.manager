using Microsoft.EntityFrameworkCore;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;
using MESManager.Domain.Enums;
using MESManager.Infrastructure.Data;

namespace MESManager.Infrastructure.Services;

public class CommessaAppService : ICommessaAppService
{
    private readonly MesManagerDbContext _context;
    
    public CommessaAppService(MesManagerDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<CommessaDto>> GetListaAsync()
    {
        return await _context.Commesse
            .Include(c => c.Articolo)
            .Include(c => c.Cliente)
            .Select(c => new CommessaDto
            {
                Id = c.Id,
                Codice = c.Codice,
                ArticoloId = c.ArticoloId,
                ClienteId = c.ClienteId,
                QuantitaRichiesta = c.QuantitaRichiesta,
                DataConsegna = c.DataConsegna,
                Stato = c.Stato.ToString(),
                RiferimentoOrdineCliente = c.RiferimentoOrdineCliente,
                UltimaModifica = c.UltimaModifica,
                TimestampSync = c.TimestampSync
            })
            .ToListAsync();
    }
    
    public async Task<CommessaDto?> GetByIdAsync(Guid id)
    {
        var commessa = await _context.Commesse
            .Include(c => c.Articolo)
            .Include(c => c.Cliente)
            .FirstOrDefaultAsync(c => c.Id == id);
            
        if (commessa == null) return null;
        
        return new CommessaDto
        {
            Id = commessa.Id,
            Codice = commessa.Codice,
            ArticoloId = commessa.ArticoloId,
            ClienteId = commessa.ClienteId,
            QuantitaRichiesta = commessa.QuantitaRichiesta
        };
    }
    
    public async Task<CommessaDto> CreaAsync(CommessaDto dto)
    {
        var commessa = new Commessa
        {
            Codice = dto.Codice,
            ArticoloId = dto.ArticoloId,
            ClienteId = dto.ClienteId,
            QuantitaRichiesta = dto.QuantitaRichiesta,
            Stato = StatoCommessa.Aperta
        };
        
        _context.Commesse.Add(commessa);
        await _context.SaveChangesAsync();
        
        return new CommessaDto
        {
            Id = commessa.Id,
            Codice = commessa.Codice,
            ArticoloId = commessa.ArticoloId,
            ClienteId = commessa.ClienteId,
            QuantitaRichiesta = commessa.QuantitaRichiesta
        };
    }
    
    public async Task<CommessaDto> AggiornaAsync(Guid id, CommessaDto dto)
    {
        var commessa = await _context.Commesse.FindAsync(id);
        if (commessa == null) throw new Exception("Commessa non trovata");
        
        commessa.Codice = dto.Codice;
        commessa.ArticoloId = dto.ArticoloId;
        commessa.ClienteId = dto.ClienteId;
        commessa.QuantitaRichiesta = dto.QuantitaRichiesta;
        
        await _context.SaveChangesAsync();
        
        return new CommessaDto
        {
            Id = commessa.Id,
            Codice = commessa.Codice,
            ArticoloId = commessa.ArticoloId,
            ClienteId = commessa.ClienteId,
            QuantitaRichiesta = commessa.QuantitaRichiesta
        };
    }
    
    public async Task EliminaAsync(Guid id)
    {
        var commessa = await _context.Commesse.FindAsync(id);
        if (commessa == null) throw new Exception("Commessa non trovata");
        
        _context.Commesse.Remove(commessa);
        await _context.SaveChangesAsync();
    }
}
