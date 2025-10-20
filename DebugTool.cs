using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WinWork.Data;
using WinWork.Models;

namespace WinWork.DebugTool;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("WinWork Database Debug Tool");
        Console.WriteLine("=============================");
        
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dbPath = Path.Combine(appDataPath, "WinWork", "WinWork.db");
        
        Console.WriteLine($"Database path: {dbPath}");
        Console.WriteLine($"Database exists: {File.Exists(dbPath)}");
        
        if (!File.Exists(dbPath))
        {
            Console.WriteLine("Database file does not exist!");
            return;
        }
        
        var connectionString = $"Data Source={dbPath}";
        
        var options = new DbContextOptionsBuilder<WinWorkDbContext>()
            .UseSqlite(connectionString)
            .Options;
            
        using var context = new WinWorkDbContext(options);
        
        try
        {
            var totalLinks = await context.Links.CountAsync();
            Console.WriteLine($"Total links in database: {totalLinks}");
            
            var rootLinks = await context.Links.Where(l => l.ParentId == null).ToListAsync();
            Console.WriteLine($"Root links: {rootLinks.Count}");
            
            foreach (var link in rootLinks)
            {
                Console.WriteLine($"  - {link.Name} (Type: {link.Type}, ID: {link.Id})");
            }
            
            var allLinks = await context.Links.OrderBy(l => l.Id).ToListAsync();
            Console.WriteLine("\nAll links:");
            foreach (var link in allLinks)
            {
                Console.WriteLine($"  ID: {link.Id}, Name: {link.Name}, Type: {link.Type}, ParentId: {link.ParentId}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading database: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}
