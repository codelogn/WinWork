using System.Diagnostics;
using System.Text.RegularExpressions;
using WinWork.Models;

namespace WinWork.Core.Services;

/// <summary>
/// Service implementation for opening various types of links
/// </summary>
public class LinkOpenerService : ILinkOpenerService
{
    private readonly ISettingsService? _settingsService;

    public LinkOpenerService(ISettingsService? settingsService = null)
    {
        _settingsService = settingsService;
    }
    public async Task<bool> OpenAsync(Link link)
    {
        if (link == null)
            return false;

        if (link.Type == LinkType.Terminal)
        {
            // For Terminal type, use TerminalType first; fallback to Url for older entries
            var shell = !string.IsNullOrWhiteSpace(link.TerminalType) ? link.TerminalType : (link.Url ?? string.Empty);
            return await OpenTerminalAsync(shell, link.Command);
        }

        if (string.IsNullOrWhiteSpace(link.Url))
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
                LinkType.Terminal => await OpenTerminalAsync(url, null),
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
            LinkType.Terminal => true,
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
        if (string.IsNullOrWhiteSpace(appPath))
            return false;

        try
        {
            // Parse the application path and arguments
            var (fileName, arguments) = ParseApplicationPath(appPath);
            
            if (!File.Exists(fileName))
                return false;

            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = true
            };

            return await StartProcessAsync(psi);
        }
        catch
        {
            return false;
        }
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

        // Use Uri.TryCreate for robust validation (accepts fragments, queries, etc.)
        if (Uri.TryCreate(url, UriKind.Absolute, out var uriResult))
        {
            return uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps;
        }
        return false;
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
            // Parse the application path to separate executable from arguments
            var (fileName, _) = ParseApplicationPath(appPath);
            
            if (string.IsNullOrWhiteSpace(fileName))
                return false;
                
            var extension = Path.GetExtension(fileName)?.ToLower();
            
            // Common executable extensions
            var validExtensions = new[] { ".exe", ".bat", ".cmd", ".com", ".msi" };
            return validExtensions.Contains(extension);
        }
        catch
        {
            return false;
        }
    }

    private (string fileName, string arguments) ParseApplicationPath(string appPath)
    {
        if (string.IsNullOrWhiteSpace(appPath))
            return (string.Empty, string.Empty);

        appPath = appPath.Trim();

        // Handle quoted paths
        if (appPath.StartsWith("\""))
        {
            var endQuoteIndex = appPath.IndexOf("\"", 1);
            if (endQuoteIndex > 0)
            {
                var fileName = appPath.Substring(1, endQuoteIndex - 1);
                var arguments = appPath.Length > endQuoteIndex + 1 ? 
                    appPath.Substring(endQuoteIndex + 1).Trim() : string.Empty;
                return (fileName, arguments);
            }
        }

        // Handle unquoted paths - split on first space that's not part of a path
        var parts = appPath.Split(' ', 2);
        if (parts.Length == 1)
        {
            return (parts[0], string.Empty);
        }

        // Check if the first part is a valid file path
        if (File.Exists(parts[0]))
        {
            return (parts[0], parts[1]);
        }

        // If not, try to find the executable by looking for common patterns
        var words = appPath.Split(' ');
        for (int i = 1; i < words.Length; i++)
        {
            var potentialPath = string.Join(" ", words.Take(i + 1));
            if (File.Exists(potentialPath))
            {
                var remainingArgs = i + 1 < words.Length ? 
                    string.Join(" ", words.Skip(i + 1)) : string.Empty;
                return (potentialPath, remainingArgs);
            }
        }

        // Fallback - assume first part is the executable
        return (parts[0], parts[1]);
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

    private async Task<bool> OpenTerminalAsync(string shell, string? command)
    {
        // shell: expected values like "PowerShell", "Git Bash", "CMD" or path to shell
        // command: command(s) to run in the terminal
        if (string.IsNullOrWhiteSpace(shell)) return false;

        try
        {
            shell = shell.Trim();

            if (shell.Equals("PowerShell", StringComparison.OrdinalIgnoreCase))
            {
                // Prefer configured PowerShell path if available
                var psPath = _settingsService != null ? await _settingsService.GetTerminalPowerShellPathAsync() : "powershell.exe";
                var psi = new ProcessStartInfo
                {
                    FileName = string.IsNullOrWhiteSpace(psPath) ? "powershell.exe" : psPath,
                    Arguments = $"-NoExit -Command \"{(command ?? string.Empty).Replace("\"", "\\\"") }\"",
                    UseShellExecute = true
                };
                return await StartProcessAsync(psi);
            }

            if (shell.Equals("Git Bash", StringComparison.OrdinalIgnoreCase) || shell.Equals("GitBash", StringComparison.OrdinalIgnoreCase))
            {
                // Prefer configured Git Bash path if available
                var configuredGit = _settingsService != null ? await _settingsService.GetTerminalGitBashPathAsync() : string.Empty;
                if (!string.IsNullOrWhiteSpace(configuredGit) && File.Exists(configuredGit))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = configuredGit,
                        Arguments = $"--login -i -c \"{(command ?? string.Empty).Replace("\"", "\\\"") }\"",
                        UseShellExecute = true
                    };
                    return await StartProcessAsync(psi);
                }

                // Try common Git Bash path
                var gitBashPath = "C:\\Program Files\\Git\\git-bash.exe";
                if (File.Exists(gitBashPath))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = gitBashPath,
                        Arguments = $"--login -i -c \"{(command ?? string.Empty).Replace("\"", "\\\"") }\"",
                        UseShellExecute = true
                    };
                    return await StartProcessAsync(psi);
                }

                // Fallback to bash if available
                var bashPath = "C:\\Program Files\\Git\\usr\\bin\\bash.exe";
                if (File.Exists(bashPath))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = bashPath,
                        Arguments = $"-lc \"{(command ?? string.Empty).Replace("\"", "\\\"") }\"",
                        UseShellExecute = true
                    };
                    return await StartProcessAsync(psi);
                }

                return false;
            }

            if (shell.Equals("CMD", StringComparison.OrdinalIgnoreCase) || shell.Equals("Command Prompt", StringComparison.OrdinalIgnoreCase))
            {
                var configuredCmd = _settingsService != null ? await _settingsService.GetTerminalCmdPathAsync() : "cmd.exe";
                var psi = new ProcessStartInfo
                {
                    FileName = string.IsNullOrWhiteSpace(configuredCmd) ? "cmd.exe" : configuredCmd,
                    Arguments = $"/k {(command ?? string.Empty)}",
                    UseShellExecute = true
                };
                return await StartProcessAsync(psi);
            }

            // If shell is a path to an executable, just start it with the command as arguments
            if (File.Exists(shell))
            {
                var psi = new ProcessStartInfo
                {
                    FileName = shell,
                    Arguments = command ?? string.Empty,
                    UseShellExecute = true
                };
                return await StartProcessAsync(psi);
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
