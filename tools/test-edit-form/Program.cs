using System;
using Microsoft.EntityFrameworkCore;
using WinWork.Data;
using WinWork.Models;
using WinWork.UI.ViewModels;
using System.Linq;

var db = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\WinWork\\winwork.db";
var options = new DbContextOptionsBuilder<WinWorkDbContext>()
    .UseSqlite($"Data Source={db}")
    .Options;

using var ctx = new WinWorkDbContext(options);
var terminalLinks = ctx.Links.Where(l => l.Type == LinkType.Terminal).OrderByDescending(l => l.Id).ToList();
Console.WriteLine($"Found {terminalLinks.Count} terminal links (showing top 5):");
foreach (var l in terminalLinks.Take(5))
{
    Console.WriteLine($" - Id={l.Id}, Name='{l.Name}', Url='{l.Url}', TerminalType='{l.TerminalType}', Command='{l.Command}'");
}

if (!terminalLinks.Any()) return 0;

var link = terminalLinks.First();
var vm = new LinkDialogViewModel();
vm.SetEditMode(link);

Console.WriteLine("\nViewModel state after SetEditMode:");
Console.WriteLine($"SelectedLinkType={vm.SelectedLinkType}");
var match = vm.LinkTypes.FirstOrDefault(x => x.Type == vm.SelectedLinkType);
Console.WriteLine($"SelectedLinkTypeDisplay={match?.DisplayName}");
Console.WriteLine($"IsNotesType={vm.IsNotesType}");
Console.WriteLine($"RequiresUrl={vm.RequiresUrl}");
Console.WriteLine($"Url='{vm.Url}'");
Console.WriteLine($"TerminalType='{vm.TerminalType}'");
Console.WriteLine($"TerminalShell='{vm.TerminalShell}'");
Console.WriteLine($"Command='{vm.Command}'");

return 0;