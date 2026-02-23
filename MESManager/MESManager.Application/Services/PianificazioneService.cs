using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;

namespace MESManager.Application.Services;

public class PianificazioneService : IPianificazioneService
{
    private readonly IRicettaRepository _ricettaRepo;
    
    public PianificazioneService(IRicettaRepository ricettaRepo)
    {
        _ricettaRepo = ricettaRepo;
    }
    
    public int CalcolaDurataPrevistaMinuti(int tempoCicloSecondi, int numeroFigure, decimal quantitaRichiesta, int tempoSetupMinuti)
    {
        if (tempoCicloSecondi <= 0 || numeroFigure <= 0)
        {
            // Se mancano dati produttivi, usa 8 ore (480 minuti) come default
            // Include setup + 8 ore di lavorazione standard
            return tempoSetupMinuti + 480; // 8 ore = 480 minuti
        }

        // Calcola il tempo totale di produzione in secondi
        // (TempoCiclo * QuantitaRichiesta / NumeroFigure) perché NumeroFigure indica quanti pezzi escono per ciclo
        decimal cicliNecessari = quantitaRichiesta / numeroFigure;
        decimal tempoProduzioneTotaleSecondi = tempoCicloSecondi * cicliNecessari;
        
        // Converti in minuti e aggiungi il tempo di setup
        int tempoProduzioneMinuti = (int)Math.Ceiling(tempoProduzioneTotaleSecondi / 60);
        
        return tempoSetupMinuti + tempoProduzioneMinuti;
    }

    public DateTime CalcolaDataFinePrevista(DateTime dataInizio, int durataMinuti, int oreLavorativeGiornaliere, int giorniLavorativiSettimanali)
    {
        // Crea calendario fittizio per compatibilità
        var calendarioDefault = new CalendarioLavoroDto
        {
            Lunedi = true, Martedi = true, Mercoledi = true, Giovedi = true, Venerdi = true,
            Sabato = giorniLavorativiSettimanali > 5,
            Domenica = giorniLavorativiSettimanali > 6,
            OraInizio = new TimeOnly(8, 0),
            OraFine = new TimeOnly(8 + oreLavorativeGiornaliere, 0)
        };
        return CalcolaDataFinePrevistaConFestivi(dataInizio, durataMinuti, calendarioDefault, new HashSet<DateOnly>());
    }

    /// <summary>
    /// METODO PRINCIPALE - Calcola data fine prevista considerando calendario lavoro completo
    /// </summary>
    public DateTime CalcolaDataFinePrevistaConFestivi(DateTime dataInizio, int durataMinuti, CalendarioLavoroDto calendario, HashSet<DateOnly> festivi)
    {
        if (durataMinuti <= 0)
        {
            return dataInizio;
        }

        // Normalizza data inizio all'orario lavorativo
        var dataFine = NormalizzaInizioGiorno(dataInizio, calendario.OraInizio);
        var minutiRimanenti = durataMinuti;
        var minutiPerGiorno = (int)(calendario.OraFine - calendario.OraInizio).TotalMinutes;

        while (minutiRimanenti > 0)
        {
            // Salta giorni NON lavorativi (controlla calendario specifico) e festivi
            while (!IsGiornoLavorativo(dataFine, calendario) || festivi.Contains(DateOnly.FromDateTime(dataFine)))
            {
                dataFine = dataFine.AddDays(1).Date + calendario.OraInizio.ToTimeSpan();
            }

            // Calcola minuti disponibili oggi (rispetta OraInizio/OraFine)
            var oraCorrente = TimeOnly.FromDateTime(dataFine);
            int minutiDisponibiliOggi;
            
            if (oraCorrente < calendario.OraInizio)
            {
                // Siamo prima dell'inizio lavoro: disponibile tutto il giorno
                minutiDisponibiliOggi = minutiPerGiorno;
                dataFine = dataFine.Date + calendario.OraInizio.ToTimeSpan();
            }
            else if (oraCorrente >= calendario.OraFine)
            {
                // Siamo dopo la fine lavoro: passa al giorno successivo
                dataFine = dataFine.Date.AddDays(1) + calendario.OraInizio.ToTimeSpan();
                continue;
            }
            else
            {
                // Siamo nell'orario lavorativo: calcola minuti rimanenti oggi
                minutiDisponibiliOggi = (int)(calendario.OraFine - oraCorrente).TotalMinutes;
            }
            
            if (minutiRimanenti <= minutiDisponibiliOggi)
            {
                // Aggiungi i minuti rimanenti e termina
                dataFine = dataFine.AddMinutes(minutiRimanenti);
                minutiRimanenti = 0;
            }
            else
            {
                // Usa tutto il tempo disponibile oggi e passa al successivo
                minutiRimanenti -= minutiDisponibiliOggi;
                dataFine = dataFine.Date.AddDays(1) + calendario.OraInizio.ToTimeSpan();
            }
        }

        return dataFine;
    }

    /// <summary>
    /// [DEPRECATO] Overload legacy per backward compatibility
    /// </summary>
    [Obsolete("Usare overload con CalendarioLavoroDto per rispettare le impostazioni calendario utente")]
    public DateTime CalcolaDataFinePrevistaConFestivi(DateTime dataInizio, int durataMinuti, int oreLavorativeGiornaliere, int giorniLavorativiSettimanali, HashSet<DateOnly> festivi)
    {
        // Delega al metodo principale creando calendario fittizio
        var calendarioDefault = new CalendarioLavoroDto
        {
            Lunedi = true, Martedi = true, Mercoledi = true, Giovedi = true, Venerdi = true,
            Sabato = giorniLavorativiSettimanali > 5,
            Domenica = giorniLavorativiSettimanali > 6,
            OraInizio = new TimeOnly(8, 0),
            OraFine = new TimeOnly(8 + oreLavorativeGiornaliere, 0)
        };
        
        return CalcolaDataFinePrevistaConFestivi(dataInizio, durataMinuti, calendarioDefault, festivi);
    }

    /// <summary>
    /// Helper: Verifica se un giorno è lavorativo secondo il calendario
    /// </summary>
    private bool IsGiornoLavorativo(DateTime data, CalendarioLavoroDto calendario)
    {
        return data.DayOfWeek switch
        {
            DayOfWeek.Monday => calendario.Lunedi,
            DayOfWeek.Tuesday => calendario.Martedi,
            DayOfWeek.Wednesday => calendario.Mercoledi,
            DayOfWeek.Thursday => calendario.Giovedi,
            DayOfWeek.Friday => calendario.Venerdi,
            DayOfWeek.Saturday => calendario.Sabato,
            DayOfWeek.Sunday => calendario.Domenica,
            _ => false
        };
    }

    /// <summary>
    /// Helper: Normalizza una data all'orario di inizio lavoro se necessario
    /// </summary>
    private DateTime NormalizzaInizioGiorno(DateTime data, TimeOnly oraInizio)
    {
        var ora = TimeOnly.FromDateTime(data);
        if (ora < oraInizio)
        {
            return data.Date + oraInizio.ToTimeSpan();
        }
        return data;
    }

    public string GetColoreStato(string stato)
    {
        return stato.ToLower() switch
        {
            "incorso" or "in corso" => "#4CAF50", // Verde
            "inritardo" or "in ritardo" => "#F44336", // Rosso
            "completata" or "completato" => "#2196F3", // Blu
            "pianificata" => "#FF9800", // Arancione
            "sospesa" => "#9E9E9E", // Grigio
            _ => "#757575" // Grigio scuro default
        };
    }
    
    /// <summary>
    /// Mappa batch di commesse a DTOs per Gantt con animeLookup pre-caricato (fix N+1 queries)
    /// Il chiamante deve fornire animeLookup già caricato dal DbContext
    /// </summary>
    public async Task<List<CommessaGanttDto>> MapToGanttDtoBatchAsync(
        List<Commessa> commesse, 
        ImpostazioniProduzione impostazioni,
        Dictionary<string, Anime>? animeLookup = null)
    {
        // animeLookup deve essere fornito dal chiamante che ha accesso al DbContext
        animeLookup ??= new Dictionary<string, Anime>();
        
        // Batch lookup Ricette per performance
        var articoloIds = commesse
            .Where(c => c.ArticoloId.HasValue)
            .Select(c => c.ArticoloId!.Value)
            .Distinct()
            .ToList();
            
        var ricetteLookup = await _ricettaRepo.GetRicetteInfoByArticoloIdAsync(articoloIds);
        
        return await Task.FromResult(commesse.Select(c =>
        {
            Anime? anime = null;
            if (c.Articolo != null && animeLookup.TryGetValue(c.Articolo.Codice, out var a))
            {
                anime = a;
            }
            
            var tempoCiclo = c.Articolo?.TempoCiclo ?? 0;
            var numeroFigure = c.Articolo?.NumeroFigure ?? 0;
            var datiIncompleti = tempoCiclo <= 0 || numeroFigure <= 0;
            
            // Setup effettivo (override o default)
            int setupEffettivo = c.SetupStimatoMinuti ?? impostazioni.TempoSetupMinuti;
            
            var durataMinuti = CalcolaDurataPrevistaMinuti(
                tempoCiclo,
                numeroFigure,
                c.QuantitaRichiesta,
                setupEffettivo
            );
            
            // NumeroMacchina è già int? - niente conversione necessaria
            int? numeroMacchinaInt = c.NumeroMacchina;
            
            // Calcola DataFinePrevisione se mancante ma presente DataInizioPrevisione
            DateTime? dataFinePrevisione = c.DataFinePrevisione;
            if (c.DataInizioPrevisione.HasValue && !dataFinePrevisione.HasValue && durataMinuti > 0)
            {
                dataFinePrevisione = CalcolaDataFinePrevista(
                    c.DataInizioPrevisione.Value,
                    durataMinuti,
                    impostazioni.OreLavorativeGiornaliere,
                    impostazioni.GiorniLavorativiSettimanali
                );
            }
            
            // Verifica vincolo data fine superato
            bool vincoloDataFineSuperato = false;
            if (c.VincoloDataFine.HasValue && dataFinePrevisione.HasValue)
            {
                vincoloDataFineSuperato = dataFinePrevisione > c.VincoloDataFine;
            }
            
            return new CommessaGanttDto
            {
                Id = c.Id,
                Codice = anime?.CodiceCassa ?? c.Articolo?.Codice ?? c.Codice,
                CodiceCassa = anime?.CodiceCassa,
                Description = c.Description ?? "",
                NumeroMacchina = numeroMacchinaInt,
                NomeMacchina = numeroMacchinaInt.HasValue ? $"Macchina {numeroMacchinaInt}" : null,
                OrdineSequenza = c.OrdineSequenza,
                DataInizioPrevisione = c.DataInizioPrevisione,
                DataFinePrevisione = dataFinePrevisione,
                DataInizioProduzione = c.DataInizioProduzione,
                DataFineProduzione = c.DataFineProduzione,
                QuantitaRichiesta = c.QuantitaRichiesta,
                UoM = c.UoM,
                DataConsegna = c.DataConsegna,
                TempoCicloSecondi = tempoCiclo,
                NumeroFigure = numeroFigure,
                TempoSetupMinuti = setupEffettivo,
                DurataPrevistaMinuti = durataMinuti,
                Stato = c.Stato.ToString(),
                ColoreStato = GetColoreStato(c.Stato.ToString()),
                StatoProgramma = c.StatoProgramma.ToString(),
                PercentualeCompletamento = CalcolaPercentualeCompletamento(c),
                DatiIncompleti = datiIncompleti,
                Priorita = c.Priorita,
                Bloccata = c.Bloccata,
                VincoloDataInizio = c.VincoloDataInizio,
                VincoloDataFine = c.VincoloDataFine,
                VincoloDataFineSuperato = vincoloDataFineSuperato,
                ClasseLavorazione = c.ClasseLavorazione,
                
                // Ricetta configurata
                HasRicetta = c.ArticoloId.HasValue && ricetteLookup.ContainsKey(c.ArticoloId.Value),
                NumeroParametri = c.ArticoloId.HasValue && ricetteLookup.TryGetValue(c.ArticoloId.Value, out var ric) ? ric.NumeroParametri : 0,
                RicettaUltimaModifica = c.ArticoloId.HasValue && ricetteLookup.TryGetValue(c.ArticoloId.Value, out var ric2) ? ric2.UltimaModifica : null
            };
        }).ToList());
    }
    
    /// <summary>
    /// Calcola la percentuale di completamento in base allo stato e alle date
    /// </summary>
    public decimal CalcolaPercentualeCompletamento(Commessa commessa)
    {
        if (commessa.DataFineProduzione.HasValue)
        {
            return 100m; // Completata
        }
        
        if (commessa.DataInizioProduzione.HasValue && !commessa.DataFineProduzione.HasValue)
        {
            // In produzione: calcola in base al tempo trascorso
            if (commessa.DataInizioPrevisione.HasValue && commessa.DataFinePrevisione.HasValue)
            {
                var now = DateTime.Now;
                var totalDuration = (commessa.DataFinePrevisione.Value - commessa.DataInizioPrevisione.Value).TotalMinutes;
                var elapsed = (now - commessa.DataInizioPrevisione.Value).TotalMinutes;
                
                if (totalDuration > 0)
                {
                    var percentage = (decimal)(elapsed / totalDuration * 100);
                    return Math.Min(99m, Math.Max(0m, percentage)); // Cap tra 0-99%
                }
            }
            return 50m; // Default in produzione senza date
        }
        
        return 0m; // Non ancora iniziata
    }
}
