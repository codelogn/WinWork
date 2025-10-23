using System;
using Microsoft.EntityFrameworkCore;
using WinWork.Data;
using WinWork.Models;

// Quick utility to insert a Terminal link using EF so we can verify DB columns
var options = new DbContextOptionsBuilder<WinWorkDbContext>()
    .UseSqlite($"Data Source={Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\WinWork\\winwork.db")
    .Options;

using var ctx = new WinWorkDbContext(options);

var link = new Link
{
    Name = "Test Terminal Item",
    Type = LinkType.Terminal,
    TerminalType = "PowerShell",
    Command = "echo Hello from terminal item",
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow,
    SortOrder = 9999
};

ctx.Links.Add(link);
ctx.SaveChanges();

Console.WriteLine($"Inserted Link Id = {link.Id}");

var fetched = ctx.Links.Find(link.Id);
Console.WriteLine($"Fetched row: Id={fetched.Id}, Name='{fetched.Name}', Type={fetched.Type}, TerminalType='{fetched.TerminalType}', Url='{fetched.Url}', Command='{fetched.Command}'");
