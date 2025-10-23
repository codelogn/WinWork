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

    // Convert a Windows path (C:\...) to a MSYS-style path (/c/...) for bash/mintty
    private static string ConvertToMsysPath(string windowsPath)
    {
        if (string.IsNullOrWhiteSpace(windowsPath)) return windowsPath;
        try
        {
            var full = Path.GetFullPath(windowsPath).Replace('\\', '/');
            // C:/path -> /c/path
            if (Regex.IsMatch(full, "^[A-Za-z]:/"))
            {
                var drive = char.ToLower(full[0]);
                return "/" + drive + full.Substring(2);
            }
            return full;
        }
        catch
        {
            return windowsPath;
        }
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
                Console.WriteLine($"[LinkOpener] Configured Git Bash value from settings: '{configuredGit}'");
                if (!string.IsNullOrWhiteSpace(configuredGit))
                {
                    // Support configured value that may include arguments (exe + args). Parse it.
                    var (configuredExe, configuredArgs) = ParseApplicationPath(configuredGit);
                    Console.WriteLine($"[LinkOpener] Parsed configuredExe='{configuredExe}', configuredArgs='{configuredArgs}'");
                    if (!string.IsNullOrWhiteSpace(configuredExe) && File.Exists(configuredExe))
                    {
                        Console.WriteLine($"[LinkOpener] Found configured exe at: {configuredExe}");
                        // Build arguments by merging configured args with the command wrapper.
                        if (string.IsNullOrWhiteSpace(command))
                        {
                            var startInfo = new ProcessStartInfo
                            {
                                FileName = configuredExe,
                                Arguments = configuredArgs,
                                UseShellExecute = true
                            };
                            if (await StartProcessAsync(startInfo)) return true;
                        }
                        else
                        {
                            var cmdEscaped = (command ?? string.Empty).Replace("\"", "\\\"");
                            var exeName = Path.GetFileName(configuredExe).ToLowerInvariant();

                            // Merge configured args then append a wrapper that keeps bash interactive.
                            string finalArgs;
                            bool useShellExecute = true;

                            if (exeName.Contains("mintty"))
                            {
                                // mintty: pass through configured args and then run bash -lc "...; exec bash"
                                finalArgs = (string.IsNullOrWhiteSpace(configuredArgs) ? string.Empty : configuredArgs + " ")
                                    + $"-e \"/usr/bin/bash\" -lc \"{cmdEscaped}; exec bash\"";
                                useShellExecute = false;
                            }
                            else if (exeName.Contains("bash"))
                            {
                                // bash.exe: pass configured args then -lc
                                finalArgs = (string.IsNullOrWhiteSpace(configuredArgs) ? string.Empty : configuredArgs + " ")
                                    + $"-lc \"{cmdEscaped}; exec bash\"";
                            }
                            else if (exeName.Contains("git-bash"))
                            {
                                // git-bash.exe launcher: append configured args and a -c wrapper (may not be supported by launcher)
                                finalArgs = (string.IsNullOrWhiteSpace(configuredArgs) ? string.Empty : configuredArgs + " ")
                                    + $"--login -i -c \"{cmdEscaped}; exec bash\"";
                            }
                            else
                            {
                                // Generic executable: pass configured args and the command as-is
                                finalArgs = (string.IsNullOrWhiteSpace(configuredArgs) ? string.Empty : configuredArgs + " ") + cmdEscaped;
                            }

                            var startInfo = new ProcessStartInfo
                            {
                                FileName = configuredExe,
                                Arguments = finalArgs,
                                UseShellExecute = useShellExecute
                            };
                            Console.WriteLine($"[LinkOpener] Starting configured exe: {configuredExe} {finalArgs} (UseShellExecute={useShellExecute})");
                            if (await StartProcessAsync(startInfo)) return true;
                        }
                    }

                    // If configured path didn't start (or didn't exist), fall back to searching common locations
                    try
                    {
                        var dir = Path.GetDirectoryName(configuredGit) ?? string.Empty;
                        var possibleMintty = Path.Combine(dir, "usr", "bin", "mintty.exe");
                        Console.WriteLine($"[LinkOpener] Checking for possible mintty at: {possibleMintty}");
                        var possibleBash = Path.Combine(dir, "usr", "bin", "bash.exe");
                        Console.WriteLine($"[LinkOpener] Checking for possible bash at: {possibleBash}");

                        if (File.Exists(possibleMintty))
                        {
                            if (string.IsNullOrWhiteSpace(command))
                            {
                                var startInfo = new ProcessStartInfo
                                {
                                    FileName = possibleMintty,
                                    Arguments = "-e \"/usr/bin/bash\" -i -l",
                                    UseShellExecute = false
                                };
                                return await StartProcessAsync(startInfo);
                            }

                            var tempPath = Path.Combine(Path.GetTempPath(), $"winwork_cmd_{Guid.NewGuid():N}.sh");
                            var scriptContent = command + Environment.NewLine + "exec bash" + Environment.NewLine;
                            File.WriteAllText(tempPath, scriptContent);
                            var msysPath = ConvertToMsysPath(tempPath);

                            var args = $"-e \"/usr/bin/bash\" -lc \"source {msysPath}; exec bash\"";

                            var startInfo2 = new ProcessStartInfo
                            {
                                FileName = possibleMintty,
                                Arguments = args,
                                UseShellExecute = false
                            };
                            Console.WriteLine($"[LinkOpener] Launching mintty: {possibleMintty} {args}");
                            return await StartProcessAsync(startInfo2);
                        }

                        if (File.Exists(possibleBash))
                        {
                            if (string.IsNullOrWhiteSpace(command))
                            {
                                var startInfo = new ProcessStartInfo
                                {
                                    FileName = possibleBash,
                                    Arguments = "-i",
                                    UseShellExecute = true
                                };
                                return await StartProcessAsync(startInfo);
                            }

                            var tempPath = Path.Combine(Path.GetTempPath(), $"winwork_cmd_{Guid.NewGuid():N}.sh");
                            var scriptContent = command + Environment.NewLine + "exec bash" + Environment.NewLine;
                            File.WriteAllText(tempPath, scriptContent);
                            var msysPath = ConvertToMsysPath(tempPath);

                            var args = $"-lc \"source {msysPath}; exec bash\"";

                            var startInfo3 = new ProcessStartInfo
                            {
                                FileName = possibleBash,
                                Arguments = args,
                                UseShellExecute = true
                            };
                            Console.WriteLine($"[LinkOpener] Launching bash.exe: {possibleBash} {args}");
                            return await StartProcessAsync(startInfo3);
                        }
                    }
                    catch
                    {
                        // fallthrough
                    }

                    return false;
                }

                // Try common Git Bash path
                var gitBashPath = "C:\\Program Files\\Git\\git-bash.exe";
                if (File.Exists(gitBashPath))
                {
                    // Prefer the underlying bash.exe located relative to git-bash.exe
                    try
                    {
                        var dir = Path.GetDirectoryName(gitBashPath) ?? string.Empty;
                        var possibleMintty = Path.Combine(dir, "usr", "bin", "mintty.exe");
                        var possibleBash = Path.Combine(dir, "usr", "bin", "bash.exe");

                        if (File.Exists(possibleMintty))
                        {
                            var cmdEscaped = (command ?? string.Empty).Replace("\"", "\\\"");
                            var args = string.IsNullOrWhiteSpace(cmdEscaped)
                                ? "-e \"/usr/bin/bash\" -i -l"
                                : $"-e \"/usr/bin/bash\" -lc \"{cmdEscaped}; exec bash\"";

                            var psi = new ProcessStartInfo
                            {
                                FileName = possibleMintty,
                                Arguments = args,
                                UseShellExecute = false
                            };
                            return await StartProcessAsync(psi);
                        }

                        if (File.Exists(possibleBash))
                        {
                            var cmdEscaped = (command ?? string.Empty).Replace("\"", "\\\"");
                            var args = string.IsNullOrWhiteSpace(cmdEscaped)
                                ? "-i"
                                : $"-lc \"{cmdEscaped}; exec bash\"";

                            var psi = new ProcessStartInfo
                            {
                                FileName = possibleBash,
                                Arguments = args,
                                UseShellExecute = true
                            };
                            return await StartProcessAsync(psi);
                        }
                    }
                    catch
                    {
                        // fallthrough to launching git-bash.exe
                    }

                    // Fallback: start git-bash.exe directly
                    var cmdEscapedFallback2 = (command ?? string.Empty).Replace("\"", "\\\"");
                    var argsFallback2 = string.IsNullOrWhiteSpace(cmdEscapedFallback2)
                        ? "--login -i"
                        : $"--login -i -c \"{cmdEscapedFallback2}; exec bash\"";

                    var psi2 = new ProcessStartInfo
                    {
                        FileName = gitBashPath,
                        Arguments = argsFallback2,
                        UseShellExecute = true
                    };
                    return await StartProcessAsync(psi2);
                }

                // Fallback to bash if available
                var bashPath = "C:\\Program Files\\Git\\usr\\bin\\bash.exe";
                if (File.Exists(bashPath))
                {
                    var cmdEscaped = (command ?? string.Empty).Replace("\"", "\\\"");
                    var args = string.IsNullOrWhiteSpace(cmdEscaped)
                        ? "-i"
                        : $"-lc \"{cmdEscaped}; exec bash\"";

                    var psi = new ProcessStartInfo
                    {
                        FileName = bashPath,
                        Arguments = args,
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
