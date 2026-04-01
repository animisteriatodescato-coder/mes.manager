using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Constants;
using MESManager.Domain.Entities;
using MESManager.Domain.Enums;
using MESManager.Infrastructure.Data;

namespace MESManager.Application.Services;

public class ManutenzioneService : IManutenzioneService
{
    private readonly MesManagerDbContext _db;
    private readonly ILogger<ManutenzioneService> _logger;
    private readonly string _fotoBasePath;

    public ManutenzioneService(MesManagerDbContext db, ILogger<ManutenzioneService> logger)
    {
        _db = db;
        _logger = logger;
        _fotoBasePath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "uploads", "manutenzioni");
        Directory.CreateDirectory(_fotoBasePath);
    }

    // ──────────────────────────────────────────────────────────
    // CATALOGO ATTIVITÀ
    // ──────────────────────────────────────────────────────────

    public async Task<List<ManutenzioneAttivitaDto>> GetAttivitaAsync(TipoFrequenzaManutenzione? tipo = null)
    {
        var query = _db.ManutenzioneAttivita.Where(a => a.Attiva);
        if (tipo.HasValue)
            query = query.Where(a => a.TipoFrequenza == tipo.Value);

        return await query.OrderBy(a => a.TipoFrequenza).ThenBy(a => a.Ordine)
            .Select(a => MapAttivita(a))
            .ToListAsync();
    }

    public async Task<ManutenzioneAttivitaDto> CreateAttivitaAsync(ManutenzioneAttivitaDto dto)
    {
        var entity = new ManutenzioneAttivita
        {
            Id = Guid.NewGuid(),
            Nome = dto.Nome,
            TipoFrequenza = dto.TipoFrequenza,
            Ordine = dto.Ordine,
            Attiva = dto.Attiva,
            CicliSogliaPLC = dto.CicliSogliaPLC
        };
        _db.ManutenzioneAttivita.Add(entity);
        await _db.SaveChangesAsync();
        _logger.LogInformation("[MANUTENZIONE] Attività creata: {Nome} ({Tipo})", entity.Nome, entity.TipoFrequenza);
        return MapAttivita(entity);
    }

    public async Task<ManutenzioneAttivitaDto?> UpdateAttivitaAsync(ManutenzioneAttivitaDto dto)
    {
        var entity = await _db.ManutenzioneAttivita.FindAsync(dto.Id);
        if (entity == null) return null;
        entity.Nome = dto.Nome;
        entity.TipoFrequenza = dto.TipoFrequenza;
        entity.Ordine = dto.Ordine;
        entity.Attiva = dto.Attiva;
        entity.CicliSogliaPLC = dto.CicliSogliaPLC;
        await _db.SaveChangesAsync();
        return MapAttivita(entity);
    }

    public async Task<bool> DeleteAttivitaAsync(Guid id)
    {
        var entity = await _db.ManutenzioneAttivita.FindAsync(id);
        if (entity == null) return false;
        entity.Attiva = false; // soft-delete
        await _db.SaveChangesAsync();
        return true;
    }

    // ──────────────────────────────────────────────────────────
    // SCHEDE
    // ──────────────────────────────────────────────────────────

    public async Task<List<ManutenzioneSchedaDto>> GetSchedeAsync(
        Guid? macchinaId = null,
        TipoFrequenzaManutenzione? tipo = null,
        DateTime? dal = null,
        DateTime? al = null)
    {
        var query = _db.ManutenzioneSchede
            .Include(s => s.Macchina)
            .Include(s => s.Righe).ThenInclude(r => r.Attivita)
            .AsQueryable();

        if (macchinaId.HasValue) query = query.Where(s => s.MacchinaId == macchinaId.Value);
        if (tipo.HasValue) query = query.Where(s => s.TipoFrequenza == tipo.Value);
        if (dal.HasValue) query = query.Where(s => s.DataEsecuzione >= dal.Value);
        if (al.HasValue) query = query.Where(s => s.DataEsecuzione <= al.Value);

        var list = await query.OrderByDescending(s => s.DataEsecuzione).ToListAsync();
        return list.Select(MapScheda).ToList();
    }

    public async Task<ManutenzioneSchedaDto?> GetSchedaByIdAsync(Guid id)
    {
        var entity = await _db.ManutenzioneSchede
            .Include(s => s.Macchina)
            .Include(s => s.Righe).ThenInclude(r => r.Attivita)
            .FirstOrDefaultAsync(s => s.Id == id);
        return entity == null ? null : MapScheda(entity);
    }

    public async Task<ManutenzioneSchedaDto> CreateSchedaAsync(NuovaSchedaRequest request)
    {
        var attivita = await _db.ManutenzioneAttivita
            .Where(a => a.Attiva && a.TipoFrequenza == request.TipoFrequenza)
            .OrderBy(a => a.Ordine)
            .ToListAsync();

        var scheda = new ManutenzioneScheda
        {
            Id = Guid.NewGuid(),
            MacchinaId = request.MacchinaId,
            TipoFrequenza = request.TipoFrequenza,
            DataEsecuzione = request.DataEsecuzione,
            OperatoreId = request.OperatoreId,
            NomeOperatore = request.NomeOperatore,
            Note = request.Note,
            Stato = StatoSchedaManutenzione.InCompilazione,
            Righe = attivita.Select(a => new ManutenzioneRiga
            {
                Id = Guid.NewGuid(),
                AttivitaId = a.Id,
                Esito = EsitoAttivitaManutenzione.NonEseguita
            }).ToList()
        };

        _db.ManutenzioneSchede.Add(scheda);
        await _db.SaveChangesAsync();

        _logger.LogInformation("[MANUTENZIONE] Scheda creata: {Id} macchina={MacchinaId} tipo={Tipo} ({NRighe} righe)",
            scheda.Id, scheda.MacchinaId, scheda.TipoFrequenza, scheda.Righe.Count);

        return (await GetSchedaByIdAsync(scheda.Id))!;
    }

    public async Task<ManutenzioneSchedaDto?> ChiudiSchedaAsync(Guid id)
    {
        var scheda = await _db.ManutenzioneSchede
            .Include(s => s.Righe)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (scheda == null) return null;

        var haAnomalie = scheda.Righe.Any(r => r.Esito == EsitoAttivitaManutenzione.Anomalia);
        scheda.Stato = haAnomalie ? StatoSchedaManutenzione.ConAnomalie : StatoSchedaManutenzione.Completata;
        scheda.DataChiusura = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("[MANUTENZIONE] Scheda chiusa: {Id} stato={Stato}", id, scheda.Stato);
        return await GetSchedaByIdAsync(id);
    }

    // ──────────────────────────────────────────────────────────
    // RIGHE
    // ──────────────────────────────────────────────────────────

    public async Task<ManutenzioneSchedaDto?> UpdateRigaAsync(Guid schedaId, ManutenzioneRigaDto dto)
    {
        var riga = await _db.ManutenzioneRighe.FindAsync(dto.Id);
        if (riga == null) return null;

        riga.Esito = dto.Esito;
        riga.Commento = dto.Commento;
        await _db.SaveChangesAsync();

        return await GetSchedaByIdAsync(schedaId);
    }

    public async Task<string?> UploadFotoRigaAsync(Guid rigaId, Stream fileStream, string fileName)
    {
        var riga = await _db.ManutenzioneRighe.FindAsync(rigaId);
        if (riga == null) return null;

        // Elimina foto precedente
        if (!string.IsNullOrEmpty(riga.FotoPath))
        {
            var oldPath = Path.Combine(_fotoBasePath, riga.FotoPath);
            if (File.Exists(oldPath)) File.Delete(oldPath);
        }

        var ext = Path.GetExtension(fileName);
        if (!FileConstants.IsFoto(fileName))
        {
            _logger.LogWarning("[MANUTENZIONE] Upload foto rifiutato: estensione non supportata {Ext}", ext);
            return null;
        }

        var newFileName = $"{rigaId}{ext}";
        var fullPath = Path.Combine(_fotoBasePath, newFileName);

        await using var stream = File.Create(fullPath);
        await fileStream.CopyToAsync(stream);

        riga.FotoPath = newFileName;
        await _db.SaveChangesAsync();

        _logger.LogInformation("[MANUTENZIONE] Foto caricata per riga {RigaId}: {File}", rigaId, newFileName);
        return $"/uploads/manutenzioni/{newFileName}";
    }

    public async Task<bool> DeleteFotoRigaAsync(Guid rigaId)
    {
        var riga = await _db.ManutenzioneRighe.FindAsync(rigaId);
        if (riga == null || string.IsNullOrEmpty(riga.FotoPath)) return false;

        var fullPath = Path.Combine(_fotoBasePath, riga.FotoPath);
        if (File.Exists(fullPath)) File.Delete(fullPath);

        riga.FotoPath = null;
        await _db.SaveChangesAsync();
        return true;
    }

    // ──────────────────────────────────────────────────────────
    // SEED DATI INIZIALI
    // ──────────────────────────────────────────────────────────

    public async Task SeedAttivitaDefaultAsync()
    {
        if (await _db.ManutenzioneAttivita.AnyAsync()) return;

        var attivita = new List<ManutenzioneAttivita>
        {
            // Settimanali (da Excel MANUTENZIONE SETTIMANALE)
            new() { Id = Guid.NewGuid(), Nome = "Tenuta Tubazioni",       TipoFrequenza = TipoFrequenzaManutenzione.Settimanale, Ordine = 1, Attiva = true },
            new() { Id = Guid.NewGuid(), Nome = "Serraggio Bulloneria",   TipoFrequenza = TipoFrequenzaManutenzione.Settimanale, Ordine = 2, Attiva = true },
            new() { Id = Guid.NewGuid(), Nome = "Raschiatore Pulitore",   TipoFrequenza = TipoFrequenzaManutenzione.Settimanale, Ordine = 3, Attiva = true },
            new() { Id = Guid.NewGuid(), Nome = "Ingrassaggio",           TipoFrequenza = TipoFrequenzaManutenzione.Settimanale, Ordine = 4, Attiva = true },
            new() { Id = Guid.NewGuid(), Nome = "Scambiatore di Calore",  TipoFrequenza = TipoFrequenzaManutenzione.Settimanale, Ordine = 5, Attiva = true },

            // Mensili (da Excel MANUTENZIONE MENSILE)
            new() { Id = Guid.NewGuid(), Nome = "Verifica ingrassaggio canotti di scorrimento", TipoFrequenza = TipoFrequenzaManutenzione.Mensile, Ordine = 1, Attiva = true },
            new() { Id = Guid.NewGuid(), Nome = "Controllo perdite cilindri pneumatici e idraulici", TipoFrequenza = TipoFrequenzaManutenzione.Mensile, Ordine = 2, Attiva = true },
            new() { Id = Guid.NewGuid(), Nome = "Ispezione valvole idrauliche per perdite",    TipoFrequenza = TipoFrequenzaManutenzione.Mensile, Ordine = 3, Attiva = true },
            new() { Id = Guid.NewGuid(), Nome = "Pulizia filtri aria e olio",                  TipoFrequenza = TipoFrequenzaManutenzione.Mensile, Ordine = 4, Attiva = true },
        };

        _db.ManutenzioneAttivita.AddRange(attivita);
        await _db.SaveChangesAsync();
        _logger.LogInformation("[MANUTENZIONE] Seed attività default completato: {Count} attività inserite", attivita.Count);
    }

    // ──────────────────────────────────────────────────────────
    // MAPPING PRIVATI
    // ──────────────────────────────────────────────────────────

    private static ManutenzioneAttivitaDto MapAttivita(ManutenzioneAttivita a) => new()
    {
        Id = a.Id,
        Nome = a.Nome,
        TipoFrequenza = a.TipoFrequenza,
        Ordine = a.Ordine,
        Attiva = a.Attiva,
        CicliSogliaPLC = a.CicliSogliaPLC
    };

    private static ManutenzioneSchedaDto MapScheda(ManutenzioneScheda s) => new()
    {
        Id = s.Id,
        MacchinaId = s.MacchinaId,
        NomeMacchina = s.Macchina?.Nome ?? string.Empty,
        CodiceMacchina = s.Macchina?.Codice ?? string.Empty,
        TipoFrequenza = s.TipoFrequenza,
        DataEsecuzione = s.DataEsecuzione,
        OperatoreId = s.OperatoreId,
        NomeOperatore = s.NomeOperatore,
        Note = s.Note,
        Stato = s.Stato,
        DataChiusura = s.DataChiusura,
        Righe = s.Righe
            .OrderBy(r => r.Attivita?.Ordine ?? 0)
            .Select(r => new ManutenzioneRigaDto
            {
                Id = r.Id,
                SchedaId = r.SchedaId,
                AttivitaId = r.AttivitaId,
                NomeAttivita = r.Attivita?.Nome ?? string.Empty,
                OrdineAttivita = r.Attivita?.Ordine ?? 0,
                Esito = r.Esito,
                Commento = r.Commento,
                FotoPath = r.FotoPath,
                CicloMacchinaAlEsecuzione = r.CicloMacchinaAlEsecuzione
            }).ToList()
    };
}
