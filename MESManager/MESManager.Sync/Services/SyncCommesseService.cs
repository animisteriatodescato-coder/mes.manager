using Microsoft.EntityFrameworkCore;
using MESManager.Domain.Entities;
using MESManager.Domain.Enums;
using MESManager.Infrastructure.Data;
using MESManager.Sync.Backup;
using MESManager.Sync.Repositories;

namespace MESManager.Sync.Services;

public class SyncCommesseService
{
    private readonly MagoRepository _magoRepo;
    private readonly MesManagerDbContext _context;
    private readonly JsonBackupService _backup;

    public SyncCommesseService(
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
            Modulo = "Commesse"
        };

        try
        {
            var syncState = await _context.SyncStates
                .FirstOrDefaultAsync(s => s.Modulo == "Commesse", cancellationToken);

            var lastSync = syncState?.UltimaSyncRiuscita;
            var commesseMago = await _magoRepo.GetCommesseAsync(lastSync);

            if (commesseMago.Any())
            {
                log.FileBackupPath = await _backup.SaveBackupAsync("Commesse", commesseMago);
            }

            foreach (var commessaMago in commesseMago)
            {
                // Trova/crea cliente
                var cliente = await _context.Clienti
                    .FirstOrDefaultAsync(c => c.Codice == commessaMago.Customer, cancellationToken);

                var articolo = await _context.Articoli
                    .FirstOrDefaultAsync(a => a.Codice == commessaMago.Item, cancellationToken);

                // Usa la combinazione InternalOrdNo + Item come chiave univoca (come PlcMagoSync)
                var codiceCommessa = $"{commessaMago.InternalOrdNo}-{commessaMago.Item}";
                var commessa = await _context.Commesse
                    .FirstOrDefaultAsync(c => c.Codice == codiceCommessa, cancellationToken);

                if (commessa == null)
                {
                    commessa = new Commessa
                    {
                        Id = Guid.NewGuid(),
                        Codice = codiceCommessa,
                        ClienteId = cliente?.Id,
                        ArticoloId = articolo?.Id,
                        QuantitaRichiesta = decimal.TryParse(commessaMago.Qty, out var q) ? q : 0,
                        DataConsegna = DateTime.TryParse(commessaMago.ExpectedDeliveryDate, out var dc) ? dc : null,
                        Stato = MapStatoCommessaDaMago(commessaMago.Delivered, commessaMago.Invoiced),
                        RiferimentoOrdineCliente = commessaMago.YourReference,
                        UltimaModifica = DateTime.TryParse(commessaMago.TBModified, out var um) ? um : DateTime.MinValue,
                        TimestampSync = DateTime.Now
                    };
                    _context.Commesse.Add(commessa);
                    log.Nuovi++;
                }
                else if (DateTime.TryParse(commessaMago.TBModified, out var um3) && um3 > commessa.UltimaModifica)
                {
                    commessa.ClienteId = cliente?.Id;
                    commessa.ArticoloId = articolo?.Id;
                    commessa.QuantitaRichiesta = decimal.TryParse(commessaMago.Qty, out var q2) ? q2 : 0;
                    commessa.DataConsegna = DateTime.TryParse(commessaMago.ExpectedDeliveryDate, out var dc2) ? dc2 : null;
                    // Aggiorna lo stato da Mago
                    commessa.Stato = MapStatoCommessaDaMago(commessaMago.Delivered, commessaMago.Invoiced);
                    commessa.RiferimentoOrdineCliente = commessaMago.YourReference;
                    commessa.UltimaModifica = DateTime.TryParse(commessaMago.TBModified, out var um2) ? um2 : DateTime.MinValue;
                    commessa.TimestampSync = DateTime.Now;
                    log.Aggiornati++;
                }
                else
                {
                    // Aggiorna comunque ClienteId, ArticoloId e Stato
                    bool updated = false;
                    if (commessa.ClienteId != cliente?.Id)
                    {
                        commessa.ClienteId = cliente?.Id;
                        updated = true;
                    }
                    if (commessa.ArticoloId != articolo?.Id)
                    {
                        commessa.ArticoloId = articolo?.Id;
                        updated = true;
                    }
                    
                    // Aggiorna sempre lo stato da Mago, indipendentemente dalla data di modifica
                    var nuovoStato = MapStatoCommessaDaMago(commessaMago.Delivered, commessaMago.Invoiced);
                    if (commessa.Stato != nuovoStato)
                    {
                        commessa.Stato = nuovoStato;
                        updated = true;
                    }
                    
                    if (updated)
                    {
                        commessa.TimestampSync = DateTime.Now;
                        log.Aggiornati++;
                    }
                    else
                    {
                        log.Ignorati++;
                    }
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (syncState == null)
            {
                syncState = new SyncState
                {
                    Id = Guid.NewGuid(),
                    Modulo = "Commesse"
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

    private StatoCommessa MapStatoCommessaDaMago(string delivered, string invoiced)
    {
        // Delivered = 0 -> Chiusa (consegnata)
        // Delivered = 1 -> Aperta (da consegnare)
        if (delivered == "0")
            return StatoCommessa.Chiusa;
        
        return StatoCommessa.Aperta;
    }
}
