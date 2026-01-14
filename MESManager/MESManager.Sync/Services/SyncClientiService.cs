using Microsoft.EntityFrameworkCore;
using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;
using MESManager.Sync.Backup;
using MESManager.Sync.Repositories;

namespace MESManager.Sync.Services;

public class SyncClientiService
{
    private readonly MagoRepository _magoRepo;
    private readonly MesManagerDbContext _context;
    private readonly JsonBackupService _backup;

    public SyncClientiService(
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
            Modulo = "Clienti"
        };

        try
        {
            // Ottieni ultima sync riuscita
            var syncState = await _context.SyncStates
                .FirstOrDefaultAsync(s => s.Modulo == "Clienti", cancellationToken);

            var lastSync = syncState?.UltimaSyncRiuscita;

            // Leggi da Mago
            var clientiMago = (await _magoRepo.GetClientiAsync(lastSync))
                .GroupBy(c => c.Codice)
                .Select(g => g.First())
                .ToList();

            // Backup dati
            if (clientiMago.Any())
            {
                log.FileBackupPath = await _backup.SaveBackupAsync("Clienti", clientiMago);
            }

            // Sincronizza
            foreach (var clienteMago in clientiMago)
            {
                var cliente = await _context.Clienti
                    .FirstOrDefaultAsync(c => c.Codice == clienteMago.Codice, cancellationToken);

                if (cliente == null)
                {
                    // Nuovo cliente
                    cliente = new Cliente
                    {
                        Id = Guid.NewGuid(),
                        Codice = clienteMago.Codice,
                        RagioneSociale = clienteMago.Nome,
                        Email = clienteMago.Email,
                        Note = clienteMago.Note,
                        Attivo = true,
                        UltimaModifica = DateTime.TryParse(clienteMago.UltimaModifica, out var um) ? um : DateTime.MinValue,
                        TimestampSync = DateTime.Now
                    };
                    _context.Clienti.Add(cliente);
                    log.Nuovi++;
                }
                else
                {
                    // Aggiorna sempre i dati del cliente esistente
                    if (DateTime.TryParse(clienteMago.UltimaModifica, out var um3) && um3 > cliente.UltimaModifica)
                    {
                        cliente.RagioneSociale = clienteMago.Nome;
                        cliente.Email = clienteMago.Email;
                        cliente.Note = clienteMago.Note;
                        cliente.Attivo = true;
                        cliente.UltimaModifica = um3;
                        cliente.TimestampSync = DateTime.Now;
                        log.Aggiornati++;
                    }
                    else
                    {
                        // Aggiorna solo il timestamp di sync
                        cliente.TimestampSync = DateTime.Now;
                        log.Ignorati++;
                    }
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Aggiorna SyncState
            if (syncState == null)
            {
                syncState = new SyncState
                {
                    Id = Guid.NewGuid(),
                    Modulo = "Clienti"
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
