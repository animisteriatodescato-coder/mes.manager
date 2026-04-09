using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Enums;
using MESManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MESManager.Infrastructure.Services;

/// <summary>
/// Servizio AI assistant basato su OpenAI function calling.
/// Sicurezza: l'AI NON esegue SQL libero — chiama solo funzioni C# pre-approvate.
/// </summary>
public class AiAssistantService : IAiAssistantService
{
    private readonly MesManagerDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiAssistantService> _logger;

    private const string ApiEndpoint = "https://api.openai.com/v1/chat/completions";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    public AiAssistantService(
        MesManagerDbContext context,
        IConfiguration configuration,
        ILogger<AiAssistantService> logger)
    {
        _context       = context;
        _configuration = configuration;
        _logger        = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<string> AskAsync(IList<AiChatMessage> history, string userMessage, CancellationToken ct = default)
    {
        var apiKey = _configuration["OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("sk-your"))
            return "⚠️ API Key OpenAI non configurata. Aggiungere 'OpenAI:ApiKey' in appsettings.Secrets.json.";

        var model   = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
        var messages = BuildMessages(history, userMessage);
        var tools    = BuildTools();

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        try
        {
        // Prima chiamata — il modello può richiedere un tool call
        var req1 = new OaiRequest { Model = model, Messages = messages, Tools = tools, ToolChoice = "auto" };
        var res1 = await CallApiAsync(http, req1, ct);

        var choice1 = res1.Choices?.FirstOrDefault();
        if (choice1 == null) return "❌ Risposta vuota da OpenAI.";

        // Se il modello ha chiamato uno o più tool, li eseguiamo e richiamiamo
        if (choice1.FinishReason == "tool_calls" && choice1.Message?.ToolCalls?.Length > 0)
        {
            // Aggiungi il messaggio assistant (con tool_calls) alla conversazione
            messages.Add(new OaiMessage
            {
                Role      = "assistant",
                Content   = choice1.Message.Content,
                ToolCalls = choice1.Message.ToolCalls
            });

            // Esegui ogni tool e aggiungi il risultato
            foreach (var tc in choice1.Message.ToolCalls)
            {
                var result = await ExecuteToolAsync(tc, ct);
                messages.Add(new OaiMessage
                {
                    Role       = "tool",
                    Content    = result,
                    ToolCallId = tc.Id
                });
            }

            // Seconda chiamata — risposta finale in linguaggio naturale
            var req2 = new OaiRequest { Model = model, Messages = messages };
            var res2 = await CallApiAsync(http, req2, ct);
            return res2.Choices?.FirstOrDefault()?.Message?.Content ?? "❌ Nessuna risposta.";
        }

        return choice1.Message?.Content ?? "❌ Nessuna risposta.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AskAsync: errore comunicazione OpenAI");
            return $"❌ Errore OpenAI: {ex.Message}";
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MESSAGE BUILDING
    // ─────────────────────────────────────────────────────────────────────────

    private static List<OaiMessage> BuildMessages(IList<AiChatMessage> history, string userMessage)
    {
        var systemPrompt = $"""
            Sei l'assistente AI di MESManager, il sistema MES (Manufacturing Execution System) di Todescato.
            Rispondi SEMPRE in italiano. Sii conciso, diretto e formatta i dati in modo leggibile (elenchi puntati, orari HH:mm).
            Usa le funzioni disponibili per rispondere. Non inventare mai dati che non provengono dal database.
            Data e ora corrente: {DateTime.Now:dddd d MMMM yyyy HH:mm}
            """;

        var messages = new List<OaiMessage> { new() { Role = "system", Content = systemPrompt } };

        // Includi max ultimi 10 messaggi della history per contenere il numero di token
        foreach (var m in history.TakeLast(10))
            messages.Add(new OaiMessage { Role = m.Role, Content = m.Content });

        messages.Add(new OaiMessage { Role = "user", Content = userMessage });
        return messages;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TOOLS DEFINITION
    // ─────────────────────────────────────────────────────────────────────────

    private static List<OaiTool> BuildTools() =>
    [
        Tool("get_operators_start_times",
             "Restituisce gli orari di inizio e fine lavoro degli operatori per una data specifica.",
             new() { ["date"] = Param("Data in formato YYYY-MM-DD. Usa la data odierna se non specificata.") },
             ["date"]),

        Tool("get_machines_status",
             "Restituisce lo stato attuale di tutte le macchine (PLCRealtime): stato, operatore attivo, cicli.",
             new(), []),

        Tool("get_active_orders",
             "Restituisce le commesse aperte o in lavorazione con data di consegna e avanzamento.",
             new(), []),

        Tool("get_kpi_day",
             "Restituisce i KPI produttivi per macchina in una data: % automatico, allarmi, emergenze.",
             new() { ["date"] = Param("Data in formato YYYY-MM-DD.") },
             ["date"]),

        Tool("get_alarms",
             "Restituisce gli eventi di allarme o emergenza registrati per una data.",
             new() { ["date"] = Param("Data in formato YYYY-MM-DD.") },
             ["date"])
    ];

    private static OaiTool Tool(string name, string desc, Dictionary<string, OaiParamProp> props, string[] required) =>
        new()
        {
            Function = new OaiFunction
            {
                Name        = name,
                Description = desc,
                Parameters  = new OaiParameters { Properties = props, Required = required }
            }
        };

    private static OaiParamProp Param(string desc) => new() { Description = desc };

    // ─────────────────────────────────────────────────────────────────────────
    // TOOL EXECUTION
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<string> ExecuteToolAsync(OaiToolCall tc, CancellationToken ct)
    {
        try
        {
            var args = JsonDocument.Parse(tc.Function.Arguments).RootElement;
            return tc.Function.Name switch
            {
                "get_operators_start_times" => await GetOperatorsStartTimesAsync(args, ct),
                "get_machines_status"       => await GetMachinesStatusAsync(ct),
                "get_active_orders"         => await GetActiveOrdersAsync(ct),
                "get_kpi_day"               => await GetKpiDayAsync(args, ct),
                "get_alarms"                => await GetAlarmsAsync(args, ct),
                _                           => $"Funzione '{tc.Function.Name}' non riconosciuta."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore esecuzione tool {Tool}", tc.Function.Name);
            return $"Errore nell'esecuzione: {ex.Message}";
        }
    }

    // ── Tool: operatori oggi ─────────────────────────────────────────────────

    private async Task<string> GetOperatorsStartTimesAsync(JsonElement args, CancellationToken ct)
    {
        var date = ParseDate(args, "date", DateTime.Today);
        var from = date.Date;
        var to   = from.AddDays(1);

        var data = await _context.PLCStorico
            .Include(p => p.Operatore)
            .Where(p => p.DataOra >= from && p.DataOra < to && p.OperatoreId != null)
            .GroupBy(p => p.OperatoreId)
            .Select(g => new
            {
                Nome    = g.First().Operatore != null
                              ? g.First().Operatore!.Nome + " " + g.First().Operatore!.Cognome
                              : "N/D",
                Numero  = g.First().NumeroOperatore,
                Inizio  = g.Min(p => p.DataOra),
                Ultimo  = g.Max(p => p.DataOra)
            })
            .OrderBy(g => g.Inizio)
            .ToListAsync(ct);

        if (!data.Any())
            return $"Nessun operatore registrato per il {date:dd/MM/yyyy}.";

        var sb = new StringBuilder();
        sb.AppendLine($"Operatori del {date:dd/MM/yyyy} ({data.Count} rilevati):");
        foreach (var op in data)
            sb.AppendLine($"• {op.Nome} (#{op.Numero}): inizio {op.Inizio:HH:mm} — ultimo rilevamento {op.Ultimo:HH:mm}");

        return sb.ToString();
    }

    // ── Tool: stato macchine ─────────────────────────────────────────────────

    private async Task<string> GetMachinesStatusAsync(CancellationToken ct)
    {
        var data = await _context.PLCRealtime
            .Include(p => p.Macchina)
            .Include(p => p.Operatore)
            .OrderBy(p => p.Macchina.Codice)
            .ToListAsync(ct);

        if (!data.Any()) return "Nessuna macchina rilevata nel sistema.";

        var sb = new StringBuilder();
        sb.AppendLine($"Stato macchine ({DateTime.Now:HH:mm}):");
        foreach (var m in data)
        {
            var op = m.Operatore != null ? $"{m.Operatore.Nome} {m.Operatore.Cognome}" : "nessuno";
            sb.AppendLine($"• {m.Macchina.Codice} — {m.StatoMacchina} | Op: {op} | Cicli: {m.CicliFatti}");
        }
        return sb.ToString();
    }

    // ── Tool: commesse aperte ────────────────────────────────────────────────

    private async Task<string> GetActiveOrdersAsync(CancellationToken ct)
    {
        var data = await _context.Commesse
            .Where(c => c.Stato == StatoCommessa.Aperta || c.Stato == StatoCommessa.InLavorazione)
            .OrderBy(c => c.DataConsegna)
            .Take(25)
            .Select(c => new
            {
                c.Codice,
                Stato        = c.Stato.ToString(),
                Cliente      = c.CompanyName ?? "N/D",
                Consegna     = c.DataConsegna,
                Qta          = c.QuantitaRichiesta
            })
            .ToListAsync(ct);

        if (!data.Any()) return "Nessuna commessa aperta o in lavorazione.";

        var sb = new StringBuilder();
        sb.AppendLine($"Commesse attive ({data.Count}):");
        foreach (var c in data)
        {
            var cons = c.Consegna.HasValue ? c.Consegna.Value.ToString("dd/MM/yy") : "—";
            sb.AppendLine($"• {c.Codice} [{c.Stato}] — {c.Cliente} — Cons. {cons} — {c.Qta} pz");
        }
        return sb.ToString();
    }

    // ── Tool: KPI giornalieri ────────────────────────────────────────────────

    private async Task<string> GetKpiDayAsync(JsonElement args, CancellationToken ct)
    {
        var date = ParseDate(args, "date", DateTime.Today);
        var from = date.Date;
        var to   = from.AddDays(1);

        var data = await _context.PLCStorico
            .Include(p => p.Macchina)
            .Where(p => p.DataOra >= from && p.DataOra < to)
            .GroupBy(p => new { p.MacchinaId, p.Macchina.Codice, p.Macchina.Nome })
            .Select(g => new
            {
                g.Key.Codice,
                g.Key.Nome,
                Totale       = g.Count(),
                Automatico   = g.Count(p => p.StatoMacchina != null &&
                                             (p.StatoMacchina.Contains("AUTOMATICO") || p.StatoMacchina.Contains("CICLO"))),
                Allarme      = g.Count(p => p.StatoMacchina != null && p.StatoMacchina.Contains("ALLARME")),
                Emergenza    = g.Count(p => p.StatoMacchina != null && p.StatoMacchina.Contains("EMERGENZA"))
            })
            .OrderBy(g => g.Codice)
            .ToListAsync(ct);

        if (!data.Any()) return $"Nessun dato disponibile per {date:dd/MM/yyyy}.";

        var sb = new StringBuilder();
        sb.AppendLine($"KPI del {date:dd/MM/yyyy}:");
        foreach (var m in data)
        {
            var autoPerc = m.Totale > 0 ? m.Automatico * 100.0 / m.Totale : 0;
            sb.AppendLine($"• {m.Codice} ({m.Nome}): AUTO {autoPerc:F0}% | Allarmi {m.Allarme} | Emergenze {m.Emergenza} | Rilevamenti {m.Totale}");
        }
        return sb.ToString();
    }

    // ── Tool: allarmi ────────────────────────────────────────────────────────

    private async Task<string> GetAlarmsAsync(JsonElement args, CancellationToken ct)
    {
        var date = ParseDate(args, "date", DateTime.Today);
        var from = date.Date;
        var to   = from.AddDays(1);

        var data = await _context.PLCStorico
            .Include(p => p.Macchina)
            .Include(p => p.Operatore)
            .Where(p => p.DataOra >= from && p.DataOra < to &&
                        p.StatoMacchina != null &&
                        (p.StatoMacchina.Contains("ALLARME") || p.StatoMacchina.Contains("EMERGENZA")))
            .OrderBy(p => p.DataOra)
            .Take(50)
            .ToListAsync(ct);

        if (!data.Any()) return $"Nessun allarme registrato per {date:dd/MM/yyyy}.";

        var sb = new StringBuilder();
        sb.AppendLine($"Allarmi del {date:dd/MM/yyyy} ({data.Count} eventi):");
        foreach (var ev in data)
        {
            var op = ev.Operatore != null ? $"{ev.Operatore.Nome} {ev.Operatore.Cognome}" : "N/D";
            sb.AppendLine($"• {ev.DataOra:HH:mm} [{ev.Macchina.Codice}] {ev.StatoMacchina} — Op: {op}");
        }
        return sb.ToString();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPER
    // ─────────────────────────────────────────────────────────────────────────

    private static DateTime ParseDate(JsonElement args, string key, DateTime fallback)
    {
        if (args.TryGetProperty(key, out var el) && el.GetString() is string s && DateTime.TryParse(s, out var d))
            return d;
        return fallback;
    }

    private async Task<OaiResponse> CallApiAsync(HttpClient http, OaiRequest request, CancellationToken ct)
    {
        var body = JsonSerializer.Serialize(request, JsonOpts);
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using var resp = await http.PostAsync(ApiEndpoint, content, ct);

        if (!resp.IsSuccessStatusCode)
        {
            var errorBody = await resp.Content.ReadAsStringAsync(ct);
            _logger.LogError("OpenAI HTTP {Status}: {Body}", (int)resp.StatusCode, errorBody);
            throw new HttpRequestException(
                $"OpenAI HTTP {(int)resp.StatusCode}: {errorBody[..Math.Min(300, errorBody.Length)]}");
        }

        return await resp.Content.ReadFromJsonAsync<OaiResponse>(JsonOpts, ct)
               ?? throw new InvalidOperationException("OpenAI ha restituito una risposta vuota.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // OPENAI REQUEST / RESPONSE MODELS  (privati — non esposti all'esterno)
    // ─────────────────────────────────────────────────────────────────────────

    private sealed record OaiRequest
    {
        public string        Model      { get; init; } = "gpt-4o-mini";
        public List<OaiMessage> Messages { get; init; } = [];
        public List<OaiTool>?   Tools    { get; init; }
        public string?          ToolChoice { get; init; }
    }

    private sealed record OaiMessage
    {
        public string          Role       { get; init; } = "user";
        public string?         Content    { get; init; }
        public OaiToolCall[]?  ToolCalls  { get; init; }
        public string?         ToolCallId { get; init; }
    }

    private sealed record OaiTool
    {
        public string      Type     { get; init; } = "function";
        public OaiFunction Function { get; init; } = new();
    }

    private sealed record OaiFunction
    {
        public string         Name        { get; init; } = string.Empty;
        public string         Description { get; init; } = string.Empty;
        public OaiParameters  Parameters  { get; init; } = new();
    }

    private sealed record OaiParameters
    {
        public string                          Type       { get; init; } = "object";
        public Dictionary<string, OaiParamProp> Properties { get; init; } = [];
        public string[]                        Required   { get; init; } = [];
    }

    private sealed record OaiParamProp
    {
        public string Type        { get; init; } = "string";
        public string Description { get; init; } = string.Empty;
    }

    private sealed record OaiToolCall
    {
        public string              Id       { get; init; } = string.Empty;
        public string              Type     { get; init; } = "function";
        public OaiToolCallFunction Function { get; init; } = new();
    }

    private sealed record OaiToolCallFunction
    {
        public string Name      { get; init; } = string.Empty;
        public string Arguments { get; init; } = string.Empty;
    }

    private sealed record OaiResponse
    {
        public OaiChoice[]? Choices { get; init; }
    }

    private sealed record OaiChoice
    {
        public string?     FinishReason { get; init; }
        public OaiMessage? Message      { get; init; }
    }
}
