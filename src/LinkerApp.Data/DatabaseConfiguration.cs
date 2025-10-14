using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LinkerApp.Data.Repositories;

namespace LinkerApp.Data;

/// <summary>
/// Database configuration and setup
/// </summary>
public static class DatabaseConfiguration
{
    /// <summary>
    /// Default database file name
    /// </summary>
    public const string DefaultDatabaseFileName = "linkerapp.db";

    /// <summary>
    /// Gets the default database path in the user's AppData folder
    /// </summary>
    public static string GetDefaultDatabasePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "LinkerApp");
        
        // Ensure directory exists
        Directory.CreateDirectory(appFolder);
        
        return Path.Combine(appFolder, DefaultDatabaseFileName);
    }

    /// <summary>
    /// Configure Entity Framework services
    /// </summary>
    public static IServiceCollection AddLinkerAppDatabase(this IServiceCollection services, string? connectionString = null)
    {
        connectionString ??= $"Data Source={GetDefaultDatabasePath()}";

        services.AddDbContext<LinkerAppDbContext>(options =>
        {
            options.UseSqlite(connectionString);
            
#if DEBUG
            options.EnableSensitiveDataLogging();
            options.LogTo(Console.WriteLine, LogLevel.Information);
#endif
        });

        // Register repositories
        services.AddScoped<ILinkRepository, LinkRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();

        return services;
    }

    /// <summary>
    /// Initialize and migrate the database
    /// </summary>
    public static async Task InitializeDatabaseAsync(LinkerAppDbContext context)
    {
        try
        {
            // Ensure database is created and migrated
            await context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            // Log the error (in a real app, use proper logging)
            Console.WriteLine($"Database initialization failed: {ex.Message}");
            throw;
        }
    }
}