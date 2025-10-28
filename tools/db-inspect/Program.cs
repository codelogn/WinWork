using System;
using System.Data;
using Microsoft.Data.Sqlite;

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

conn.Close();
return 0;

