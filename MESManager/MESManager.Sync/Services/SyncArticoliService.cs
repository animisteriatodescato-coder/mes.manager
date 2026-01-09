using Microsoft.EntityFrameworkCore;
using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;
using MESManager.Sync.Backup;
using MESManager.Sync.Repositories;

namespace MESManager.Sync.Services;

public class SyncArticoliService
{
    private readonly MagoRepository _magoRepo;
    private readonly MesManagerDbContext _context;
    private readonly JsonBackupService _backup;

    public SyncArticoliService(
        MagoRepository magoRepo,
        MesManagerDbContext context,
        JsonBackupService backup)
    {
        _magoRepo = magoRepo;
        _context = context;
        _backup = backup;
    }

    public async Task<LogSyncEntry> SyncAsync(CancellationToken cancellationToken = default)
    {
        var log = new LogSyncEntry
        {
            Id = Guid.NewGuid(),
            DataOra = DateTime.Now,
            Modulo = "Articoli"
        };

        try
        {
            var syncState = await _context.SyncStates
                .FirstOrDefaultAsync(s => s.Modulo == "Articoli", cancellationToken);

            var lastSync = syncState?.UltimaSyncRiuscita;
            var articoliMago = await _magoRepo.GetArticoliAsync(lastSync);

            if (articoliMago.Any())
            {
                log.FileBackupPath = await _backup.SaveBackupAsync("Articoli", articoliMago);
            }

            foreach (var articoloMago in articoliMago)
            {
                var articolo = await _context.Articoli
                    .FirstOrDefaultAsync(a => a.Codice == articoloMago.Codice, cancellationToken);

                if (articolo == null)
                {
                    articolo = new Articolo
                    {
                        Id = Guid.NewGuid(),
                        Codice = articoloMago.Codice,
                        Descrizione = articoloMago.Descrizione,
                        Prezzo = articoloMago.Prezzo,
                        Attivo = articoloMago.Attivo,
                        UltimaModifica = DateTime.TryParse(articoloMago.UltimaModifica, out var um) ? um : DateTime.MinValue,
                        TimestampSync = DateTime.Now
                    };
                    _context.Articoli.Add(articolo);
                    log.Nuovi++;
                }
                else if (DateTime.TryParse(articoloMago.UltimaModifica, out var um3) && um3 > articolo.UltimaModifica)
                {
                    articolo.Descrizione = articoloMago.Descrizione;
                    articolo.Prezzo = articoloMago.Prezzo;
                    articolo.Attivo = articoloMago.Attivo;
                    articolo.UltimaModifica = DateTime.TryParse(articoloMago.UltimaModifica, out var um2) ? um2 : DateTime.MinValue;
                    articolo.TimestampSync = DateTime.Now;
                    log.Aggiornati++;
                }
                else
                {
                    log.Ignorati++;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (syncState == null)
            {
                syncState = new SyncState
                {
                    Id = Guid.NewGuid(),
                    Modulo = "Articoli"
                };
                _context.SyncStates.Add(syncState);
            }
            syncState.UltimaSyncRiuscita = DateTime.Now;

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            log.Errori = 1;
            log.MessaggioErrore = ex.Message;
        }

        _context.LogSync.Add(log);
        await _context.SaveChangesAsync(cancellationToken);

        return log;
    }
}
