using System.Diagnostics;
using System.Text.RegularExpressions;
using LinkerApp.Models;

namespace LinkerApp.Core.Services;

/// <summary>
/// Service implementation for opening various types of links
/// </summary>
public class LinkOpenerService : ILinkOpenerService
{
    public async Task<bool> OpenAsync(Link link)
    {
        if (link == null || string.IsNullOrWhiteSpace(link.Url))
            return false;

        return await OpenAsync(link.Url, link.Type);
    }

    public async Task<bool> OpenAsync(string url, LinkType type)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        try
        {
            return type switch
            {
                LinkType.WebUrl => await OpenWebUrlAsync(url),
                LinkType.FilePath => await OpenFilePathAsync(url),
                LinkType.FolderPath => await OpenFolderPathAsync(url),
                LinkType.Application => await OpenApplicationAsync(url),
                LinkType.WindowsStoreApp => await OpenWindowsStoreAppAsync(url),
                LinkType.SystemLocation => await OpenSystemLocationAsync(url),
                _ => false
            };
        }
        catch (Exception ex)
        {
            // Log the exception (in a real app, use proper logging)
            Console.WriteLine($"Error opening link {url}: {ex.Message}");
            return false;
        }
    }

    public bool CanOpen(LinkType type)
    {
        return type switch
        {
            LinkType.Folder => false, // Folders can't be opened directly
            LinkType.WebUrl => true,
            LinkType.FilePath => true,
            LinkType.FolderPath => true,
            LinkType.Application => true,
            LinkType.WindowsStoreApp => true,
            LinkType.SystemLocation => true,
            _ => false
        };
    }

    public bool ValidateLink(string url, LinkType type)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return type switch
        {
            LinkType.WebUrl => ValidateWebUrl(url),
            LinkType.FilePath => ValidateFilePath(url),
            LinkType.FolderPath => ValidateFolderPath(url),
            LinkType.Application => ValidateApplicationPath(url),
            LinkType.WindowsStoreApp => ValidateWindowsStoreApp(url),
            LinkType.SystemLocation => ValidateSystemLocation(url),
            _ => false
        };
    }

    private async Task<bool> OpenWebUrlAsync(string url)
    {
        // Ensure URL has protocol
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
        {
            url = "https://" + url;
        }

        var psi = new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        };

        return await StartProcessAsync(psi);
    }

    private async Task<bool> OpenFilePathAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return false;

        var psi = new ProcessStartInfo
        {
            FileName = filePath,
            UseShellExecute = true
        };

        return await StartProcessAsync(psi);
    }

    private async Task<bool> OpenFolderPathAsync(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            return false;

        var psi = new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\"{folderPath}\"",
            UseShellExecute = false
        };

        return await StartProcessAsync(psi);
    }

    private async Task<bool> OpenApplicationAsync(string appPath)
    {
        if (!File.Exists(appPath))
            return false;

        var psi = new ProcessStartInfo
        {
            FileName = appPath,
            UseShellExecute = true
        };

        return await StartProcessAsync(psi);
    }

    private async Task<bool> OpenWindowsStoreAppAsync(string appUri)
    {
        // Windows Store apps use ms-windows-store: or custom protocols
        var psi = new ProcessStartInfo
        {
            FileName = appUri,
            UseShellExecute = true
        };

        return await StartProcessAsync(psi);
    }

    private async Task<bool> OpenSystemLocationAsync(string location)
    {
        // System locations like ms-settings:, shell: commands, etc.
        var psi = new ProcessStartInfo
        {
            FileName = location,
            UseShellExecute = true
        };

        return await StartProcessAsync(psi);
    }

    private async Task<bool> StartProcessAsync(ProcessStartInfo psi)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var process = Process.Start(psi);
                return process != null;
            }
            catch
            {
                return false;
            }
        });
    }

    private bool ValidateWebUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        // Allow URLs with or without protocol
        var urlPattern = @"^(https?://)?([\da-z\.-]+)\.([a-z\.]{2,6})([/\w \.-]*)*/?$";
        return Regex.IsMatch(url, urlPattern, RegexOptions.IgnoreCase);
    }

    private bool ValidateFilePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        try
        {
            // Check if path is valid format (doesn't need to exist for validation)
            var fullPath = Path.GetFullPath(filePath);
            return !string.IsNullOrWhiteSpace(Path.GetFileName(fullPath));
        }
        catch
        {
            return false;
        }
    }

    private bool ValidateFolderPath(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            return false;

        try
        {
            // Check if path is valid format
            Path.GetFullPath(folderPath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool ValidateApplicationPath(string appPath)
    {
        if (string.IsNullOrWhiteSpace(appPath))
            return false;

        try
        {
            var fullPath = Path.GetFullPath(appPath);
            var extension = Path.GetExtension(fullPath)?.ToLower();
            
            // Common executable extensions
            var validExtensions = new[] { ".exe", ".bat", ".cmd", ".com", ".msi" };
            return validExtensions.Contains(extension);
        }
        catch
        {
            return false;
        }
    }

    private bool ValidateWindowsStoreApp(string appUri)
    {
        if (string.IsNullOrWhiteSpace(appUri))
            return false;

        // Windows Store URIs typically start with ms-windows-store: or custom protocol
        return appUri.Contains("://") || appUri.StartsWith("ms-");
    }

    private bool ValidateSystemLocation(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
            return false;

        // System locations: shell:, ms-settings:, etc.
        var systemPrefixes = new[] { "shell:", "ms-settings:", "ms-", "control.exe", "rundll32.exe" };
        return systemPrefixes.Any(prefix => location.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}