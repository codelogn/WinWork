using System.Collections.Generic;
using WinWork.Models;

namespace WinWork.UI.ViewModels;

public static class LinkTypeProvider
{
    // Single source of truth for available link types
    public static IReadOnlyList<LinkTypeItem> DefaultLinkTypes { get; } = new List<LinkTypeItem>
    {
        new(LinkType.Folder, "📁 Folder", "Organize items into groups"),
        new(LinkType.WebUrl, "🌐 Web URL", "Website or web page"),
        new(LinkType.FilePath, "📄 File", "Local file or document"),
        new(LinkType.FolderPath, "📂 Folder Path", "Open folder in Explorer"),
        new(LinkType.Application, "💻 Application", "Executable program"),
        new(LinkType.Notes, "📝 Notes", "Text notes and memos"),
        new(LinkType.Terminal, "🖥️ Terminal", "Open a terminal and execute commands")
    };
}
