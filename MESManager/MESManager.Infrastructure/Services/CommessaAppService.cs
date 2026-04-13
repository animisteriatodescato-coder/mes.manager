using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Globalization;
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

    public async Task<List<string>> GetClienteNomiDistinctAsync()
    {
        return await _context.Commesse
            .Where(c => c.CompanyName != null && c.CompanyName != string.Empty)
            .Select(c => c.CompanyName!)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync();
    }
    
    public async Task<List<CommessaDto>> GetListaAsync()
    {
        var commesse = new List<CommessaProjection>();

        var connectionString = _context.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string MESManagerDb non configurata.");
        }

        using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT
    c.Id,
    c.Codice,
    c.SaleOrdId,
    c.InternalOrdNo,
    c.ExternalOrdNo,
    c.Line,
    c.ArticoloId,
    c.ClienteId,
    c.CompanyName,
    c.Description,
    c.QuantitaRichiesta,
    c.UoM,
    c.DataConsegna,
    CAST(c.Stato AS nvarchar(50)) AS StatoRaw,
    CAST(c.StatoProgramma AS nvarchar(50)) AS StatoProgrammaRaw,
    c.DataCambioStatoProgramma,
    c.RiferimentoOrdineCliente,
    c.OurReference,
    CAST(c.NumeroMacchina AS nvarchar(50)) AS NumeroMacchinaRaw,
    CAST(c.OrdineSequenza AS nvarchar(50)) AS OrdineSequenzaRaw,
    c.DataInizioPrevisione,
    c.DataFinePrevisione,
    c.DataInizioProduzione,
    c.DataFineProduzione,
    c.UltimaModifica,
    c.TimestampSync,
    a.Codice AS ArticoloCodice,
    a.Descrizione AS ArticoloDescrizione,
    a.Prezzo AS ArticoloPrezzo,
    cl.RagioneSociale AS ClienteRagioneSociale
FROM Commesse c
LEFT JOIN Articoli a ON a.Id = c.ArticoloId
LEFT JOIN Clienti cl ON cl.Id = c.ClienteId";

            using var reader = await command.ExecuteReaderAsync();
            var ordinalId = reader.GetOrdinal("Id");
            var ordinalCodice = reader.GetOrdinal("Codice");
            var ordinalSaleOrdId = reader.GetOrdinal("SaleOrdId");
            var ordinalInternalOrdNo = reader.GetOrdinal("InternalOrdNo");
            var ordinalExternalOrdNo = reader.GetOrdinal("ExternalOrdNo");
            var ordinalLine = reader.GetOrdinal("Line");
            var ordinalArticoloId = reader.GetOrdinal("ArticoloId");
            var ordinalClienteId = reader.GetOrdinal("ClienteId");
            var ordinalCompanyName = reader.GetOrdinal("CompanyName");
            var ordinalDescription = reader.GetOrdinal("Description");
            var ordinalQuantitaRichiesta = reader.GetOrdinal("QuantitaRichiesta");
            var ordinalUoM = reader.GetOrdinal("UoM");
            var ordinalDataConsegna = reader.GetOrdinal("DataConsegna");
            var ordinalStatoRaw = reader.GetOrdinal("StatoRaw");
            var ordinalStatoProgrammaRaw = reader.GetOrdinal("StatoProgrammaRaw");
            var ordinalDataCambioStatoProgramma = reader.GetOrdinal("DataCambioStatoProgramma");
            var ordinalRiferimentoOrdineCliente = reader.GetOrdinal("RiferimentoOrdineCliente");
            var ordinalOurReference = reader.GetOrdinal("OurReference");
            var ordinalNumeroMacchinaRaw = reader.GetOrdinal("NumeroMacchinaRaw");
            var ordinalOrdineSequenzaRaw = reader.GetOrdinal("OrdineSequenzaRaw");
            var ordinalDataInizioPrevisione = reader.GetOrdinal("DataInizioPrevisione");
            var ordinalDataFinePrevisione = reader.GetOrdinal("DataFinePrevisione");
            var ordinalDataInizioProduzione = reader.GetOrdinal("DataInizioProduzione");
            var ordinalDataFineProduzione = reader.GetOrdinal("DataFineProduzione");
            var ordinalUltimaModifica = reader.GetOrdinal("UltimaModifica");
            var ordinalTimestampSync = reader.GetOrdinal("TimestampSync");
            var ordinalArticoloCodice = reader.GetOrdinal("ArticoloCodice");
            var ordinalArticoloDescrizione = reader.GetOrdinal("ArticoloDescrizione");
            var ordinalArticoloPrezzo = reader.GetOrdinal("ArticoloPrezzo");
            var ordinalClienteRagioneSociale = reader.GetOrdinal("ClienteRagioneSociale");

            while (await reader.ReadAsync())
            {
                var statoRaw = GetNullableString(reader, ordinalStatoRaw);
                var statoProgrammaRaw = GetNullableString(reader, ordinalStatoProgrammaRaw);
                var ordineSequenzaRaw = GetNullableString(reader, ordinalOrdineSequenzaRaw);

                commesse.Add(new CommessaProjection
                {
                    Id = reader.GetGuid(ordinalId),
                    Codice = reader.GetString(ordinalCodice),
                    SaleOrdId = GetNullableString(reader, ordinalSaleOrdId),
                    InternalOrdNo = GetNullableString(reader, ordinalInternalOrdNo),
                    ExternalOrdNo = GetNullableString(reader, ordinalExternalOrdNo),
                    Line = GetNullableString(reader, ordinalLine),
                    ArticoloId = GetNullableGuid(reader, ordinalArticoloId),
                    ClienteId = GetNullableGuid(reader, ordinalClienteId),
                    CompanyName = GetNullableString(reader, ordinalCompanyName),
                    Description = GetNullableString(reader, ordinalDescription),
                    QuantitaRichiesta = GetDecimal(reader, ordinalQuantitaRichiesta),
                    UoM = GetNullableString(reader, ordinalUoM),
                    DataConsegna = GetNullableDateTime(reader, ordinalDataConsegna),
                    Stato = ParseStatoCommessa(statoRaw),
                    StatoProgramma = ParseStatoProgramma(statoProgrammaRaw),
                    DataCambioStatoProgramma = GetNullableDateTime(reader, ordinalDataCambioStatoProgramma),
                    RiferimentoOrdineCliente = GetNullableString(reader, ordinalRiferimentoOrdineCliente),
                    OurReference = GetNullableString(reader, ordinalOurReference),
                    NumeroMacchinaRaw = GetNullableString(reader, ordinalNumeroMacchinaRaw),
                    OrdineSequenza = ParseInt(ordineSequenzaRaw, 0),
                    DataInizioPrevisione = GetNullableDateTime(reader, ordinalDataInizioPrevisione),
                    DataFinePrevisione = GetNullableDateTime(reader, ordinalDataFinePrevisione),
                    DataInizioProduzione = GetNullableDateTime(reader, ordinalDataInizioProduzione),
                    DataFineProduzione = GetNullableDateTime(reader, ordinalDataFineProduzione),
                    UltimaModifica = reader.GetDateTime(ordinalUltimaModifica),
                    TimestampSync = reader.GetDateTime(ordinalTimestampSync),
                    ArticoloCodice = GetNullableString(reader, ordinalArticoloCodice),
                    ArticoloDescrizione = GetNullableString(reader, ordinalArticoloDescrizione),
                    ArticoloPrezzo = GetNullableDecimal(reader, ordinalArticoloPrezzo),
                    ClienteRagioneSociale = GetNullableString(reader, ordinalClienteRagioneSociale)
                });
            }
        }
            
        var articoloCodes = commesse
            .Where(c => !string.IsNullOrEmpty(c.ArticoloCodice))
            .Select(c => c.ArticoloCodice!)
            .Distinct()
            .ToList();
            
        var animeData = await _context.Anime
            .Where(a => articoloCodes.Contains(a.CodiceArticolo))
            .ToListAsync();
            
        var animeLookup = animeData
            .GroupBy(a => a.CodiceArticolo)
            .ToDictionary(g => g.Key, g => g.First());
        
        // Carica le ricette per gli articoli
        var articoloIds = commesse
            .Where(c => c.ArticoloId.HasValue)
            .Select(c => c.ArticoloId!.Value)
            .Distinct()
            .ToList();
        
        var ricette = await _context.Ricette
            .Where(r => articoloIds.Contains(r.ArticoloId))
            .Include(r => r.Parametri)
            .Include(r => r.Articolo)
            .Select(r => new 
            { 
                r.ArticoloId, 
                NumeroParametri = r.Parametri.Count,
                UltimaModifica = r.Articolo.UltimaModifica
            })
            .ToListAsync();
        
        var ricettaLookup = ricette.ToDictionary(r => r.ArticoloId);
        
        return commesse.Select(c =>
        {
            Anime? anime = null;
            if (!string.IsNullOrEmpty(c.ArticoloCodice) && animeLookup.TryGetValue(c.ArticoloCodice, out var a))
            {
                anime = a;
            }

            var numeroMacchina = ParseNullableInt(c.NumeroMacchinaRaw);
            
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
                ClienteRagioneSociale = c.ClienteRagioneSociale,
                CompanyName = c.CompanyName, // Mantenuto per fallback (dati Mago)
                ArticoloCodice = c.ArticoloCodice,
                ArticoloDescrizione = c.ArticoloDescrizione,
                ArticoloPrezzo = c.ArticoloPrezzo,
                
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
                NumeroMacchina = numeroMacchina,
                OrdineSequenza = c.OrdineSequenza,
                
                // Pianificazione produzione
                DataInizioPrevisione = c.DataInizioPrevisione,
                DataFinePrevisione = c.DataFinePrevisione,
                DataInizioProduzione = c.DataInizioProduzione,
                DataFineProduzione = c.DataFineProduzione,
                
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
                ArmataL = anime?.ArmataL,
                
                // Flag ricetta configurata
                HasRicetta = c.ArticoloId.HasValue && ricettaLookup.ContainsKey(c.ArticoloId.Value),
                NumeroParametri = c.ArticoloId.HasValue && ricettaLookup.TryGetValue(c.ArticoloId.Value, out var ric) ? ric.NumeroParametri : 0,
                RicettaUltimaModifica = c.ArticoloId.HasValue && ricettaLookup.TryGetValue(c.ArticoloId.Value, out var ric2) ? ric2.UltimaModifica : null
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

    // ❌ DISABILITATO v1.31: Assegnazione macchina SOLO da Gantt (master source)
    // Se serve assegnare macchina → usa Gantt Macchine
    // Programma = vista read-only auto-sync del Gantt
    [Obsolete("Usa Gantt per assegnare macchine. Programma è read-only.")]
    public async Task AggiornaNumeroMacchinaAsync(Guid id, int? numeroMacchina)
    {
        throw new InvalidOperationException(
            "Assegnazione macchina disabilitata da questa interfaccia. " +
            "Usa Gantt Macchine come master source. " +
            "Programma si sincronizza automaticamente."
        );
        
        // CODICE ORIGINALE COMMENTATO:
        // var commessa = await _context.Commesse.FindAsync(id);
        // if (commessa == null) throw new Exception("Commessa non trovata");
        // var vecchioNumeroMacchina = commessa.NumeroMacchina;
        // commessa.NumeroMacchina = numeroMacchina;
        // ❌ RIMOSSO: Auto-marcatura StatoProgramma = Programmata (crea confusione)
        // commessa.UltimaModifica = DateTime.Now;
        // await _context.SaveChangesAsync();
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
            if (commessa.NumeroMacchina.HasValue)
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
        string codiceCommessa = commessa?.Codice ?? string.Empty;
        
        return storico.Select(s => new StoricoProgrammazioneDto
        {
            Id = s.Id,
            CommessaId = s.CommessaId,
            NumeroCommessa = codiceCommessa,
            StatoPrecedente = s.StatoPrecedente?.ToString() ?? "",
            StatoNuovo = s.StatoNuovo.ToString(),
            DataModifica = s.DataModifica,
            UtenteModifica = s.UtenteModifica ?? "",
            Note = s.Note ?? ""
        }).ToList();
    }

    public async Task RiordinaCommessaAsync(Guid commessaId, int? nuovoNumeroMacchina, int nuovaPosizioneIndex)
    {
        var commessa = await _context.Commesse.FindAsync(commessaId);
        if (commessa == null) throw new Exception("Commessa non trovata");

        var vecchioNumeroMacchina = commessa.NumeroMacchina;
        var cambioMacchina = vecchioNumeroMacchina != nuovoNumeroMacchina;

        // 1. Aggiorna la macchina e lo stato se necessario
        commessa.NumeroMacchina = nuovoNumeroMacchina;
        commessa.UltimaModifica = DateTime.Now;

        // ❌ RIMOSSO v1.31: Auto-marcatura StatoProgramma creava confusione
        // Ora: assegnazione macchina non implica "programmata" automaticamente

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
        if (cambioMacchina && vecchioNumeroMacchina.HasValue)
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

    private static string? GetNullableString(DbDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static Guid? GetNullableGuid(DbDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : reader.GetGuid(ordinal);
    }

    private static DateTime? GetNullableDateTime(DbDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }

    private static decimal GetDecimal(DbDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? 0m : reader.GetDecimal(ordinal);
    }

    private static decimal? GetNullableDecimal(DbDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
    }

    private static int ParseInt(string? value, int fallback)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;
    }

    private static int? ParseNullableInt(string? value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static StatoCommessa ParseStatoCommessa(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return StatoCommessa.Aperta;
        }

        if (int.TryParse(value, out var numeric) && Enum.IsDefined(typeof(StatoCommessa), numeric))
        {
            return (StatoCommessa)numeric;
        }

        return Enum.TryParse<StatoCommessa>(value, true, out var parsed)
            ? parsed
            : StatoCommessa.Aperta;
    }

    private static StatoProgramma ParseStatoProgramma(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return StatoProgramma.NonProgrammata;
        }

        if (int.TryParse(value, out var numeric) && Enum.IsDefined(typeof(StatoProgramma), numeric))
        {
            return (StatoProgramma)numeric;
        }

        return Enum.TryParse<StatoProgramma>(value, true, out var parsed)
            ? parsed
            : StatoProgramma.NonProgrammata;
    }

    private sealed class CommessaProjection
    {
        public Guid Id { get; set; }
        public string Codice { get; set; } = string.Empty;
        public string? SaleOrdId { get; set; }
        public string? InternalOrdNo { get; set; }
        public string? ExternalOrdNo { get; set; }
        public string? Line { get; set; }
        public Guid? ArticoloId { get; set; }
        public Guid? ClienteId { get; set; }
        public string? ClienteRagioneSociale { get; set; }
        public string? CompanyName { get; set; }
        public string? ArticoloCodice { get; set; }
        public string? ArticoloDescrizione { get; set; }
        public decimal? ArticoloPrezzo { get; set; }
        public string? Description { get; set; }
        public decimal QuantitaRichiesta { get; set; }
        public string? UoM { get; set; }
        public DateTime? DataConsegna { get; set; }
        public StatoCommessa Stato { get; set; }
        public StatoProgramma StatoProgramma { get; set; }
        public DateTime? DataCambioStatoProgramma { get; set; }
        public string? RiferimentoOrdineCliente { get; set; }
        public string? OurReference { get; set; }
        public string? NumeroMacchinaRaw { get; set; }
        public int OrdineSequenza { get; set; }
        public DateTime? DataInizioPrevisione { get; set; }
        public DateTime? DataFinePrevisione { get; set; }
        public DateTime? DataInizioProduzione { get; set; }
        public DateTime? DataFineProduzione { get; set; }
        public DateTime UltimaModifica { get; set; }
        public DateTime TimestampSync { get; set; }
    }
}
