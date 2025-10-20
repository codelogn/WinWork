using System.Drawing;
using System.Windows.Forms;

namespace WinWork.Core.Interfaces;

/// <summary>
/// Interface for system tray integration service
/// </summary>
public interface ISystemTrayService
{
    /// <summary>
    /// Event fired when user requests to show main window
    /// </summary>
    event EventHandler? ShowMainWindow;
    
    /// <summary>
    /// Event fired when user requests to exit application
    /// </summary>
    event EventHandler? ExitApplication;

    /// <summary>
    /// Initialize the system tray icon
    /// </summary>
    /// <param name="applicationName">Name to display in tooltip</param>
    /// <param name="icon">Icon to display (optional)</param>
    void Initialize(string applicationName, Icon? icon = null);

    /// <summary>
    /// Show the system tray icon
    /// </summary>
    void Show();

    /// <summary>
    /// Hide the system tray icon
    /// </summary>
    void Hide();

    /// <summary>
    /// Show a balloon tip notification
    /// </summary>
    void ShowBalloonTip(string title, string text, ToolTipIcon icon = ToolTipIcon.Info, int timeout = 3000);

    /// <summary>
    /// Update the tray icon
    /// </summary>
    void UpdateIcon(Icon icon);

    /// <summary>
    /// Update the tooltip text
    /// </summary>
    void UpdateText(string text);

    /// <summary>
    /// Gets whether the tray icon is visible
    /// </summary>
    bool IsVisible { get; }
}
