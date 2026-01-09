using MESManager.Domain.Entities;

namespace MESManager.Sync.Services;

public interface ISyncCoordinator
{
    Task<LogSyncEntry> SyncClientiAsync(CancellationToken cancellationToken = default);
    Task<LogSyncEntry> SyncArticoliAsync(CancellationToken cancellationToken = default);
    Task<LogSyncEntry> SyncCommesseAsync(CancellationToken cancellationToken = default);
    Task<List<LogSyncEntry>> SyncTuttoAsync(CancellationToken cancellationToken = default);
}

public class SyncCoordinator : ISyncCoordinator
{
    private readonly SyncClientiService _syncClienti;
    private readonly SyncArticoliService _syncArticoli;
    private readonly SyncCommesseService _syncCommesse;

    public SyncCoordinator(
        SyncClientiService syncClienti,
        SyncArticoliService syncArticoli,
        SyncCommesseService syncCommesse)
    {
        _syncClienti = syncClienti;
        _syncArticoli = syncArticoli;
        _syncCommesse = syncCommesse;
    }

    public async Task<LogSyncEntry> SyncClientiAsync(CancellationToken cancellationToken = default)
    {
        return await _syncClienti.SyncAsync(cancellationToken);
    }

    public async Task<LogSyncEntry> SyncArticoliAsync(CancellationToken cancellationToken = default)
    {
        return await _syncArticoli.SyncAsync(cancellationToken);
    }

    public async Task<LogSyncEntry> SyncCommesseAsync(CancellationToken cancellationToken = default)
    {
        return await _syncCommesse.SyncAsync(cancellationToken);
    }

    public async Task<List<LogSyncEntry>> SyncTuttoAsync(CancellationToken cancellationToken = default)
    {
        var logs = new List<LogSyncEntry>();

        // Sincronizza in ordine: prima clienti, poi articoli, poi commesse
        logs.Add(await SyncClientiAsync(cancellationToken));
        logs.Add(await SyncArticoliAsync(cancellationToken));
        logs.Add(await SyncCommesseAsync(cancellationToken));

        return logs;
    }
}
