using Microsoft.EntityFrameworkCore;
using MESManager.Domain.Entities;

namespace MESManager.Infrastructure.Data;

public class MesManagerDbContext : DbContext
{
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
    public DbSet<UtenteApp> UtentiApp => Set<UtenteApp>();
    public DbSet<PreferenzaUtente> PreferenzeUtente => Set<PreferenzaUtente>();
    public DbSet<PlcServiceStatus> PlcServiceStatus => Set<PlcServiceStatus>();
    public DbSet<PlcSyncLog> PlcSyncLogs => Set<PlcSyncLog>();
    public DbSet<TechnicalIssue> TechnicalIssues => Set<TechnicalIssue>();
    public DbSet<Festivo> Festivi => Set<Festivo>();
    
    // Modulo Preventivi
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<QuoteRow> QuoteRows => Set<QuoteRow>();
    public DbSet<QuoteAttachment> QuoteAttachments => Set<QuoteAttachment>();
    public DbSet<PriceList> PriceLists => Set<PriceList>();
    public DbSet<PriceListItem> PriceListItems => Set<PriceListItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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

        // Configurazione UtenteApp
        modelBuilder.Entity<UtenteApp>()
            .HasIndex(u => u.Nome)
            .IsUnique();

        // Relazione 1:N UtenteApp-PreferenzaUtente
        modelBuilder.Entity<PreferenzaUtente>()
            .HasOne(p => p.UtenteApp)
            .WithMany(u => u.Preferenze)
            .HasForeignKey(p => p.UtenteAppId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indice composto per ricerca veloce preferenze
        modelBuilder.Entity<PreferenzaUtente>()
            .HasIndex(p => new { p.UtenteAppId, p.Chiave })
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
        // MODULO PREVENTIVI
        // =====================================

        // Quote - Preventivi
        modelBuilder.Entity<Quote>()
            .HasIndex(q => q.Number)
            .IsUnique();
        
        modelBuilder.Entity<Quote>()
            .HasIndex(q => q.Date);
        
        modelBuilder.Entity<Quote>()
            .HasIndex(q => q.Status);
        
        modelBuilder.Entity<Quote>()
            .HasIndex(q => q.ClienteId);

        modelBuilder.Entity<Quote>()
            .Property(q => q.Subtotal)
            .HasPrecision(18, 4);
        
        modelBuilder.Entity<Quote>()
            .Property(q => q.DiscountTotal)
            .HasPrecision(18, 4);
        
        modelBuilder.Entity<Quote>()
            .Property(q => q.TaxableAmount)
            .HasPrecision(18, 4);
        
        modelBuilder.Entity<Quote>()
            .Property(q => q.VatTotal)
            .HasPrecision(18, 4);
        
        modelBuilder.Entity<Quote>()
            .Property(q => q.GrandTotal)
            .HasPrecision(18, 4);

        // Relazione Quote -> Cliente
        modelBuilder.Entity<Quote>()
            .HasOne(q => q.Cliente)
            .WithMany()
            .HasForeignKey(q => q.ClienteId)
            .OnDelete(DeleteBehavior.SetNull);

        // Relazione Quote -> PriceList
        modelBuilder.Entity<Quote>()
            .HasOne(q => q.PriceList)
            .WithMany(pl => pl.Quotes)
            .HasForeignKey(q => q.PriceListId)
            .OnDelete(DeleteBehavior.SetNull);

        // QuoteRow - Righe preventivo
        modelBuilder.Entity<QuoteRow>()
            .HasOne(r => r.Quote)
            .WithMany(q => q.Rows)
            .HasForeignKey(r => r.QuoteId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<QuoteRow>()
            .HasOne(r => r.PriceListItem)
            .WithMany(i => i.QuoteRows)
            .HasForeignKey(r => r.PriceListItemId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<QuoteRow>()
            .Property(r => r.Quantity)
            .HasPrecision(18, 4);
        
        modelBuilder.Entity<QuoteRow>()
            .Property(r => r.UnitPrice)
            .HasPrecision(18, 4);
        
        modelBuilder.Entity<QuoteRow>()
            .Property(r => r.DiscountPercent)
            .HasPrecision(5, 2);
        
        modelBuilder.Entity<QuoteRow>()
            .Property(r => r.VatPercent)
            .HasPrecision(5, 2);
        
        modelBuilder.Entity<QuoteRow>()
            .Property(r => r.RowTotal)
            .HasPrecision(18, 4);
        
        modelBuilder.Entity<QuoteRow>()
            .Property(r => r.VatAmount)
            .HasPrecision(18, 4);

        modelBuilder.Entity<QuoteRow>()
            .HasIndex(r => r.QuoteId);

        // QuoteAttachment - Allegati preventivo
        modelBuilder.Entity<QuoteAttachment>()
            .HasOne(a => a.Quote)
            .WithMany(q => q.Attachments)
            .HasForeignKey(a => a.QuoteId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<QuoteAttachment>()
            .HasIndex(a => a.QuoteId);

        // PriceList - Listini prezzi
        modelBuilder.Entity<PriceList>()
            .HasIndex(pl => new { pl.Name, pl.Version })
            .IsUnique();
        
        modelBuilder.Entity<PriceList>()
            .HasIndex(pl => pl.IsDefault);
        
        modelBuilder.Entity<PriceList>()
            .HasIndex(pl => pl.ValidFrom);

        // PriceListItem - Voci listino
        modelBuilder.Entity<PriceListItem>()
            .HasOne(i => i.PriceList)
            .WithMany(pl => pl.Items)
            .HasForeignKey(i => i.PriceListId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PriceListItem>()
            .HasIndex(i => new { i.PriceListId, i.Code })
            .IsUnique();
        
        modelBuilder.Entity<PriceListItem>()
            .HasIndex(i => i.Category);

        modelBuilder.Entity<PriceListItem>()
            .Property(i => i.BasePrice)
            .HasPrecision(18, 4);
        
        modelBuilder.Entity<PriceListItem>()
            .Property(i => i.VatRate)
            .HasPrecision(5, 2);
    }
}
