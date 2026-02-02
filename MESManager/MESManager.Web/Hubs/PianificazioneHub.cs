using MESManager.Application.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace MESManager.Web.Hubs;

/// <summary>
/// Hub SignalR per sincronizzazione real-time della pianificazione.
/// Gestisce eventi tra Gantt, Programma Macchine e altri client.
/// </summary>
public class PianificazioneHub : Hub
{
    private readonly ILogger<PianificazioneHub> _logger;

    public PianificazioneHub(ILogger<PianificazioneHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client PianificazioneHub connesso: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client PianificazioneHub disconnesso: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Sottoscrive un client agli aggiornamenti di una specifica macchina.
    /// </summary>
    public async Task SubscribeToMachine(int numeroMacchina)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"macchina_{numeroMacchina}");
        _logger.LogDebug("Client {ConnectionId} sottoscritto a macchina {NumeroMacchina}", Context.ConnectionId, numeroMacchina);
    }

    /// <summary>
    /// Rimuove la sottoscrizione dagli aggiornamenti di una specifica macchina.
    /// </summary>
    public async Task UnsubscribeFromMachine(int numeroMacchina)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"macchina_{numeroMacchina}");
        _logger.LogDebug("Client {ConnectionId} disiscritto da macchina {NumeroMacchina}", Context.ConnectionId, numeroMacchina);
    }

    /// <summary>
    /// Sottoscrive il client a tutti gli aggiornamenti del Gantt.
    /// </summary>
    public async Task SubscribeToGantt()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "gantt_updates");
        _logger.LogDebug("Client {ConnectionId} sottoscritto a gantt_updates", Context.ConnectionId);
    }

    /// <summary>
    /// Rimuove la sottoscrizione dagli aggiornamenti del Gantt.
    /// </summary>
    public async Task UnsubscribeFromGantt()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "gantt_updates");
        _logger.LogDebug("Client {ConnectionId} disiscritto da gantt_updates", Context.ConnectionId);
    }
}

/// <summary>
/// Servizio per inviare notifiche SignalR relative alla pianificazione.
/// </summary>
public class PianificazioneNotificationService
{
    private readonly IHubContext<PianificazioneHub> _hubContext;
    private readonly ILogger<PianificazioneNotificationService> _logger;

    public PianificazioneNotificationService(
        IHubContext<PianificazioneHub> hubContext,
        ILogger<PianificazioneNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Notifica tutti i client che le commesse sono state aggiornate.
    /// </summary>
    public async Task NotifyCommesseUpdatedAsync(List<CommessaGanttDto> commesseAggiornate, int? macchinaOrigine = null)
    {
        var notification = new PianificazioneUpdateNotification
        {
            Type = "CommesseUpdated",
            CommesseAggiornate = commesseAggiornate,
            MacchinaOrigine = macchinaOrigine,
            Timestamp = DateTime.Now
        };

        await _hubContext.Clients.Group("gantt_updates").SendAsync("PianificazioneUpdated", notification);
        
        _logger.LogDebug("Notificati {Count} aggiornamenti commesse", commesseAggiornate.Count);
    }

    /// <summary>
    /// Notifica tutti i client che una commessa è stata spostata.
    /// </summary>
    public async Task NotifyCommessaMovedAsync(Guid commessaId, int targetMacchina, int? macchinaOrigine)
    {
        var notification = new PianificazioneUpdateNotification
        {
            Type = "CommessaMoved",
            CommessaId = commessaId,
            TargetMacchina = targetMacchina,
            MacchinaOrigine = macchinaOrigine,
            Timestamp = DateTime.Now
        };

        await _hubContext.Clients.Group("gantt_updates").SendAsync("PianificazioneUpdated", notification);
        
        // Notifica anche i gruppi delle macchine coinvolte
        await _hubContext.Clients.Group($"macchina_{targetMacchina}").SendAsync("MacchinaUpdated", notification);
        
        if (macchinaOrigine.HasValue && macchinaOrigine != targetMacchina)
        {
            await _hubContext.Clients.Group($"macchina_{macchinaOrigine}").SendAsync("MacchinaUpdated", notification);
        }

        _logger.LogDebug("Notificato spostamento commessa {CommessaId} a macchina {TargetMacchina}", commessaId, targetMacchina);
    }

    /// <summary>
    /// Notifica tutti i client di un ricalcolo completo.
    /// </summary>
    public async Task NotifyFullRecalculationAsync()
    {
        var notification = new PianificazioneUpdateNotification
        {
            Type = "FullRecalculation",
            Timestamp = DateTime.Now
        };

        await _hubContext.Clients.Group("gantt_updates").SendAsync("PianificazioneUpdated", notification);
        
        _logger.LogDebug("Notificato ricalcolo completo");
    }
}

/// <summary>
/// DTO per le notifiche di aggiornamento pianificazione via SignalR.
/// </summary>
public class PianificazioneUpdateNotification
{
    public string Type { get; set; } = string.Empty;
    public Guid? CommessaId { get; set; }
    public int? TargetMacchina { get; set; }
    public int? MacchinaOrigine { get; set; }
    public List<CommessaGanttDto>? CommesseAggiornate { get; set; }
    public DateTime Timestamp { get; set; }
}
