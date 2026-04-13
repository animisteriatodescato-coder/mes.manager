using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;

namespace MESManager.Infrastructure.Services;

public class PreventivoService : IPreventivoService
{
    private readonly MesManagerDbContext _db;
    private readonly ILogger<PreventivoService> _logger;

    public PreventivoService(MesManagerDbContext db, ILogger<PreventivoService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ── Tipi Sabbia ────────────────────────────────────────────────────────

    public async Task<List<PreventivoTipoSabbiaDto>> GetTipiSabbiaAsync()
        => await _db.PreventivoTipiSabbia
            .Where(x => x.Attivo)
            .OrderBy(x => x.Ordine)
            .Select(x => MapSabbia(x))
            .ToListAsync();

    public async Task<PreventivoTipoSabbiaDto> CreateTipoSabbiaAsync(PreventivoTipoSabbiaDto dto)
    {
        var entity = new PreventivoTipoSabbia
        {
            Id = Guid.NewGuid(),
            Codice = dto.Codice,
            Nome = dto.Nome,
            Famiglia = dto.Famiglia,
            EuroOra = dto.EuroOra,
            PrezzoKg = dto.PrezzoKg,
            SpariDefault = dto.SpariDefault,
            Attivo = dto.Attivo,
            Ordine = dto.Ordine
        };
        _db.PreventivoTipiSabbia.Add(entity);
        await _db.SaveChangesAsync();
        _logger.LogInformation("[PREVENTIVI] Tipo sabbia creato: {Nome}", entity.Nome);
        return MapSabbia(entity);
    }

    public async Task<PreventivoTipoSabbiaDto?> UpdateTipoSabbiaAsync(PreventivoTipoSabbiaDto dto)
    {
        var entity = await _db.PreventivoTipiSabbia.FindAsync(dto.Id);
        if (entity == null) return null;
        entity.Codice = dto.Codice;
        entity.Nome = dto.Nome;
        entity.Famiglia = dto.Famiglia;
        entity.EuroOra = dto.EuroOra;
        entity.PrezzoKg = dto.PrezzoKg;
        entity.SpariDefault = dto.SpariDefault;
        entity.Attivo = dto.Attivo;
        entity.Ordine = dto.Ordine;
        await _db.SaveChangesAsync();
        return MapSabbia(entity);
    }

    public async Task<bool> DeleteTipoSabbiaAsync(Guid id)
    {
        var entity = await _db.PreventivoTipiSabbia.FindAsync(id);
        if (entity == null) return false;
        entity.Attivo = false; // soft delete
        await _db.SaveChangesAsync();
        return true;
    }

    // ── Tipi Vernice ───────────────────────────────────────────────────────

    public async Task<List<PreventivoTipoVerniceDto>> GetTipiVerniceAsync()
        => await _db.PreventivoTipiVernice
            .Where(x => x.Attivo)
            .OrderBy(x => x.Ordine)
            .Select(x => MapVernice(x))
            .ToListAsync();

    public async Task<PreventivoTipoVerniceDto> CreateTipoVerniceAsync(PreventivoTipoVerniceDto dto)
    {
        var entity = new PreventivoTipoVernice
        {
            Id = Guid.NewGuid(),
            Codice = dto.Codice,
            Nome = dto.Nome,
            Famiglia = dto.Famiglia,
            PrezzoKg = dto.PrezzoKg,
            PercentualeApplicazione = dto.PercentualeApplicazione,
            Attivo = dto.Attivo,
            Ordine = dto.Ordine
        };
        _db.PreventivoTipiVernice.Add(entity);
        await _db.SaveChangesAsync();
        _logger.LogInformation("[PREVENTIVI] Tipo vernice creato: {Nome}", entity.Nome);
        return MapVernice(entity);
    }

    public async Task<PreventivoTipoVerniceDto?> UpdateTipoVerniceAsync(PreventivoTipoVerniceDto dto)
    {
        var entity = await _db.PreventivoTipiVernice.FindAsync(dto.Id);
        if (entity == null) return null;
        entity.Codice = dto.Codice;
        entity.Nome = dto.Nome;
        entity.Famiglia = dto.Famiglia;
        entity.PrezzoKg = dto.PrezzoKg;
        entity.PercentualeApplicazione = dto.PercentualeApplicazione;
        entity.Attivo = dto.Attivo;
        entity.Ordine = dto.Ordine;
        await _db.SaveChangesAsync();
        return MapVernice(entity);
    }

    public async Task<bool> DeleteTipoVerniceAsync(Guid id)
    {
        var entity = await _db.PreventivoTipiVernice.FindAsync(id);
        if (entity == null) return false;
        entity.Attivo = false;
        await _db.SaveChangesAsync();
        return true;
    }

    // ── Preventivi ─────────────────────────────────────────────────────────

    public async Task<List<PreventivoDto>> GetAllAsync()
        => await _db.Preventivi
            .OrderByDescending(p => p.DataCreazione)
            .Select(p => MapPreventivo(p))
            .ToListAsync();

    public async Task<PreventivoDto?> GetByIdAsync(Guid id)
    {
        var entity = await _db.Preventivi.FindAsync(id);
        return entity == null ? null : MapPreventivo(entity);
    }

    public async Task<PreventivoDto> CreateAsync(PreventivoDto dto)
    {
        var entity = MapToEntity(dto);
        entity.Id = Guid.NewGuid();
        entity.DataCreazione = DateTime.UtcNow;
        entity.NumeroPreventivo = Math.Max(await _db.Preventivi.MaxAsync(p => (int?)p.NumeroPreventivo) ?? 0, 999) + 1;
        _db.Preventivi.Add(entity);
        await _db.SaveChangesAsync();
        _logger.LogInformation("[PREVENTIVI] Creato: {Cliente} - {Codice}", entity.Cliente, entity.CodiceArticolo);
        return MapPreventivo(entity);
    }

    public async Task<PreventivoDto?> UpdateAsync(PreventivoDto dto)
    {
        var entity = await _db.Preventivi.FindAsync(dto.Id);
        if (entity == null) return null;
        AggiornaDaDto(entity, dto);
        await _db.SaveChangesAsync();
        return MapPreventivo(entity);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _db.Preventivi.FindAsync(id);
        if (entity == null) return false;
        _db.Preventivi.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<PreventivoDto?> UpdateStatoAsync(Guid id, string stato)
    {
        var entity = await _db.Preventivi.FindAsync(id);
        if (entity == null) return null;
        entity.Stato = stato;
        await _db.SaveChangesAsync();
        return MapPreventivo(entity);
    }

    // ── Calcolo ────────────────────────────────────────────────────────────
    public PreventivoCalcoloResult CalcolaConLotto(PreventivoDto dto, int lotto, decimal margine = 0)
    {
        var dtoLotto = new PreventivoDto
        {
            Figure = dto.Figure, PesoAnima = dto.PesoAnima, Lotto = lotto,
            SpariOrari = dto.SpariOrari, CostoAttrezzatura = dto.CostoAttrezzatura,
            EuroOraSabbia = dto.EuroOraSabbia, PrezzoSabbiaKg = dto.PrezzoSabbiaKg,
            VerniciaturaRichiesta = dto.VerniciaturaRichiesta,
            CostoVerniceKg = dto.CostoVerniceKg, PercentualeVernice = dto.PercentualeVernice,
            VerniciaturaPzOra = dto.VerniciaturaPzOra, EuroOraVerniciatura = dto.EuroOraVerniciatura,
            IncollaggioRichiesto = dto.IncollaggioRichiesto, EuroOraIncollaggio = dto.EuroOraIncollaggio,
            IncollaggioPzOra = dto.IncollaggioPzOra,
            ImballaggioRichiesto = dto.ImballaggioRichiesto, EuroOraImballaggio = dto.EuroOraImballaggio,
            ImballaggioPzOra = dto.ImballaggioPzOra
        };
        var result = Calcola(dtoLotto);
        if (margine > 0)
        {
            result.Margine = margine;
            result.PrezzoVendita = result.PrezzoVendita * (1 + margine / 100m);
        }
        return result;
    }
    public PreventivoCalcoloResult Calcola(PreventivoDto dto)
    {
        var result = new PreventivoCalcoloResult();
        if (dto.Figure <= 0 || dto.PesoAnima <= 0 || dto.Lotto <= 0 || dto.SpariOrari <= 0)
            return result;

        // Lavorazione = EuroOra / SpariOrari
        result.Lavorazione = dto.EuroOraSabbia > 0 ? dto.EuroOraSabbia / dto.SpariOrari : 0;
        // Sabbia totale cassa = PrezzoKg × PesoAnima × Figure
        result.Sabbia = dto.PrezzoSabbiaKg * dto.PesoAnima * dto.Figure;
        // (Lav + Sabbia) / Figure
        result.LavSabFig = (result.Lavorazione + result.Sabbia) / dto.Figure;
        // Ripartizione attrezzatura = CostoAtt / Lotto
        result.RipartizioneAtt = dto.CostoAttrezzatura / dto.Lotto;
        // Costo anima
        result.CostoAnima = result.LavSabFig + result.RipartizioneAtt;

        if (dto.VerniciaturaRichiesta)
        {
            // Verniciatura materiale = CostoKg × (Perc/100) × PesoAnima
            result.VernMateriale = dto.CostoVerniceKg * (dto.PercentualeVernice / 100m) * dto.PesoAnima;
            // Verniciatura manodopera = EuroOraVern / PzOra
            result.VernManodopera = dto.VerniciaturaPzOra > 0 ? dto.EuroOraVerniciatura / dto.VerniciaturaPzOra : 0;
            result.VernTot = result.VernMateriale + result.VernManodopera;
        }

        if (dto.IncollaggioRichiesto && dto.IncollaggioPzOra > 0)
            result.Incollaggio = dto.EuroOraIncollaggio / dto.IncollaggioPzOra;
        if (dto.ImballaggioRichiesto && dto.ImballaggioPzOra > 0)
            result.Imballaggio = dto.EuroOraImballaggio / dto.ImballaggioPzOra;

        // Prezzo vendita = CostoAnima + Verniciatura (incoll/imb sono VOCE SEPARATA)
        result.PrezzoVendita = result.CostoAnima + result.VernTot;

        return result;
    }

    // ── Mapping privati ────────────────────────────────────────────────────

    private static PreventivoTipoSabbiaDto MapSabbia(PreventivoTipoSabbia x) => new()
    {
        Id = x.Id,
        Codice = x.Codice,
        Nome = x.Nome,
        Famiglia = x.Famiglia,
        EuroOra = x.EuroOra,
        PrezzoKg = x.PrezzoKg,
        SpariDefault = x.SpariDefault,
        Attivo = x.Attivo,
        Ordine = x.Ordine
    };

    private static PreventivoTipoVerniceDto MapVernice(PreventivoTipoVernice x) => new()
    {
        Id = x.Id,
        Codice = x.Codice,
        Nome = x.Nome,
        Famiglia = x.Famiglia,
        PrezzoKg = x.PrezzoKg,
        PercentualeApplicazione = x.PercentualeApplicazione,
        Attivo = x.Attivo,
        Ordine = x.Ordine
    };

    private static PreventivoDto MapPreventivo(Preventivo p) => new()
    {
        Id = p.Id,
        NumeroPreventivo = p.NumeroPreventivo,
        DataCreazione = p.DataCreazione.ToLocalTime(),
        Cliente = p.Cliente,
        CodiceArticolo = p.CodiceArticolo,
        Descrizione = p.Descrizione,
        NoteCliente = p.NoteCliente,
        TipoSabbiaId = p.TipoSabbiaId,
        SabbiaSnapshot = p.SabbiaSnapshot,
        EuroOraSabbia = p.EuroOraSabbia,
        PrezzoSabbiaKg = p.PrezzoSabbiaKg,
        Figure = p.Figure,
        PesoAnima = p.PesoAnima,
        Lotto = p.Lotto,
        Lotto2 = p.Lotto2,
        Lotto3 = p.Lotto3,
        Lotto4 = p.Lotto4,
        SpariOrari = p.SpariOrari,
        CostoAttrezzatura = p.CostoAttrezzatura,
        Margine1 = p.Margine1,
        Margine2 = p.Margine2,
        Margine3 = p.Margine3,
        Margine4 = p.Margine4,
        VerniciaturaRichiesta = p.VerniciaturaRichiesta,
        TipoVerniceId = p.TipoVerniceId,
        VerniceSnapshot = p.VerniceSnapshot,
        CostoVerniceKg = p.CostoVerniceKg,
        PercentualeVernice = p.PercentualeVernice,
        VerniciaturaPzOra = p.VerniciaturaPzOra,
        EuroOraVerniciatura = p.EuroOraVerniciatura,
        IncollaggioRichiesto = p.IncollaggioRichiesto,
        EuroOraIncollaggio = p.EuroOraIncollaggio,
        IncollaggioPzOra = p.IncollaggioPzOra,
        ImballaggioRichiesto = p.ImballaggioRichiesto,
        EuroOraImballaggio = p.EuroOraImballaggio,
        ImballaggioPzOra = p.ImballaggioPzOra,
        CalcCostoAnima = p.CalcCostoAnima,
        CalcVerniciaturaTot = p.CalcVerniciaturaTot,
        CalcPrezzoVendita = p.CalcPrezzoVendita,
        Stato = p.Stato
    };

    private static Preventivo MapToEntity(PreventivoDto dto) => new()
    {
        Cliente = dto.Cliente,
        CodiceArticolo = dto.CodiceArticolo,
        Descrizione = dto.Descrizione,
        NoteCliente = dto.NoteCliente,
        TipoSabbiaId = dto.TipoSabbiaId,
        SabbiaSnapshot = dto.SabbiaSnapshot,
        EuroOraSabbia = dto.EuroOraSabbia,
        PrezzoSabbiaKg = dto.PrezzoSabbiaKg,
        Figure = dto.Figure,
        PesoAnima = dto.PesoAnima,
        Lotto = dto.Lotto,
        Lotto2 = dto.Lotto2,
        Lotto3 = dto.Lotto3,
        Lotto4 = dto.Lotto4,
        SpariOrari = dto.SpariOrari,
        CostoAttrezzatura = dto.CostoAttrezzatura,
        Margine1 = dto.Margine1,
        Margine2 = dto.Margine2,
        Margine3 = dto.Margine3,
        Margine4 = dto.Margine4,
        VerniciaturaRichiesta = dto.VerniciaturaRichiesta,
        TipoVerniceId = dto.TipoVerniceId,
        VerniceSnapshot = dto.VerniceSnapshot,
        CostoVerniceKg = dto.CostoVerniceKg,
        PercentualeVernice = dto.PercentualeVernice,
        VerniciaturaPzOra = dto.VerniciaturaPzOra,
        EuroOraVerniciatura = dto.EuroOraVerniciatura,
        IncollaggioRichiesto = dto.IncollaggioRichiesto,
        EuroOraIncollaggio = dto.EuroOraIncollaggio,
        IncollaggioPzOra = dto.IncollaggioPzOra,
        ImballaggioRichiesto = dto.ImballaggioRichiesto,
        EuroOraImballaggio = dto.EuroOraImballaggio,
        ImballaggioPzOra = dto.ImballaggioPzOra,
        CalcCostoAnima = dto.CalcCostoAnima,
        CalcVerniciaturaTot = dto.CalcVerniciaturaTot,
        CalcPrezzoVendita = dto.CalcPrezzoVendita,
        Stato = dto.Stato
    };

    private static void AggiornaDaDto(Preventivo entity, PreventivoDto dto)
    {
        entity.Cliente = dto.Cliente;
        entity.CodiceArticolo = dto.CodiceArticolo;
        entity.Descrizione = dto.Descrizione;
        entity.NoteCliente = dto.NoteCliente;
        entity.TipoSabbiaId = dto.TipoSabbiaId;
        entity.SabbiaSnapshot = dto.SabbiaSnapshot;
        entity.EuroOraSabbia = dto.EuroOraSabbia;
        entity.PrezzoSabbiaKg = dto.PrezzoSabbiaKg;
        entity.Figure = dto.Figure;
        entity.PesoAnima = dto.PesoAnima;
        entity.Lotto = dto.Lotto;
        entity.Lotto2 = dto.Lotto2;
        entity.Lotto3 = dto.Lotto3;
        entity.Lotto4 = dto.Lotto4;
        entity.SpariOrari = dto.SpariOrari;
        entity.CostoAttrezzatura = dto.CostoAttrezzatura;
        entity.Margine1 = dto.Margine1;
        entity.Margine2 = dto.Margine2;
        entity.Margine3 = dto.Margine3;
        entity.Margine4 = dto.Margine4;
        entity.VerniciaturaRichiesta = dto.VerniciaturaRichiesta;
        entity.TipoVerniceId = dto.TipoVerniceId;
        entity.VerniceSnapshot = dto.VerniceSnapshot;
        entity.CostoVerniceKg = dto.CostoVerniceKg;
        entity.PercentualeVernice = dto.PercentualeVernice;
        entity.VerniciaturaPzOra = dto.VerniciaturaPzOra;
        entity.EuroOraVerniciatura = dto.EuroOraVerniciatura;
        entity.IncollaggioRichiesto = dto.IncollaggioRichiesto;
        entity.EuroOraIncollaggio = dto.EuroOraIncollaggio;
        entity.IncollaggioPzOra = dto.IncollaggioPzOra;
        entity.ImballaggioRichiesto = dto.ImballaggioRichiesto;
        entity.EuroOraImballaggio = dto.EuroOraImballaggio;
        entity.ImballaggioPzOra = dto.ImballaggioPzOra;
        entity.CalcCostoAnima = dto.CalcCostoAnima;
        entity.CalcVerniciaturaTot = dto.CalcVerniciaturaTot;
        entity.CalcPrezzoVendita = dto.CalcPrezzoVendita;
        entity.Stato = dto.Stato;
    }
}
