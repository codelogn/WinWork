using LinkerApp.Core.Interfaces;
using LinkerApp.Models;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text;

namespace LinkerApp.Core.Services;

/// <summary>
/// Service for importing and exporting links
/// </summary>
public class ImportExportService : IImportExportService
{
    public async Task<IEnumerable<Link>> ImportChromeBookmarksAsync(string filePath)
    {
        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var bookmarks = JsonSerializer.Deserialize<ChromeBookmarks>(json);
            
            var links = new List<Link>();
            if (bookmarks?.roots?.bookmarks_bar?.children != null)
            {
                ProcessChromeBookmarkFolder(bookmarks.roots.bookmarks_bar.children, links, null);
            }
            
            return links;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to import Chrome bookmarks: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<Link>> ImportFirefoxBookmarksAsync(string filePath)
    {
        try
        {
            var html = await File.ReadAllTextAsync(filePath);
            return ParseFirefoxBookmarksHtml(html);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to import Firefox bookmarks: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<Link>> ImportEdgeBookmarksAsync(string filePath)
    {
        // Edge uses the same format as Chrome
        return await ImportChromeBookmarksAsync(filePath);
    }

    public async Task<bool> ExportToJsonAsync(IEnumerable<Link> links, string filePath)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(links.ToArray(), options);
            await File.WriteAllTextAsync(filePath, json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ExportToHtmlAsync(IEnumerable<Link> links, string filePath)
    {
        try
        {
            var html = GenerateBookmarksHtml(links);
            await File.WriteAllTextAsync(filePath, html);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ExportToCsvAsync(IEnumerable<Link> links, string filePath)
    {
        try
        {
            var csv = new StringBuilder();
            csv.AppendLine("Name,URL,Description,Type,CreatedAt");
            
            foreach (var link in links)
            {
                var name = EscapeCsv(link.Name);
                var url = EscapeCsv(link.Url);
                var description = EscapeCsv(link.Description ?? "");
                var type = link.Type.ToString();
                var createdAt = link.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                
                csv.AppendLine($"{name},{url},{description},{type},{createdAt}");
            }
            
            await File.WriteAllTextAsync(filePath, csv.ToString());
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<Link>> ImportFromJsonAsync(string filePath)
    {
        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var links = JsonSerializer.Deserialize<Link[]>(json);
            return links ?? Array.Empty<Link>();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to import from JSON: {ex.Message}", ex);
        }
    }

    private void ProcessChromeBookmarkFolder(ChromeBookmarkItem[] items, List<Link> links, int? parentId)
    {
        foreach (var item in items)
        {
            if (item.type == "folder")
            {
                var folder = new Link
                {
                    Name = item.name ?? "Unnamed Folder",
                    Type = LinkType.Folder,
                    ParentId = parentId,
                    CreatedAt = DateTime.Now,
                    Url = ""
                };
                links.Add(folder);
                
                if (item.children != null)
                {
                    // For simplicity, we'll use the list index as ID
                    ProcessChromeBookmarkFolder(item.children, links, links.Count);
                }
            }
            else if (item.type == "url")
            {
                var link = new Link
                {
                    Name = item.name ?? "Unnamed Link",
                    Url = item.url ?? "",
                    Type = DetermineLinktType(item.url ?? ""),
                    ParentId = parentId,
                    CreatedAt = DateTime.Now
                };
                links.Add(link);
            }
        }
    }

    private IEnumerable<Link> ParseFirefoxBookmarksHtml(string html)
    {
        var links = new List<Link>();
        
        // Simple regex-based parsing for Firefox bookmarks HTML
        var linkPattern = @"<A[^>]+HREF=""([^""]+)""[^>]*>([^<]+)</A>";
        var matches = Regex.Matches(html, linkPattern, RegexOptions.IgnoreCase);
        
        foreach (Match match in matches)
        {
            var url = match.Groups[1].Value;
            var name = match.Groups[2].Value;
            
            links.Add(new Link
            {
                Name = name,
                Url = url,
                Type = DetermineLinktType(url),
                CreatedAt = DateTime.Now
            });
        }
        
        return links;
    }

    private string GenerateBookmarksHtml(IEnumerable<Link> links)
    {
        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE NETSCAPE-Bookmark-file-1>");
        html.AppendLine("<META HTTP-EQUIV=\"Content-Type\" CONTENT=\"text/html; charset=UTF-8\">");
        html.AppendLine("<TITLE>Bookmarks</TITLE>");
        html.AppendLine("<H1>Bookmarks</H1>");
        html.AppendLine("<DL><p>");
        
        foreach (var link in links.Where(l => l.Type != LinkType.Folder))
        {
            html.AppendLine($"    <DT><A HREF=\"{link.Url}\">{link.Name}</A>");
            if (!string.IsNullOrEmpty(link.Description))
            {
                html.AppendLine($"    <DD>{link.Description}");
            }
        }
        
        html.AppendLine("</DL><p>");
        return html.ToString();
    }

    private string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
            
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        
        return value;
    }

    private LinkType DetermineLinktType(string url)
    {
        if (string.IsNullOrEmpty(url))
            return LinkType.WebUrl;

        if (url.StartsWith("http://") || url.StartsWith("https://"))
            return LinkType.WebUrl;
        
        if (url.StartsWith("file://"))
            return LinkType.FilePath;
            
        if (Directory.Exists(url))
            return LinkType.FolderPath;
            
        if (File.Exists(url))
            return LinkType.FilePath;
            
        return LinkType.WebUrl;
    }
}

// Chrome bookmarks data structures
public class ChromeBookmarks
{
    public ChromeRoots? roots { get; set; }
}

public class ChromeRoots
{
    public ChromeBookmarkFolder? bookmarks_bar { get; set; }
}

public class ChromeBookmarkFolder
{
    public ChromeBookmarkItem[]? children { get; set; }
}

public class ChromeBookmarkItem
{
    public string? name { get; set; }
    public string? type { get; set; }
    public string? url { get; set; }
    public ChromeBookmarkItem[]? children { get; set; }
}