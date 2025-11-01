using Microsoft.EntityFrameworkCore;
using WinWork.Models;

namespace WinWork.Data;

/// <summary>
/// Entity Framework DbContext for the LinkerApp database
/// </summary>
public class WinWorkDbContext : DbContext
{
    public WinWorkDbContext(DbContextOptions<WinWorkDbContext> options)
        : base(options)
    {
    }

    public DbSet<Link> Links { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<LinkTag> LinkTags { get; set; }
    public DbSet<AppSettings> AppSettings { get; set; }
    public DbSet<WinWork.Models.HotNav> HotNavs { get; set; }
    public DbSet<WinWork.Models.HotNavRoot> HotNavRoots { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Link entity
        modelBuilder.Entity<Link>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Url).HasMaxLength(2048);
            entity.Property(e => e.Command).HasMaxLength(2000);
            entity.Property(e => e.TerminalType).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Notes).HasMaxLength(10000);
            entity.Property(e => e.IconPath).HasMaxLength(500);
            entity.Property(e => e.Type).HasConversion<int>();

            // Self-referencing relationship for hierarchical structure
            entity.HasOne(e => e.Parent)
                  .WithMany(e => e.Children)
                  .HasForeignKey(e => e.ParentId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Index on ParentId for performance
            entity.HasIndex(e => e.ParentId);
            entity.HasIndex(e => e.SortOrder);
            entity.HasIndex(e => new { e.ParentId, e.SortOrder });
        });

        // Configure Tag entity
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Color).IsRequired().HasMaxLength(7); // #FFFFFF format
            entity.Property(e => e.Description).HasMaxLength(500);

            // Unique constraint on tag name
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Configure LinkTag junction table
        modelBuilder.Entity<LinkTag>(entity =>
        {
            entity.HasKey(e => new { e.LinkId, e.TagId });

            entity.HasOne(e => e.Link)
                  .WithMany(e => e.LinkTags)
                  .HasForeignKey(e => e.LinkId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Tag)
                  .WithMany(e => e.LinkTags)
                  .HasForeignKey(e => e.TagId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure AppSettings entity
        modelBuilder.Entity<AppSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Value).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);

            // Unique constraint on setting key
            entity.HasIndex(e => e.Key).IsUnique();
        });

        // Seed some default data
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Use a fixed datetime for seeded data to avoid dynamic values
        var seedDateTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Default tags
        modelBuilder.Entity<Tag>().HasData(
            new Tag { Id = 1, Name = "Work", Color = "#0078d4", Description = "Work-related links", CreatedAt = seedDateTime, UpdatedAt = seedDateTime },
            new Tag { Id = 2, Name = "Personal", Color = "#107c10", Description = "Personal links", CreatedAt = seedDateTime, UpdatedAt = seedDateTime },
            new Tag { Id = 3, Name = "Development", Color = "#e74c3c", Description = "Development tools and resources", CreatedAt = seedDateTime, UpdatedAt = seedDateTime },
            new Tag { Id = 4, Name = "Frequently Used", Color = "#f39c12", Description = "Most frequently accessed links", CreatedAt = seedDateTime, UpdatedAt = seedDateTime },
            new Tag { Id = 5, Name = "Learning", Color = "#9b59b6", Description = "Educational resources", CreatedAt = seedDateTime, UpdatedAt = seedDateTime }
        );

        // Default application settings
        modelBuilder.Entity<AppSettings>().HasData(
            new AppSettings { Id = 1, Key = "Theme", Value = "Dark", Description = "Application theme (Light/Dark)", UpdatedAt = seedDateTime },
            new AppSettings { Id = 2, Key = "GlobalHotkey", Value = "Ctrl+Alt+L", Description = "Global hotkey to show application", UpdatedAt = seedDateTime },
            new AppSettings { Id = 3, Key = "MinimizeToTray", Value = "true", Description = "Minimize to system tray when closed", UpdatedAt = seedDateTime },
            new AppSettings { Id = 4, Key = "StartWithWindows", Value = "false", Description = "Start application with Windows", UpdatedAt = seedDateTime },
            new AppSettings { Id = 5, Key = "AutoBackup", Value = "true", Description = "Automatically backup data", UpdatedAt = seedDateTime },
            new AppSettings { Id = 6, Key = "BackupInterval", Value = "7", Description = "Backup interval in days", UpdatedAt = seedDateTime },
            // Terminal configuration defaults
            new AppSettings { Id = 7, Key = "Terminal.PowerShellPath", Value = "powershell.exe", Description = "Path to PowerShell executable", UpdatedAt = seedDateTime },
            new AppSettings { Id = 8, Key = "Terminal.GitBashPath", Value = "", Description = "Path to Git Bash executable (optional)", UpdatedAt = seedDateTime },
            new AppSettings { Id = 9, Key = "Terminal.CmdPath", Value = "cmd.exe", Description = "Path to CMD executable", UpdatedAt = seedDateTime },
            new AppSettings { Id = 10, Key = "Terminal.Default", Value = "PowerShell", Description = "Default terminal when opening Terminal items", UpdatedAt = seedDateTime }
        );

        // Sample root folders
        modelBuilder.Entity<Link>().HasData(
            new Link 
            { 
                Id = 1, 
                Name = "Bookmarks", 
                Type = LinkType.Folder, 
                SortOrder = 1,
                ParentId = null,
                Description = "Root folder for all bookmarks",
                CreatedAt = seedDateTime,
                UpdatedAt = seedDateTime
            },
            new Link 
            { 
                Id = 2, 
                Name = "Development Tools", 
                Type = LinkType.Folder, 
                SortOrder = 2,
                ParentId = null,
                Description = "Development-related applications and links",
                CreatedAt = seedDateTime,
                UpdatedAt = seedDateTime
            },
            new Link 
            { 
                Id = 3, 
                Name = "Productivity", 
                Type = LinkType.Folder, 
                SortOrder = 3,
                ParentId = null,
                Description = "Productivity applications and resources",
                CreatedAt = seedDateTime,
                UpdatedAt = seedDateTime
            }
        );

        // Seed HotNav groups and roots (basic paths)
        modelBuilder.Entity<HotNav>().HasData(
            new HotNav { Id = 1, Name = "Projects", IncludeFiles = true, MaxDepth = 4, SortOrder = 1, CreatedAt = seedDateTime, UpdatedAt = seedDateTime },
            new HotNav { Id = 2, Name = "Utilities", IncludeFiles = false, MaxDepth = 3, SortOrder = 2, CreatedAt = seedDateTime, UpdatedAt = seedDateTime }
        );

        modelBuilder.Entity<HotNavRoot>().HasData(
            new HotNavRoot { Id = 1, HotNavId = 1, Path = "C:\\Users\\Public\\Documents", SortOrder = 1, CreatedAt = seedDateTime, UpdatedAt = seedDateTime },
            new HotNavRoot { Id = 2, HotNavId = 2, Path = "C:\\ProgramData", SortOrder = 1, CreatedAt = seedDateTime, UpdatedAt = seedDateTime }
        );
    }
}
