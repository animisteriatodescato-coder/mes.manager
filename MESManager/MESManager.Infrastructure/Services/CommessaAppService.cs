using Microsoft.EntityFrameworkCore;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;
using MESManager.Domain.Enums;
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
                
                // Riferimenti
                RiferimentoOrdineCliente = c.RiferimentoOrdineCliente,
                OurReference = c.OurReference,
                
                // Programmazione Macchine
                NumeroMacchina = c.NumeroMacchina,
                
                // Audit
                UltimaModifica = c.UltimaModifica,
                TimestampSync = c.TimestampSync,
                
                // Anime properties
                UnitaMisura = anime?.UnitaMisura,
                Larghezza = anime?.Larghezza,
                Altezza = anime?.Altezza,
                Profondita = anime?.Profondita,
                Imballo = anime?.Imballo,
                NoteAnime = anime?.Note,
                Allegato = anime?.Allegato,
                Peso = anime?.Peso,
                Ubicazione = anime?.Ubicazione,
                Ciclo = anime?.Ciclo,
                CodiceCassa = anime?.CodiceCassa,
                CodiceAnime = anime?.CodiceAnime,
                MacchineSuDisponibili = anime?.MacchineSuDisponibili,
                TrasmettiTutto = anime?.TrasmettiTutto,
                
                // Campi aggiuntivi per etichetta
                Sabbia = anime?.Sabbia,
                SabbiaDescrizione = anime?.Sabbia, // Sabbia usa già la descrizione come codice
                Vernice = anime?.Vernice,
                VerniceDescrizione = !string.IsNullOrEmpty(anime?.Vernice) && VerniceLookup.TryGetValue(anime.Vernice, out var vernDesc) ? vernDesc : anime?.Vernice,
                Colla = anime?.Colla,
                CollaDescrizione = anime?.Colla, // Per ora usiamo il codice
                QuantitaPiano = anime?.QuantitaPiano,
                NumeroPiani = anime?.NumeroPiani,
                ClienteAnime = anime?.Cliente
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

    public async Task AggiornaNumeroMacchinaAsync(Guid id, int? numeroMacchina)
    {
        var commessa = await _context.Commesse.FindAsync(id);
        if (commessa == null) throw new Exception("Commessa non trovata");
        
        commessa.NumeroMacchina = numeroMacchina;
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
