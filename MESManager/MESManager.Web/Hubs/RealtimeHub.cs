using MESManager.Application.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace MESManager.Web.Hubs;

/// <summary>
/// Hub SignalR per comunicazioni real-time dei dati PLC.
/// Consente al server di pushare aggiornamenti ai client senza polling.
/// </summary>
public class RealtimeHub : Hub
{
    private readonly ILogger<RealtimeHub> _logger;

    public RealtimeHub(ILogger<RealtimeHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client SignalR connesso: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client SignalR disconnesso: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Invia un messaggio generico a tutti i client.
    /// </summary>
    public async Task SendUpdate(string message)
    {
        await Clients.All.SendAsync("ReceiveUpdate", message);
    }

    /// <summary>
    /// Chiamato dai client per richiedere un refresh immediato dei dati.
    /// </summary>
    public async Task RequestRefresh()
    {
        _logger.LogDebug("Client {ConnectionId} ha richiesto un refresh", Context.ConnectionId);
        await Clients.Caller.SendAsync("RefreshRequested");
    }

    /// <summary>
    /// Sottoscrive un client agli aggiornamenti di una specifica macchina.
    /// </summary>
    public async Task SubscribeToMachine(string macchinaId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"machine_{macchinaId}");
        _logger.LogDebug("Client {ConnectionId} sottoscritto a macchina {MacchinaId}", Context.ConnectionId, macchinaId);
    }

    /// <summary>
    /// Rimuove la sottoscrizione agli aggiornamenti di una specifica macchina.
    /// </summary>
    public async Task UnsubscribeFromMachine(string macchinaId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"machine_{macchinaId}");
        _logger.LogDebug("Client {ConnectionId} disiscritto da macchina {MacchinaId}", Context.ConnectionId, macchinaId);
    }
}
