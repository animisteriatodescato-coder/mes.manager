using MESManager.Application.Interfaces;

namespace MESManager.Application.Services;

public class PianificazioneService : IPianificazioneService
{
    public int CalcolaDurataPrevistaMinuti(int tempoCicloSecondi, int numeroFigure, decimal quantitaRichiesta, int tempoSetupMinuti)
    {
        if (tempoCicloSecondi <= 0 || numeroFigure <= 0)
        {
            // Se non ci sono dati produttivi, restituisci solo il tempo di setup
            return tempoSetupMinuti;
        }

        // Calcola il tempo totale di produzione in secondi
        // (TempoCiclo * QuantitaRichiesta) perché NumeroFigure indica quanti pezzi escono per ciclo
        decimal cicliNecessari = quantitaRichiesta / numeroFigure;
        decimal tempoProduzioneTotaleSecondi = tempoCicloSecondi * cicliNecessari;
        
        // Converti in minuti e aggiungi il tempo di setup
        int tempoProduzioneMinuti = (int)Math.Ceiling(tempoProduzioneTotaleSecondi / 60);
        
        return tempoSetupMinuti + tempoProduzioneMinuti;
    }

    public DateTime CalcolaDataFinePrevista(DateTime dataInizio, int durataMinuti, int oreLavorativeGiornaliere, int giorniLavorativiSettimanali)
    {
        if (durataMinuti <= 0)
        {
            return dataInizio;
        }

        var dataFine = dataInizio;
        var minutiRimanenti = durataMinuti;
        var minutiPerGiorno = oreLavorativeGiornaliere * 60;

        while (minutiRimanenti > 0)
        {
            // Salta i weekend se necessario
            if (giorniLavorativiSettimanali == 5)
            {
                // Lunedì = 1, Domenica = 0
                while (dataFine.DayOfWeek == DayOfWeek.Saturday || dataFine.DayOfWeek == DayOfWeek.Sunday)
                {
                    dataFine = dataFine.AddDays(1);
                }
            }

            // Calcola quanti minuti possiamo aggiungere in questo giorno
            var minutiDisponibiliOggi = minutiPerGiorno;
            
            if (minutiRimanenti <= minutiDisponibiliOggi)
            {
                // Aggiungi i minuti rimanenti e termina
                dataFine = dataFine.AddMinutes(minutiRimanenti);
                minutiRimanenti = 0;
            }
            else
            {
                // Usa tutto il giorno e passa al successivo
                minutiRimanenti -= minutiDisponibiliOggi;
                dataFine = dataFine.AddDays(1);
            }
        }

        return dataFine;
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
}
