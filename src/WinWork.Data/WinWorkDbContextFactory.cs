using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WinWork.Data;

/// <summary>
/// Design-time factory for WinWorkDbContext to support EF migrations
/// </summary>
public class WinWorkDbContextFactory : IDesignTimeDbContextFactory<WinWorkDbContext>
{
    public WinWorkDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<WinWorkDbContext>();
        
        // Use a default connection string for design time
        var connectionString = $"Data Source={DatabaseConfiguration.GetDefaultDatabasePath()}";
        optionsBuilder.UseSqlite(connectionString);

        return new WinWorkDbContext(optionsBuilder.Options);
    }
}
