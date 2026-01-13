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
            MacchinaNumero = int.TryParse(p.Macchina.Codice.Replace("M", ""), out int num) ? num.ToString("D2") : p.Macchina.Codice,
            MacchianaNome = p.Macchina.Codice,
            
            CicliFatti = p.CicliFatti,
            QuantitaDaProdurre = p.QuantitaDaProdurre,
            CicliScarti = p.CicliScarti,
            BarcodeLavorazione = p.BarcodeLavorazione,
            
            NumeroOperatore = p.NumeroOperatore > 0 ? p.NumeroOperatore : (int?)null,
            NomeOperatore = p.Operatore != null ? $"{p.Operatore.Nome} {p.Operatore.Cognome}" : (p.NumeroOperatore > 0 ? p.NumeroOperatore.ToString() : null),
            
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

        return result.Select(p =>
        {
            int cicliFatti = 0,
                qtaDaProd = 0,
                cicliScarti = 0,
                tempoMedioRil = 0,
                tempoMedio = 0,
                figure = 0,
                barcode = 0;
            string? nuovaProdTs = null,
                inizioSetupTs = null,
                fineSetupTs = null,
                inProduzioneTs = null;

            if (!string.IsNullOrWhiteSpace(p.Dati))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(p.Dati);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("CicliFatti", out var cf)) cicliFatti = cf.GetInt32();
                    if (root.TryGetProperty("QuantitaDaProdurre", out var qdp)) qtaDaProd = qdp.GetInt32();
                    if (root.TryGetProperty("CicliScarti", out var cs)) cicliScarti = cs.GetInt32();
                    if (root.TryGetProperty("TempoMedioRilevato", out var tmr)) tempoMedioRil = tmr.GetInt32();
                    if (root.TryGetProperty("TempoMedio", out var tm)) tempoMedio = tm.GetInt32();
                    if (root.TryGetProperty("Figure", out var fg)) figure = fg.GetInt32();
                    if (root.TryGetProperty("BarcodeLavorazione", out var bc)) barcode = bc.GetInt32();

                    if (root.TryGetProperty("NuovaProduzioneTs", out var npts) && npts.ValueKind == System.Text.Json.JsonValueKind.String)
                        nuovaProdTs = npts.GetString();
                    if (root.TryGetProperty("InizioSetupTs", out var ists) && ists.ValueKind == System.Text.Json.JsonValueKind.String)
                        inizioSetupTs = ists.GetString();
                    if (root.TryGetProperty("FineSetupTs", out var fsts) && fsts.ValueKind == System.Text.Json.JsonValueKind.String)
                        fineSetupTs = fsts.GetString();
                    if (root.TryGetProperty("InProduzioneTs", out var ipts) && ipts.ValueKind == System.Text.Json.JsonValueKind.String)
                        inProduzioneTs = ipts.GetString();
                }
                catch
                {
                    // Ignora errori di parsing: manterremo i default
                }
            }

            return new PlcStoricoDto
            {
                Id = p.Id,
                MacchinaId = p.MacchinaId,
                MacchinaNumero = int.TryParse(p.Macchina.Codice.Replace("M", ""), out int num) ? num.ToString("D2") : p.Macchina.Codice,
                MacchianaNome = p.Macchina.Codice,

                Timestamp = p.DataOra,
                StatoMacchina = p.StatoMacchina ?? "Sconosciuto",

                NumeroOperatore = p.NumeroOperatore > 0 ? p.NumeroOperatore : (int?)null,
                NomeOperatore = p.Operatore != null ? $"{p.Operatore.Nome} {p.Operatore.Cognome}" : (p.NumeroOperatore > 0 ? p.NumeroOperatore.ToString() : null),

                BarcodeLavorazione = barcode,
                CicliFatti = cicliFatti,
                QuantitaDaProdurre = qtaDaProd,
                CicliScarti = cicliScarti,
                TempoMedioRilevato = tempoMedioRil,
                TempoMedio = tempoMedio,
                Figure = figure,

                NuovaProduzioneTs = nuovaProdTs,
                InizioSetupTs = inizioSetupTs,
                FineSetupTs = fineSetupTs,
                InProduzioneTs = inProduzioneTs,

                Dati = p.Dati
            };
        }).ToList();
    }

    public async Task<List<PlcStoricoDto>> GetAllStoricoAsync(DateTime? from, DateTime? to)
    {
        var query = _context.PLCStorico
            .Include(p => p.Macchina)
            .Include(p => p.Operatore)
            .AsQueryable();

        if (from.HasValue)
            query = query.Where(p => p.DataOra >= from.Value);

        if (to.HasValue)
            query = query.Where(p => p.DataOra <= to.Value);

        var result = await query
            .OrderByDescending(p => p.DataOra)
            .Take(5000)
            .ToListAsync();

        return result.Select(p =>
        {
            int cicliFatti = 0,
                qtaDaProd = 0,
                cicliScarti = 0,
                tempoMedioRil = 0,
                tempoMedio = 0,
                figure = 0,
                barcode = 0;
            string? nuovaProdTs = null,
                inizioSetupTs = null,
                fineSetupTs = null,
                inProduzioneTs = null;

            if (!string.IsNullOrWhiteSpace(p.Dati))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(p.Dati);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("CicliFatti", out var cf)) cicliFatti = cf.GetInt32();
                    if (root.TryGetProperty("QuantitaDaProdurre", out var qdp)) qtaDaProd = qdp.GetInt32();
                    if (root.TryGetProperty("CicliScarti", out var cs)) cicliScarti = cs.GetInt32();
                    if (root.TryGetProperty("TempoMedioRilevato", out var tmr)) tempoMedioRil = tmr.GetInt32();
                    if (root.TryGetProperty("TempoMedio", out var tm)) tempoMedio = tm.GetInt32();
                    if (root.TryGetProperty("Figure", out var fg)) figure = fg.GetInt32();
                    if (root.TryGetProperty("BarcodeLavorazione", out var bc)) barcode = bc.GetInt32();

                    if (root.TryGetProperty("NuovaProduzioneTs", out var npts) && npts.ValueKind == System.Text.Json.JsonValueKind.String)
                        nuovaProdTs = npts.GetString();
                    if (root.TryGetProperty("InizioSetupTs", out var ists) && ists.ValueKind == System.Text.Json.JsonValueKind.String)
                        inizioSetupTs = ists.GetString();
                    if (root.TryGetProperty("FineSetupTs", out var fsts) && fsts.ValueKind == System.Text.Json.JsonValueKind.String)
                        fineSetupTs = fsts.GetString();
                    if (root.TryGetProperty("InProduzioneTs", out var ipts) && ipts.ValueKind == System.Text.Json.JsonValueKind.String)
                        inProduzioneTs = ipts.GetString();
                }
                catch
                {
                    // Ignora errori di parsing
                }
            }

            return new PlcStoricoDto
            {
                Id = p.Id,
                MacchinaId = p.MacchinaId,
                MacchinaNumero = int.TryParse(p.Macchina.Codice.Replace("M", ""), out int num) ? num.ToString("D2") : p.Macchina.Codice,
                MacchianaNome = p.Macchina.Codice,

                Timestamp = p.DataOra,
                StatoMacchina = p.StatoMacchina ?? "Sconosciuto",

                NumeroOperatore = p.NumeroOperatore > 0 ? p.NumeroOperatore : (int?)null,
                NomeOperatore = p.Operatore != null ? $"{p.Operatore.Nome} {p.Operatore.Cognome}" : (p.NumeroOperatore > 0 ? p.NumeroOperatore.ToString() : null),

                BarcodeLavorazione = barcode,
                CicliFatti = cicliFatti,
                QuantitaDaProdurre = qtaDaProd,
                CicliScarti = cicliScarti,
                TempoMedioRilevato = tempoMedioRil,
                TempoMedio = tempoMedio,
                Figure = figure,

                NuovaProduzioneTs = nuovaProdTs,
                InizioSetupTs = inizioSetupTs,
                FineSetupTs = fineSetupTs,
                InProduzioneTs = inProduzioneTs,

                Dati = p.Dati
            };
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
            MacchinaNumero = int.TryParse(e.Macchina.Codice.Replace("M", ""), out int num) ? num.ToString("D2") : e.Macchina.Codice,
            MacchianaNome = e.Macchina.Codice,
            
            Timestamp = e.DataOra,
            TipoEvento = e.TipoEvento,
            Dettagli = e.Dettagli
        }).ToList();
    }
}
