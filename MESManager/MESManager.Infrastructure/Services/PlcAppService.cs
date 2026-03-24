using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Enums;
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

    /// <summary>
    /// Timeout in minuti per considerare una macchina disconnessa nel Realtime.
    /// Se l'ultimo aggiornamento è più vecchio di questo valore, la macchina è considerata non connessa.
    /// </summary>
    private const int CONNECTION_TIMEOUT_MINUTES = 2;

    /// <summary>
    /// Timeout in minuti usato nel Gantt per il trailing "NON CONNESSA" dopo l'ultimo record attivo.
    /// Deve essere > massimo intervallo tra due record PLCStorico consecutivi:
    /// 20 cicli × ciclo max (~60s) = ~20 min. Usiamo 30 min per sicurezza.
    /// </summary>
    private const int GANTT_INACTIVITY_TIMEOUT_MINUTES = 30;

    public async Task<List<PlcRealtimeDto>> GetRealtimeDataAsync()
    {
        // Filtra solo macchine con IP configurato (come da impostazioni Gantt)
        var query = await _context.PLCRealtime
            .Include(p => p.Macchina)
            .Include(p => p.Operatore)
            .Where(p => !string.IsNullOrWhiteSpace(p.Macchina.IndirizzoPLC))
            .OrderBy(p => p.Macchina.Codice)
            .ToListAsync();

        var now = DateTime.Now;

        // Batch query prossimo articolo programmato per macchina (no N+1)
        var numeriMacchine = query.Select(p => p.Macchina.OrdineVisualizazione).Distinct().ToList();
        var prossimiArticoli = await _context.Commesse
            .Include(c => c.Articolo)
            .Where(c => c.NumeroMacchina != null
                     && numeriMacchine.Contains(c.NumeroMacchina.Value)
                     && c.StatoProgramma == StatoProgramma.Programmata
                     && c.DataInizioPrevisione != null)
            .OrderBy(c => c.NumeroMacchina)
            .ThenBy(c => c.OrdineSequenza)
            .ThenBy(c => c.DataInizioPrevisione)
            .ToListAsync();

        var prossimoPerMacchina = prossimiArticoli
            .GroupBy(c => c.NumeroMacchina!.Value)
            .ToDictionary(g => g.Key, g => g.First().Articolo?.Codice);

        return query.Select(p => {
            // Determina se la macchina è connessa
            var haIp = !string.IsNullOrWhiteSpace(p.Macchina.IndirizzoPLC);
            var datiRecenti = (now - p.DataUltimoAggiornamento).TotalMinutes <= CONNECTION_TIMEOUT_MINUTES;
            var isConnessa = haIp && datiRecenti;
            
            // Se non connessa, imposta stato speciale
            var statoMacchina = isConnessa ? p.StatoMacchina : "NON CONNESSA";

            var machineNumber = p.Macchina.OrdineVisualizazione;
            
            return new PlcRealtimeDto
            {
                MacchinaId = p.MacchinaId,
                MacchinaNumero = int.TryParse(p.Macchina.Codice.Replace("M", ""), out int num) ? num.ToString("D2") : p.Macchina.Codice,
                MacchianaNome = p.Macchina.Codice,
                IndirizzoPLC = p.Macchina.IndirizzoPLC,
                IsConnessa = isConnessa,
                
                CicliFatti = p.CicliFatti,
                QuantitaDaProdurre = p.QuantitaDaProdurre,
                CicliScarti = p.CicliScarti,
                BarcodeLavorazione = p.BarcodeLavorazione,
                
                NumeroOperatore = p.NumeroOperatore > 0 ? p.NumeroOperatore : (int?)null,
                NomeOperatore = p.Operatore != null ? $"{p.Operatore.Nome} {p.Operatore.Cognome}" : (p.NumeroOperatore > 0 ? p.NumeroOperatore.ToString() : null),
                
                TempoMedioRilevato = p.TempoMedioRilevato,
                TempoMedio = p.TempoMedio,
                Figure = p.Figure,
                
                StatoMacchina = statoMacchina,
                QuantitaRaggiunta = p.QuantitaRaggiunta,
                
                UltimoAggiornamento = p.DataUltimoAggiornamento,

                ProssimoArticoloCodice = prossimoPerMacchina.TryGetValue(machineNumber, out var codice) ? codice : null
            };
        }).ToList();
    }

    public async Task<List<PlcStoricoDto>> GetStoricoAsync(Guid macchinaId, DateTime? from, DateTime? to)
    {
        var query = _context.PLCStorico
            .Include(p => p.Macchina)
            .Include(p => p.Operatore)
            // Filtra solo macchine con PLC configurato — coerente con GetRealtimeDataAsync
            .Where(p => !string.IsNullOrWhiteSpace(p.Macchina.IndirizzoPLC))
            .Where(p => p.MacchinaId == macchinaId);

        if (from.HasValue)
            query = query.Where(p => p.DataOra >= from.Value);

        if (to.HasValue)
            query = query.Where(p => p.DataOra <= to.Value);

        var result = await query
            .OrderByDescending(p => p.DataOra)
            .Take(1000)
            .ToListAsync();

        return result.Select(p => MapToStoricoDto(p)).ToList();
    }

    public async Task<List<PlcStoricoDto>> GetAllStoricoAsync(DateTime? from, DateTime? to, int? limit = 5000)
    {
        var query = _context.PLCStorico
            .Include(p => p.Macchina)
            .Include(p => p.Operatore)
            // Filtra solo macchine con PLC configurato — coerente con GetRealtimeDataAsync
            .Where(p => !string.IsNullOrWhiteSpace(p.Macchina.IndirizzoPLC));

        if (from.HasValue)
            query = query.Where(p => p.DataOra >= from.Value);

        if (to.HasValue)
            query = query.Where(p => p.DataOra <= to.Value);

        var orderedQuery = query.OrderByDescending(p => p.DataOra);

        var result = limit.HasValue
            ? await orderedQuery.Take(limit.Value).ToListAsync()
            : await orderedQuery.ToListAsync();

        return result.Select(p => MapToStoricoDto(p)).ToList();
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

    /// <summary>
    /// Segmenta i record PLCStorico in intervalli a stato costante.
    /// Logica (Soluzione B — data-driven):
    /// - Record intermedi: il segmento copre esattamente [T_i, T_{i+1}]. Nessuna inferenza sui gap.
    ///   Se PlcSync ha scritto un record "NON CONNESSA" nel DB, apparirà come barra nel Gantt.
    /// - Ultimo record dello scope: se il suo stato è "NON CONNESSA" estende fino a fine_range;
    ///   altrimenti dura CONNECTION_TIMEOUT_MINUTES, poi si aggiunge un trailing NON CONNESSA
    ///   fino a fine_range (copre il silenzio nella coda del range interrogato).
    /// </summary>
    public async Task<List<PlcGanttSegmentoDto>> GetGanttStoricoAsync(DateTime from, DateTime to, Guid? macchinaId = null)
    {
        var query = _context.PLCStorico
            .Include(p => p.Macchina)
            .Include(p => p.Operatore)
            .Where(p => !string.IsNullOrWhiteSpace(p.Macchina.IndirizzoPLC))
            .Where(p => p.DataOra >= from && p.DataOra <= to);

        if (macchinaId.HasValue)
            query = query.Where(p => p.MacchinaId == macchinaId.Value);

        var records = await query
            .OrderBy(p => p.MacchinaId)
            .ThenBy(p => p.DataOra)
            .ToListAsync();

        var segmenti = new List<PlcGanttSegmentoDto>();
        var fine_range = to < DateTime.Now ? to : DateTime.Now;

        foreach (var gruppo in records.GroupBy(p => p.MacchinaId))
        {
            var lista = gruppo.ToList();
            for (int i = 0; i < lista.Count; i++)
            {
                var rec = lista[i];
                var inizio = rec.DataOra;
                bool isLast = i == lista.Count - 1;

                DateTime fine;
                if (!isLast)
                {
                    // Record intermedio: copre fino all'inizio del record successivo.
                    // Il gap tra i record è già il dato reale (nessuna inferenza).
                    fine = lista[i + 1].DataOra;
                }
                else if (rec.StatoMacchina == "NON CONNESSA")
                {
                    // Ultimo record è NON CONNESSA: estende fino a fine_range.
                    fine = fine_range;
                }
                else
                {
                    // Ultimo record attivo: lo stato dura GANTT_INACTIVITY_TIMEOUT_MINUTES,
                    // poi trailing NON CONNESSA (macchina ha smesso di rispondere).
                    // Valore alto (30 min) perché PLCStorico scrive solo a cambio stato o ogni 20 cicli.
                    fine = rec.DataOra.AddMinutes(GANTT_INACTIVITY_TIMEOUT_MINUTES);
                    if (fine > fine_range) fine = fine_range;
                }

                if (fine <= inizio) continue;

                segmenti.Add(BuildSegmento(rec, inizio, fine));

                // Trailing NON CONNESSA dopo l'ultimo record attivo
                if (isLast && rec.StatoMacchina != "NON CONNESSA" && fine < fine_range)
                {
                    segmenti.Add(new PlcGanttSegmentoDto
                    {
                        MacchinaId         = rec.MacchinaId,
                        MacchianaNome      = rec.Macchina.Codice,
                        Inizio             = fine,
                        Fine               = fine_range,
                        StatoMacchina      = "NON CONNESSA",
                        NomeOperatore      = null,
                        NumeroOperatore    = null,
                        CicliFatti         = 0,
                        BarcodeLavorazione = 0
                    });
                }
            }
        }

        return segmenti.OrderBy(s => s.MacchianaNome).ThenBy(s => s.Inizio).ToList();
    }

    /// <summary>Helper: costruisce un PlcGanttSegmentoDto da un record PLCStorico.</summary>
    private static PlcGanttSegmentoDto BuildSegmento(Domain.Entities.PLCStorico rec, DateTime inizio, DateTime fine)
        => new PlcGanttSegmentoDto
        {
            MacchinaId         = rec.MacchinaId,
            MacchianaNome      = rec.Macchina.Codice,
            Inizio             = inizio,
            Fine               = fine,
            StatoMacchina      = rec.StatoMacchina ?? "Sconosciuto",
            NomeOperatore      = rec.Operatore != null
                ? $"{rec.Operatore.Nome} {rec.Operatore.Cognome}"
                : (rec.NumeroOperatore > 0 ? rec.NumeroOperatore.ToString() : null),
            NumeroOperatore    = rec.NumeroOperatore > 0 ? rec.NumeroOperatore : (int?)null,
            CicliFatti         = 0,
            BarcodeLavorazione = 0
            // Colore: popolato dal controller via MesDesignTokens.PlcStatoColore()
        };

    /// <summary>Mappa un entity PLCStorico a PlcStoricoDto — unica fonte di verità, evita duplicazione.</summary>
    private static PlcStoricoDto MapToStoricoDto(Domain.Entities.PLCStorico p)
    {
        ParseDatiStorico(p.Dati,
            out int cicliFatti, out int qtaDaProd, out int cicliScarti,
            out int tempoMedioRil, out int tempoMedio, out int figure, out int barcode,
            out string? nuovaProdTs, out string? inizioSetupTs, out string? fineSetupTs, out string? inProduzioneTs);

        return new PlcStoricoDto
        {
            Id             = p.Id,
            MacchinaId     = p.MacchinaId,
            MacchinaNumero = int.TryParse(p.Macchina.Codice.Replace("M", ""), out int num) ? num.ToString("D2") : p.Macchina.Codice,
            MacchianaNome  = p.Macchina.Codice,

            Timestamp     = p.DataOra,
            StatoMacchina = p.StatoMacchina ?? "Sconosciuto",

            NumeroOperatore = p.NumeroOperatore > 0 ? p.NumeroOperatore : (int?)null,
            NomeOperatore = p.Operatore != null
                ? $"{p.Operatore.Nome} {p.Operatore.Cognome}"
                : (p.NumeroOperatore > 0 ? p.NumeroOperatore.ToString() : null),

            BarcodeLavorazione = barcode,
            CicliFatti         = cicliFatti,
            QuantitaDaProdurre = qtaDaProd,
            CicliScarti        = cicliScarti,
            TempoMedioRilevato = tempoMedioRil,
            TempoMedio         = tempoMedio,
            Figure             = figure,

            NuovaProduzioneTs = nuovaProdTs,
            InizioSetupTs     = inizioSetupTs,
            FineSetupTs       = fineSetupTs,
            InProduzioneTs    = inProduzioneTs,

            Dati = p.Dati
        };
    }

    /// <summary>Parsing del blob JSON Dati da PLCStorico — unica implementazione.</summary>
    private static void ParseDatiStorico(
        string? dati,
        out int cicliFatti, out int qtaDaProd, out int cicliScarti,
        out int tempoMedioRil, out int tempoMedio, out int figure, out int barcode,
        out string? nuovaProdTs, out string? inizioSetupTs, out string? fineSetupTs, out string? inProduzioneTs)
    {
        cicliFatti = qtaDaProd = cicliScarti = tempoMedioRil = tempoMedio = figure = barcode = 0;
        nuovaProdTs = inizioSetupTs = fineSetupTs = inProduzioneTs = null;

        if (string.IsNullOrWhiteSpace(dati)) return;

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(dati);
            var root = doc.RootElement;
            if (root.TryGetProperty("CicliFatti",         out var cf))  cicliFatti    = cf.GetInt32();
            if (root.TryGetProperty("QuantitaDaProdurre", out var qdp)) qtaDaProd     = qdp.GetInt32();
            if (root.TryGetProperty("CicliScarti",        out var cs))  cicliScarti   = cs.GetInt32();
            if (root.TryGetProperty("TempoMedioRilevato", out var tmr)) tempoMedioRil = tmr.GetInt32();
            if (root.TryGetProperty("TempoMedio",         out var tm))  tempoMedio    = tm.GetInt32();
            if (root.TryGetProperty("Figure",             out var fg))  figure        = fg.GetInt32();
            if (root.TryGetProperty("BarcodeLavorazione", out var bc))  barcode       = bc.GetInt32();

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
            // Ignora errori di parsing: i valori default sono già stati impostati
        }
    }

    /// <summary>
    /// KPI aggregati per macchina: % tempo in ogni categoria di stato.
    /// Riutilizza GetGanttStoricoAsync per evitare duplicazione della logica di segmentazione.
    /// </summary>
    public async Task<List<PlcKpiStoricoDto>> GetKpiStoricoAsync(DateTime from, DateTime to, Guid? macchinaId = null)
    {
        var segmenti = await GetGanttStoricoAsync(from, to, macchinaId);

        return segmenti
            .GroupBy(s => s.MacchinaId)
            .Select(g =>
            {
                var totale = g.Sum(s => s.DurataMinuti);
                return new PlcKpiStoricoDto
                {
                    MacchinaId      = g.Key,
                    MacchianaNome   = g.First().MacchianaNome,
                    TotaleMinuti    = totale,
                    MinutiAutomatico = g.Where(s =>
                        s.StatoMacchina.Contains("AUTOMATICO", StringComparison.OrdinalIgnoreCase) ||
                        s.StatoMacchina.Contains("CICLO",      StringComparison.OrdinalIgnoreCase))
                        .Sum(s => s.DurataMinuti),
                    MinutiAllarme   = g.Where(s => s.StatoMacchina.Contains("ALLARME",    StringComparison.OrdinalIgnoreCase)).Sum(s => s.DurataMinuti),
                    MinutiEmergenza = g.Where(s => s.StatoMacchina.Contains("EMERGENZA",  StringComparison.OrdinalIgnoreCase)).Sum(s => s.DurataMinuti),
                    MinutiManuale   = g.Where(s => s.StatoMacchina.Contains("MANUALE",    StringComparison.OrdinalIgnoreCase)).Sum(s => s.DurataMinuti),
                    MinutiSetup     = g.Where(s =>
                        s.StatoMacchina.Contains("SETUP",        StringComparison.OrdinalIgnoreCase) ||
                        s.StatoMacchina.Contains("ATTREZZAGGIO", StringComparison.OrdinalIgnoreCase))
                        .Sum(s => s.DurataMinuti),
                    MinutiAltro = g.Where(s =>
                        !s.StatoMacchina.Contains("AUTOMATICO",   StringComparison.OrdinalIgnoreCase) &&
                        !s.StatoMacchina.Contains("CICLO",        StringComparison.OrdinalIgnoreCase) &&
                        !s.StatoMacchina.Contains("ALLARME",      StringComparison.OrdinalIgnoreCase) &&
                        !s.StatoMacchina.Contains("EMERGENZA",    StringComparison.OrdinalIgnoreCase) &&
                        !s.StatoMacchina.Contains("MANUALE",      StringComparison.OrdinalIgnoreCase) &&
                        !s.StatoMacchina.Contains("SETUP",        StringComparison.OrdinalIgnoreCase) &&
                        !s.StatoMacchina.Contains("ATTREZZAGGIO", StringComparison.OrdinalIgnoreCase))
                        .Sum(s => s.DurataMinuti)
                };
            })
            .OrderBy(k => k.MacchianaNome)
            .ToList();
    }
}
