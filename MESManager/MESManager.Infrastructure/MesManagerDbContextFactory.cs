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
        optionsBuilder.UseSqlServer("Server=localhost\\SQLEXPRESS01;Database=MESManager;Trusted_Connection=True;TrustServerCertificate=True;",
            sqlOptions => sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null));
        return new MesManagerDbContext(optionsBuilder.Options);
    }
}
