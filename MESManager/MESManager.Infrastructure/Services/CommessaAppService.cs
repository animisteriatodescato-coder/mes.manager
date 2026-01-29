using Microsoft.EntityFrameworkCore;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;
using MESManager.Domain.Enums;
using MESManager.Domain.Constants;
using MESManager.Infrastructure.Data;

namespace MESManager.Infrastructure.Services;

public class CommessaAppService : ICommessaAppService
{
    private readonly MesManagerDbContext _context;
    
    // Lookup tables statiche (stesse di AnimeService)
    private static readonly Dictionary<string, string> VerniceLookup = new()
    {
        { "-1", "" },
        { "-2", "YELLOW COVER" },
        { "-3", "CASTING COVER ZR" },
        { "-4", "CASTING COVER RK" },
        { "-5", "CASTINGCOVER 2001" },
        { "-6", "ARCOPAL 9030" },
        { "-7", "HYDRO COVER 22 Z" },
        { "-8", "FGR 55" }
    };
    
    public CommessaAppService(MesManagerDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<CommessaDto>> GetListaAsync()
    {
        var commesse = await _context.Commesse
            .Include(c => c.Articolo)
            .Include(c => c.Cliente)
            .ToListAsync();
            
        var articoloCodes = commesse
            .Where(c => c.Articolo != null)
            .Select(c => c.Articolo!.Codice)
            .Distinct()
            .ToList();
            
        var animeData = await _context.Anime
            .Where(a => articoloCodes.Contains(a.CodiceArticolo))
            .ToListAsync();
            
        var animeLookup = animeData
            .GroupBy(a => a.CodiceArticolo)
            .ToDictionary(g => g.Key, g => g.First());
        
        return commesse.Select(c =>
        {
            Anime? anime = null;
            if (c.Articolo != null && animeLookup.TryGetValue(c.Articolo.Codice, out var a))
            {
                anime = a;
            }
            
            return new CommessaDto
            {
                Id = c.Id,
                Codice = c.Codice,
                
                // Riferimenti Mago
                SaleOrdId = c.SaleOrdId,
                InternalOrdNo = c.InternalOrdNo,
                ExternalOrdNo = c.ExternalOrdNo,
                Line = c.Line,
                
                // Relazioni
                ArticoloId = c.ArticoloId,
                ClienteId = c.ClienteId,
                ClienteRagioneSociale = c.CompanyName ?? (c.Cliente != null ? c.Cliente.RagioneSociale : null),
                CompanyName = c.CompanyName,
                ArticoloCodice = c.Articolo != null ? c.Articolo.Codice : null,
                ArticoloDescrizione = c.Articolo != null ? c.Articolo.Descrizione : null,
                ArticoloPrezzo = c.Articolo != null ? c.Articolo.Prezzo : null,
                
                // Dati commessa
                Description = c.Description,
                QuantitaRichiesta = c.QuantitaRichiesta,
                UoM = c.UoM,
                DataConsegna = c.DataConsegna,
                Stato = c.Stato.ToString(),
                
                // Stato programma interno
                StatoProgramma = c.StatoProgramma.ToString(),
                DataCambioStatoProgramma = c.DataCambioStatoProgramma,
                
                // Riferimenti
                RiferimentoOrdineCliente = c.RiferimentoOrdineCliente,
                OurReference = c.OurReference,
                
                // Programmazione Macchine
                NumeroMacchina = c.NumeroMacchina,
                OrdineSequenza = c.OrdineSequenza,
                
                // Audit
                UltimaModifica = c.UltimaModifica,
                TimestampSync = c.TimestampSync,
                
                // Anime properties
                UnitaMisura = anime?.UnitaMisura,
                Larghezza = anime?.Larghezza,
                Altezza = anime?.Altezza,
                Profondita = anime?.Profondita,
                Imballo = anime?.Imballo,
                ImballoDescrizione = (anime != null && anime.Imballo.HasValue && LookupTables.ImballoInt.TryGetValue(anime.Imballo.Value, out var imbDesc)) ? imbDesc : null,
                NoteAnime = anime?.Note,
                Allegato = anime?.Allegato,
                Peso = anime?.Peso,
                Ubicazione = anime?.Ubicazione,
                Ciclo = anime?.Ciclo,
                CodiceCassa = anime?.CodiceCassa,
                CodiceAnime = anime?.CodiceAnime,
                MacchineSuDisponibili = anime?.MacchineSuDisponibili,
                MacchineSuDisponibiliDescrizione = anime?.MacchineSuDisponibili, // Già contiene i nomi macchine (es. "M001;M002")
                TrasmettiTutto = anime?.TrasmettiTutto,
                
                // Campi aggiuntivi per etichetta
                Sabbia = anime?.Sabbia,
                SabbiaDescrizione = (anime != null && !string.IsNullOrEmpty(anime.Sabbia) && LookupTables.Sabbia.TryGetValue(anime.Sabbia, out var sabDesc)) ? sabDesc : anime?.Sabbia,
                Vernice = anime?.Vernice,
                VerniceDescrizione = (anime != null && !string.IsNullOrEmpty(anime.Vernice) && VerniceLookup.TryGetValue(anime.Vernice, out var vernDesc)) ? vernDesc : anime?.Vernice,
                Colla = anime?.Colla,
                CollaDescrizione = (anime != null && !string.IsNullOrEmpty(anime.Colla) && LookupTables.Colla.TryGetValue(anime.Colla, out var collaDesc)) ? collaDesc : anime?.Colla,
                QuantitaPiano = anime?.QuantitaPiano,
                NumeroPiani = anime?.NumeroPiani,
                ClienteAnime = anime?.Cliente,
                
                // Campi anime aggiuntivi
                TogliereSparo = anime?.TogliereSparo,
                Figure = anime?.Figure,
                Maschere = anime?.Maschere,
                Assemblata = anime?.Assemblata,
                ArmataL = anime?.ArmataL
            };
        }).ToList();
    }
    
    public async Task<CommessaDto?> GetByIdAsync(Guid id)
    {
        var commessa = await _context.Commesse
            .Include(c => c.Articolo)
            .Include(c => c.Cliente)
            .FirstOrDefaultAsync(c => c.Id == id);
            
        if (commessa == null) return null;
        
        return new CommessaDto
        {
            Id = commessa.Id,
            Codice = commessa.Codice,
            ArticoloId = commessa.ArticoloId,
            ClienteId = commessa.ClienteId,
            QuantitaRichiesta = commessa.QuantitaRichiesta
        };
    }
    
    public async Task<CommessaDto> CreaAsync(CommessaDto dto)
    {
        var commessa = new Commessa
        {
            Codice = dto.Codice,
            ArticoloId = dto.ArticoloId,
            ClienteId = dto.ClienteId,
            QuantitaRichiesta = dto.QuantitaRichiesta,
            Stato = StatoCommessa.Aperta
        };
        
        _context.Commesse.Add(commessa);
        await _context.SaveChangesAsync();
        
        return new CommessaDto
        {
            Id = commessa.Id,
            Codice = commessa.Codice,
            ArticoloId = commessa.ArticoloId,
            ClienteId = commessa.ClienteId,
            QuantitaRichiesta = commessa.QuantitaRichiesta
        };
    }
    
    public async Task<CommessaDto> AggiornaAsync(Guid id, CommessaDto dto)
    {
        var commessa = await _context.Commesse.FindAsync(id);
        if (commessa == null) throw new Exception("Commessa non trovata");
        
        commessa.Codice = dto.Codice;
        commessa.ArticoloId = dto.ArticoloId;
        commessa.ClienteId = dto.ClienteId;
        commessa.QuantitaRichiesta = dto.QuantitaRichiesta;
        
        await _context.SaveChangesAsync();
        
        return new CommessaDto
        {
            Id = commessa.Id,
            Codice = commessa.Codice,
            ArticoloId = commessa.ArticoloId,
            ClienteId = commessa.ClienteId,
            QuantitaRichiesta = commessa.QuantitaRichiesta
        };
    }
    
    public async Task AggiornaStatoAsync(Guid id, string stato)
    {
        var commessa = await _context.Commesse.FindAsync(id);
        if (commessa == null) throw new Exception("Commessa non trovata");
        
        commessa.Stato = Enum.Parse<StatoCommessa>(stato);
        await _context.SaveChangesAsync();
    }

    public async Task AggiornaNumeroMacchinaAsync(Guid id, string? numeroMacchina)
    {
        var commessa = await _context.Commesse.FindAsync(id);
        if (commessa == null) throw new Exception("Commessa non trovata");
        
        var vecchioNumeroMacchina = commessa.NumeroMacchina;
        commessa.NumeroMacchina = numeroMacchina;
        
        // Se assegno una macchina e lo stato è NonProgrammata, passa automaticamente a Programmata
        if (!string.IsNullOrEmpty(numeroMacchina) && commessa.StatoProgramma == StatoProgramma.NonProgrammata)
        {
            var statoPrecedente = commessa.StatoProgramma;
            commessa.StatoProgramma = StatoProgramma.Programmata;
            commessa.DataCambioStatoProgramma = DateTime.Now;
            
            // Crea record storico per il cambio automatico
            var storico = new StoricoProgrammazione
            {
                Id = Guid.NewGuid(),
                CommessaId = id,
                StatoPrecedente = statoPrecedente,
                StatoNuovo = StatoProgramma.Programmata,
                DataModifica = DateTime.Now,
                Note = $"Cambio automatico per assegnazione macchina {numeroMacchina}"
            };
            _context.StoricoProgrammazione.Add(storico);
        }
        
        commessa.UltimaModifica = DateTime.Now;
        await _context.SaveChangesAsync();
    }

    public async Task AggiornaStatoProgrammaAsync(Guid id, string statoProgramma, string? note = null, string? utente = null)
    {
        var commessa = await _context.Commesse.FindAsync(id);
        if (commessa == null) throw new Exception("Commessa non trovata");
        
        var nuovoStato = Enum.Parse<StatoProgramma>(statoProgramma);
        var statoPrecedente = commessa.StatoProgramma;
        
        // Se passo a NonProgrammata o Archiviata, rimuovo la macchina assegnata
        if (nuovoStato == StatoProgramma.NonProgrammata || nuovoStato == StatoProgramma.Archiviata)
        {
            if (!string.IsNullOrEmpty(commessa.NumeroMacchina))
            {
                note = (note ?? "") + $" [Rimossa macchina {commessa.NumeroMacchina}]";
                commessa.NumeroMacchina = null;
            }
        }
        
        // Crea record storico
        var storico = new StoricoProgrammazione
        {
            Id = Guid.NewGuid(),
            CommessaId = id,
            StatoPrecedente = statoPrecedente,
            StatoNuovo = nuovoStato,
            DataModifica = DateTime.Now,
            UtenteModifica = utente,
            Note = note
        };
        _context.StoricoProgrammazione.Add(storico);
        
        // Aggiorna commessa
        commessa.StatoProgramma = nuovoStato;
        commessa.DataCambioStatoProgramma = DateTime.Now;
        commessa.UltimaModifica = DateTime.Now;
        
        await _context.SaveChangesAsync();
    }

    public async Task<List<StoricoProgrammazioneDto>> GetStoricoProgrammazioneAsync(Guid commessaId)
    {
        var storico = await _context.StoricoProgrammazione
            .Where(s => s.CommessaId == commessaId)
            .OrderByDescending(s => s.DataModifica)
            .ToListAsync();
        
        // Ottiene il codice commessa
        var commessa = await _context.Commesse.FindAsync(commessaId);
        var codiceCommessa = commessa?.Codice;
        
        return storico.Select(s => new StoricoProgrammazioneDto
        {
            Id = s.Id,
            CommessaId = s.CommessaId,
            NumeroCommessa = codiceCommessa,
            StatoPrecedente = s.StatoPrecedente.ToString(),
            StatoNuovo = s.StatoNuovo.ToString(),
            DataModifica = s.DataModifica,
            UtenteModifica = s.UtenteModifica,
            Note = s.Note
        }).ToList();
    }

    public async Task RiordinaCommessaAsync(Guid commessaId, string nuovoNumeroMacchina, int nuovaPosizioneIndex)
    {
        var commessa = await _context.Commesse.FindAsync(commessaId);
        if (commessa == null) throw new Exception("Commessa non trovata");

        var vecchioNumeroMacchina = commessa.NumeroMacchina;
        var cambioMacchina = vecchioNumeroMacchina != nuovoNumeroMacchina;

        // 1. Aggiorna la macchina e lo stato se necessario
        commessa.NumeroMacchina = nuovoNumeroMacchina;
        commessa.UltimaModifica = DateTime.Now;

        // Se assegno una macchina e lo stato è NonProgrammata, passa automaticamente a Programmata
        if (!string.IsNullOrEmpty(nuovoNumeroMacchina) && commessa.StatoProgramma == StatoProgramma.NonProgrammata)
        {
            var statoPrecedente = commessa.StatoProgramma;
            commessa.StatoProgramma = StatoProgramma.Programmata;
            commessa.DataCambioStatoProgramma = DateTime.Now;

            var storico = new StoricoProgrammazione
            {
                Id = Guid.NewGuid(),
                CommessaId = commessaId,
                StatoPrecedente = statoPrecedente,
                StatoNuovo = StatoProgramma.Programmata,
                DataModifica = DateTime.Now,
                Note = $"Cambio automatico per riordinamento su macchina {nuovoNumeroMacchina}"
            };
            _context.StoricoProgrammazione.Add(storico);
        }

        // 2. Ricalcola OrdineSequenza per la macchina di DESTINAZIONE
        var commesseDestinazione = await _context.Commesse
            .Where(c => c.NumeroMacchina == nuovoNumeroMacchina && c.Id != commessaId)
            .OrderBy(c => c.OrdineSequenza)
            .ThenBy(c => c.DataConsegna)
            .ToListAsync();

        // Inserisce la commessa nella posizione corretta
        var listaOrdinata = new List<Commessa>();
        for (int i = 0; i < commesseDestinazione.Count; i++)
        {
            if (i == nuovaPosizioneIndex)
            {
                listaOrdinata.Add(commessa);
            }
            listaOrdinata.Add(commesseDestinazione[i]);
        }
        // Se la posizione è alla fine o oltre
        if (nuovaPosizioneIndex >= commesseDestinazione.Count)
        {
            listaOrdinata.Add(commessa);
        }

        // Assegna ordineSequenza sequenziale
        for (int i = 0; i < listaOrdinata.Count; i++)
        {
            listaOrdinata[i].OrdineSequenza = i + 1;
        }

        // 3. Se cambio macchina, ricalcola anche OrdineSequenza per macchina di ORIGINE
        if (cambioMacchina && !string.IsNullOrEmpty(vecchioNumeroMacchina))
        {
            var commesseOrigine = await _context.Commesse
                .Where(c => c.NumeroMacchina == vecchioNumeroMacchina && c.Id != commessaId)
                .OrderBy(c => c.OrdineSequenza)
                .ThenBy(c => c.DataConsegna)
                .ToListAsync();

            for (int i = 0; i < commesseOrigine.Count; i++)
            {
                commesseOrigine[i].OrdineSequenza = i + 1;
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task EliminaAsync(Guid id)
    {
        var commessa = await _context.Commesse.FindAsync(id);
        if (commessa == null) throw new Exception("Commessa non trovata");
        
        _context.Commesse.Remove(commessa);
        await _context.SaveChangesAsync();
    }
}
