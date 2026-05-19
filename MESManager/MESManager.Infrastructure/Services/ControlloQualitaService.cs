using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;
using MESManager.Domain.Enums;
using MESManager.Infrastructure.Data;

namespace MESManager.Infrastructure.Services;

public class ControlloQualitaService : IControlloQualitaService
{
    private readonly IDbContextFactory<MesManagerDbContext> _dbFactory;
    private readonly ILogger<ControlloQualitaService> _logger;

    public ControlloQualitaService(
        IDbContextFactory<MesManagerDbContext> dbFactory,
        ILogger<ControlloQualitaService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<List<ControlloQualitaAttivitaDto>> GetAttivitaAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.ControlloQualitaAttivita
            .Where(a => a.Attiva)
            .OrderBy(a => a.Ordine)
            .Select(a => MapAttivita(a))
            .ToListAsync();
    }

    public async Task<List<ControlloQualitaSchedaDto>> GetSchedeAsync(Guid? macchinaId = null, DateTime? dal = null, DateTime? al = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var query = db.ControlloQualitaSchede
            .Include(s => s.Macchina)
            .Include(s => s.Righe).ThenInclude(r => r.Attivita)
            .AsNoTracking()
            .AsQueryable();

        if (macchinaId.HasValue) query = query.Where(s => s.MacchinaId == macchinaId.Value);
        if (dal.HasValue) query = query.Where(s => s.DataEsecuzione >= dal.Value);
        if (al.HasValue) query = query.Where(s => s.DataEsecuzione <= al.Value);

        var list = await query.OrderByDescending(s => s.DataEsecuzione).ToListAsync();
        return list.Select(MapScheda).ToList();
    }

    public async Task<ControlloQualitaSchedaDto?> GetSchedaByIdAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entity = await db.ControlloQualitaSchede
            .Include(s => s.Macchina)
            .Include(s => s.Righe).ThenInclude(r => r.Attivita)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);

        return entity == null ? null : MapScheda(entity);
    }

    public async Task<ControlloQualitaSchedaDto> CreateSchedaAsync(NuovaSchedaControlloQualitaRequest request)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var macchinaCodice = await db.Macchine
            .Where(m => m.Id == request.MacchinaId)
            .Select(m => m.Codice)
            .FirstOrDefaultAsync() ?? string.Empty;

        var attivita = await db.ControlloQualitaAttivita
            .Where(a => a.Attiva && (a.MacchinaCodiceFiltro == null || a.MacchinaCodiceFiltro == macchinaCodice))
            .OrderBy(a => a.Ordine)
            .ToListAsync();

        var scheda = new ControlloQualitaScheda
        {
            Id = Guid.NewGuid(),
            MacchinaId = request.MacchinaId,
            DataEsecuzione = request.DataEsecuzione.Date,
            OperatoreId = request.OperatoreId,
            NomeOperatore = request.NomeOperatore,
            Note = request.Note,
            Stato = StatoSchedaControlloQualita.InCompilazione,
            Righe = attivita.Select(a => new ControlloQualitaRiga
            {
                Id = Guid.NewGuid(),
                AttivitaId = a.Id,
                Esito = EsitoControlloQualita.NonEseguito
            }).ToList()
        };

        db.ControlloQualitaSchede.Add(scheda);
        await db.SaveChangesAsync();

        _logger.LogInformation("[QUALITA] Scheda controllo creata: {Id} macchina={MacchinaId} ({NRighe} righe)",
            scheda.Id, scheda.MacchinaId, scheda.Righe.Count);

        return (await GetSchedaByIdAsync(scheda.Id))!;
    }

    public async Task<ControlloQualitaSchedaDto> GetOrCreateSchedaAsync(Guid macchinaId, DateTime data, string? operatoreId, string? nomeOperatore)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.ControlloQualitaSchede
            .Include(s => s.Macchina)
            .Include(s => s.Righe).ThenInclude(r => r.Attivita)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.MacchinaId == macchinaId && s.DataEsecuzione.Date == data.Date);

        if (existing != null)
            return MapScheda(existing);

        return await CreateSchedaAsync(new NuovaSchedaControlloQualitaRequest
        {
            MacchinaId = macchinaId,
            DataEsecuzione = data.Date,
            OperatoreId = operatoreId,
            NomeOperatore = nomeOperatore
        });
    }

    public async Task<ControlloQualitaSchedaDto?> UpdateRigaAsync(Guid schedaId, ControlloQualitaRigaDto dto)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var riga = await db.ControlloQualitaRighe.FindAsync(dto.Id);
        if (riga == null) return null;

        riga.Esito = dto.Esito;
        riga.Commento = string.IsNullOrWhiteSpace(dto.Commento) ? null : dto.Commento.Trim();
        riga.DataUltimaModifica = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return await GetSchedaByIdAsync(schedaId);
    }

    public async Task<ControlloQualitaSchedaDto?> ChiudiSchedaAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var scheda = await db.ControlloQualitaSchede
            .Include(s => s.Righe)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (scheda == null) return null;

        var haProblemi = scheda.Righe.Any(r => r.Esito == EsitoControlloQualita.Problema);
        scheda.Stato = haProblemi ? StatoSchedaControlloQualita.ConProblemi : StatoSchedaControlloQualita.Completata;
        scheda.DataChiusura = DateTime.UtcNow;

        await db.SaveChangesAsync();
        _logger.LogInformation("[QUALITA] Scheda controllo chiusa: {Id} stato={Stato}", id, scheda.Stato);

        return await GetSchedaByIdAsync(id);
    }

    public async Task SeedAttivitaDefaultAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        if (await db.ControlloQualitaAttivita.AnyAsync()) return;

        var attivita = new List<ControlloQualitaAttivita>
        {
            new() { Id = Guid.NewGuid(), Ordine = 1, Nome = "Temperatura stampi", Dettaglio = "Verificare temperatura stampi e colore anime marrone chiaro.", Attiva = true },
            new() { Id = Guid.NewGuid(), Ordine = 2, Nome = "Tempo di ciclo", Dettaglio = "Controllare che il tempo di ciclo sia coerente con la produzione in corso.", Attiva = true },
            new() { Id = Guid.NewGuid(), Ordine = 3, Nome = "Rilascio dagli stampi", Dettaglio = "Controllare il corretto rilascio delle anime dagli stampi.", Attiva = true },
            new() { Id = Guid.NewGuid(), Ordine = 4, Nome = "Visivo anime finite", Dettaglio = "Verificare sbavature, parti mancanti, superfici irregolari o porose.", Attiva = true },
            new() { Id = Guid.NewGuid(), Ordine = 5, Nome = "Pulizia anime e cassa", Dettaglio = "Anime prive di polvere/residui di sabbia e cassa senza incrostazioni.", Attiva = true },
            new() { Id = Guid.NewGuid(), Ordine = 6, Nome = "Peso anime vuote", Dettaglio = "Controllare il peso delle anime vuote.", Attiva = true },
            new() { Id = Guid.NewGuid(), Ordine = 7, Nome = "Controlli dimensionali", Dettaglio = "Quando necessario, controllo a campione con calibro e maschere di controllo.", Attiva = true, QuandoNecessario = true },
            new() { Id = Guid.NewGuid(), Ordine = 8, Nome = "Resistenza meccanica", Dettaglio = "Verificare resistenza alle sollecitazioni, incollaggi e prova rottura/compressione.", Attiva = true }
        };

        db.ControlloQualitaAttivita.AddRange(attivita);
        await db.SaveChangesAsync();
        _logger.LogInformation("[QUALITA] Seed controlli qualita default completato: {Count} attivita inserite", attivita.Count);
    }

    private static ControlloQualitaAttivitaDto MapAttivita(ControlloQualitaAttivita a) => new()
    {
        Id = a.Id,
        Nome = a.Nome,
        Dettaglio = a.Dettaglio,
        Ordine = a.Ordine,
        Attiva = a.Attiva,
        FontSize = a.FontSize,
        QuandoNecessario = a.QuandoNecessario,
        RichiedeNotaSeProblema = a.RichiedeNotaSeProblema,
        MacchinaCodiceFiltro = a.MacchinaCodiceFiltro
    };

    private static ControlloQualitaSchedaDto MapScheda(ControlloQualitaScheda s) => new()
    {
        Id = s.Id,
        MacchinaId = s.MacchinaId,
        NomeMacchina = s.Macchina?.Nome ?? string.Empty,
        CodiceMacchina = s.Macchina?.Codice ?? string.Empty,
        DataEsecuzione = s.DataEsecuzione,
        OperatoreId = s.OperatoreId,
        NomeOperatore = s.NomeOperatore,
        Note = s.Note,
        Stato = s.Stato,
        DataChiusura = s.DataChiusura,
        Righe = s.Righe
            .OrderBy(r => r.Attivita?.Ordine ?? 0)
            .Select(r => new ControlloQualitaRigaDto
            {
                Id = r.Id,
                SchedaId = r.SchedaId,
                AttivitaId = r.AttivitaId,
                NomeAttivita = r.Attivita?.Nome ?? string.Empty,
                DettaglioAttivita = r.Attivita?.Dettaglio,
                OrdineAttivita = r.Attivita?.Ordine ?? 0,
                QuandoNecessario = r.Attivita?.QuandoNecessario ?? false,
                RichiedeNotaSeProblema = r.Attivita?.RichiedeNotaSeProblema ?? true,
                Esito = r.Esito,
                Commento = r.Commento,
                DataUltimaModifica = r.DataUltimaModifica
            }).ToList()
    };
}
