using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;
using MESManager.Domain.Enums;
using MESManager.Infrastructure.Data;

namespace MESManager.Infrastructure.Services;

public class ManutenzioneCassaService : IManutenzioneCassaService
{
    private readonly MesManagerDbContext _db;
    private readonly ILogger<ManutenzioneCassaService> _logger;

    public ManutenzioneCassaService(MesManagerDbContext db, ILogger<ManutenzioneCassaService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ──────────────────────────────────────────────────────────
    // CATALOGO ATTIVITÀ
    // ──────────────────────────────────────────────────────────

    public async Task<List<ManutenzioneCassaAttivitaDto>> GetAttivitaAsync()
        => await _db.ManutenzioneCassaAttivita
            .Where(a => a.Attiva)
            .OrderBy(a => a.Ordine)
            .Select(a => MapAttivita(a))
            .ToListAsync();

    public async Task<ManutenzioneCassaAttivitaDto> CreateAttivitaAsync(ManutenzioneCassaAttivitaDto dto)
    {
        var entity = new ManutenzioneCassaAttivita
        {
            Id = Guid.NewGuid(),
            Nome = dto.Nome,
            Ordine = dto.Ordine,
            Attiva = true,
            FontSize = dto.FontSize > 0 ? dto.FontSize : 11
        };
        _db.ManutenzioneCassaAttivita.Add(entity);
        await _db.SaveChangesAsync();
        _logger.LogInformation("[MANUT-CASSA] Attività creata: {Nome}", entity.Nome);
        return MapAttivita(entity);
    }

    public async Task<ManutenzioneCassaAttivitaDto?> UpdateAttivitaAsync(ManutenzioneCassaAttivitaDto dto)
    {
        var entity = await _db.ManutenzioneCassaAttivita.FindAsync(dto.Id);
        if (entity == null) return null;
        entity.Nome = dto.Nome;
        entity.Ordine = dto.Ordine;
        entity.Attiva = dto.Attiva;
        entity.FontSize = dto.FontSize > 0 ? dto.FontSize : 11;
        await _db.SaveChangesAsync();
        return MapAttivita(entity);
    }

    public async Task<bool> DeleteAttivitaAsync(Guid id)
    {
        var entity = await _db.ManutenzioneCassaAttivita.FindAsync(id);
        if (entity == null) return false;
        entity.Attiva = false; // soft-delete
        await _db.SaveChangesAsync();
        return true;
    }

    // ──────────────────────────────────────────────────────────
    // SCHEDE
    // ──────────────────────────────────────────────────────────

    public async Task<List<ManutenzioneCassaSchedaDto>> GetSchedeAsync(
        string? codiceCassa = null,
        DateTime? dal = null,
        DateTime? al = null)
    {
        var query = _db.ManutenzioneCasseSchede
            .Include(s => s.Righe).ThenInclude(r => r.Attivita)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(codiceCassa))
            query = query.Where(s => s.CodiceCassa == codiceCassa);
        if (dal.HasValue)
            query = query.Where(s => s.DataEsecuzione >= dal.Value);
        if (al.HasValue)
            query = query.Where(s => s.DataEsecuzione <= al.Value);

        var list = await query.OrderByDescending(s => s.DataEsecuzione).ToListAsync();
        return list.Select(MapScheda).ToList();
    }

    public async Task<ManutenzioneCassaSchedaDto?> GetSchedaByIdAsync(Guid id)
    {
        var entity = await _db.ManutenzioneCasseSchede
            .Include(s => s.Righe).ThenInclude(r => r.Attivita)
            .FirstOrDefaultAsync(s => s.Id == id);
        return entity == null ? null : MapScheda(entity);
    }

    public async Task<ManutenzioneCassaSchedaDto> GetOrCreateSchedaAsync(
        string codiceCassa,
        DateTime data,
        string? operatoreId,
        string? nomeOperatore)
    {
        var existing = await _db.ManutenzioneCasseSchede
            .Include(s => s.Righe).ThenInclude(r => r.Attivita)
            .FirstOrDefaultAsync(s => s.CodiceCassa == codiceCassa
                && s.DataEsecuzione.Date == data.Date);

        if (existing != null)
        {
            // Se la scheda è ancora in compilazione, aggiorna l'operatore con chi la sta aprendo ora
            if (existing.Stato == StatoSchedaManutenzione.InCompilazione
                && !string.IsNullOrEmpty(nomeOperatore)
                && existing.NomeOperatore != nomeOperatore)
            {
                existing.OperatoreId = operatoreId;
                existing.NomeOperatore = nomeOperatore;
                await _db.SaveChangesAsync();
            }
            return MapScheda(existing);
        }

        return await CreateSchedaInternalAsync(new NuovaSchedaCassaRequest
        {
            CodiceCassa = codiceCassa,
            DataEsecuzione = data.Date,
            OperatoreId = operatoreId,
            NomeOperatore = nomeOperatore
        });
    }

    private async Task<ManutenzioneCassaSchedaDto> CreateSchedaInternalAsync(NuovaSchedaCassaRequest request)
    {
        var attivita = await _db.ManutenzioneCassaAttivita
            .Where(a => a.Attiva)
            .OrderBy(a => a.Ordine)
            .ToListAsync();

        var scheda = new ManutenzioneCassaScheda
        {
            Id = Guid.NewGuid(),
            CodiceCassa = request.CodiceCassa,
            DataEsecuzione = request.DataEsecuzione,
            OperatoreId = request.OperatoreId,
            NomeOperatore = request.NomeOperatore,
            Note = request.Note,
            Stato = StatoSchedaManutenzione.InCompilazione,
            Righe = attivita.Select(a => new ManutenzioneCassaRiga
            {
                Id = Guid.NewGuid(),
                AttivitaId = a.Id,
                Esito = EsitoAttivitaManutenzione.NonEseguita
            }).ToList()
        };

        _db.ManutenzioneCasseSchede.Add(scheda);
        await _db.SaveChangesAsync();

        _logger.LogInformation("[MANUT-CASSA] Scheda creata: {Id} cassa={CodiceCassa} ({NRighe} righe)",
            scheda.Id, scheda.CodiceCassa, scheda.Righe.Count);

        return (await GetSchedaByIdAsync(scheda.Id))!;
    }

    public async Task<ManutenzioneCassaSchedaDto?> ChiudiSchedaAsync(Guid id)
    {
        var scheda = await _db.ManutenzioneCasseSchede
            .Include(s => s.Righe)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (scheda == null) return null;

        var haAnomalie = scheda.Righe.Any(r => r.Esito == EsitoAttivitaManutenzione.Anomalia);
        scheda.Stato = haAnomalie ? StatoSchedaManutenzione.ConAnomalie : StatoSchedaManutenzione.Completata;
        scheda.DataChiusura = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("[MANUT-CASSA] Scheda chiusa: {Id} stato={Stato}", id, scheda.Stato);
        return await GetSchedaByIdAsync(id);
    }

    // ──────────────────────────────────────────────────────────
    // RIGHE
    // ──────────────────────────────────────────────────────────

    public async Task<ManutenzioneCassaSchedaDto?> UpdateRigaAsync(Guid schedaId, ManutenzioneCassaRigaDto dto)
    {
        var riga = await _db.ManutenzioneCasseRighe.FindAsync(dto.Id);
        if (riga == null) return null;

        riga.Esito = dto.Esito;
        riga.Commento = dto.Commento;
        await _db.SaveChangesAsync();

        return await GetSchedaByIdAsync(schedaId);
    }

    // ──────────────────────────────────────────────────────────
    // CASSE DISPONIBILI
    // ──────────────────────────────────────────────────────────

    public async Task<List<string>> GetCasseDisponibiliAsync()
        => await _db.Anime
            .Where(a => !string.IsNullOrEmpty(a.CodiceCassa))
            .Select(a => a.CodiceCassa!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

    // ──────────────────────────────────────────────────────────
    // SEED DATI INIZIALI
    // ──────────────────────────────────────────────────────────

    public async Task SeedAttivitaDefaultAsync()
    {
        if (await _db.ManutenzioneCassaAttivita.AnyAsync()) return;

        var attivita = new List<ManutenzioneCassaAttivita>
        {
            new() { Id = Guid.NewGuid(), Nome = "Pulizia generale cassa",                Ordine = 1, Attiva = true },
            new() { Id = Guid.NewGuid(), Nome = "Verifica usura superfici a contatto",   Ordine = 2, Attiva = true },
            new() { Id = Guid.NewGuid(), Nome = "Controllo tenuta / guarnizioni",        Ordine = 3, Attiva = true },
            new() { Id = Guid.NewGuid(), Nome = "Lubrificazione cerniere e perni",       Ordine = 4, Attiva = true },
            new() { Id = Guid.NewGuid(), Nome = "Ispezione cricche e crepe",             Ordine = 5, Attiva = true },
            new() { Id = Guid.NewGuid(), Nome = "Verifica sistema di espulsione",        Ordine = 6, Attiva = true },
            new() { Id = Guid.NewGuid(), Nome = "Controllo stato piani e contropiani",  Ordine = 7, Attiva = true },
            new() { Id = Guid.NewGuid(), Nome = "Verifica inserti / inserts",            Ordine = 8, Attiva = true },
        };

        _db.ManutenzioneCassaAttivita.AddRange(attivita);
        await _db.SaveChangesAsync();
        _logger.LogInformation("[MANUT-CASSA] Seed attività default completato: {Count} attività inserite", attivita.Count);
    }

    // ──────────────────────────────────────────────────────────
    // MAPPING PRIVATI
    // ──────────────────────────────────────────────────────────

    private static ManutenzioneCassaAttivitaDto MapAttivita(ManutenzioneCassaAttivita a) => new()
    {
        Id = a.Id,
        Nome = a.Nome,
        Ordine = a.Ordine,
        Attiva = a.Attiva,
        FontSize = a.FontSize
    };

    private static ManutenzioneCassaSchedaDto MapScheda(ManutenzioneCassaScheda s) => new()
    {
        Id = s.Id,
        CodiceCassa = s.CodiceCassa,
        DataEsecuzione = s.DataEsecuzione,
        OperatoreId = s.OperatoreId,
        NomeOperatore = s.NomeOperatore,
        Note = s.Note,
        Stato = s.Stato,
        DataChiusura = s.DataChiusura,
        Righe = s.Righe
            .OrderBy(r => r.Attivita?.Ordine ?? 0)
            .Select(r => new ManutenzioneCassaRigaDto
            {
                Id = r.Id,
                SchedaId = r.SchedaId,
                AttivitaId = r.AttivitaId,
                NomeAttivita = r.Attivita?.Nome ?? string.Empty,
                OrdineAttivita = r.Attivita?.Ordine ?? 0,
                Esito = r.Esito,
                Commento = r.Commento
            }).ToList()
    };
}
