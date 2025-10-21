using WinWork.Models;
using WinWork.Data.Repositories;

namespace WinWork.Core.Services;

/// <summary>
/// Service implementation for application settings management
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly ISettingsRepository _settingsRepository;
    
    // Default settings
    private readonly Dictionary<string, string> _defaults = new()
    {
        { "Theme", "Dark" },
        { "GlobalHotkey", "Ctrl+Alt+L" },
        { "MinimizeToTray", "true" },
        { "StartWithWindows", "false" },
        { "ShowNotifications", "true" },
        { "AutoBackup", "true" },
        { "BackupInterval", "7" }
    };
    public async Task<bool> GetShowNotificationsAsync()
    {
        var value = await GetSettingAsync<bool>("ShowNotifications");
        return value ?? true;
    }

    public async Task<bool> SetShowNotificationsAsync(bool showNotifications)
    {
        return await SetSettingAsync("ShowNotifications", showNotifications);
    }

    public SettingsService(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public async Task<T?> GetSettingAsync<T>(string key) where T : struct
    {
        var value = await _settingsRepository.GetValueAsync<T>(key);
        
        // If not found, try to get and convert default value
        if (!value.HasValue && _defaults.TryGetValue(key, out var defaultValue))
        {
            // Set the default value in the database and return it
            await _settingsRepository.SetValueAsync(key, defaultValue);
            return await _settingsRepository.GetValueAsync<T>(key);
        }

        return value;
    }

    public async Task<string?> GetSettingAsync(string key)
    {
        var value = await _settingsRepository.GetValueAsync(key);
        
        // If not found, use default and save it
        if (value == null && _defaults.TryGetValue(key, out var defaultValue))
        {
            await _settingsRepository.SetValueAsync(key, defaultValue);
            return defaultValue;
        }

        return value;
    }

    public async Task<bool> SetSettingAsync<T>(string key, T value) where T : struct
    {
        return await _settingsRepository.SetValueAsync(key, value);
    }

    public async Task<bool> SetSettingAsync(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        return await _settingsRepository.SetValueAsync(key, value ?? string.Empty);
    }

    public async Task<IEnumerable<AppSettings>> GetAllSettingsAsync()
    {
        return await _settingsRepository.GetAllAsync();
    }

    public async Task<bool> ResetToDefaultsAsync()
    {
        try
        {
            foreach (var (key, value) in _defaults)
            {
                await _settingsRepository.SetValueAsync(key, value);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    // Convenience methods for common settings
    public async Task<string> GetThemeAsync()
    {
        return await GetSettingAsync("Theme") ?? "Dark";
    }

    public async Task<bool> SetThemeAsync(string theme)
    {
        if (string.IsNullOrWhiteSpace(theme))
            return false;

        var validThemes = new[] { "Light", "Dark", "Auto" };
        if (!validThemes.Contains(theme, StringComparer.OrdinalIgnoreCase))
            return false;

        return await SetSettingAsync("Theme", theme);
    }

    public async Task<string> GetGlobalHotkeyAsync()
    {
        return await GetSettingAsync("GlobalHotkey") ?? "Ctrl+Alt+L";
    }

    public async Task<bool> SetGlobalHotkeyAsync(string hotkey)
    {
        if (string.IsNullOrWhiteSpace(hotkey))
            return false;

        // Basic validation for hotkey format
        if (!IsValidHotkey(hotkey))
            return false;

        return await SetSettingAsync("GlobalHotkey", hotkey);
    }

    public async Task<bool> GetMinimizeToTrayAsync()
    {
        var value = await GetSettingAsync<bool>("MinimizeToTray");
        return value ?? true;
    }

    public async Task<bool> SetMinimizeToTrayAsync(bool minimizeToTray)
    {
        return await SetSettingAsync("MinimizeToTray", minimizeToTray);
    }

    public async Task<bool> GetStartWithWindowsAsync()
    {
        var value = await GetSettingAsync<bool>("StartWithWindows");
        return value ?? false;
    }

    public async Task<bool> SetStartWithWindowsAsync(bool startWithWindows)
    {
        var success = await SetSettingAsync("StartWithWindows", startWithWindows);
        
        if (success)
        {
            // In a real implementation, you would also update Windows registry
            // to actually enable/disable startup with Windows
            await UpdateWindowsStartupAsync(startWithWindows);
        }

        return success;
    }

    private bool IsValidHotkey(string hotkey)
    {
        if (string.IsNullOrWhiteSpace(hotkey))
            return false;

        // Basic validation - should contain at least one modifier key
        var validModifiers = new[] { "Ctrl", "Alt", "Shift", "Win" };
        return validModifiers.Any(modifier => hotkey.Contains(modifier, StringComparison.OrdinalIgnoreCase));
    }

    private async Task UpdateWindowsStartupAsync(bool enable)
    {
        // This would be implemented to actually modify Windows registry
        // for startup behavior. For now, it's a placeholder.
        await Task.Delay(1); // Placeholder
        
        /*
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            if (enable)
            {
                var appPath = Process.GetCurrentProcess().MainModule?.FileName;
                if (appPath != null)
                {
                    key?.SetValue("LinkerApp", appPath);
                }
            }
            else
            {
                key?.DeleteValue("LinkerApp", false);
            }
        }
        catch
        {
            // Handle registry access errors
        }
        */
    }
}
