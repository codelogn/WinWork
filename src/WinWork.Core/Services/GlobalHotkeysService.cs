using System.Runtime.InteropServices;
using System.Windows.Forms;
using WinWork.Core.Interfaces;

namespace WinWork.Core.Services;

/// <summary>
/// Service for managing global hotkeys using Windows API
/// </summary>
public class GlobalHotkeysService : IGlobalHotkeysService, IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly Dictionary<string, int> _registeredHotKeys;
    private readonly IntPtr _windowHandle;
    private int _nextId = 1;
    private bool _disposed = false;

    public event EventHandler<string>? HotKeyPressed;

    public GlobalHotkeysService()
    {
        _registeredHotKeys = new Dictionary<string, int>();
        // For now, we'll use a placeholder window handle
        // In a real implementation, this would be the main window handle
        _windowHandle = IntPtr.Zero;
    }

    public bool RegisterHotKey(string name, Keys key, ModifierKeys modifiers)
    {
        try
        {
            if (_registeredHotKeys.ContainsKey(name))
            {
                UnregisterHotKey(name);
            }

            var id = _nextId++;
            uint mod = ConvertModifiers(modifiers);
            uint vk = (uint)key;

            if (RegisterHotKey(_windowHandle, id, mod, vk))
            {
                _registeredHotKeys[name] = id;
                return true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to register hotkey {name}: {ex.Message}");
        }

        return false;
    }

    public bool UnregisterHotKey(string name)
    {
        try
        {
            if (_registeredHotKeys.TryGetValue(name, out var id))
            {
                if (UnregisterHotKey(_windowHandle, id))
                {
                    _registeredHotKeys.Remove(name);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to unregister hotkey {name}: {ex.Message}");
        }

        return false;
    }

    public void UnregisterAllHotKeys()
    {
        var names = _registeredHotKeys.Keys.ToList();
        foreach (var name in names)
        {
            UnregisterHotKey(name);
        }
    }

    public bool IsHotKeyRegistered(string name)
    {
        return _registeredHotKeys.ContainsKey(name);
    }

    public IEnumerable<string> GetRegisteredHotKeyNames()
    {
        return _registeredHotKeys.Keys.ToList();
    }

    private static uint ConvertModifiers(ModifierKeys modifiers)
    {
        uint mod = 0;
        if (modifiers.HasFlag(ModifierKeys.Alt)) mod |= 0x0001;
        if (modifiers.HasFlag(ModifierKeys.Control)) mod |= 0x0002;
        if (modifiers.HasFlag(ModifierKeys.Shift)) mod |= 0x0004;
        if (modifiers.HasFlag(ModifierKeys.Windows)) mod |= 0x0008;
        return mod;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                UnregisterAllHotKeys();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
