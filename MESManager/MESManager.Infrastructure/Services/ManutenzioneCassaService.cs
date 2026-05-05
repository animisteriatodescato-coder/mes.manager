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
        if (entity == null) return null;
        var dto = MapScheda(entity);
        // Arricchisci con dati cliente da Anime (via CodiceCassa)
        var anime = await _db.Anime
            .Where(a => a.CodiceCassa == entity.CodiceCassa)
            .Select(a => new { a.Cliente, a.DescrizioneArticolo, a.CodiceArticolo })
            .FirstOrDefaultAsync();
        if (anime != null)
        {
            dto.Cliente = anime.Cliente;
            dto.ArticoloDescrizione = anime.DescrizioneArticolo;
            dto.CodiceArticolo = anime.CodiceArticolo;
        }
        // Storico stati
        dto.StoricoStati = await _db.SchedeStatoLog
            .Where(l => l.SchedaId == id && l.TipoScheda == Domain.Enums.TipoSchedaManutenzione.Cassa)
            .OrderByDescending(l => l.DataCambio)
            .Select(l => new SchedaStatoLogDto
            {
                Id = l.Id,
                StatoPrecedente = l.StatoPrecedente,
                StatoNuovo = l.StatoNuovo,
                DataCambio = l.DataCambio,
                OperatoreId = l.OperatoreId,
                NomeOperatore = l.NomeOperatore
            }).ToListAsync();
        return dto;
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

        var nextCode = await _db.ManutenzioneCasseSchede.AnyAsync()
            ? await _db.ManutenzioneCasseSchede.MaxAsync(s => s.CodiceRiferimento) + 1
            : 1000;

        var scheda = new ManutenzioneCassaScheda
        {
            Id = Guid.NewGuid(),
            CodiceRiferimento = nextCode,
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

        var statoVecchio = scheda.Stato;
        var haAnomalie = scheda.Righe.Any(r => r.Esito == EsitoAttivitaManutenzione.Anomalia);
        scheda.Stato = haAnomalie ? StatoSchedaManutenzione.ConAnomalie : StatoSchedaManutenzione.Completata;
        scheda.DataChiusura = DateTime.UtcNow;

        _db.SchedeStatoLog.Add(new Domain.Entities.SchedaStatoLog
        {
            SchedaId = id,
            TipoScheda = Domain.Enums.TipoSchedaManutenzione.Cassa,
            StatoPrecedente = statoVecchio,
            StatoNuovo = scheda.Stato,
            NomeOperatore = scheda.NomeOperatore,
            OperatoreId = scheda.OperatoreId
        });
        await _db.SaveChangesAsync();

        _logger.LogInformation("[MANUT-CASSA] Scheda chiusa: {Id} stato={Stato}", id, scheda.Stato);
        return await GetSchedaByIdAsync(id);
    }

    public async Task<ManutenzioneCassaSchedaDto?> CambiaStatoAsync(Guid id, StatoSchedaManutenzione nuovoStato, string? operatoreId, string? nomeOperatore)
    {
        var scheda = await _db.ManutenzioneCasseSchede.FindAsync(id);
        if (scheda == null) return null;

        var statoVecchio = scheda.Stato;
        scheda.Stato = nuovoStato;

        if (nuovoStato is StatoSchedaManutenzione.Completata or StatoSchedaManutenzione.ConAnomalie)
            scheda.DataChiusura ??= DateTime.UtcNow;
        else if (nuovoStato == StatoSchedaManutenzione.InCompilazione)
            scheda.DataChiusura = null;

        _db.SchedeStatoLog.Add(new Domain.Entities.SchedaStatoLog
        {
            SchedaId = id,
            TipoScheda = Domain.Enums.TipoSchedaManutenzione.Cassa,
            StatoPrecedente = statoVecchio,
            StatoNuovo = nuovoStato,
            OperatoreId = operatoreId,
            NomeOperatore = nomeOperatore
        });
        await _db.SaveChangesAsync();

        _logger.LogInformation("[MANUT-CASSA] Stato cambiato: {Id} {Da} -> {A} da {Op}",
            id, statoVecchio, nuovoStato, nomeOperatore);
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

    public async Task<bool> SaveNoteAsync(Guid schedaId, string? note)
    {
        var scheda = await _db.ManutenzioneCasseSchede.FindAsync(schedaId);
        if (scheda == null) return false;
        scheda.Note = note;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SaveProblematicheAsync(Guid schedaId, List<string> problematiche)
    {
        var scheda = await _db.ManutenzioneCasseSchede.FindAsync(schedaId);
        if (scheda == null) return false;
        var voci = problematiche.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
        scheda.ProblematicheJson = voci.Count > 0
            ? System.Text.Json.JsonSerializer.Serialize(voci)
            : null;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteSchedaAsync(Guid id)
    {
        var scheda = await _db.ManutenzioneCasseSchede
            .Include(s => s.Righe)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (scheda == null) return false;

        // Elimina prima gli allegati sul disco (via tabella separata, no cascade automatico)
        var allegati = await _db.ManutenzioneCasseAllegati.Where(a => a.SchedaId == id).ToListAsync();
        foreach (var a in allegati)
        {
            if (System.IO.File.Exists(a.PathFile))
                System.IO.File.Delete(a.PathFile);
        }
        _db.ManutenzioneCasseAllegati.RemoveRange(allegati);

        // Righe: cascade via FK, ma le rimuoviamo esplicitamente per sicurezza
        _db.ManutenzioneCasseSchede.Remove(scheda);
        await _db.SaveChangesAsync();

        _logger.LogInformation("[MANUT-CASSA] Scheda eliminata: Id={Id}, Cassa={Cassa}", id, scheda.CodiceCassa);
        return true;
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
        CodiceRiferimento = s.CodiceRiferimento,
        CodiceCassa = s.CodiceCassa,
        DataEsecuzione = s.DataEsecuzione,
        OperatoreId = s.OperatoreId,
        NomeOperatore = s.NomeOperatore,
        Note = s.Note,
        Problematiche = string.IsNullOrWhiteSpace(s.ProblematicheJson)
            ? new()
            : System.Text.Json.JsonSerializer.Deserialize<List<string>>(s.ProblematicheJson) ?? new(),
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
