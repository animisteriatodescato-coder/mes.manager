using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MESManager.Infrastructure.Services;

public class OperatoreAppService : IOperatoreAppService
{
    private readonly MesManagerDbContext _context;

    public OperatoreAppService(MesManagerDbContext context)
    {
        _context = context;
    }

    public async Task<List<OperatoreDto>> GetAllAsync()
    {
        var operatori = await _context.Operatori
            .OrderBy(o => o.NumeroOperatore)
            .ToListAsync();

        var result = new List<OperatoreDto>();
        
        foreach (var o in operatori)
        {
            // Calcola ore lavorate dallo storico (ultimi 30 giorni)
            var dataInizio = DateTime.Now.AddDays(-30);
            var storicoCount = await _context.PLCStorico
                .Where(s => s.OperatoreId == o.Id && s.DataOra >= dataInizio)
                .CountAsync();
            
            // Stima: ogni record storico = circa 20 cicli, media 2 minuti/ciclo = 40 minuti
            // Questo è un calcolo approssimativo, potrebbe essere raffinato
            var oreLavorate = (storicoCount * 40.0) / 60.0;  // converti minuti in ore

            result.Add(new OperatoreDto
            {
                Id = o.Id,
                NumeroOperatore = o.NumeroOperatore ?? 0,
                Nome = o.Nome,
                Cognome = o.Cognome,
                Attivo = o.Attivo,
                DataAssunzione = o.DataAssunzione,
                DataLicenziamento = o.DataLicenziamento,
                OreLavorate = oreLavorate > 0 ? oreLavorate : null
            });
        }

        return result;
    }

    public async Task<OperatoreDto?> GetByIdAsync(Guid id)
    {
        var operatore = await _context.Operatori.FindAsync(id);
        if (operatore == null) return null;

        return new OperatoreDto
        {
            Id = operatore.Id,
            NumeroOperatore = operatore.NumeroOperatore ?? 0,
            Nome = operatore.Nome,
            Cognome = operatore.Cognome,
            Attivo = operatore.Attivo,
            DataAssunzione = operatore.DataAssunzione,
            DataLicenziamento = operatore.DataLicenziamento
        };
    }

    public async Task<OperatoreDto> CreateAsync(OperatoreDto dto)
    {
        // Verifica che il numero operatore non esista già
        var exists = await _context.Operatori.AnyAsync(o => o.NumeroOperatore == dto.NumeroOperatore);
        if (exists)
            throw new InvalidOperationException($"Operatore con numero {dto.NumeroOperatore} già esistente");

        var operatore = new Operatore
        {
            Id = Guid.NewGuid(),
            NumeroOperatore = dto.NumeroOperatore,
            Nome = dto.Nome,
            Cognome = dto.Cognome,
            Attivo = dto.Attivo,
            DataAssunzione = dto.DataAssunzione,
            DataLicenziamento = dto.DataLicenziamento
        };

        _context.Operatori.Add(operatore);
        await _context.SaveChangesAsync();

        dto.Id = operatore.Id;
        return dto;
    }

    public async Task<OperatoreDto> UpdateAsync(OperatoreDto dto)
    {
        var operatore = await _context.Operatori.FindAsync(dto.Id);
        if (operatore == null)
            throw new InvalidOperationException("Operatore non trovato");

        // Verifica che il numero operatore non sia già usato da un altro operatore
        var exists = await _context.Operatori.AnyAsync(o => o.NumeroOperatore == dto.NumeroOperatore && o.Id != dto.Id);
        if (exists)
            throw new InvalidOperationException($"Operatore con numero {dto.NumeroOperatore} già esistente");

        operatore.NumeroOperatore = dto.NumeroOperatore;
        operatore.Nome = dto.Nome;
        operatore.Cognome = dto.Cognome;
        operatore.Attivo = dto.Attivo;
        operatore.DataAssunzione = dto.DataAssunzione;
        operatore.DataLicenziamento = dto.DataLicenziamento;

        await _context.SaveChangesAsync();
        return dto;
    }

    public async Task DeleteAsync(Guid id)
    {
        var operatore = await _context.Operatori.FindAsync(id);
        if (operatore == null)
            throw new InvalidOperationException("Operatore non trovato");

        _context.Operatori.Remove(operatore);
        await _context.SaveChangesAsync();
    }
}
