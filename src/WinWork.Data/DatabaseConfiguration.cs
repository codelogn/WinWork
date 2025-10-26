using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WinWork.Data.Repositories;

namespace WinWork.Data;

/// <summary>
/// Database configuration and setup
/// </summary>
public static class DatabaseConfiguration
{
    /// <summary>
    /// Default database file name
    /// </summary>
    public const string DefaultDatabaseFileName = "winwork.db";

    /// <summary>
    /// Gets the default database path in the user's AppData folder
    /// </summary>
    public static string GetDefaultDatabasePath()
    {
        try
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            Console.WriteLine($"AppData path: {appDataPath}");
            
            var appFolder = Path.Combine(appDataPath, "WinWork");
            Console.WriteLine($"App folder path: {appFolder}");
            
            // Ensure directory exists with proper permissions
            if (!Directory.Exists(appFolder))
            {
                Console.WriteLine($"Creating application directory: {appFolder}");
                Directory.CreateDirectory(appFolder);
            }
            
            var dbPath = Path.Combine(appFolder, DefaultDatabaseFileName);
            Console.WriteLine($"Database path: {dbPath}");
            
            return dbPath;
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Failed to create database directory: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Configure Entity Framework services
    /// </summary>
    public static IServiceCollection AddWinWorkDatabase(this IServiceCollection services, string? connectionString = null)
    {
        connectionString ??= $"Data Source={GetDefaultDatabasePath()}";

        services.AddDbContext<WinWorkDbContext>(options =>
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
    /// Initialize the database, ensuring it exists and has the correct schema
    /// </summary>
    public static async Task InitializeDatabaseAsync(WinWorkDbContext context)
    {
        try
        {
            Console.WriteLine("Starting database initialization...");
            var dbPath = GetDefaultDatabasePath();
            Console.WriteLine($"Database path: {dbPath}");
            Console.WriteLine($"Database directory exists: {Directory.Exists(Path.GetDirectoryName(dbPath))}");
            Console.WriteLine($"Database file exists: {File.Exists(dbPath)}");

            // First check if database exists and is accessible
            Console.WriteLine("Checking database connection...");
            bool dbExists = await context.Database.CanConnectAsync();
            Console.WriteLine($"Can connect to database: {dbExists}");
            
            if (!dbExists)
            {
                Console.WriteLine("Database does not exist, creating new database...");
                // Create database directory if it doesn't exist
                var dbDir = Path.GetDirectoryName(dbPath);
                if (!Directory.Exists(dbDir))
                {
                    Console.WriteLine($"Creating database directory: {dbDir}");
                    Directory.CreateDirectory(dbDir!);
                }

                // Create database and schema
                Console.WriteLine("Creating database schema...");
                await context.Database.EnsureCreatedAsync();
                
                // Add initial seed data
                Console.WriteLine("Adding initial seed data...");
                await SeedInitialDataAsync(context);
                Console.WriteLine("Seed data added successfully.");
            }
            else
            {
                Console.WriteLine("Database exists, verifying schema...");
                // Database exists, verify schema
                bool hasSchema = await VerifySchemaAsync(context);
                Console.WriteLine($"Schema verification result: {hasSchema}");

                if (!hasSchema)
                {
                    Console.WriteLine("Schema invalid, recreating database...");
                    // Recreate schema if missing
                    await context.Database.EnsureDeletedAsync();
                    await context.Database.EnsureCreatedAsync();
                    await SeedInitialDataAsync(context);
                    Console.WriteLine("Database recreated successfully.");
                }
            }
            Console.WriteLine("Database initialization completed successfully.");
        }
        catch (Exception ex)
        {
            var errorMessage = $"Database initialization failed: {ex.Message}\n";
            if (ex.InnerException != null)
            {
                errorMessage += $"Inner exception: {ex.InnerException.Message}\n";
                errorMessage += $"Inner stack trace: {ex.InnerException.StackTrace}";
            }
            Console.WriteLine(errorMessage);
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw new ApplicationException(errorMessage, ex);
        }
    }
    
    /// <summary>
    /// Verify that all required tables exist in the database
    /// </summary>
    private static async Task<bool> VerifySchemaAsync(WinWorkDbContext context)
    {
        try
        {
            // Try to query each table to verify schema
            _ = await context.Links.FirstOrDefaultAsync();
            _ = await context.Tags.FirstOrDefaultAsync();
            _ = await context.LinkTags.FirstOrDefaultAsync();
            _ = await context.AppSettings.FirstOrDefaultAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Add initial seed data to a fresh database
    /// </summary>
    private static async Task SeedInitialDataAsync(WinWorkDbContext context)
    {
        // Use a fixed datetime for seeded data
        var now = DateTime.UtcNow;

        // Add default tags
        var tags = new[]
        {
            new WinWork.Models.Tag { Name = "Work", Color = "#0078d4", Description = "Work-related links", CreatedAt = now, UpdatedAt = now },
            new WinWork.Models.Tag { Name = "Personal", Color = "#107c10", Description = "Personal links", CreatedAt = now, UpdatedAt = now },
            new WinWork.Models.Tag { Name = "Development", Color = "#e74c3c", Description = "Development tools and resources", CreatedAt = now, UpdatedAt = now }
        };
        await context.Tags.AddRangeAsync(tags);

        // Add default settings
        var settings = new[]
        {
            new WinWork.Models.AppSettings { Key = "Theme", Value = "Dark", Description = "Application theme (Light/Dark)", UpdatedAt = now },
            new WinWork.Models.AppSettings { Key = "GlobalHotkey", Value = "Ctrl+Alt+L", Description = "Global hotkey to show application", UpdatedAt = now },
            new WinWork.Models.AppSettings { Key = "MinimizeToTray", Value = "true", Description = "Minimize to system tray when closed", UpdatedAt = now },
            new WinWork.Models.AppSettings { Key = "StartWithWindows", Value = "false", Description = "Start application with Windows", UpdatedAt = now },
            new WinWork.Models.AppSettings { Key = "AutoBackup", Value = "true", Description = "Automatically backup data", UpdatedAt = now }
        };
        await context.AppSettings.AddRangeAsync(settings);

        // Add root folders
        var rootFolders = new[]
        {
            new WinWork.Models.Link 
            { 
                Name = "📂 Bookmarks",
                Type = WinWork.Models.LinkType.Folder,
                SortOrder = 1,
                ParentId = null,
                Description = "Root folder for all bookmarks",
                CreatedAt = now,
                UpdatedAt = now
            },
            new WinWork.Models.Link 
            {
                Name = "🔧 Development Tools",
                Type = WinWork.Models.LinkType.Folder,
                SortOrder = 2,
                ParentId = null,
                Description = "Development tools and resources",
                CreatedAt = now,
                UpdatedAt = now
            }
        };
        await context.Links.AddRangeAsync(rootFolders);

        // Save all changes
        await context.SaveChangesAsync();
    }
}
