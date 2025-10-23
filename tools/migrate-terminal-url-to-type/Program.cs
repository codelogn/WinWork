using System;
using Microsoft.Data.Sqlite;

// One-off utility to migrate existing Terminal rows that have Url set into TerminalType when TerminalType is null
var dbPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\WinWork\\winwork.db";
Console.WriteLine($"DB: {dbPath}");
if (!System.IO.File.Exists(dbPath))
{
    Console.WriteLine("DB not found");
    return 1;
}

using var conn = new SqliteConnection($"Data Source={dbPath}");
conn.Open();

// Update links where Type == Terminal and TerminalType IS NULL and Url is not empty
using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = @"UPDATE Links SET TerminalType = Url WHERE Type = $type AND (TerminalType IS NULL OR TRIM(TerminalType) = '') AND (Url IS NOT NULL AND TRIM(Url) <> '');";
    cmd.Parameters.AddWithValue("$type", 8); // LinkType.Terminal == 8
    var rows = cmd.ExecuteNonQuery();
    Console.WriteLine($"Updated {rows} rows.");
}

conn.Close();
return 0;