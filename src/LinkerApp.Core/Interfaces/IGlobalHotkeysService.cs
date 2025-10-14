using System.Windows.Forms;

namespace LinkerApp.Core.Interfaces;

/// <summary>
/// Modifier keys for hotkey combinations
/// </summary>
[Flags]
public enum ModifierKeys
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Windows = 8
}

/// <summary>
/// Interface for global hotkeys management service
/// </summary>
public interface IGlobalHotkeysService
{
    /// <summary>
    /// Event fired when a registered hotkey is pressed
    /// </summary>
    event EventHandler<string>? HotKeyPressed;

    /// <summary>
    /// Register a global hotkey
    /// </summary>
    /// <param name="name">Unique name for the hotkey</param>
    /// <param name="key">The key</param>
    /// <param name="modifiers">Key modifiers (Ctrl, Alt, Shift, Win)</param>
    /// <returns>True if registration successful</returns>
    bool RegisterHotKey(string name, Keys key, ModifierKeys modifiers);

    /// <summary>
    /// Unregister a hotkey by name
    /// </summary>
    /// <param name="name">Name of the hotkey to unregister</param>
    /// <returns>True if unregistration successful</returns>
    bool UnregisterHotKey(string name);

    /// <summary>
    /// Unregister all hotkeys
    /// </summary>
    void UnregisterAllHotKeys();

    /// <summary>
    /// Check if a hotkey is registered
    /// </summary>
    /// <param name="name">Name of the hotkey</param>
    /// <returns>True if registered</returns>
    bool IsHotKeyRegistered(string name);

    /// <summary>
    /// Get all registered hotkey names
    /// </summary>
    /// <returns>Collection of hotkey names</returns>
    IEnumerable<string> GetRegisteredHotKeyNames();
}