using MESManager.Domain.Entities;
using MESManager.Domain.Enums;
using MESManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MESManager.Web.Services;

public static class E2ETestDataSeeder
{
    public static async Task SeedAsync(MesManagerDbContext db, ILogger logger)
    {
        await db.Database.MigrateAsync();

        var now = DateTime.UtcNow;

        if (!await db.CalendarioLavoro.AnyAsync())
        {
            db.CalendarioLavoro.Add(new CalendarioLavoro
            {
                Id = Guid.NewGuid(),
                Lunedi = true,
                Martedi = true,
                Mercoledi = true,
                Giovedi = true,
                Venerdi = true,
                Sabato = false,
                Domenica = false,
                OraInizio = new TimeOnly(8, 0),
                OraFine = new TimeOnly(17, 0),
                DataCreazione = now,
                DataModifica = now
            });
        }

        if (!await db.ImpostazioniGantt.AnyAsync())
        {
            db.ImpostazioniGantt.Add(new ImpostazioniGantt
            {
                Id = Guid.NewGuid(),
                AbilitaTempoAttrezzaggio = true,
                TempoAttrezzaggioMinutiDefault = 30,
                DataCreazione = now,
                DataModifica = now
            });
        }

        if (!await db.Macchine.AnyAsync())
        {
            db.Macchine.AddRange(
                new Macchina
                {
                    Id = Guid.NewGuid(),
                    Codice = "MC-01",
                    Nome = "Macchina 1",
                    Stato = StatoMacchina.InFunzione,
                    AttivaInGantt = true,
                    OrdineVisualizazione = 1,
                    IndirizzoPLC = "192.168.0.101"
                },
                new Macchina
                {
                    Id = Guid.NewGuid(),
                    Codice = "MC-02",
                    Nome = "Macchina 2",
                    Stato = StatoMacchina.Ferma,
                    AttivaInGantt = true,
                    OrdineVisualizazione = 2,
                    IndirizzoPLC = "192.168.0.102"
                }
            );
        }

        Cliente cliente;
        if (!await db.Clienti.AnyAsync())
        {
            cliente = new Cliente
            {
                Id = Guid.NewGuid(),
                Codice = "CL-001",
                RagioneSociale = "Cliente E2E",
                Email = "e2e@example.local",
                Note = "Seed E2E",
                Attivo = true,
                UltimaModifica = now,
                TimestampSync = now
            };
            db.Clienti.Add(cliente);
        }
        else
        {
            cliente = await db.Clienti.FirstAsync();
        }

        Articolo articolo;
        if (!await db.Articoli.AnyAsync())
        {
            articolo = new Articolo
            {
                Id = Guid.NewGuid(),
                Codice = "AR-001",
                Descrizione = "Articolo E2E",
                Prezzo = 10.5m,
                Attivo = true,
                UltimaModifica = now,
                TimestampSync = now,
                TempoCiclo = 60,
                NumeroFigure = 2,
                ClasseLavorazione = "STD"
            };
            db.Articoli.Add(articolo);
        }
        else
        {
            articolo = await db.Articoli.FirstAsync();
        }

        if (!await db.Commesse.AnyAsync())
        {
            db.Commesse.AddRange(
                new Commessa
                {
                    Id = Guid.NewGuid(),
                    Codice = "E2E-001",
                    InternalOrdNo = "E2E-001",
                    Description = "Commessa programmata 1",
                    QuantitaRichiesta = 100,
                    UoM = "pz",
                    DataConsegna = now.Date.AddDays(7),
                    Stato = StatoCommessa.Aperta,
                    ClienteId = cliente.Id,
                    ArticoloId = articolo.Id,
                    CompanyName = cliente.RagioneSociale,
                    NumeroMacchina = 1,
                    OrdineSequenza = 1,
                    DataInizioPrevisione = now.AddHours(1),
                    DataFinePrevisione = now.AddHours(3),
                    Priorita = 50,
                    StatoProgramma = StatoProgramma.Programmata,
                    UltimaModifica = now,
                    TimestampSync = now
                },
                new Commessa
                {
                    Id = Guid.NewGuid(),
                    Codice = "E2E-002",
                    InternalOrdNo = "E2E-002",
                    Description = "Commessa programmata 2",
                    QuantitaRichiesta = 80,
                    UoM = "pz",
                    DataConsegna = now.Date.AddDays(10),
                    Stato = StatoCommessa.Aperta,
                    ClienteId = cliente.Id,
                    ArticoloId = articolo.Id,
                    CompanyName = cliente.RagioneSociale,
                    NumeroMacchina = 2,
                    OrdineSequenza = 2,
                    DataInizioPrevisione = now.AddHours(4),
                    DataFinePrevisione = now.AddHours(6),
                    Priorita = 60,
                    StatoProgramma = StatoProgramma.Programmata,
                    UltimaModifica = now,
                    TimestampSync = now
                },
                new Commessa
                {
                    Id = Guid.NewGuid(),
                    Codice = "E2E-003",
                    InternalOrdNo = "E2E-003",
                    Description = "Commessa non programmata",
                    QuantitaRichiesta = 50,
                    UoM = "pz",
                    DataConsegna = now.Date.AddDays(5),
                    Stato = StatoCommessa.Aperta,
                    ClienteId = cliente.Id,
                    ArticoloId = articolo.Id,
                    CompanyName = cliente.RagioneSociale,
                    NumeroMacchina = null,
                    OrdineSequenza = 0,
                    Priorita = 90,
                    StatoProgramma = StatoProgramma.NonProgrammata,
                    UltimaModifica = now,
                    TimestampSync = now
                }
            );
        }

        if (!await db.Anime.AnyAsync())
        {
            db.Anime.Add(new Anime
            {
                Id = 1,
                CodiceArticolo = "AN-001",
                DescrizioneArticolo = "Anima E2E",
                Cliente = cliente.RagioneSociale,
                UnitaMisura = "pz",
                QuantitaPiano = 10,
                NumeroPiani = 5,
                DataImportazione = now,
                ModificatoLocalmente = false
            });
        }

        if (!await db.Operatori.AnyAsync())
        {
            db.Operatori.Add(new Operatore
            {
                Id = Guid.NewGuid(),
                NumeroOperatore = 1,
                Matricola = "OP-001",
                Nome = "Mario",
                Cognome = "Rossi",
                Attivo = true,
                DataAssunzione = now.Date.AddYears(-1)
            });
        }

        await db.SaveChangesAsync();
        logger.LogInformation("E2E seed completato");
    }
}
