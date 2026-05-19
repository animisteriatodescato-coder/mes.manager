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
    private readonly IDbContextFactory<MesManagerDbContext> _dbFactory;
    private readonly IConfiguration              _configuration;
    private readonly IAiSettingsReader           _settingsReader;
    private readonly OpenAiProvider              _openAiProvider;
    private readonly OllamaProvider              _ollamaProvider;
    private readonly GeminiProvider              _geminiProvider;
    private readonly ILogger<AiAssistantService> _logger;

    public AiAssistantService(
        IDbContextFactory<MesManagerDbContext> dbFactory,
        IConfiguration               configuration,
        IAiSettingsReader            settingsReader,
        ILogger<AiAssistantService>  logger)
    {
        _dbFactory      = dbFactory;
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
        IList<AiChatMessage> history,
        string userMessage,
        IList<AiChatAttachment>? attachments = null,
        CancellationToken ct = default)
    {
        var config = _settingsReader.GetConfig();
        var imageAttachments = NormalizeImageAttachments(attachments);

        return config.ProviderType switch
        {
            "Ollama" => await AskWithOllamaAsync(config, history, userMessage, imageAttachments, ct),
            "Gemini" => await AskWithGeminiAsync(config, history, userMessage, imageAttachments, ct),
            _        => await AskWithOpenAiAsync(config, history, userMessage, imageAttachments, ct),
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
        AiProviderConfig config, IList<AiChatMessage> history, string userMessage, IList<AiChatAttachment> attachments, CancellationToken ct)
    {
        var apiKey = _configuration["OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("sk-your"))
            return "⚠️ API Key OpenAI non configurata. Aggiungere 'OpenAI:ApiKey' in appsettings.Secrets.json.";

        var model    = config.OpenAiModel;
        var messages = BuildMessages(history, userMessage, attachments);
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
        AiProviderConfig config, IList<AiChatMessage> history, string userMessage, IList<AiChatAttachment> attachments, CancellationToken ct)
    {
        if (attachments.Count > 0)
        {
            return "Gli screenshot sono supportati con i provider Gemini e OpenAI configurati con modelli vision. Con Ollama servirebbe configurare esplicitamente un modello multimodale locale e il relativo formato API.";
        }

        var model   = config.OllamaModel;
        var baseUrl = config.OllamaBaseUrl;
        var messages = BuildMessages(history, userMessage, []);
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

            var enrichedSystem = GetOaiContentText(originalMessages.FirstOrDefault(m => m.Role == "system")?.Content) ?? "";
            enrichedSystem += $"\n\n--- DATI ATTUALI DAL DATABASE ---\n{context}\n--- FINE DATI ---\n";
            enrichedSystem += "\nUsa i dati sopra per rispondere. Non inventare dati non presenti.";

            var messages = new List<OaiMessage> { new() { Role = "system", Content = enrichedSystem } };
            messages.AddRange(originalMessages.Where(m => m.Role != "system"));

            var req = new OaiRequest { Model = config.OllamaModel, Messages = messages };
            var res = await _ollamaProvider.CallAsync(req, config.OllamaBaseUrl, ct);
            return GetOaiContentText(res.Choices?.FirstOrDefault()?.Message?.Content) ?? "❌ Nessuna risposta da Ollama.";
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
        AiProviderConfig config, IList<AiChatMessage> history, string userMessage, IList<AiChatAttachment> attachments, CancellationToken ct)
    {
        var apiKey = _configuration["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("YOUR_"))
            return "⚠️ API Key Gemini non configurata. Aggiungere 'Gemini:ApiKey' in appsettings.Secrets.json.";

        var systemInstruction = new GeminiContent { Parts = [new GeminiPart { Text = BuildSystemPrompt() }] };
        var contents          = BuildGeminiContents(history, userMessage, attachments);
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
        {
            _logger.LogWarning("ProcessGeminiResponseAsync: risposta senza candidates o content. FinishReason={FR}",
                response.Candidates?.FirstOrDefault()?.FinishReason ?? "N/A");
            return "❌ Risposta vuota da Gemini.";
        }

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

    private static List<GeminiContent> BuildGeminiContents(
        IList<AiChatMessage> history,
        string userMessage,
        IList<AiChatAttachment> attachments)
    {
        var contents = new List<GeminiContent>();
        foreach (var m in history.TakeLast(10))
        {
            var role = m.Role == "assistant" ? "model" : "user";
            contents.Add(new GeminiContent { Role = role, Parts = [new GeminiPart { Text = m.Content }] });
        }

        var parts = new List<GeminiPart> { new() { Text = userMessage } };
        foreach (var attachment in attachments)
        {
            parts.Add(new GeminiPart
            {
                InlineData = new GeminiInlineData
                {
                    MimeType = attachment.ContentType,
                    Data     = attachment.Base64Data
                }
            });
        }

        contents.Add(new GeminiContent { Role = "user", Parts = parts });
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
                    ["date"]),

                GemFun("get_production_interval",
                    "Analizza la produzione e gli stati PLC per intervallo, opzionalmente filtrando una macchina.",
                    new()
                    {
                        ["date_from"] = GemParam("Inizio intervallo in formato YYYY-MM-DD o YYYY-MM-DD HH:mm."),
                        ["date_to"] = GemParam("Fine intervallo in formato YYYY-MM-DD o YYYY-MM-DD HH:mm."),
                        ["machine"] = GemParam("Codice macchina opzionale, esempio M005.")
                    },
                    ["date_from", "date_to"]),

                GemFun("get_gantt_orders",
                    "Restituisce le commesse pianificate nel Gantt/Programma Macchine per intervallo.",
                    new()
                    {
                        ["date_from"] = GemParam("Inizio intervallo in formato YYYY-MM-DD."),
                        ["date_to"] = GemParam("Fine intervallo in formato YYYY-MM-DD."),
                        ["machine"] = GemParam("Codice macchina opzionale, esempio M005.")
                    },
                    ["date_from", "date_to"]),

                GemFun("search_orders",
                    "Cerca commesse per codice, cliente o descrizione e restituisce stato, consegna e programmazione.",
                    new()
                    {
                        ["query"] = GemParam("Testo da cercare in codice, cliente o descrizione."),
                        ["max"] = GemParam("Numero massimo risultati, default 10.")
                    },
                    ["query"]),

                GemFun("search_articles",
                    "Cerca articoli/anime per codice o descrizione e restituisce dati produttivi utili.",
                    new()
                    {
                        ["query"] = GemParam("Testo da cercare in codice o descrizione."),
                        ["max"] = GemParam("Numero massimo risultati, default 10.")
                    },
                    ["query"])
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
            Type       = "object",
            Properties = props,
            Required   = required is { Length: > 0 } ? required : null
        } : null
    };

    private static GeminiParamProp GemParam(string desc) => new() { Type = "string", Description = desc };

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

            return GetOaiContentText(res2.Choices?.FirstOrDefault()?.Message?.Content) ?? "❌ Nessuna risposta.";
        }

        return GetOaiContentText(choice.Message?.Content) ?? "❌ Nessuna risposta.";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MESSAGE BUILDING
    // ─────────────────────────────────────────────────────────────────────────

    private static string BuildSystemPrompt() =>
        $"""
        Sei l'assistente AI di MESManager, il sistema MES (Manufacturing Execution System) di Todescato.
        Rispondi SEMPRE in italiano. Sii conciso, diretto e formatta i dati in modo leggibile (elenchi puntati, orari HH:mm).
        Puoi analizzare screenshot allegati dall'utente e incrociarli con i dati reali del gestionale tramite le funzioni disponibili.
        Usa i tool quando servono dati reali. Non inventare mai dati che non provengono dal database o dallo screenshot allegato.
        Non puoi eseguire SQL libero: puoi solo usare le funzioni applicative autorizzate.
        Data e ora corrente: {DateTime.Now:dddd d MMMM yyyy HH:mm}
        """;

    private static List<OaiMessage> BuildMessages(
        IList<AiChatMessage> history,
        string userMessage,
        IList<AiChatAttachment> attachments)
    {
        var messages = new List<OaiMessage> { new() { Role = "system", Content = BuildSystemPrompt() } };

        foreach (var m in history.TakeLast(10))
            messages.Add(new OaiMessage { Role = m.Role, Content = m.Content });

        if (attachments.Count == 0)
        {
            messages.Add(new OaiMessage { Role = "user", Content = userMessage });
            return messages;
        }

        var parts = new List<OaiContentPart> { new() { Type = "text", Text = userMessage } };
        parts.AddRange(attachments.Select(a => new OaiContentPart
        {
            Type     = "image_url",
            ImageUrl = new OaiImageUrl { Url = a.DataUrl, Detail = "auto" }
        }));
        messages.Add(new OaiMessage { Role = "user", Content = parts });
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
             ["date"]),

        Tool("get_production_interval",
             "Analizza la produzione e gli stati PLC per intervallo, opzionalmente filtrando una macchina.",
             new()
             {
                 ["date_from"] = Param("Inizio intervallo in formato YYYY-MM-DD o YYYY-MM-DD HH:mm."),
                 ["date_to"] = Param("Fine intervallo in formato YYYY-MM-DD o YYYY-MM-DD HH:mm."),
                 ["machine"] = Param("Codice macchina opzionale, esempio M005.")
             },
             ["date_from", "date_to"]),

        Tool("get_gantt_orders",
             "Restituisce le commesse pianificate nel Gantt/Programma Macchine per intervallo.",
             new()
             {
                 ["date_from"] = Param("Inizio intervallo in formato YYYY-MM-DD."),
                 ["date_to"] = Param("Fine intervallo in formato YYYY-MM-DD."),
                 ["machine"] = Param("Codice macchina opzionale, esempio M005.")
             },
             ["date_from", "date_to"]),

        Tool("search_orders",
             "Cerca commesse per codice, cliente o descrizione e restituisce stato, consegna e programmazione.",
             new()
             {
                 ["query"] = Param("Testo da cercare in codice, cliente o descrizione."),
                 ["max"] = Param("Numero massimo risultati, default 10.")
             },
             ["query"]),

        Tool("search_articles",
             "Cerca articoli/anime per codice o descrizione e restituisce dati produttivi utili.",
             new()
             {
                 ["query"] = Param("Testo da cercare in codice o descrizione."),
                 ["max"] = Param("Numero massimo risultati, default 10.")
             },
             ["query"])
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
                "get_production_interval"   => await GetProductionIntervalAsync(args, ct),
                "get_gantt_orders"          => await GetGanttOrdersAsync(args, ct),
                "search_orders"             => await SearchOrdersAsync(args, ct),
                "search_articles"           => await SearchArticlesAsync(args, ct),
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
        sb.AppendLine(await GetGanttOrdersAsync(today, ct));
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
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var data = await db.PLCStorico
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
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var data = await db.PLCRealtime
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
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var data = await db.Commesse
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
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var data = await db.PLCStorico
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
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var data = await db.PLCStorico
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

    private async Task<string> GetProductionIntervalAsync(JsonElement args, CancellationToken ct)
    {
        var from = ParseDateTime(args, "date_from", DateTime.Today);
        var to   = ParseDateTimeEnd(args, "date_to", from.AddDays(1));
        var machineText = GetString(args, "machine");
        var machineNumber = TryGetMachineNumber(machineText);

        if (to <= from) to = from.AddDays(1);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var query = db.PLCStorico
            .Include(p => p.Macchina)
            .Where(p => p.DataOra >= from && p.DataOra < to);

        if (machineNumber.HasValue)
        {
            var machineCode = FormatMachineCode(machineNumber.Value);
            query = query.Where(p => p.Macchina.Codice == machineCode);
        }

        var data = await query
            .GroupBy(p => new { p.MacchinaId, p.Macchina.Codice, p.Macchina.Nome })
            .Select(g => new
            {
                g.Key.Codice,
                g.Key.Nome,
                Rilevamenti = g.Count(),
                Primo = g.Min(p => p.DataOra),
                Ultimo = g.Max(p => p.DataOra),
                Automatico = g.Count(p => p.StatoMacchina != null &&
                    (p.StatoMacchina.Contains("AUTOMATICO") || p.StatoMacchina.Contains("CICLO"))),
                Allarme = g.Count(p => p.StatoMacchina != null && p.StatoMacchina.Contains("ALLARME")),
                Emergenza = g.Count(p => p.StatoMacchina != null && p.StatoMacchina.Contains("EMERGENZA")),
                Operatori = g.Select(p => p.NumeroOperatore).Distinct().Count()
            })
            .OrderBy(g => g.Codice)
            .ToListAsync(ct);

        if (!data.Any())
            return $"Nessun dato PLC storico tra {from:dd/MM/yyyy HH:mm} e {to:dd/MM/yyyy HH:mm}.";

        var sb = new StringBuilder();
        sb.AppendLine($"Analisi produzione {from:dd/MM/yyyy HH:mm} - {to:dd/MM/yyyy HH:mm}:");
        foreach (var m in data)
        {
            var autoPerc = m.Rilevamenti > 0 ? m.Automatico * 100.0 / m.Rilevamenti : 0;
            sb.AppendLine($"• {m.Codice} ({m.Nome}): AUTO {autoPerc:F0}% | Allarmi {m.Allarme} | Emergenze {m.Emergenza} | Operatori {m.Operatori} | Rilevamenti {m.Rilevamenti} | Copertura {m.Primo:HH:mm}-{m.Ultimo:HH:mm}");
        }
        return sb.ToString();
    }

    private async Task<string> GetGanttOrdersAsync(JsonElement args, CancellationToken ct)
    {
        var from = ParseDateTime(args, "date_from", DateTime.Today);
        var to   = ParseDateTimeEnd(args, "date_to", from.AddDays(1));
        var machineNumber = TryGetMachineNumber(GetString(args, "machine"));

        if (to <= from) to = from.AddDays(1);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var query = db.Commesse
            .Include(c => c.Articolo)
            .Where(c => c.DataInizioPrevisione.HasValue &&
                        c.DataFinePrevisione.HasValue &&
                        c.DataInizioPrevisione.Value < to &&
                        c.DataFinePrevisione.Value >= from);

        if (machineNumber.HasValue)
            query = query.Where(c => c.NumeroMacchina == machineNumber.Value);

        var data = await query
            .OrderBy(c => c.NumeroMacchina)
            .ThenBy(c => c.DataInizioPrevisione)
            .Take(80)
            .Select(c => new
            {
                c.Codice,
                c.CompanyName,
                c.Description,
                Articolo = c.Articolo != null ? c.Articolo.Codice : "",
                c.NumeroMacchina,
                c.QuantitaRichiesta,
                Stato = c.Stato.ToString(),
                StatoProgramma = c.StatoProgramma.ToString(),
                c.DataInizioPrevisione,
                c.DataFinePrevisione,
                c.OrdineSequenza
            })
            .ToListAsync(ct);

        if (!data.Any())
            return $"Nessuna commessa pianificata tra {from:dd/MM/yyyy HH:mm} e {to:dd/MM/yyyy HH:mm}.";

        var sb = new StringBuilder();
        sb.AppendLine($"Commesse Gantt/Programma tra {from:dd/MM/yyyy HH:mm} e {to:dd/MM/yyyy HH:mm} ({data.Count}):");
        foreach (var c in data)
        {
            var macchina = c.NumeroMacchina.HasValue ? FormatMachineCode(c.NumeroMacchina.Value) : "N/D";
            sb.AppendLine($"• {macchina} #{c.OrdineSequenza} | {c.Codice} [{c.Stato}/{c.StatoProgramma}] | {c.DataInizioPrevisione:dd/MM HH:mm}-{c.DataFinePrevisione:dd/MM HH:mm} | {c.QuantitaRichiesta} pz | {c.CompanyName ?? "N/D"} | Art. {c.Articolo}");
        }
        return sb.ToString();
    }

    private async Task<string> SearchOrdersAsync(JsonElement args, CancellationToken ct)
    {
        var queryText = GetString(args, "query");
        var max = Math.Clamp(GetInt(args, "max", 10), 1, 25);
        if (string.IsNullOrWhiteSpace(queryText))
            return "Specifica un testo da cercare nelle commesse.";

        var like = $"%{queryText.Trim()}%";
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var data = await db.Commesse
            .Include(c => c.Articolo)
            .Where(c => EF.Functions.Like(c.Codice, like) ||
                        (c.CompanyName != null && EF.Functions.Like(c.CompanyName, like)) ||
                        (c.Description != null && EF.Functions.Like(c.Description, like)) ||
                        (c.Articolo != null && EF.Functions.Like(c.Articolo.Codice, like)))
            .OrderBy(c => c.DataConsegna)
            .ThenBy(c => c.Codice)
            .Take(max)
            .Select(c => new
            {
                c.Codice,
                c.CompanyName,
                c.Description,
                Articolo = c.Articolo != null ? c.Articolo.Codice : "",
                Stato = c.Stato.ToString(),
                StatoProgramma = c.StatoProgramma.ToString(),
                c.QuantitaRichiesta,
                c.DataConsegna,
                c.NumeroMacchina,
                c.DataInizioPrevisione,
                c.DataFinePrevisione
            })
            .ToListAsync(ct);

        if (!data.Any()) return $"Nessuna commessa trovata per '{queryText}'.";

        var sb = new StringBuilder();
        sb.AppendLine($"Commesse trovate per '{queryText}' ({data.Count}):");
        foreach (var c in data)
        {
            var macchina = c.NumeroMacchina.HasValue ? FormatMachineCode(c.NumeroMacchina.Value) : "non programmata";
            var consegna = c.DataConsegna.HasValue ? c.DataConsegna.Value.ToString("dd/MM/yyyy") : "N/D";
            var slot = c.DataInizioPrevisione.HasValue
                ? $"{c.DataInizioPrevisione:dd/MM HH:mm}-{c.DataFinePrevisione:dd/MM HH:mm}"
                : "nessuno slot";
            sb.AppendLine($"• {c.Codice} [{c.Stato}/{c.StatoProgramma}] | {c.CompanyName ?? "N/D"} | Cons. {consegna} | {c.QuantitaRichiesta} pz | {macchina} | {slot} | Art. {c.Articolo}");
        }
        return sb.ToString();
    }

    private async Task<string> SearchArticlesAsync(JsonElement args, CancellationToken ct)
    {
        var queryText = GetString(args, "query");
        var max = Math.Clamp(GetInt(args, "max", 10), 1, 25);
        if (string.IsNullOrWhiteSpace(queryText))
            return "Specifica un testo da cercare negli articoli/anime.";

        var like = $"%{queryText.Trim()}%";
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var articoli = await db.Articoli
            .Where(a => EF.Functions.Like(a.Codice, like) || EF.Functions.Like(a.Descrizione, like))
            .OrderBy(a => a.Codice)
            .Take(max)
            .Select(a => new
            {
                a.Codice,
                a.Descrizione,
                a.Prezzo,
                a.TempoCiclo,
                a.NumeroFigure,
                a.ClasseLavorazione,
                a.Attivo
            })
            .ToListAsync(ct);

        var anime = await db.Anime
            .Where(a => EF.Functions.Like(a.CodiceArticolo, like) ||
                        EF.Functions.Like(a.DescrizioneArticolo, like) ||
                        (a.Cliente != null && EF.Functions.Like(a.Cliente, like)))
            .OrderBy(a => a.CodiceArticolo)
            .Take(max)
            .Select(a => new
            {
                a.CodiceArticolo,
                a.DescrizioneArticolo,
                a.Cliente,
                a.Ciclo,
                a.Figure,
                a.MacchineSuDisponibili,
                a.Colla,
                a.Sabbia,
                a.Vernice
            })
            .ToListAsync(ct);

        if (!articoli.Any() && !anime.Any())
            return $"Nessun articolo/anima trovata per '{queryText}'.";

        var sb = new StringBuilder();
        sb.AppendLine($"Risultati articoli/anime per '{queryText}':");
        foreach (var a in articoli)
        {
            sb.AppendLine($"• Articolo {a.Codice} | {a.Descrizione} | Ciclo {a.TempoCiclo}s | Figure {a.NumeroFigure} | Prezzo {a.Prezzo:C2} | Classe {a.ClasseLavorazione ?? "N/D"} | {(a.Attivo ? "attivo" : "non attivo")}");
        }
        foreach (var a in anime)
        {
            sb.AppendLine($"• Anima {a.CodiceArticolo} | {a.DescrizioneArticolo} | Cliente {a.Cliente ?? "N/D"} | Ciclo {a.Ciclo ?? "N/D"} | Figure {a.Figure ?? "N/D"} | Macchine {a.MacchineSuDisponibili ?? "N/D"} | Colla {a.Colla ?? "N/D"} | Sabbia {a.Sabbia ?? "N/D"} | Vernice {a.Vernice ?? "N/D"}");
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

    private static DateTime ParseDateTime(JsonElement args, string key, DateTime fallback)
    {
        if (args.TryGetProperty(key, out var el) && el.GetString() is string s && DateTime.TryParse(s, out var d))
            return d;
        return fallback;
    }

    private static DateTime ParseDateTimeEnd(JsonElement args, string key, DateTime fallback)
    {
        if (!args.TryGetProperty(key, out var el) || el.GetString() is not string s || !DateTime.TryParse(s, out var d))
            return fallback;

        return s.Trim().Length <= 10 ? d.Date.AddDays(1) : d;
    }

    private static string? GetString(JsonElement args, string key)
    {
        return args.TryGetProperty(key, out var el) ? el.GetString() : null;
    }

    private static int GetInt(JsonElement args, string key, int fallback)
    {
        if (!args.TryGetProperty(key, out var el)) return fallback;
        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var n)) return n;
        return int.TryParse(el.GetString(), out var parsed) ? parsed : fallback;
    }

    private static int? TryGetMachineNumber(string? machine)
    {
        if (string.IsNullOrWhiteSpace(machine)) return null;
        var digits = new string(machine.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var number) && number > 0 ? number : null;
    }

    private static string FormatMachineCode(int number) => $"M{number:000}";

    private static string? GetOaiContentText(object? content)
    {
        return content switch
        {
            null => null,
            string s => s,
            JsonElement { ValueKind: JsonValueKind.String } el => el.GetString(),
            JsonElement el => el.GetRawText(),
            _ => content.ToString()
        };
    }

    private static List<AiChatAttachment> NormalizeImageAttachments(IList<AiChatAttachment>? attachments)
    {
        if (attachments is null || attachments.Count == 0) return [];

        var supported = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/png",
            "image/jpeg",
            "image/webp"
        };

        return attachments
            .Where(a => supported.Contains(a.ContentType) && !string.IsNullOrWhiteSpace(a.Base64Data))
            .Take(3)
            .ToList();
    }
}
