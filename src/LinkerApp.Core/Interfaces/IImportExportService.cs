using LinkerApp.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LinkerApp.Core.Interfaces;

/// <summary>
/// Interface for import/export functionality
/// </summary>
public interface IImportExportService
{
    /// <summary>
    /// Import bookmarks from Chrome bookmarks file
    /// </summary>
    Task<IEnumerable<Link>> ImportChromeBookmarksAsync(string filePath);

    /// <summary>
    /// Import bookmarks from Firefox bookmarks HTML file
    /// </summary>
    Task<IEnumerable<Link>> ImportFirefoxBookmarksAsync(string filePath);

    /// <summary>
    /// Import bookmarks from Edge bookmarks file
    /// </summary>
    Task<IEnumerable<Link>> ImportEdgeBookmarksAsync(string filePath);

    /// <summary>
    /// Export links to JSON format
    /// </summary>
    Task<bool> ExportToJsonAsync(IEnumerable<Link> links, string filePath);

    /// <summary>
    /// Export links to HTML format (bookmarks style)
    /// </summary>
    Task<bool> ExportToHtmlAsync(IEnumerable<Link> links, string filePath);

    /// <summary>
    /// Export links to CSV format
    /// </summary>
    Task<bool> ExportToCsvAsync(IEnumerable<Link> links, string filePath);

    /// <summary>
    /// Import links from JSON format
    /// </summary>
    Task<IEnumerable<Link>> ImportFromJsonAsync(string filePath);
}