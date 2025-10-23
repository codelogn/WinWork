using System;
using Microsoft.Data.Sqlite;

if (args.Length < 2)
{
    Console.WriteLine("Usage: set-appsetting <Key> <Value>");
    return 1;
}

var key = args[0];
var value = args[1];
var dbPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\WinWork\\winwork.db";
if (!System.IO.File.Exists(dbPath))
{
    Console.WriteLine($"Database file not found: {dbPath}");
    return 2;
}

var connStr = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();
using var conn = new SqliteConnection(connStr);
conn.Open();

using var cmd = conn.CreateCommand();
cmd.CommandText = "INSERT INTO AppSettings (\"Key\", \"Value\", \"UpdatedAt\") VALUES ($k,$v,datetime('now')) ON CONFLICT(\"Key\") DO UPDATE SET \"Value\" = $v, \"UpdatedAt\" = datetime('now');";
cmd.Parameters.AddWithValue("$k", key);
cmd.Parameters.AddWithValue("$v", value);
var rc = cmd.ExecuteNonQuery();
Console.WriteLine($"Updated {key} => '{value}' ({rc} rows affected)");
conn.Close();
return 0;
