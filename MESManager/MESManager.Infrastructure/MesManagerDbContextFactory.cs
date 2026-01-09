using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MESManager.Infrastructure.Data;

namespace MESManager.Infrastructure;

public class MesManagerDbContextFactory : IDesignTimeDbContextFactory<MesManagerDbContext>
{
    public MesManagerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MesManagerDbContext>();
        // Usa la stessa connection string della tua applicazione
        optionsBuilder.UseSqlServer("Server=localhost\\SQLEXPRESS;Database=MESManager;Trusted_Connection=True;TrustServerCertificate=True;");
        return new MesManagerDbContext(optionsBuilder.Options);
    }
}
