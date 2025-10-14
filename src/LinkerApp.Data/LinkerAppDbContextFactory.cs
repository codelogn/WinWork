using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LinkerApp.Data;

/// <summary>
/// Design-time factory for LinkerAppDbContext to support EF migrations
/// </summary>
public class LinkerAppDbContextFactory : IDesignTimeDbContextFactory<LinkerAppDbContext>
{
    public LinkerAppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LinkerAppDbContext>();
        
        // Use a default connection string for design time
        var connectionString = $"Data Source={DatabaseConfiguration.GetDefaultDatabasePath()}";
        optionsBuilder.UseSqlite(connectionString);

        return new LinkerAppDbContext(optionsBuilder.Options);
    }
}