using WinWork.Models;

namespace WinWork.Core.Services;

/// <summary>
/// Service interface for application settings management
/// </summary>
public interface ISettingsService
{
    Task<T?> GetSettingAsync<T>(string key) where T : struct;
    Task<string?> GetSettingAsync(string key);
    Task<bool> SetSettingAsync<T>(string key, T value) where T : struct;
    Task<bool> SetSettingAsync(string key, string value);
    Task<IEnumerable<AppSettings>> GetAllSettingsAsync();
    Task<bool> ResetToDefaultsAsync();
    
    // Convenience methods for common settings
    Task<string> GetThemeAsync();
    Task<bool> SetThemeAsync(string theme);
    Task<string> GetGlobalHotkeyAsync();
    Task<bool> SetGlobalHotkeyAsync(string hotkey);
    Task<bool> GetMinimizeToTrayAsync();
    Task<bool> SetMinimizeToTrayAsync(bool minimizeToTray);
    Task<bool> GetStartWithWindowsAsync();
    Task<bool> SetStartWithWindowsAsync(bool startWithWindows);
    Task<bool> GetShowNotificationsAsync();
    Task<bool> SetShowNotificationsAsync(bool showNotifications);
    Task<string> GetBackgroundColorAsync();
    Task<bool> SetBackgroundColorAsync(string color);
    Task<int> GetBackgroundOpacityAsync();
    Task<bool> SetBackgroundOpacityAsync(int opacity);
}
