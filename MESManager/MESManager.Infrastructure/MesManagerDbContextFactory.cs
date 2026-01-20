using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using MESManager.Infrastructure.Data;

namespace MESManager.Infrastructure;

public class MesManagerDbContextFactory : IDesignTimeDbContextFactory<MesManagerDbContext>
{
    public MesManagerDbContext CreateDbContext(string[] args)
    {
        // Legge la connection string dal file condiviso nella root del progetto
        var projectRoot = Directory.GetParent(Directory.GetCurrentDirectory())!.FullName;
        var configuration = new ConfigurationBuilder()
            .SetBasePath(projectRoot)
            .AddJsonFile("appsettings.Database.json", optional: false)
            .Build();

        var connectionString = configuration.GetConnectionString("MESManagerDb")
            ?? throw new InvalidOperationException("Connection string 'MESManagerDb' not found in appsettings.Database.json");

        var optionsBuilder = new DbContextOptionsBuilder<MesManagerDbContext>();
        optionsBuilder.UseSqlServer(connectionString,
            sqlOptions => sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null));
        return new MesManagerDbContext(optionsBuilder.Options);
    }
}
