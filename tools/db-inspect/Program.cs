using System;
using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WinWork.Data;

// Simple inspector for the WinWork SQLite DB
// Usage: dotnet run --project tools/db-inspect

var dbPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\WinWork\\winwork.db";
Console.WriteLine($"Inspecting DB: {dbPath}");
if (!System.IO.File.Exists(dbPath))
{
    Console.WriteLine("Database file not found.");
    return 1;
}

var connStr = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();
using var conn = new SqliteConnection(connStr);
conn.Open();

// Check if Links table exists
using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = "PRAGMA table_info('Links');";
    using var reader = cmd.ExecuteReader();
    bool hasCommand = false;
    Console.WriteLine("Links table columns:");
    while (reader.Read())
    {
        var name = reader.GetString(1);
        var type = reader.GetString(2);
        Console.WriteLine($" - {name} ({type})");
        if (name.Equals("Command", StringComparison.OrdinalIgnoreCase)) hasCommand = true;
    }
    Console.WriteLine($"\nLinks.Command column present: {hasCommand}\n");
}

// Check HotNavs table
using (var cmdHot = conn.CreateCommand())
{
    cmdHot.CommandText = "PRAGMA table_info('HotNavs');";
    using var readerHot = cmdHot.ExecuteReader();
    Console.WriteLine("HotNavs table columns:");
    var anyHot = false;
    while (readerHot.Read())
    {
        anyHot = true;
        var name = readerHot.GetString(1);
        var type = readerHot.GetString(2);
        Console.WriteLine($" - {name} ({type})");
    }
    if (!anyHot) Console.WriteLine(" - (HotNavs table not found)");
}

using (var cmdHotCount = conn.CreateCommand())
{
    cmdHotCount.CommandText = "SELECT COUNT(1) FROM sqlite_master WHERE type='table' AND name='HotNavs';";
    var exists = Convert.ToInt32(cmdHotCount.ExecuteScalar() ?? 0) > 0;
    Console.WriteLine($"\nHotNavs table present: {exists}");
}

using (var cmdHotData = conn.CreateCommand())
{
    try
    {
        cmdHotData.CommandText = "SELECT Id, Name, IncludeFiles, MaxDepth, SortOrder FROM HotNavs ORDER BY SortOrder LIMIT 10;";
        using var r = cmdHotData.ExecuteReader();
        Console.WriteLine("\nHotNavs rows (up to 10):");
        var any = false;
        while (r.Read())
        {
            any = true;
            Console.WriteLine($" - Id={r.GetInt32(0)}, Name='{r.GetString(1)}', IncludeFiles={r.GetInt32(2)}, MaxDepth={(r.IsDBNull(3)?"null":r.GetInt32(3).ToString())}, SortOrder={r.GetInt32(4)}");
        }
        if (!any) Console.WriteLine(" - (no rows)");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Could not query HotNavs table: {ex.Message}");
    }
}

// Query AppSettings for terminal keys and appearance settings
var keys = new[] { "Terminal.PowerShellPath", "Terminal.GitBashPath", "Terminal.CmdPath", "Terminal.Default", "BackgroundColor", "WindowTransparency" };
using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = "SELECT \"Key\", \"Value\" FROM \"AppSettings\" WHERE \"Key\" IN ($k1,$k2,$k3,$k4,$k5,$k6);";
    cmd.Parameters.AddWithValue("$k1", keys[0]);
    cmd.Parameters.AddWithValue("$k2", keys[1]);
    cmd.Parameters.AddWithValue("$k3", keys[2]);
    cmd.Parameters.AddWithValue("$k4", keys[3]);
    cmd.Parameters.AddWithValue("$k5", keys[4]);
    cmd.Parameters.AddWithValue("$k6", keys[5]);

    using var reader = cmd.ExecuteReader();
    Console.WriteLine("AppSettings (selected keys):");
    var found = 0;
    while (reader.Read())
    {
        found++;
        Console.WriteLine($" - {reader.GetString(0)} = '{reader.GetString(1)}'");
    }
    if (found == 0) Console.WriteLine(" - (none found)");
}

// List terminal links
using (var countCmd = conn.CreateCommand())
{
    countCmd.CommandText = "SELECT COUNT(1) FROM Links WHERE Type = $type;";
    countCmd.Parameters.AddWithValue("$type", 8);
    var count = Convert.ToInt32(countCmd.ExecuteScalar() ?? 0);
    Console.WriteLine($"\nTerminal links count: {count}");
}

using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = "SELECT Id, Name, Url, TerminalType, Command FROM Links WHERE Type = $type ORDER BY Id DESC LIMIT 20;";
    cmd.Parameters.AddWithValue("$type", 8);
    using var reader = cmd.ExecuteReader();
    Console.WriteLine("\nRecent Terminal Links:");
    var any = false;
    while (reader.Read())
    {
        any = true;
        var id = reader.GetInt32(0);
        var name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
        var url = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
        var termType = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
        var cmdText = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);
        Console.WriteLine($" - Id={id}, Name='{name}', Url='{url}', TerminalType='{termType}', Command='{cmdText}'");
    }
    if (!any) Console.WriteLine(" - (none found)");
}
// Also list all AppSettings entries for detailed inspection
using (var cmdAll = conn.CreateCommand())
{
    cmdAll.CommandText = "SELECT \"Key\", \"Value\", \"UpdatedAt\" FROM \"AppSettings\" ORDER BY \"Key\";";
    using var readerAll = cmdAll.ExecuteReader();
    Console.WriteLine("\nAll AppSettings:");
    var anyAll = false;
    while (readerAll.Read())
    {
        anyAll = true;
        var k = readerAll.IsDBNull(0) ? "" : readerAll.GetString(0);
        var v = readerAll.IsDBNull(1) ? "" : readerAll.GetString(1);
        var u = readerAll.IsDBNull(2) ? "" : readerAll.GetString(2);
        Console.WriteLine($" - {k} = '{v}' (UpdatedAt: {u})");
    }
    if (!anyAll) Console.WriteLine(" - (none)");
}
// Show EF Migrations history table if present
using (var cmdHist = conn.CreateCommand())
{
    try
    {
        cmdHist.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='__EFMigrationsHistory';";
        var exists = Convert.ToInt32(cmdHist.ExecuteScalar() ?? 0) > 0;
        Console.WriteLine($"\n__EFMigrationsHistory present: {exists}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Could not check migrations history table: {ex.Message}");
    }
}

// Create a WinWorkDbContext using the same SQLite file and enumerate migrations via EF
try
{
    var options = new DbContextOptionsBuilder<WinWorkDbContext>()
        .UseSqlite(connStr)
        .Options;

    using var efCtx = new WinWorkDbContext(options);
    Console.WriteLine("\nEF Core - migrations visible to DbContext:");
    try
    {
        var all = efCtx.Database.GetMigrations();
        var applied = efCtx.Database.GetAppliedMigrations();
        var pending = efCtx.Database.GetPendingMigrations();
        Console.WriteLine($"All migrations: {string.Join(", ", all)}");
        Console.WriteLine($"Applied migrations: {string.Join(", ", applied)}");
        Console.WriteLine($"Pending migrations: {string.Join(", ", pending)}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"EF migration enumeration failed: {ex.Message}");
    }

}
catch (Exception ex)
{
    Console.WriteLine($"Failed to create WinWorkDbContext for inspection: {ex.Message}");
}

conn.Close();
return 0;

