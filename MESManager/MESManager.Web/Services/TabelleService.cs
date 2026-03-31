using System.Text.Json;
using MESManager.Domain.Constants;

namespace MESManager.Web.Services;

/// <summary>
/// Implementazione di ITabelleService con persistenza su file JSON.
/// All'avvio carica da tabelle-config.json (se esiste) oppure usa i valori
/// di default da LookupTables e aggiorna i dizionari statici in memoria.
/// Al salvataggio scrive su disco E aggiorna LookupTables (per AnimeService,
/// CommessaAppService e tutti i servizi che usano la classe statica).
/// </summary>
public class TabelleService : ITabelleService
{
    private readonly string _filePath;
    private readonly ILogger<TabelleService> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private Dictionary<string, string> _colla   = new();
    private Dictionary<string, string> _vernice  = new();
    private Dictionary<string, string> _sabbia   = new();
    private Dictionary<string, string> _imballo  = new();

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public TabelleService(IWebHostEnvironment env, ILogger<TabelleService> logger)
    {
        _logger = logger;
        _filePath = Path.Combine(env.ContentRootPath, "tabelle-config.json");
        CaricaDati();
    }

    // ─── Lettura ───────────────────────────────────────────────────────────

    public Dictionary<string, string> GetColla()   => new(_colla);
    public Dictionary<string, string> GetVernice()  => new(_vernice);
    public Dictionary<string, string> GetSabbia()   => new(_sabbia);
    public Dictionary<string, string> GetImballo()  => new(_imballo);

    public List<LookupItem> GetCollaList()   => ToDtoList(_colla);
    public List<LookupItem> GetVerniceList()  => ToDtoList(_vernice);
    public List<LookupItem> GetSabbiaList()   => ToDtoList(_sabbia);
    public List<LookupItem> GetImballoList()  => ToDtoList(_imballo);

    // ─── Scrittura ─────────────────────────────────────────────────────────

    public Task SalvaCollaAsync(List<LookupItem> items)
    {
        _colla = items.ToDictionary(i => i.Codice, i => i.Descrizione);
        SincronizzaStatico();
        return PersistiAsync();
    }

    public Task SalvaVerniceAsync(List<LookupItem> items)
    {
        _vernice = items.ToDictionary(i => i.Codice, i => i.Descrizione);
        SincronizzaStatico();
        return PersistiAsync();
    }

    public Task SalvaSabbiaAsync(List<LookupItem> items)
    {
        _sabbia = items.ToDictionary(i => i.Codice, i => i.Descrizione);
        SincronizzaStatico();
        return PersistiAsync();
    }

    public Task SalvaImballoAsync(List<LookupItem> items)
    {
        _imballo = items.ToDictionary(i => i.Codice, i => i.Descrizione);
        // Aggiorna anche ImballoInt (compatibilità legacy usata da AnimeService)
        SincronizzaStatico();
        return PersistiAsync();
    }

    // ─── Privati ────────────────────────────────────────────────────────────

    private void CaricaDati()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var cfg  = JsonSerializer.Deserialize<TabelleConfig>(json, _jsonOpts);
                if (cfg != null)
                {
                    _colla   = cfg.Colla   ?? new();
                    _vernice  = cfg.Vernice  ?? new();
                    _sabbia   = cfg.Sabbia   ?? new();
                    _imballo  = cfg.Imballo  ?? new();
                    SincronizzaStatico();
                    _logger.LogInformation("TabelleService: caricato da {File}", _filePath);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TabelleService: impossibile leggere {File}, uso valori default", _filePath);
        }

        // Fallback: clona i valori di default da LookupTables
        _colla   = new Dictionary<string, string>(LookupTables.Colla);
        _vernice  = new Dictionary<string, string>(LookupTables.Vernice);
        _sabbia   = new Dictionary<string, string>(LookupTables.Sabbia);
        _imballo  = new Dictionary<string, string>(LookupTables.Imballo);
        _logger.LogInformation("TabelleService: nessun file trovato, uso valori LookupTables hardcoded");
    }

    /// <summary>
    /// Sincronizza i dizionari in-memory con LookupTables statico,
    /// così AnimeService / CommessaAppService ottengono i valori aggiornati
    /// senza dover essere refactorizzati.
    /// </summary>
    private void SincronizzaStatico()
    {
        LookupTables.Aggiorna(_colla, _vernice, _sabbia, _imballo);
    }

    private async Task PersistiAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var cfg = new TabelleConfig
            {
                Colla   = _colla,
                Vernice  = _vernice,
                Sabbia   = _sabbia,
                Imballo  = _imballo
            };
            var json = JsonSerializer.Serialize(cfg, _jsonOpts);
            await File.WriteAllTextAsync(_filePath, json);
            _logger.LogInformation("TabelleService: salvato su {File}", _filePath);
        }
        finally
        {
            _lock.Release();
        }
    }

    private static List<LookupItem> ToDtoList(Dictionary<string, string> dict) =>
        dict.Select(kv => new LookupItem { Codice = kv.Key, Descrizione = kv.Value }).ToList();

    private class TabelleConfig
    {
        public Dictionary<string, string>? Colla   { get; set; }
        public Dictionary<string, string>? Vernice  { get; set; }
        public Dictionary<string, string>? Sabbia   { get; set; }
        public Dictionary<string, string>? Imballo  { get; set; }
    }
}
