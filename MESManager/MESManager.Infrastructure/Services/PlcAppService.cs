using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using MESManager.Infrastructure.Data;

namespace MESManager.Infrastructure.Services;

public class PlcAppService : IPlcAppService
{
    private readonly MesManagerDbContext _context;

    public PlcAppService(MesManagerDbContext context)
    {
        _context = context;
    }

    public async Task<List<PlcRealtimeDto>> GetRealtimeDataAsync()
    {
        var query = await _context.PLCRealtime
            .Include(p => p.Macchina)
            .Include(p => p.Operatore)
            .OrderBy(p => p.Macchina.Codice)
            .ToListAsync();

        return query.Select(p => new PlcRealtimeDto
        {
            MacchinaId = p.MacchinaId,
            MacchinaNumero = p.Macchina.Codice,
            MacchianNome = p.Macchina.Nome,
            
            CicliFatti = p.CicliFatti,
            QuantitaDaProdurre = p.QuantitaDaProdurre,
            CicliScarti = p.CicliScarti,
            BarcodeLavorazione = p.BarcodeLavorazione,
            
            NumeroOperatore = p.Operatore?.NumeroOperatore,
            NomeOperatore = p.Operatore != null ? $"{p.Operatore.Nome} {p.Operatore.Cognome}" : null,
            
            TempoMedioRilevato = p.TempoMedioRilevato,
            TempoMedio = p.TempoMedio,
            Figure = p.Figure,
            
            StatoMacchina = p.StatoMacchina,
            QuantitaRaggiunta = p.QuantitaRaggiunta,
            
            UltimoAggiornamento = p.DataUltimoAggiornamento
        }).ToList();
    }

    public async Task<List<PlcStoricoDto>> GetStoricoAsync(Guid macchinaId, DateTime? from, DateTime? to)
    {
        var query = _context.PLCStorico
            .Include(p => p.Macchina)
            .Include(p => p.Operatore)
            .Where(p => p.MacchinaId == macchinaId);

        if (from.HasValue)
            query = query.Where(p => p.DataOra >= from.Value);

        if (to.HasValue)
            query = query.Where(p => p.DataOra <= to.Value);

        var result = await query
            .OrderByDescending(p => p.DataOra)
            .Take(1000)
            .ToListAsync();

        return result.Select(p => new PlcStoricoDto
        {
            Id = p.Id,
            MacchinaId = p.MacchinaId,
            MacchinaNumero = p.Macchina.Codice,
            MacchianNome = p.Macchina.Nome,
            
            Timestamp = p.DataOra,
            StatoMacchina = p.StatoMacchina ?? "Sconosciuto",
            
            NumeroOperatore = p.Operatore?.NumeroOperatore,
            NomeOperatore = p.Operatore != null ? $"{p.Operatore.Nome} {p.Operatore.Cognome}" : null,
            
            Dati = p.Dati
        }).ToList();
    }

    public async Task<List<EventoPLCDto>> GetEventiAsync(Guid macchinaId, DateTime? from, DateTime? to)
    {
        var query = _context.EventiPLC
            .Include(e => e.Macchina)
            .Where(e => e.MacchinaId == macchinaId);

        if (from.HasValue)
            query = query.Where(e => e.DataOra >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.DataOra <= to.Value);

        var result = await query
            .OrderByDescending(e => e.DataOra)
            .Take(500)
            .ToListAsync();

        return result.Select(e => new EventoPLCDto
        {
            Id = e.Id,
            MacchinaId = e.MacchinaId,
            MacchinaNumero = e.Macchina.Codice,
            MacchianNome = e.Macchina.Nome,
            
            Timestamp = e.DataOra,
            TipoEvento = e.TipoEvento,
            Dettagli = e.Dettagli
        }).ToList();
    }
}
