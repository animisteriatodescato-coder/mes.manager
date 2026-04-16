using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MESManager.Domain.Entities;
using MESManager.Domain.Enums;
using MESManager.Infrastructure.Entities;
using System.Globalization;

namespace MESManager.Infrastructure.Data;

public class MesManagerDbContext : IdentityDbContext<ApplicationUser>
{
    private static readonly HashSet<string> StringDbTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "nvarchar",
        "varchar",
        "nchar",
        "char",
        "text",
        "ntext"
    };

    private static readonly Dictionary<string, bool> ColumnStringCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object SchemaLock = new();
    public MesManagerDbContext(DbContextOptions<MesManagerDbContext> options) : base(options)
    {
    }

    public DbSet<Macchina> Macchine => Set<Macchina>();
    public DbSet<Articolo> Articoli => Set<Articolo>();
    public DbSet<Ricetta> Ricette => Set<Ricetta>();
    public DbSet<ParametroRicetta> ParametriRicetta => Set<ParametroRicetta>();
    public DbSet<Commessa> Commesse => Set<Commessa>();
    public DbSet<Cliente> Clienti => Set<Cliente>();
    public DbSet<Operatore> Operatori => Set<Operatore>();
    public DbSet<Manutenzione> Manutenzioni => Set<Manutenzione>();
    public DbSet<EventoPLC> EventiPLC => Set<EventoPLC>();
    public DbSet<PLCRealtime> PLCRealtime => Set<PLCRealtime>();
    public DbSet<PLCStorico> PLCStorico => Set<PLCStorico>();
    public DbSet<ConfigurazionePLC> ConfigurazioniPLC => Set<ConfigurazionePLC>();
    public DbSet<LogEvento> LogEventi => Set<LogEvento>();
    public DbSet<LogSyncEntry> LogSync => Set<LogSyncEntry>();
    public DbSet<SyncState> SyncStates => Set<SyncState>();
    public DbSet<Anime> Anime => Set<Anime>();
    public DbSet<ImpostazioniProduzione> ImpostazioniProduzione => Set<ImpostazioniProduzione>();
    public DbSet<CalendarioLavoro> CalendarioLavoro => Set<CalendarioLavoro>();
    public DbSet<ImpostazioniGantt> ImpostazioniGantt => Set<ImpostazioniGantt>();
    public DbSet<AllegatoArticolo> AllegatiArticoli => Set<AllegatoArticolo>();
    public DbSet<StoricoProgrammazione> StoricoProgrammazione => Set<StoricoProgrammazione>();
    public DbSet<PreferenzaUtente> PreferenzeUtente => Set<PreferenzaUtente>();
    public DbSet<PlcServiceStatus> PlcServiceStatus => Set<PlcServiceStatus>();
    public DbSet<PlcSyncLog> PlcSyncLogs => Set<PlcSyncLog>();
    public DbSet<TechnicalIssue> TechnicalIssues => Set<TechnicalIssue>();
    public DbSet<Festivo> Festivi => Set<Festivo>();
    
    // Modulo Manutenzioni (Schede)
    public DbSet<ManutenzioneAttivita> ManutenzioneAttivita => Set<ManutenzioneAttivita>();
    public DbSet<ManutenzioneScheda> ManutenzioneSchede => Set<ManutenzioneScheda>();
    public DbSet<ManutenzioneRiga> ManutenzioneRighe => Set<ManutenzioneRiga>();
    public DbSet<AnomaliaStandardManutenzione> AnomalieStandardManutenzione => Set<AnomaliaStandardManutenzione>();

    // Modulo Manutenzioni Casse d'Anima (v1.65.14)
    public DbSet<ManutenzioneCassaAttivita> ManutenzioneCassaAttivita => Set<ManutenzioneCassaAttivita>();
    public DbSet<ManutenzioneCassaScheda> ManutenzioneCasseSchede => Set<ManutenzioneCassaScheda>();
    public DbSet<ManutenzioneCassaRiga> ManutenzioneCasseRighe => Set<ManutenzioneCassaRiga>();
    public DbSet<ManutenzioneCassaAllegato> ManutenzioneCasseAllegati => Set<ManutenzioneCassaAllegato>();

    // Modulo Preventivi (v1.64.0)
    public DbSet<PreventivoTipoSabbia> PreventivoTipiSabbia => Set<PreventivoTipoSabbia>();
    public DbSet<PreventivoTipoVernice> PreventivoTipiVernice => Set<PreventivoTipoVernice>();
    public DbSet<Preventivo> Preventivi => Set<Preventivo>();
    public DbSet<PreventivoRevisione> PreventivoRevisioni => Set<PreventivoRevisione>();
    public DbSet<PreventivoTemplate> PreventivoTemplates => Set<PreventivoTemplate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var statoCommessaConverter = new ValueConverter<StatoCommessa, string>(
            value => value.ToString(),
            value => ParseStatoCommessa(value));

        var statoProgrammaConverter = new ValueConverter<StatoProgramma, string>(
            value => value.ToString(),
            value => ParseStatoProgramma(value));

        var intToStringConverter = new ValueConverter<int, string>(
            value => value.ToString(CultureInfo.InvariantCulture),
            value => ParseInt(value, 0));

        var nullableIntToStringConverter = new ValueConverter<int?, string>(
            value => value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : string.Empty,
            value => ParseNullableInt(value));

        var decimalToStringConverter = new ValueConverter<decimal, string>(
            value => value.ToString(CultureInfo.InvariantCulture),
            value => ParseDecimal(value, 0m));

        // Relazione 1:1 Articolo-Ricetta
        modelBuilder.Entity<Ricetta>()
            .HasOne(r => r.Articolo)
            .WithOne(a => a.Ricetta)
            .HasForeignKey<Ricetta>(r => r.ArticoloId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relazione 1:N Ricetta-ParametroRicetta
        modelBuilder.Entity<ParametroRicetta>()
            .HasOne(p => p.Ricetta)
            .WithMany(r => r.Parametri)
            .HasForeignKey(p => p.RicettaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relazione 1:N Articolo-Commessa
        modelBuilder.Entity<Commessa>()
            .HasOne(c => c.Articolo)
            .WithMany(a => a.Commesse)
            .HasForeignKey(c => c.ArticoloId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relazione 1:N Cliente-Commessa
        modelBuilder.Entity<Commessa>()
            .HasOne(c => c.Cliente)
            .WithMany(cl => cl.Commesse)
            .HasForeignKey(c => c.ClienteId)
            .OnDelete(DeleteBehavior.SetNull);

        // Gestione conversioni enum in base al tipo colonna nel DB
        if (IsColumnString("Commesse", "Stato"))
        {
            modelBuilder.Entity<Commessa>()
                .Property(c => c.Stato)
                .HasConversion(statoCommessaConverter)
                .HasMaxLength(50);
        }

        if (IsColumnString("Commesse", "StatoProgramma"))
        {
            modelBuilder.Entity<Commessa>()
                .Property(c => c.StatoProgramma)
                .HasConversion(statoProgrammaConverter)
                .HasMaxLength(50);
        }

        modelBuilder.Entity<Commessa>()
            .Property(c => c.NumeroMacchina)
            .HasConversion(nullableIntToStringConverter)
            .HasMaxLength(50);

        if (IsColumnString("Commesse", "OrdineSequenza"))
        {
            modelBuilder.Entity<Commessa>()
                .Property(c => c.OrdineSequenza)
                .HasConversion(intToStringConverter)
                .HasMaxLength(50);
        }

        if (IsColumnString("Commesse", "Priorita"))
        {
            modelBuilder.Entity<Commessa>()
                .Property(c => c.Priorita)
                .HasConversion(intToStringConverter)
                .HasMaxLength(50);
        }

        if (IsColumnString("Commesse", "SetupStimatoMinuti"))
        {
            modelBuilder.Entity<Commessa>()
                .Property(c => c.SetupStimatoMinuti)
                .HasConversion(nullableIntToStringConverter)
                .HasMaxLength(50);
        }

        if (IsColumnString("Commesse", "QuantitaRichiesta"))
        {
            modelBuilder.Entity<Commessa>()
                .Property(c => c.QuantitaRichiesta)
                .HasConversion(decimalToStringConverter)
                .HasMaxLength(50);
        }

        if (IsColumnString("Articoli", "TempoCiclo"))
        {
            modelBuilder.Entity<Articolo>()
                .Property(a => a.TempoCiclo)
                .HasConversion(intToStringConverter)
                .HasMaxLength(50);
        }

        if (IsColumnString("Articoli", "NumeroFigure"))
        {
            modelBuilder.Entity<Articolo>()
                .Property(a => a.NumeroFigure)
                .HasConversion(intToStringConverter)
                .HasMaxLength(50);
        }

        if (IsColumnString("Articoli", "Prezzo"))
        {
            modelBuilder.Entity<Articolo>()
                .Property(a => a.Prezzo)
                .HasConversion(decimalToStringConverter)
                .HasMaxLength(50);
        }

        // Relazione 1:N Macchina-EventoPLC
        modelBuilder.Entity<EventoPLC>()
            .HasOne(e => e.Macchina)
            .WithMany(m => m.EventiPLC)
            .HasForeignKey(e => e.MacchinaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relazione 1:N Macchina-Manutenzione
        modelBuilder.Entity<Manutenzione>()
            .HasOne(m => m.Macchina)
            .WithMany(mac => mac.Manutenzioni)
            .HasForeignKey(m => m.MacchinaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relazione 1:N Macchina-ConfigurazionePLC
        modelBuilder.Entity<ConfigurazionePLC>()
            .HasOne(c => c.Macchina)
            .WithMany(m => m.ConfigurazioniPLC)
            .HasForeignKey(c => c.MacchinaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configurazione precision per decimal
        modelBuilder.Entity<Articolo>()
            .Property(a => a.Prezzo)
            .HasPrecision(18, 4);

        modelBuilder.Entity<Commessa>()
            .Property(c => c.QuantitaRichiesta)
            .HasPrecision(18, 4);

        // Optimistic Concurrency Control su Commessa
        modelBuilder.Entity<Commessa>()
            .Property(c => c.RowVersion)
            .IsRowVersion();

        // Indici per performance
        modelBuilder.Entity<Articolo>()
            .HasIndex(a => a.Codice)
            .IsUnique();

        modelBuilder.Entity<Macchina>()
            .HasIndex(m => m.Codice);

        modelBuilder.Entity<Commessa>()
            .HasIndex(c => c.Codice);

        modelBuilder.Entity<Cliente>()
            .HasIndex(c => c.Codice)
            .IsUnique();

        modelBuilder.Entity<SyncState>()
            .HasIndex(s => s.Modulo)
            .IsUnique();

        // Indici per AllegatiArticoli
        modelBuilder.Entity<AllegatoArticolo>()
            .HasIndex(a => a.CodiceArticolo);
        
        modelBuilder.Entity<AllegatoArticolo>()
            .HasIndex(a => new { a.Archivio, a.IdArchivio });
        
        modelBuilder.Entity<AllegatoArticolo>()
            .HasIndex(a => a.IdGanttOriginale);

        // Relazione 1:N Commessa-StoricoProgrammazione
        modelBuilder.Entity<StoricoProgrammazione>()
            .HasOne(s => s.Commessa)
            .WithMany(c => c.StoricoProgrammazione)
            .HasForeignKey(s => s.CommessaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indice per query veloci su storico
        modelBuilder.Entity<StoricoProgrammazione>()
            .HasIndex(s => s.CommessaId);
        
        modelBuilder.Entity<StoricoProgrammazione>()
            .HasIndex(s => s.DataModifica);

        // Indice per filtrare commesse per StatoProgramma
        modelBuilder.Entity<Commessa>()
            .HasIndex(c => c.StatoProgramma);

        // Indice composto per ricerca veloce preferenze (userId = AspNetUsers.Id stringa)
        modelBuilder.Entity<PreferenzaUtente>()
            .HasIndex(p => new { p.UserId, p.Chiave })
            .IsUnique();

        // PlcServiceStatus - riga unica
        modelBuilder.Entity<PlcServiceStatus>()
            .HasKey(p => p.Id);
        
        // PlcSyncLog - indici per query veloci
        modelBuilder.Entity<PlcSyncLog>()
            .HasIndex(l => l.Timestamp);
        
        modelBuilder.Entity<PlcSyncLog>()
            .HasIndex(l => l.MacchinaId);
        
        modelBuilder.Entity<PlcSyncLog>()
            .HasIndex(l => l.Level);
        
        modelBuilder.Entity<PlcSyncLog>()
            .HasOne(l => l.Macchina)
            .WithMany()
            .HasForeignKey(l => l.MacchinaId)
            .OnDelete(DeleteBehavior.SetNull);

        // TechnicalIssue - indici per query veloci
        modelBuilder.Entity<TechnicalIssue>()
            .Property(t => t.Title)
            .HasMaxLength(200);

        modelBuilder.Entity<TechnicalIssue>()
            .Property(t => t.DocsReferencePath)
            .HasMaxLength(260);

        modelBuilder.Entity<TechnicalIssue>()
            .HasIndex(t => t.Status);

        modelBuilder.Entity<TechnicalIssue>()
            .HasIndex(t => t.Area);

        modelBuilder.Entity<TechnicalIssue>()
            .HasIndex(t => t.Severity);

        modelBuilder.Entity<TechnicalIssue>()
            .HasIndex(t => t.Environment);

        modelBuilder.Entity<TechnicalIssue>()
            .HasIndex(t => t.CreatedAt);

        // Festivi - indici per query veloci
        modelBuilder.Entity<Festivo>()
            .HasIndex(f => f.Data);
        
        modelBuilder.Entity<Festivo>()
            .HasIndex(f => f.Ricorrente);

        // =====================================
        // MODULO MANUTENZIONI (SCHEDE)
        // =====================================

        // Relazione 1:N Macchina-ManutenzioneScheda
        modelBuilder.Entity<ManutenzioneScheda>()
            .HasOne(s => s.Macchina)
            .WithMany(m => m.SchedeManutenzione)
            .HasForeignKey(s => s.MacchinaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relazione 1:N ManutenzioneScheda-ManutenzioneRiga
        modelBuilder.Entity<ManutenzioneRiga>()
            .HasOne(r => r.Scheda)
            .WithMany(s => s.Righe)
            .HasForeignKey(r => r.SchedaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relazione 1:N ManutenzioneAttivita-ManutenzioneRiga
        modelBuilder.Entity<ManutenzioneRiga>()
            .HasOne(r => r.Attivita)
            .WithMany(a => a.Righe)
            .HasForeignKey(r => r.AttivitaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indici per query veloci
        modelBuilder.Entity<ManutenzioneScheda>()
            .HasIndex(s => s.MacchinaId);
        modelBuilder.Entity<ManutenzioneScheda>()
            .HasIndex(s => s.DataEsecuzione);
        modelBuilder.Entity<ManutenzioneRiga>()
            .HasIndex(r => r.SchedaId);
        modelBuilder.Entity<ManutenzioneAttivita>()
            .HasIndex(a => a.TipoFrequenza);

        // ── Modulo Preventivi (v1.64.0) ───────────────────────────────────
        modelBuilder.Entity<PreventivoTipoSabbia>(b =>
        {
            b.ToTable("PreventivoTipiSabbia");
            b.HasKey(x => x.Id);
            b.Property(x => x.Codice).HasMaxLength(50).IsRequired();
            b.Property(x => x.Nome).HasMaxLength(100).IsRequired();
            b.Property(x => x.Famiglia).HasMaxLength(30).IsRequired();
            b.Property(x => x.EuroOra).HasPrecision(10, 2);
            b.Property(x => x.PrezzoKg).HasPrecision(10, 4);
        });

        modelBuilder.Entity<PreventivoTipoVernice>(b =>
        {
            b.ToTable("PreventivoTipiVernice");
            b.HasKey(x => x.Id);
            b.Property(x => x.Codice).HasMaxLength(50).IsRequired();
            b.Property(x => x.Nome).HasMaxLength(100).IsRequired();
            b.Property(x => x.Famiglia).HasMaxLength(30).IsRequired();
            b.Property(x => x.PrezzoKg).HasPrecision(10, 4);
            b.Property(x => x.PercentualeApplicazione).HasPrecision(5, 2);
        });

        modelBuilder.Entity<Preventivo>(b =>
        {
            b.ToTable("Preventivi");
            b.HasKey(x => x.Id);
            b.Property(x => x.Cliente).HasMaxLength(200).IsRequired();
            b.Property(x => x.CodiceArticolo).HasMaxLength(100).IsRequired();
            b.Property(x => x.SabbiaSnapshot).HasMaxLength(200);
            b.Property(x => x.VerniceSnapshot).HasMaxLength(200);
            b.Property(x => x.Stato).HasMaxLength(30).HasDefaultValue("InAttesa");
            b.Property(x => x.EuroOraSabbia).HasPrecision(10, 2);
            b.Property(x => x.PrezzoSabbiaKg).HasPrecision(10, 4);
            b.Property(x => x.PesoAnima).HasPrecision(10, 4);
            b.Property(x => x.CostoAttrezzatura).HasPrecision(10, 2);
            b.Property(x => x.EuroOraVerniciatura).HasPrecision(10, 2);
            b.Property(x => x.EuroOraIncollaggio).HasPrecision(10, 2);
            b.Property(x => x.EuroOraImballaggio).HasPrecision(10, 2);
            b.Property(x => x.CostoVerniceKg).HasPrecision(10, 4);
            b.Property(x => x.PercentualeVernice).HasPrecision(5, 2);
            b.Property(x => x.CalcCostoAnima).HasPrecision(10, 4);
            b.Property(x => x.CalcVerniciaturaTot).HasPrecision(10, 4);
            b.Property(x => x.CalcPrezzoVendita).HasPrecision(10, 4);
            // Feature v1.65.7
            b.Property(x => x.Sconto).HasPrecision(5, 2).HasDefaultValue(0m);
            b.Property(x => x.NoteInterne).HasMaxLength(2000);
            b.Property(x => x.EmailDestinatario).HasMaxLength(300);
            b.HasOne(x => x.TipoSabbia).WithMany().HasForeignKey(x => x.TipoSabbiaId).OnDelete(DeleteBehavior.SetNull);
            b.HasOne(x => x.TipoVernice).WithMany().HasForeignKey(x => x.TipoVerniceId).OnDelete(DeleteBehavior.SetNull);
            b.HasIndex(x => x.DataCreazione);
            b.HasIndex(x => x.Cliente);
        });

        modelBuilder.Entity<PreventivoRevisione>(b =>
        {
            b.ToTable("PreventivoRevisioni");
            b.HasKey(x => x.Id);
            b.Property(x => x.NoteRevisione).HasMaxLength(500);
            b.Property(x => x.DtoJson).IsRequired();
            b.HasOne<Preventivo>().WithMany().HasForeignKey(x => x.PreventivoId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => x.PreventivoId);
        });

        modelBuilder.Entity<PreventivoTemplate>(b =>
        {
            b.ToTable("PreventivoTemplates");
            b.HasKey(x => x.Id);
            b.Property(x => x.Nome).HasMaxLength(100).IsRequired();
            b.Property(x => x.Descrizione).HasMaxLength(500);
            b.Property(x => x.ParametriJson).IsRequired();
        });

        // ── Modulo Manutenzioni Casse d'Anima (v1.65.14) ──────────────────────
        modelBuilder.Entity<ManutenzioneCassaAttivita>(b =>
        {
            b.ToTable("ManutenzioneCassaAttivita");
            b.HasKey(x => x.Id);
            b.Property(x => x.Nome).HasMaxLength(200).IsRequired();
            b.HasIndex(x => x.Attiva);
        });

        modelBuilder.Entity<ManutenzioneCassaScheda>(b =>
        {
            b.ToTable("ManutenzioneCasseSchede");
            b.HasKey(x => x.Id);
            b.Property(x => x.CodiceCassa).HasMaxLength(100).IsRequired();
            b.Property(x => x.OperatoreId).HasMaxLength(450);
            b.Property(x => x.NomeOperatore).HasMaxLength(200);
            b.HasIndex(x => x.CodiceCassa);
            b.HasIndex(x => x.DataEsecuzione);
        });

        modelBuilder.Entity<ManutenzioneCassaRiga>(b =>
        {
            b.ToTable("ManutenzioneCasseRighe");
            b.HasKey(x => x.Id);
            b.HasOne(r => r.Scheda)
                .WithMany(s => s.Righe)
                .HasForeignKey(r => r.SchedaId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(r => r.Attivita)
                .WithMany(a => a.Righe)
                .HasForeignKey(r => r.AttivitaId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => x.SchedaId);
        });

        modelBuilder.Entity<ManutenzioneCassaAllegato>(b =>
        {
            b.ToTable("ManutenzioneCasseAllegati");
            b.HasKey(x => x.Id);
            b.Property(x => x.NomeFile).HasMaxLength(260).IsRequired();
            b.Property(x => x.PathFile).HasMaxLength(500).IsRequired();
            b.Property(x => x.TipoFile).HasMaxLength(20).HasDefaultValue("Documento");
            b.Property(x => x.Estensione).HasMaxLength(20);
            b.Property(x => x.Descrizione).HasMaxLength(500);
            b.HasOne(a => a.Scheda)
                .WithMany()
                .HasForeignKey(a => a.SchedaId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => x.SchedaId);
        });
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

    private static int ParseInt(string? value, int fallback)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return fallback;
    }

    private static int? ParseNullableInt(string? value)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static decimal ParseDecimal(string? value, decimal fallback)
    {
        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return fallback;
    }

    private bool IsColumnString(string tableName, string columnName)
    {
        lock (SchemaLock)
        {
            var cacheKey = $"{tableName}.{columnName}";
            if (ColumnStringCache.TryGetValue(cacheKey, out var cachedValue))
            {
                return cachedValue;
            }

            try
            {
                using var connection = Database.GetDbConnection();
                var shouldClose = connection.State != System.Data.ConnectionState.Open;
                if (shouldClose)
                {
                    connection.Open();
                }

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @table AND COLUMN_NAME = @column";

                var tableParameter = command.CreateParameter();
                tableParameter.ParameterName = "@table";
                tableParameter.Value = tableName;
                command.Parameters.Add(tableParameter);

                var parameter = command.CreateParameter();
                parameter.ParameterName = "@column";
                parameter.Value = columnName;
                command.Parameters.Add(parameter);

                var dataType = command.ExecuteScalar() as string;
                var isString = dataType != null && StringDbTypes.Contains(dataType);
                ColumnStringCache[cacheKey] = isString;

                if (shouldClose)
                {
                    connection.Close();
                }
            }
            catch
            {
                ColumnStringCache[cacheKey] = false;
            }

            return ColumnStringCache[cacheKey];
        }
    }
}
