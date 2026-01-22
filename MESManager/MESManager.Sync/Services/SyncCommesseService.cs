using Microsoft.EntityFrameworkCore;
using MESManager.Domain.Entities;
using MESManager.Domain.Enums;
using MESManager.Infrastructure.Data;
using MESManager.Sync.Backup;
using MESManager.Sync.Repositories;
using Microsoft.Extensions.Logging;

namespace MESManager.Sync.Services;

public class SyncCommesseService
{
    private readonly MagoRepository _magoRepo;
    private readonly MesManagerDbContext _context;
    private readonly JsonBackupService _backup;
    private readonly ILogger<SyncCommesseService> _logger;

    public SyncCommesseService(
        MagoRepository magoRepo,
        MesManagerDbContext context,
        JsonBackupService backup,
        ILogger<SyncCommesseService> logger)
    {
        _magoRepo = magoRepo;
        _context = context;
        _backup = backup;
        _logger = logger;
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
                
                // NUOVA LOGICA: Aggiungi anime se codiceArticolo non presente, oppure aggiorna cliente
                var anime = await _context.Anime
                    .FirstOrDefaultAsync(a => a.CodiceArticolo == commessaMago.Item, cancellationToken);
                
                if (anime == null && !string.IsNullOrWhiteSpace(commessaMago.Item))
                {
                    _logger.LogInformation("Articolo {Codice} non presente in catalogo anime - creazione automatica", commessaMago.Item);
                    
                    anime = new Anime
                    {
                        CodiceArticolo = commessaMago.Item,
                        DescrizioneArticolo = commessaMago.Description ?? string.Empty,
                        Cliente = commessaMago.CompanyName,
                        DataImportazione = DateTime.Now,
                        ModificatoLocalmente = false
                    };
                    
                    _context.Anime.Add(anime);
                    _logger.LogInformation("✓ Anime creata: {Codice} - {Descrizione}", anime.CodiceArticolo, anime.DescrizioneArticolo);
                }
                else if (anime != null && !string.IsNullOrWhiteSpace(commessaMago.CompanyName))
                {
                    // Sincronizza il cliente dell'anime con la ragione sociale della commessa
                    if (anime.Cliente != commessaMago.CompanyName)
                    {
                        var oldCliente = anime.Cliente;
                        anime.Cliente = commessaMago.CompanyName;
                        _context.Anime.Update(anime);
                        _logger.LogInformation("✓ Cliente anime aggiornato: {Codice} - da '{OldCliente}' a '{NewCliente}'", 
                            anime.CodiceArticolo, oldCliente ?? "(vuoto)", commessaMago.CompanyName);
                    }
                }

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
                        
                        // Riferimenti Mago
                        SaleOrdId = commessaMago.SaleOrdId,
                        InternalOrdNo = commessaMago.InternalOrdNo,
                        ExternalOrdNo = commessaMago.ExternalOrdNo,
                        Line = commessaMago.Line,
                        
                        // Relazioni
                        ClienteId = cliente?.Id,
                        ArticoloId = articolo?.Id,
                        CompanyName = commessaMago.CompanyName,
                        
                        // Dati commessa
                        Description = commessaMago.Description,
                        QuantitaRichiesta = decimal.TryParse(commessaMago.Qty, out var q) ? q : 0,
                        UoM = commessaMago.UoM,
                        DataConsegna = DateTime.TryParse(commessaMago.ExpectedDeliveryDate, out var dc) ? dc : null,
                        Stato = MapStatoCommessaDaMago(commessaMago.Delivered, commessaMago.Invoiced),
                        
                        // Riferimenti
                        RiferimentoOrdineCliente = commessaMago.YourReference,
                        OurReference = commessaMago.OurReference,
                        
                        // Audit
                        UltimaModifica = DateTime.TryParse(commessaMago.TBModified, out var um) ? um : DateTime.MinValue,
                        TimestampSync = DateTime.Now
                    };
                    _context.Commesse.Add(commessa);
                    log.Nuovi++;
                }
                else if (DateTime.TryParse(commessaMago.TBModified, out var um3) && um3 > commessa.UltimaModifica)
                {
                    // Riferimenti Mago
                    commessa.SaleOrdId = commessaMago.SaleOrdId;
                    commessa.InternalOrdNo = commessaMago.InternalOrdNo;
                    commessa.ExternalOrdNo = commessaMago.ExternalOrdNo;
                    commessa.Line = commessaMago.Line;
                    
                    // Relazioni
                    commessa.ClienteId = cliente?.Id;
                    commessa.ArticoloId = articolo?.Id;
                    commessa.CompanyName = commessaMago.CompanyName;
                    
                    // Dati commessa
                    commessa.Description = commessaMago.Description;
                    commessa.QuantitaRichiesta = decimal.TryParse(commessaMago.Qty, out var q2) ? q2 : 0;
                    commessa.UoM = commessaMago.UoM;
                    commessa.DataConsegna = DateTime.TryParse(commessaMago.ExpectedDeliveryDate, out var dc2) ? dc2 : null;
                    commessa.Stato = MapStatoCommessaDaMago(commessaMago.Delivered, commessaMago.Invoiced);
                    
                    // Riferimenti
                    commessa.RiferimentoOrdineCliente = commessaMago.YourReference;
                    commessa.OurReference = commessaMago.OurReference;
                    
                    // Audit
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
        // Delivered = 0 -> Aperta (da consegnare)
        // Delivered = 1 -> Chiusa (consegnata)
        if (delivered == "0")
            return StatoCommessa.Aperta;
        
        return StatoCommessa.Chiusa;
    }
}
