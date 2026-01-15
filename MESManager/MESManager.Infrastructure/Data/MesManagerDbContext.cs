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
    }
}
