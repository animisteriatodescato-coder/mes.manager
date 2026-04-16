using System.Text;
using System.Text.Json;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Enums;
using MESManager.Infrastructure.Data;
using MESManager.Infrastructure.Services.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MESManager.Infrastructure.Services;

/// <summary>
/// Orchestratore AI per MESManager.
/// Delega la comunicazione HTTP a OpenAiProvider o OllamaProvider
/// in base alla configurazione runtime (IAiSettingsReader).
/// Mantiene tutta la logica di tool calling (function calling) e le query DB.
///
/// Sicurezza: l'AI NON esegue SQL libero — chiama solo funzioni C# pre-approvate.
/// </summary>
public class AiAssistantService : IAiAssistantService
{
    private readonly MesManagerDbContext         _context;
    private readonly IConfiguration              _configuration;
    private readonly IAiSettingsReader           _settingsReader;
    private readonly OpenAiProvider              _openAiProvider;
    private readonly OllamaProvider              _ollamaProvider;
    private readonly GeminiProvider              _geminiProvider;
    private readonly ILogger<AiAssistantService> _logger;

    public AiAssistantService(
        MesManagerDbContext          context,
        IConfiguration               configuration,
        IAiSettingsReader            settingsReader,
        ILogger<AiAssistantService>  logger)
    {
        _context        = context;
        _configuration  = configuration;
        _settingsReader = settingsReader;
        _logger         = logger;
        _openAiProvider = new OpenAiProvider();
        _ollamaProvider = new OllamaProvider();
        _geminiProvider = new GeminiProvider();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<string> AskAsync(
        IList<AiChatMessage> history, string userMessage, CancellationToken ct = default)
    {
        var config = _settingsReader.GetConfig();

        return config.ProviderType switch
        {
            "Ollama" => await AskWithOllamaAsync(config, history, userMessage, ct),
            "Gemini" => await AskWithGeminiAsync(config, history, userMessage, ct),
            _        => await AskWithOpenAiAsync(config, history, userMessage, ct),
        };
    }

    public async Task<AiHealthResult> CheckHealthAsync(CancellationToken ct = default)
    {
        var config = _settingsReader.GetConfig();

        if (config.ProviderType == "Gemini")
        {
            var geminiKey = _configuration["Gemini:ApiKey"];
            return await _geminiProvider.CheckHealthAsync(geminiKey ?? "", config.GeminiModel, ct);
        }

        if (config.ProviderType == "Ollama")
            return await _ollamaProvider.CheckHealthAsync(config.OllamaBaseUrl, ct);

        var apiKey = _configuration["OpenAI:ApiKey"];
        return await _openAiProvider.CheckHealthAsync(apiKey ?? "", config.OpenAiModel, ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // OPENAI FLOW
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<string> AskWithOpenAiAsync(
        AiProviderConfig config, IList<AiChatMessage> history, string userMessage, CancellationToken ct)
    {
        var apiKey = _configuration["OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("sk-your"))
            return "⚠️ API Key OpenAI non configurata. Aggiungere 'OpenAI:ApiKey' in appsettings.Secrets.json.";

        var model    = config.OpenAiModel;
        var messages = BuildMessages(history, userMessage);
        var tools    = BuildTools();

        try
        {
            var req1 = new OaiRequest { Model = model, Messages = messages, Tools = tools, ToolChoice = "auto" };
            var res1 = await _openAiProvider.CallAsync(req1, apiKey, ct);
            return await ProcessResponseAsync(config, res1, messages, apiKey, null, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AskWithOpenAiAsync: errore comunicazione OpenAI");
            return $"❌ Errore OpenAI: {ex.Message}";
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // OLLAMA FLOW
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<string> AskWithOllamaAsync(
        AiProviderConfig config, IList<AiChatMessage> history, string userMessage, CancellationToken ct)
    {
        var model   = config.OllamaModel;
        var baseUrl = config.OllamaBaseUrl;
        var messages = BuildMessages(history, userMessage);
        var tools    = BuildTools();

        try
        {
            var req1 = new OaiRequest { Model = model, Messages = messages, Tools = tools, ToolChoice = "auto" };
            var res1 = await _ollamaProvider.CallAsync(req1, baseUrl, ct);
            return await ProcessResponseAsync(config, res1, messages, null, baseUrl, ct);
        }
        catch (HttpRequestException ex) when ((int?)ex.StatusCode >= 400 && (int?)ex.StatusCode < 500)
        {
            // Il modello non supporta tool calling → fallback a prompt con contesto DB pre-iniettato
            _logger.LogWarning("Ollama tools non supportati ({Status}), fallback a prompt-only", ex.StatusCode);
            return await AskOllamaNoToolsFallbackAsync(config, messages, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AskWithOllamaAsync: errore comunicazione Ollama");
            return $"❌ Errore Ollama: {ex.Message}";
        }
    }

    /// <summary>
    /// Fallback per modelli Ollama senza supporto tool calling:
    /// pre-raccoglie i dati DB e li inietta nel system prompt.
    /// </summary>
    private async Task<string> AskOllamaNoToolsFallbackAsync(
        AiProviderConfig config, List<OaiMessage> originalMessages, CancellationToken ct)
    {
        try
        {
            var context = await GatherAllContextAsync(ct);

            var enrichedSystem = originalMessages.FirstOrDefault(m => m.Role == "system")?.Content ?? "";
            enrichedSystem += $"\n\n--- DATI ATTUALI DAL DATABASE ---\n{context}\n--- FINE DATI ---\n";
            enrichedSystem += "\nUsa i dati sopra per rispondere. Non inventare dati non presenti.";

            var messages = new List<OaiMessage> { new() { Role = "system", Content = enrichedSystem } };
            messages.AddRange(originalMessages.Where(m => m.Role != "system"));

            var req = new OaiRequest { Model = config.OllamaModel, Messages = messages };
            var res = await _ollamaProvider.CallAsync(req, config.OllamaBaseUrl, ct);
            return res.Choices?.FirstOrDefault()?.Message?.Content ?? "❌ Nessuna risposta da Ollama.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AskOllamaNoToolsFallbackAsync: errore");
            return $"❌ Errore Ollama (fallback): {ex.Message}";
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GEMINI FLOW
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<string> AskWithGeminiAsync(
        AiProviderConfig config, IList<AiChatMessage> history, string userMessage, CancellationToken ct)
    {
        var apiKey = _configuration["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("YOUR_"))
            return "⚠️ API Key Gemini non configurata. Aggiungere 'Gemini:ApiKey' in appsettings.Secrets.json.";

        var systemInstruction = new GeminiContent { Parts = [new GeminiPart { Text = BuildSystemPrompt() }] };
        var contents          = BuildGeminiContents(history, userMessage);
        var tools             = BuildGeminiTools();

        try
        {
            var req1 = new GeminiRequest
            {
                SystemInstruction = systemInstruction,
                Contents          = contents,
                Tools             = tools,
                ToolConfig        = new GeminiToolConfig
                {
                    FunctionCallingConfig = new GeminiFunctionCallingConfig { Mode = "AUTO" }
                }
            };
            var res1 = await _geminiProvider.CallAsync(req1, config.GeminiModel, apiKey, ct);
            return await ProcessGeminiResponseAsync(res1, systemInstruction, contents, config.GeminiModel, apiKey, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AskWithGeminiAsync: errore comunicazione Gemini");
            return $"❌ Errore Gemini: {ex.Message}";
        }
    }

    private async Task<string> ProcessGeminiResponseAsync(
        GeminiResponse      response,
        GeminiContent       systemInstruction,
        List<GeminiContent> contents,
        string              model,
        string              apiKey,
        CancellationToken   ct)
    {
        var candidate = response.Candidates?.FirstOrDefault();
        if (candidate?.Content == null)
            return "❌ Risposta vuota da Gemini.";

        var functionCallParts = candidate.Content.Parts?
            .Where(p => p.FunctionCall != null)
            .ToList() ?? [];

        if (functionCallParts.Count > 0)
        {
            // Aggiunge la risposta del modello (con i function calls) alla storia
            contents.Add(new GeminiContent { Role = "model", Parts = candidate.Content.Parts! });

            // Esegue ciascun tool call e raccoglie le risposte
            var responseParts = new List<GeminiPart>();
            foreach (var part in functionCallParts)
            {
                var fc = part.FunctionCall!;
                try
                {
                    // Riusa ExecuteToolAsync avvolgendo nel formato OAI
                    var fakeOaiCall = new OaiToolCall
                    {
                        Id       = Guid.NewGuid().ToString("N"),
                        Function = new OaiToolCallFunction
                        {
                            Name      = fc.Name,
                            Arguments = fc.Args.ValueKind == System.Text.Json.JsonValueKind.Undefined
                                        ? "{}" : fc.Args.GetRawText()
                        }
                    };
                    var result = await ExecuteToolAsync(fakeOaiCall, ct);
                    responseParts.Add(new GeminiPart
                    {
                        FunctionResponse = new GeminiFunctionResponse
                        {
                            Name     = fc.Name,
                            Response = new GeminiFunctionResponseContent { Content = result }
                        }
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ProcessGeminiResponseAsync: errore tool {Tool}", fc.Name);
                    responseParts.Add(new GeminiPart
                    {
                        FunctionResponse = new GeminiFunctionResponse
                        {
                            Name     = fc.Name,
                            Response = new GeminiFunctionResponseContent { Content = $"Errore: {ex.Message}" }
                        }
                    });
                }
            }

            // Aggiunge i risultati dei tool come messaggio "user"
            contents.Add(new GeminiContent { Role = "user", Parts = responseParts });

            // Seconda chiamata — risposta finale senza tools
            var req2 = new GeminiRequest
            {
                SystemInstruction = systemInstruction,
                Contents          = contents
            };
            var res2       = await _geminiProvider.CallAsync(req2, model, apiKey, ct);
            var candidate2 = res2.Candidates?.FirstOrDefault();
            return candidate2?.Content?.Parts?.FirstOrDefault(p => p.Text != null)?.Text
                   ?? "❌ Nessuna risposta da Gemini.";
        }

        return candidate.Content.Parts?.FirstOrDefault(p => p.Text != null)?.Text
               ?? "❌ Nessuna risposta da Gemini.";
    }

    private static List<GeminiContent> BuildGeminiContents(IList<AiChatMessage> history, string userMessage)
    {
        var contents = new List<GeminiContent>();
        foreach (var m in history.TakeLast(10))
        {
            var role = m.Role == "assistant" ? "model" : "user";
            contents.Add(new GeminiContent { Role = role, Parts = [new GeminiPart { Text = m.Content }] });
        }
        contents.Add(new GeminiContent { Role = "user", Parts = [new GeminiPart { Text = userMessage }] });
        return contents;
    }

    private static List<GeminiTool> BuildGeminiTools() =>
    [
        new GeminiTool
        {
            FunctionDeclarations =
            [
                GemFun("get_operators_start_times",
                    "Restituisce gli orari di inizio e fine lavoro degli operatori per una data specifica.",
                    new() { ["date"] = GemParam("Data in formato YYYY-MM-DD. Usa la data odierna se non specificata.") },
                    ["date"]),

                GemFun("get_machines_status",
                    "Restituisce lo stato attuale di tutte le macchine (PLCRealtime): stato, operatore attivo, cicli.",
                    null, null),

                GemFun("get_active_orders",
                    "Restituisce le commesse aperte o in lavorazione con data di consegna e avanzamento.",
                    null, null),

                GemFun("get_kpi_day",
                    "Restituisce i KPI produttivi per macchina in una data: % automatico, allarmi, emergenze.",
                    new() { ["date"] = GemParam("Data in formato YYYY-MM-DD.") },
                    ["date"]),

                GemFun("get_alarms",
                    "Restituisce gli eventi di allarme o emergenza registrati per una data.",
                    new() { ["date"] = GemParam("Data in formato YYYY-MM-DD.") },
                    ["date"])
            ]
        }
    ];

    private static GeminiFunctionDeclaration GemFun(
        string name, string desc,
        Dictionary<string, GeminiParamProp>? props,
        string[]? required) => new()
    {
        Name        = name,
        Description = desc,
        Parameters  = (props is { Count: > 0 }) ? new GeminiParameters
        {
            Type       = "OBJECT",
            Properties = props,
            Required   = required is { Length: > 0 } ? required : null
        } : null
    };

    private static GeminiParamProp GemParam(string desc) => new() { Type = "STRING", Description = desc };

    // ─────────────────────────────────────────────────────────────────────────
    // TOOL CALLING ORCHESTRATION (comune a OpenAI e Ollama con tools)
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<string> ProcessResponseAsync(
        AiProviderConfig  config,
        OaiResponse       response,
        List<OaiMessage>  messages,
        string?           openAiApiKey,
        string?           ollamaBaseUrl,
        CancellationToken ct)
    {
        var choice = response.Choices?.FirstOrDefault();
        if (choice == null) return "❌ Risposta vuota dal provider AI.";

        if (choice.FinishReason == "tool_calls" && choice.Message?.ToolCalls?.Length > 0)
        {
            messages.Add(new OaiMessage
            {
                Role      = "assistant",
                Content   = choice.Message.Content,
                ToolCalls = choice.Message.ToolCalls
            });

            foreach (var tc in choice.Message.ToolCalls)
            {
                var result = await ExecuteToolAsync(tc, ct);
                messages.Add(new OaiMessage
                {
                    Role       = "tool",
                    Content    = result,
                    ToolCallId = tc.Id
                });
            }

            var model = openAiApiKey != null ? config.OpenAiModel : config.OllamaModel;
            var req2  = new OaiRequest { Model = model, Messages = messages };

            OaiResponse res2 = openAiApiKey != null
                ? await _openAiProvider.CallAsync(req2, openAiApiKey, ct)
                : await _ollamaProvider.CallAsync(req2, ollamaBaseUrl!, ct);

            return res2.Choices?.FirstOrDefault()?.Message?.Content ?? "❌ Nessuna risposta.";
        }

        return choice.Message?.Content ?? "❌ Nessuna risposta.";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MESSAGE BUILDING
    // ─────────────────────────────────────────────────────────────────────────

    private static string BuildSystemPrompt() =>
        $"""
        Sei l'assistente AI di MESManager, il sistema MES (Manufacturing Execution System) di Todescato.
        Rispondi SEMPRE in italiano. Sii conciso, diretto e formatta i dati in modo leggibile (elenchi puntati, orari HH:mm).
        Usa le funzioni disponibili per rispondere. Non inventare mai dati che non provengono dal database.
        Data e ora corrente: {DateTime.Now:dddd d MMMM yyyy HH:mm}
        """;

    private static List<OaiMessage> BuildMessages(IList<AiChatMessage> history, string userMessage)
    {
        var messages = new List<OaiMessage> { new() { Role = "system", Content = BuildSystemPrompt() } };

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

    // ─────────────────────────────────────────────────────────────────────────
    // CONTEXT GATHERING (usato dal fallback no-tools Ollama)
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<string> GatherAllContextAsync(CancellationToken ct)
    {
        var sb   = new StringBuilder();
        var today = JsonDocument.Parse($"{{\"date\":\"{DateTime.Today:yyyy-MM-dd}\"}}").RootElement;
        sb.AppendLine(await GetMachinesStatusAsync(ct));
        sb.AppendLine(await GetActiveOrdersAsync(ct));
        sb.AppendLine(await GetAlarmsAsync(today, ct));
        return sb.ToString();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TOOL IMPLEMENTATIONS (query DB — sequenziali, no Task.WhenAll su EF Core)
    // ─────────────────────────────────────────────────────────────────────────

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
                Nome   = g.First().Operatore != null
                             ? g.First().Operatore!.Nome + " " + g.First().Operatore!.Cognome
                             : "N/D",
                Numero = g.First().NumeroOperatore,
                Inizio = g.Min(p => p.DataOra),
                Ultimo = g.Max(p => p.DataOra)
            })
            .OrderBy(g => g.Inizio)
            .ToListAsync(ct);

        if (!data.Any()) return $"Nessun operatore registrato per il {date:dd/MM/yyyy}.";

        var sb = new StringBuilder();
        sb.AppendLine($"Operatori del {date:dd/MM/yyyy} ({data.Count} rilevati):");
        foreach (var op in data)
            sb.AppendLine($"• {op.Nome} (#{op.Numero}): inizio {op.Inizio:HH:mm} — ultimo rilevamento {op.Ultimo:HH:mm}");
        return sb.ToString();
    }

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

    private async Task<string> GetActiveOrdersAsync(CancellationToken ct)
    {
        var data = await _context.Commesse
            .Where(c => c.Stato == StatoCommessa.Aperta || c.Stato == StatoCommessa.InLavorazione)
            .OrderBy(c => c.DataConsegna)
            .Take(25)
            .Select(c => new
            {
                c.Codice,
                Stato    = c.Stato.ToString(),
                Cliente  = c.CompanyName ?? "N/D",
                Consegna = c.DataConsegna,
                Qta      = c.QuantitaRichiesta
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
                Totale     = g.Count(),
                Automatico = g.Count(p => p.StatoMacchina != null &&
                                         (p.StatoMacchina.Contains("AUTOMATICO") || p.StatoMacchina.Contains("CICLO"))),
                Allarme    = g.Count(p => p.StatoMacchina != null && p.StatoMacchina.Contains("ALLARME")),
                Emergenza  = g.Count(p => p.StatoMacchina != null && p.StatoMacchina.Contains("EMERGENZA"))
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
}