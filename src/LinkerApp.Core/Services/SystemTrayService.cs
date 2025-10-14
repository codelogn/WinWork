using System.Drawing;
using System.Windows.Forms;
using LinkerApp.Core.Interfaces;

namespace LinkerApp.Core.Services;

/// <summary>
/// Service for managing system tray integration
/// </summary>
public class SystemTrayService : ISystemTrayService, IDisposable
{
    private NotifyIcon? _notifyIcon;
    private readonly ISettingsService _settingsService;
    private bool _disposed = false;

    public event EventHandler? ShowMainWindow;
    public event EventHandler? ExitApplication;

    public SystemTrayService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public void Initialize(string applicationName, Icon? icon = null)
    {
        if (_notifyIcon != null)
            return;

        _notifyIcon = new NotifyIcon
        {
            Text = applicationName,
            Icon = icon ?? SystemIcons.Application,
            Visible = false
        };

        // Create context menu
        var contextMenu = new ContextMenuStrip();
        
        var showItem = new ToolStripMenuItem("Show LinkApp")
        {
            Font = new Font(contextMenu.Font, FontStyle.Bold)
        };
        showItem.Click += (s, e) => ShowMainWindow?.Invoke(this, EventArgs.Empty);
        contextMenu.Items.Add(showItem);
        
        contextMenu.Items.Add(new ToolStripSeparator());
        
        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (s, e) => ExitApplication?.Invoke(this, EventArgs.Empty);
        contextMenu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (s, e) => ShowMainWindow?.Invoke(this, EventArgs.Empty);
    }

    public void Show()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = true;
        }
    }

    public void Hide()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
        }
    }

    public void ShowBalloonTip(string title, string text, ToolTipIcon icon = ToolTipIcon.Info, int timeout = 3000)
    {
        _notifyIcon?.ShowBalloonTip(timeout, title, text, icon);
    }

    public void UpdateIcon(Icon icon)
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Icon = icon;
        }
    }

    public void UpdateText(string text)
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Text = text;
        }
    }

    public bool IsVisible => _notifyIcon?.Visible ?? false;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _notifyIcon?.Dispose();
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