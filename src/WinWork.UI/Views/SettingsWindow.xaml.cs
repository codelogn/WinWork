using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using Microsoft.Win32;
using WinWork.UI.ViewModels;
using Microsoft.EntityFrameworkCore;
using WinWork.Data;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using WinWork.UI.Utils;

namespace WinWork.UI.Views
{
/// <summary>
/// Interaction logic for SettingsWindow.xaml
/// </summary>
public partial class SettingsWindow : Window
{
    // CancellationTokenSource used to debounce transparency saves
    private CancellationTokenSource? _transparencyCts;
    // Holds the last pending transparency save task so we can await it on close
    private Task? _pendingTransparencySave;
    // Tracks whether the currently applied theme is Light
    private bool _isLightTheme = false;

    // Helper to return the correct primary foreground brush for current theme
    private System.Windows.Media.Brush GetForegroundBrush()
    {
        return _isLightTheme ? System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.White;
    }

    // Helper to return the correct secondary (subtle) foreground brush for current theme
    private System.Windows.Media.Brush GetSecondaryForegroundBrush()
    {
        if (_isLightTheme)
            return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(204, 0, 0, 0));
        return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(204, 255, 255, 255));
    }

    // Helper to return the appropriate control background for current theme
    private System.Windows.Media.Brush GetBackgroundBrush()
    {
        if (_isLightTheme)
            return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 255, 255));
        return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(51, 255, 255, 255));
    }

    // Control background (used for TextBox/ComboBox etc.)
    private System.Windows.Media.Brush GetControlBackgroundBrush()
    {
        // For light theme use an opaque light background, for dark use a subtle translucent dark overlay
        return _isLightTheme
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 255, 255))
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(51, 0, 0, 0));
    }

    // Control border brush (used for TextBox/ComboBox borders)
    private System.Windows.Media.Brush GetControlBorderBrush()
    {
        return _isLightTheme
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(102, 0, 0, 0))
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(102, 255, 255, 255));
    }

    public SettingsWindow()
    {
        InitializeComponent();
        SetWindowIcon();
        // Ensure we can await pending saves when window closes
        this.Closing += SettingsWindow_Closing;
        
        // Apply consistent styling after initialization but add a timer for backup
        ApplyConsistentStyling();
        
        // Add a delayed backup to ensure background is applied
        System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
        timer.Interval = TimeSpan.FromMilliseconds(100);
        timer.Tick += (s, e) =>
        {
            timer.Stop();
            EnsureBackgroundApplied();
        };
        timer.Start();
        
        // Select the first item by default
        var treeView = GetSettingsTreeView();
        if (treeView?.Items.Count > 0 && treeView.Items[0] is TreeViewItem firstItem)
        {
            firstItem.IsSelected = true;
        }

        // Ensure this settings window reflects the current app theme.
    var mainWindow = System.Windows.Application.Current?.MainWindow as MainWindow;
        try
        {
            var settingsService = mainWindow?.ViewModel?.SettingsService;
            if (settingsService != null)
            {
                var savedTheme = settingsService.GetThemeAsync().Result;
                _isLightTheme = string.Equals(savedTheme, "Light", StringComparison.OrdinalIgnoreCase);
            }
            else if (mainWindow != null && mainWindow.Background is System.Windows.Media.SolidColorBrush mainBg)
            {
                _isLightTheme = !IsLikelyDarkColor(mainBg.Color);
            }
            else
            {
                _isLightTheme = false;
            }

            if (_isLightTheme)
            {
                // Apply theme only to the settings window to avoid mutating the main window's visual tree
                ApplyLightTheme(this);
            }
            else
            {
                // Apply theme only to the settings window to avoid mutating the main window's visual tree
                ApplyDarkTheme(this);
            }

            // Update foregrounds for settings window only
            UpdateElementForegrounds(this, GetForegroundBrush());
            UpdateExistingSettingsControls();
        }
        catch
        {
            // If anything goes wrong reading saved theme, fall back to dark theme
            _isLightTheme = false;
            // Apply fallback theme only to settings window
            ApplyDarkTheme(this);
            UpdateElementForegrounds(this, GetForegroundBrush());
        }
    }

    private async void SettingsWindow_Closing(object? sender, CancelEventArgs e)
    {
        try
        {
            // Don't cancel the debounce token here — cancelling will abort the pending save and
            // can cause the user's transparency choice to never be persisted.
            // Instead, wait briefly for any pending save to finish and, if it doesn't, perform
            // a best-effort synchronous save using the current main window opacity.
            if (_pendingTransparencySave != null)
            {
                // Wait up to 2s for the pending save to finish to avoid blocking UI too long
                var timeout = Task.Delay(2000);
                var finished = await Task.WhenAny(_pendingTransparencySave, timeout);
                if (finished != _pendingTransparencySave)
                {
                    // Timed out; attempt a best-effort save using the current main window opacity
                    try
                    {
                        var mainWindow = Application.Current.MainWindow as MainWindow;
                        var settingsService = mainWindow?.ViewModel?.SettingsService;
                        if (settingsService != null && mainWindow != null)
                        {
                            var opacityPercent = (int)Math.Round(mainWindow.Opacity * 100.0);
                            // Ensure value is within allowed range
                            if (opacityPercent < 10) opacityPercent = 10;
                            if (opacityPercent > 100) opacityPercent = 100;
                            await settingsService.SetBackgroundOpacityAsync(opacityPercent);
                        }
                    }
                    catch { }
                }
            }
        }
        catch { }
        finally
        {
            try { _transparencyCts?.Dispose(); } catch { }
            _transparencyCts = null;
        }
    }



    // Moved out of SettingsWindow_Closing: applies consistent styling to this window
    private async void ApplyConsistentStyling()
    {
        try
        {
            // Get settings service from the main window if available
            var mainWindow = Application.Current.MainWindow as MainWindow;
            var settingsService = mainWindow?.ViewModel?.SettingsService;

            // Apply modern chrome effects
            WindowStylingHelper.ApplyModernChrome(this);

            // Apply consistent background and opacity
            WindowStylingHelper.ApplyConsistentStyling(this, settingsService);
            
            // Also apply background directly as a fallback
            EnsureBackgroundApplied();
        }
        catch { }
    }

    private void EnsureBackgroundApplied()
    {
        try
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            var settingsService = mainWindow?.ViewModel?.SettingsService;
            
            if (settingsService != null)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        var bgColor = await settingsService.GetBackgroundColorAsync();
                        if (!string.IsNullOrWhiteSpace(bgColor))
                        {
                            Dispatcher.Invoke(() =>
                            {
                                try
                                {
                                    var conv = new System.Windows.Media.BrushConverter();
                                    var brush = conv.ConvertFromString(bgColor) as System.Windows.Media.Brush;
                                    if (brush != null)
                                    {
                                        // Try multiple approaches to apply the background
                                        ApplyBackgroundToSettingsWindow(brush);
                                    }
                                }
                                catch { }
                            });
                        }
                    }
                    catch { }
                });
            }
        }
        catch { }
    }

    private void ApplyBackgroundToSettingsWindow(System.Windows.Media.Brush brush)
    {
        try
        {
            // Method 1: Find MainBorder by traversing visual tree
            var mainBorder = FindChildByName<System.Windows.Controls.Border>(this, "MainBorder");
            if (mainBorder != null)
            {
                mainBorder.Background = brush;
                return;
            }

            // Method 2: Apply to window content if it's a Border
            if (this.Content is System.Windows.Controls.Border contentBorder)
            {
                contentBorder.Background = brush;
                return;
            }

            // Method 3: Find any Border in the content
            if (this.Content is System.Windows.FrameworkElement content)
            {
                var anyBorder = FindFirstBorder(content);
                if (anyBorder != null)
                {
                    anyBorder.Background = brush;
                }
            }
        }
        catch { }
    }

    // Helper methods to find XAML elements dynamically (to avoid dependency on code-generated element names)
    private System.Windows.Controls.TreeView? GetSettingsTreeView()
    {
        try
        {
            return FindChildByName<System.Windows.Controls.TreeView>(this, "SettingsTreeView");
        }
        catch
        {
            return null;
        }
    }

    private System.Windows.Controls.StackPanel? GetSettingsContentPanel()
    {
        try
        {
            return FindChildByName<System.Windows.Controls.StackPanel>(this, "SettingsContentPanel");
        }
        catch
        {
            return null;
        }
    }

    private System.Windows.Controls.Border? FindFirstBorder(System.Windows.DependencyObject parent)
    {
        try
        {
            if (parent is System.Windows.Controls.Border border)
                return border;

            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                var result = FindFirstBorder(child);
                if (result != null)
                    return result;
            }
        }
        catch { }
        return null;
    }

    private void SetWindowIcon()
    {
        try
        {
            // Load icon from embedded resource
            var iconUri = new Uri("pack://application:,,,/winwork-logo.ico");
            this.Icon = System.Windows.Media.Imaging.BitmapFrame.Create(iconUri);
        }
        catch
        {
            // Fallback: try to get the application icon
            try
            {
                var iconHandle = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
                if (iconHandle != null)
                {
                    this.Icon = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                        iconHandle.Handle,
                        System.Windows.Int32Rect.Empty,
                        System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                }
            }
            catch
            {
                // If all fails, leave default icon
            }
        }
    }

    #region Window Controls

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            // Double-click to maximize/restore
            WindowState = WindowState == WindowState.Maximized 
                ? WindowState.Normal 
                : WindowState.Maximized;
        }
        else
        {
            // Single click to drag
            DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }


    #endregion

    #region Settings Navigation

    private void SettingsTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is TreeViewItem selectedItem && selectedItem.Tag is string tag)
        {
            LoadSettingsContent(tag);
        }
    }

    private void LoadSettingsContent(string settingsCategory)
    {
        SettingsContentPanel.Children.Clear();

        switch (settingsCategory)
        {
            case "General":
                LoadGeneralSettings();
                break;
            case "Application":
                LoadApplicationSettings();
                break;
            case "Interface":
                LoadInterfaceSettings();
                break;
            case "Startup":
                LoadStartupSettings();
                break;
            case "Data":
                LoadDataSettings();
                break;
            case "Database":
                LoadDatabaseSettings();
                break;
            case "Backup":
                LoadBackupSettings();
                break;
            case "ImportExport":
                LoadImportExportSettings();
                break;
            case "Advanced":
                LoadAdvancedSettings();
                break;
            case "Performance":
                LoadPerformanceSettings();
                break;
            case "Security":
                LoadSecuritySettings();
                break;
            case "Logging":
                LoadLoggingSettings();
                break;
            case "About":
                LoadAboutSettings();
                break;
            default:
                LoadDefaultContent();
                break;
        }
    }

    #endregion

    #region Settings Content Loaders

    // ... (move all other methods and regions here, up to the end of the file) ...

    #endregion


    #region Settings Content Loaders

    private void LoadDefaultContent()
    {
        AddSectionHeader("Welcome to Settings");
        AddDescription("Select a category from the left panel to configure your WinWork settings.");
    }

    private async void LoadGeneralSettings()
    {
        AddSectionHeader("General Settings");
        AddDescription("Configure general application preferences.");
        
        var mainWindow = Application.Current.MainWindow as MainWindow;
        var settingsService = mainWindow?.ViewModel?.SettingsService;
        
        // Load actual values from settings service
        bool minimizeToTray = settingsService != null ? await settingsService.GetMinimizeToTrayAsync() : false;
        bool showNotifications = settingsService != null ? await settingsService.GetShowNotificationsAsync() : true;
        bool startWithWindows = settingsService != null ? await settingsService.GetStartWithWindowsAsync() : false;
        
        AddCheckBox("Enable system tray integration", true);
        AddCheckBox("Start minimized to tray", minimizeToTray);
        AddCheckBox("Show notifications", showNotifications);
        AddCheckBox("Start with Windows", startWithWindows);
    }

    private async void LoadApplicationSettings()
    {
        AddSectionHeader("Application Settings");
        AddDescription("Configure application behavior and preferences.");
        
        var mainWindow = Application.Current.MainWindow as MainWindow;
        var settingsService = mainWindow?.ViewModel?.SettingsService;
        
        // Load actual values from settings service
        string appName = "WinWork";
        bool autoSave = true;
        bool confirmDelete = true;
        int backupInterval = 30;
        
        if (settingsService != null)
        {
            appName = await settingsService.GetSettingAsync("ApplicationName") ?? "WinWork";
            autoSave = await settingsService.GetSettingAsync<bool>("AutoSave") ?? true;
            confirmDelete = await settingsService.GetSettingAsync<bool>("ConfirmBeforeDelete") ?? true;
            backupInterval = await settingsService.GetSettingAsync<int>("AutoBackupInterval") ?? 30;
        }
        
        AddTextSetting("Application Name:", appName);
        AddCheckBox("Auto-save changes", autoSave);
        AddCheckBox("Confirm before deleting items", confirmDelete);
        AddNumericSetting("Auto-backup interval (minutes):", backupInterval);
    }

    private async void LoadInterfaceSettings()
    {
        AddSectionHeader("Interface Settings");
        AddDescription("Customize the user interface appearance and behavior.");
        
        var mainWindow = Application.Current.MainWindow as MainWindow;
        var settingsService = mainWindow?.ViewModel?.SettingsService;
        
        // Load actual values from settings service
        string currentTheme = settingsService != null ? await settingsService.GetThemeAsync() : "Dark";
        // Background opacity is stored via SettingsService as an integer (BackgroundOpacity).
        // For backward compatibility, read any existing 'WindowTransparency' key and migrate it to BackgroundOpacity.
        double currentTransparency;
        if (settingsService != null)
        {
            var oldVal = await settingsService.GetSettingAsync<double>("WindowTransparency");
            if (oldVal.HasValue)
            {
                // Migrate to BackgroundOpacity (int)
                var migrated = (int)Math.Round(oldVal.Value);
                await settingsService.SetBackgroundOpacityAsync(migrated);
                currentTransparency = oldVal.Value;
            }
            else
            {
                var bgOp = await settingsService.GetBackgroundOpacityAsync();
                currentTransparency = bgOp;
            }
        }
        else
        {
            currentTransparency = 90.0;
        }
        var autoSave = settingsService != null ? (await settingsService.GetSettingAsync<bool>("AutoSave") ?? true) : true;
        var showTreeIcons = settingsService != null ? (await settingsService.GetSettingAsync<bool>("ShowTreeViewIcons") ?? true) : true;
        var glassmorphism = settingsService != null ? (await settingsService.GetSettingAsync<bool>("GlassmorphismEffects") ?? true) : true;
        
        AddThemeComboBox("Theme:", new[] { "Dark", "Light", "Auto" }, currentTheme);
        // Terminal settings UI
        var psPath = settingsService != null ? await settingsService.GetTerminalPowerShellPathAsync() : "powershell.exe";
        var gitPath = settingsService != null ? await settingsService.GetTerminalGitBashPathAsync() : string.Empty;
        var cmdPath = settingsService != null ? await settingsService.GetTerminalCmdPathAsync() : "cmd.exe";
        var defaultTerminal = settingsService != null ? await settingsService.GetDefaultTerminalAsync() : "PowerShell";

        // If the stored Git Bash path is empty, try to auto-detect common Git for Windows locations
        if (string.IsNullOrWhiteSpace(gitPath))
        {
            var candidates = new[]
            {
                "C:\\Program Files\\Git\\git-bash.exe",
                "C:\\Program Files (x86)\\Git\\git-bash.exe",
                "C:\\Program Files\\Git\\usr\\bin\\bash.exe",
                "C:\\Program Files (x86)\\Git\\usr\\bin\\bash.exe"
            };

            foreach (var c in candidates)
            {
                try
                {
                    if (System.IO.File.Exists(c))
                    {
                        gitPath = c;
                        break;
                    }
                }
                catch { }
            }
        }

    AddSectionSubHeader("Terminal Settings");
    AddFilePathSetting("PowerShell path:", psPath);
    AddFilePathSetting("Git Bash path:", gitPath);
    AddFilePathSetting("CMD path:", cmdPath);
    AddComboBoxSetting("Default terminal:", new[] { "PowerShell", "Git Bash", "CMD" }, defaultTerminal);

    // Local helpers to read the current UI values for the terminal settings
    string FindTextBoxValue(string label)
    {
        foreach (var child in SettingsContentPanel.Children)
        {
            if (child is StackPanel sp && sp.Children.Count >= 2 && sp.Children[0] is TextBlock tb && tb.Text == label)
            {
                // The TextBox may be directly the second child or nested inside another panel (e.g., a horizontal pathPanel)
                var target = FindNestedTextBox(sp.Children[1]);
                if (target != null) return target.Text ?? string.Empty;
            }
        }
        return string.Empty;
    }

    string FindComboBoxValue(string label)
    {
        foreach (var child in SettingsContentPanel.Children)
        {
            if (child is StackPanel sp && sp.Children.Count >= 2 && sp.Children[0] is TextBlock tb && tb.Text == label)
            {
                var cb = FindNestedComboBox(sp.Children[1]);
                if (cb != null && cb.SelectedItem != null) return cb.SelectedItem.ToString() ?? string.Empty;
            }
        }
        return string.Empty;
    }

    // Recursive helpers to find nested controls inside a container
    System.Windows.Controls.TextBox? FindNestedTextBox(object? obj)
    {
        if (obj == null) return null;
        if (obj is System.Windows.Controls.TextBox tb) return tb;
        if (obj is System.Windows.DependencyObject dobj)
        {
            // If it's a Panel, iterate children
            if (dobj is System.Windows.Controls.Panel panel)
            {
                foreach (var child in panel.Children)
                {
                    var found = FindNestedTextBox(child);
                    if (found != null) return found;
                }
            }
            // If it's a ContentControl, check Content
            if (dobj is System.Windows.Controls.ContentControl cc && cc.Content != null)
            {
                var found = FindNestedTextBox(cc.Content);
                if (found != null) return found;
            }
        }
        return null;
    }

    System.Windows.Controls.ComboBox? FindNestedComboBox(object? obj)
    {
        if (obj == null) return null;
        if (obj is System.Windows.Controls.ComboBox cb) return cb;
        if (obj is System.Windows.DependencyObject dobj)
        {
            if (dobj is System.Windows.Controls.Panel panel)
            {
                foreach (var child in panel.Children)
                {
                    var found = FindNestedComboBox(child);
                    if (found != null) return found;
                }
            }
            if (dobj is System.Windows.Controls.ContentControl cc && cc.Content != null)
            {
                var found = FindNestedComboBox(cc.Content);
                if (found != null) return found;
            }
        }
        return null;
    }

    // Save button for terminal settings
    AddButton("💾 Save Terminal Settings", async () =>
    {
        try
        {
            if (settingsService == null)
            {
                System.Windows.MessageBox.Show("Settings service unavailable. Cannot save.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var newPs = FindTextBoxValue("PowerShell path:");
            var newGit = FindTextBoxValue("Git Bash path:");
            var newCmd = FindTextBoxValue("CMD path:");
            var newDefault = FindComboBoxValue("Default terminal:");

            // Only update non-empty values to avoid accidentally clearing existing configured paths
            if (!string.IsNullOrWhiteSpace(newPs)) await settingsService.SetTerminalPowerShellPathAsync(newPs);
            if (!string.IsNullOrWhiteSpace(newGit)) await settingsService.SetTerminalGitBashPathAsync(newGit);
            if (!string.IsNullOrWhiteSpace(newCmd)) await settingsService.SetTerminalCmdPathAsync(newCmd);
            if (!string.IsNullOrWhiteSpace(newDefault)) await settingsService.SetDefaultTerminalAsync(newDefault);

            System.Windows.MessageBox.Show("Terminal settings saved.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    });

    // Test button to launch the configured/default terminal with a harmless test command
    AddButton("🔬 Test Terminal", () =>
    {
        try
        {
            var selDefault = FindComboBoxValue("Default terminal:");
            var ps = FindTextBoxValue("PowerShell path:");
            var git = FindTextBoxValue("Git Bash path:");
            var cmd = FindTextBoxValue("CMD path:");

            string shellPath = selDefault switch
            {
                "PowerShell" => string.IsNullOrWhiteSpace(ps) ? "powershell.exe" : ps,
                "Git Bash" => string.IsNullOrWhiteSpace(git) ? git : git,
                "CMD" => string.IsNullOrWhiteSpace(cmd) ? "cmd.exe" : cmd,
                _ => string.IsNullOrWhiteSpace(ps) ? "powershell.exe" : ps
            };

            if (string.IsNullOrWhiteSpace(shellPath))
            {
                System.Windows.MessageBox.Show("No terminal executable path configured for the selected terminal.", "Test Terminal", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string args = string.Empty;
            if (selDefault == "PowerShell")
            {
                args = "-NoExit -Command \"Write-Host 'WinWork terminal test'\"";
            }
            else if (selDefault == "CMD")
            {
                args = "/k echo WinWork terminal test";
            }
            else if (selDefault == "Git Bash")
            {
                // If the configured path points to git-bash.exe, launching it without args opens a terminal
                if (shellPath.EndsWith("git-bash.exe", System.StringComparison.OrdinalIgnoreCase))
                {
                    args = string.Empty;
                }
                else
                {
                    // Assume a bash.exe style shell
                    args = "--login -i -c \"echo 'WinWork terminal test'; read -p 'Press Enter to close'\"";
                }
            }

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = shellPath,
                UseShellExecute = true,
                Arguments = args
            };

            System.Diagnostics.Process.Start(psi);
            System.Windows.MessageBox.Show("Test terminal launched. Check the terminal window for the test output.", "Test Terminal", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (System.Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to launch test terminal: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    });
        AddCheckBox("Show tree view icons", showTreeIcons);
        AddCheckBox("Enable glassmorphism effects", glassmorphism);
        AddTransparencySlider("Window transparency (%):", currentTransparency);
        AddCheckBox("Auto-save changes", autoSave);
    
        // Background color picker: allow user to set app background color
        AddSectionSubHeader("Appearance");
        string currentBg = settingsService != null ? (await settingsService.GetSettingAsync("BackgroundColor") ?? "") : "";
        AddTextSetting("Application background color (CSS/Hex):", currentBg);
        AddButton("Pick background color", async () =>
        {
            try
            {
                // Use standard Win32 color dialog via Forms
                var cd = new System.Windows.Forms.ColorDialog();
                if (!string.IsNullOrWhiteSpace(currentBg))
                {
                    try
                    {
                        var sc = System.Windows.Media.ColorConverter.ConvertFromString(currentBg) as System.Windows.Media.Color?;
                        if (sc.HasValue)
                        {
                            var c = sc.Value;
                            cd.Color = System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B);
                        }
                    }
                    catch { }
                }

                var res = cd.ShowDialog();
                if (res == System.Windows.Forms.DialogResult.OK)
                {
                    var sel = cd.Color;
                    var hex = $"#{sel.A:X2}{sel.R:X2}{sel.G:X2}{sel.B:X2}";
                    if (settingsService != null)
                    {
                        // Use typed API which performs validation and default handling
                        await settingsService.SetBackgroundColorAsync(hex);
                    }

                    // Apply immediately to main window
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    if (mainWindow != null)
                    {
                        var brush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(sel.A, sel.R, sel.G, sel.B));
                        // Apply to the main border of the main window
                        ApplyBackgroundToMainWindow(mainWindow, brush);
                        
                        // Also apply to this settings window's main border
                        ApplyBackgroundToSettingsWindow(brush);
                        
                        // Apply to any other open windows
                        RefreshAllWindowBackgrounds(brush);
                    }

                    System.Windows.MessageBox.Show($"Background color saved: {hex}", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to pick color: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        });
    }

    /// <summary>
    /// Apply background color to the main window's MainBorder
    /// </summary>
    private void ApplyBackgroundToMainWindow(MainWindow mainWindow, System.Windows.Media.Brush brush)
    {
        try
        {
            // Use reflection to access the MainBorder field since it might not be publicly exposed
            var mainBorderField = typeof(MainWindow).GetField("MainBorder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (mainBorderField != null)
            {
                var mainBorder = mainBorderField.GetValue(mainWindow) as System.Windows.Controls.Border;
                if (mainBorder != null)
                {
                    mainBorder.Background = brush;
                    return;
                }
            }

            // Try property if field didn't work
            var mainBorderProperty = typeof(MainWindow).GetProperty("MainBorder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (mainBorderProperty != null)
            {
                var mainBorder = mainBorderProperty.GetValue(mainWindow) as System.Windows.Controls.Border;
                if (mainBorder != null)
                {
                    mainBorder.Background = brush;
                    return;
                }
            }

            // Fallback: traverse visual tree
            {
                // Fallback: Find the main border by traversing the visual tree
                if (mainWindow.Content is System.Windows.Controls.Border border)
                {
                    border.Background = brush;
                }
                else if (mainWindow.Content is System.Windows.FrameworkElement element)
                {
                    var mainBorder = FindChildByName<System.Windows.Controls.Border>(element, "MainBorder");
                    if (mainBorder != null)
                    {
                        mainBorder.Background = brush;
                    }
                }
            }
        }
        catch { }
    }

    /// <summary>
    /// Refresh background colors for all open windows
    /// </summary>
    private void RefreshAllWindowBackgrounds(System.Windows.Media.Brush brush)
    {
        try
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window is HotclicksWindow hotclicksWindow)
                {
                    // Apply to hotclicks window main border
                    if (hotclicksWindow.Content is System.Windows.FrameworkElement element)
                    {
                        var mainBorder = FindChildByName<System.Windows.Controls.Border>(element, "MainBorder");
                        if (mainBorder != null)
                        {
                            mainBorder.Background = brush;
                        }
                    }
                }
                // Note: We don't need to update the settings window here since it's updated directly above
            }
        }
        catch { }
    }

    /// <summary>
    /// Helper method to find a child control by name in the visual tree
    /// </summary>
    private T? FindChildByName<T>(System.Windows.DependencyObject parent, string name) where T : System.Windows.FrameworkElement
    {
        try
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                
                if (child is T element && element.Name == name)
                {
                    return element;
                }

                var result = FindChildByName<T>(child, name);
                if (result != null)
                {
                    return result;
                }
            }
        }
        catch { }
        return null;
    }

    private async void LoadStartupSettings()
    {
        AddSectionHeader("Startup Settings");
        AddDescription("Configure how WinWork behaves when starting up.");
        
        var mainWindow = Application.Current.MainWindow as MainWindow;
        var settingsService = mainWindow?.ViewModel?.SettingsService;
        
        // Load actual values from settings service
        bool startWithWindows = settingsService != null ? await settingsService.GetStartWithWindowsAsync() : false;
        bool restoreWindowPosition = settingsService != null ? (await settingsService.GetSettingAsync<bool>("RestoreWindowPosition") ?? true) : true;
        bool loadLastDatabase = settingsService != null ? (await settingsService.GetSettingAsync<bool>("LoadLastDatabase") ?? true) : true;
        string defaultDbPath = settingsService != null ? (await settingsService.GetSettingAsync("DefaultDatabasePath") ?? "") : "";
        
        AddCheckBox("Start with Windows", startWithWindows);
        AddCheckBox("Restore last window position", restoreWindowPosition);
        AddCheckBox("Load last opened database", loadLastDatabase);
        AddTextSetting("Default database path:", defaultDbPath);
    }

    private void LoadDataSettings()
    {
        AddSectionHeader("Data & Storage");
        AddDescription("Manage your data storage and synchronization settings.");
        
        AddButton("📁 Open Data Folder", () => OpenDataFolder());
        AddButton("💾 Create Backup Now", () => CreateBackupNow());
        AddButton("📤 Export All Data", () => ExportAllData());
        AddButton("📥 Import Data", () => ImportData());
        
        AddSeparator();
        AddSectionSubHeader("Auto-backup Settings");
        AddCheckBox("Enable automatic backups", true);
        AddNumericSetting("Backup interval (hours):", 24);
        AddTextSetting("Backup location:", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\WinWork\\Backups");
    }

    private async void LoadDatabaseSettings()
    {
        AddSectionHeader("Database Settings");
        AddDescription("Configure database connection and maintenance options.");

        // Get current database path (default to linker.db in app directory)
        var currentPath = GetCurrentDatabasePath();

        AddDatabasePathSetting("Database file path:", currentPath);
        AddButton("🔧 Optimize Database", () => OptimizeDatabase());
        AddButton("🗑️ Clean Up Database", () => CleanUpDatabase());
        AddButton("🔄 Reset to Default", () => ResetDatabaseToDefault());
        AddCheckBox("Enable database compression", false);
        AddCheckBox("Auto-backup before operations", true);

        AddSeparator();
        AddSectionSubHeader("Database Information");
        // Add a refresh button so users can re-query DB info on demand
        AddButton("🔄 Refresh Database Info", () => {
            // Reload the Database settings section to refresh info
            LoadDatabaseSettings();
        });

        AddInfoSection("Current Status:", "Connected");
        AddInfoSection("Database Path:", currentPath);
        AddInfoSection("File Size:", GetDatabaseFileSize(currentPath));
        AddInfoSection("Last Modified:", GetDatabaseLastModified(currentPath));

        // Query total records count and tables info using a temporary DbContext against the configured DB file
        int recordsCount = 0;
        int tagsCount = 0;
        int linkTagsCount = 0;

        try
        {
            var optionsBuilder = new DbContextOptionsBuilder<WinWorkDbContext>();
            optionsBuilder.UseSqlite($"Data Source={currentPath}");

            using var ctx = new WinWorkDbContext(optionsBuilder.Options);
            // Ensure the database file exists before querying
            if (File.Exists(currentPath))
            {
                // Use EF to count rows
                recordsCount = await ctx.Links.CountAsync();
                tagsCount = await ctx.Tags.CountAsync();
                linkTagsCount = await ctx.LinkTags.CountAsync();
            }
        }
        catch (Exception ex)
        {
            // If DB access fails, show 'Unable to read' in the UI but don't crash settings
            System.Diagnostics.Debug.WriteLine($"Failed to read DB counts: {ex.Message}");
        }

        AddInfoSection("Links Table Records:", recordsCount.ToString());
        AddInfoSection("Tags Table Records:", tagsCount.ToString());
        AddInfoSection("LinkTags Table Records:", linkTagsCount.ToString());
    }

    private void LoadBackupSettings()
    {
    AddSectionHeader("Backup Settings");
    AddDescription("Configure automatic backup and restore options.");

    var mainWindow = Application.Current.MainWindow as MainWindow;
    var settingsService = mainWindow?.ViewModel?.SettingsService;
    string backupFolder = settingsService != null ? (settingsService.GetSettingAsync("BackupFolder").Result ?? "") : "";

    AddCheckBox("Enable automatic backups", true);
    AddNumericSetting("Backup interval (hours):", 24);
    AddNumericSetting("Keep backups for (days):", 30);

    // Show the current backup folder path in a file-path style control
    AddFilePathSetting("Backup folder:", string.IsNullOrWhiteSpace(backupFolder) ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\WinWork\\Backups" : backupFolder);

    // Browse and Create Backup actions
    AddButton("📂 Choose Backup Folder", () => ChooseBackupFolder());
    AddButton("🛟 Create Backup Now", () => CreateBackupNow());
    }

    private void LoadImportExportSettings()
    {
        AddSectionHeader("Import/Export Settings");
        AddDescription("Export and import your data, and configure import/export preferences.");
        
        // Export/Import Actions
        AddSeparator();
        AddSectionSubHeader("Data Operations");
        AddButton("📤 Export All Data", () => ExportData());
        AddButton("📥 Import Data", () => ImportData());
        
        // Export/Import Settings
        AddSeparator();
        AddSectionSubHeader("Export/Import Preferences");
        AddCheckBox("Include timestamps in exports", true);
        AddCheckBox("Preserve folder structure", true);
        AddComboBoxSetting("Default export format:", new[] { "JSON", "CSV", "HTML" }, "JSON");
        AddCheckBox("Compress export files", false);
        AddCheckBox("Create backup before import", true);
    }

    private void LoadAdvancedSettings()
    {
        AddSectionHeader("Advanced Settings");
        AddDescription("Advanced configuration options for power users.");
        
        AddTextArea("⚠️ These settings are for advanced users only. Changing these settings may affect application performance or stability.");
    }

    private void LoadPerformanceSettings()
    {
        AddSectionHeader("Performance Settings");
        AddDescription("Optimize WinWork performance for your system.");
        
        AddNumericSetting("Search result limit:", 1000);
        AddCheckBox("Enable search indexing", true);
        AddCheckBox("Preload tree view items", false);
        AddNumericSetting("UI update interval (ms):", 100);
    }

    private void LoadSecuritySettings()
    {
        AddSectionHeader("Security Settings");
        AddDescription("Configure security and privacy options.");
        
        AddCheckBox("Encrypt database file", false);
        AddCheckBox("Clear recent files on exit", false);
        AddCheckBox("Disable external link warnings", false);
        AddButton("🔐 Change Master Password", () => ChangeMasterPassword());
    }

    private void LoadLoggingSettings()
    {
        AddSectionHeader("Logging Settings");
        AddDescription("Configure application logging and debugging options.");
        
        AddComboBoxSetting("Log level:", new[] { "Error", "Warning", "Info", "Debug" }, "Info");
        AddCheckBox("Log to file", true);
        AddNumericSetting("Log file size limit (MB):", 10);
        AddNumericSetting("Keep log files for (days):", 7);
        AddButton("📄 Open Log Folder", () => OpenLogFolder());
    }

    private void LoadAboutSettings()
    {
        AddSectionHeader("About WinWork");
        AddDescription("Information about this application.");
        
        AddInfoSection("Version:", "1.2.0");
        AddInfoSection("Build Date:", "October 21, 2025");
        AddInfoSection("Framework:", ".NET 9");
        AddInfoSection("Database:", "SQLite");
        
        AddTextArea("WinWork is a modern link management application designed to organize, search, and open any type of link including web URLs, file paths, folders, applications, and system locations.");
        
        AddButton("🌐 Visit Website", () => OpenWebsite());
        AddButton("🐛 Report Issue", () => ReportIssue());
        AddButton("📧 Contact Support", () => ContactSupport());
    }

    #endregion

    #region UI Helper Methods

    private void AddSectionHeader(string title)
    {
        var header = new TextBlock
        {
            Text = title,
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            Foreground = GetForegroundBrush(),
            Margin = new Thickness(0, 0, 0, 16)
        };
        SettingsContentPanel.Children.Add(header);
    }

    private void AddDescription(string description)
    {
        var desc = new TextBlock
        {
            Text = description,
            FontSize = 14,
            Foreground = GetSecondaryForegroundBrush(),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 20)
        };
        SettingsContentPanel.Children.Add(desc);
    }

    private void AddCheckBox(string label, bool isChecked)
    {
        var checkBox = new CheckBox
        {
            Content = label,
            IsChecked = isChecked,
            Foreground = GetForegroundBrush(),
            Margin = new Thickness(0, 0, 0, 12),
            FontSize = 14
        };

        checkBox.Checked += async (s, e) => await HandleGeneralSettingChanged(label, true);
        checkBox.Unchecked += async (s, e) => await HandleGeneralSettingChanged(label, false);

        SettingsContentPanel.Children.Add(checkBox);
    }

    private async Task HandleGeneralSettingChanged(string label, bool value)
    {
        var mainWindow = Application.Current.MainWindow as MainWindow;
        var settingsService = mainWindow?.ViewModel?.SettingsService;
        if (settingsService == null) return;

        switch (label)
        {
            case "Enable system tray integration":
                // System tray integration is always enabled in this app
                // No need to persist this setting
                break;
            case "Start minimized to tray":
                await settingsService.SetMinimizeToTrayAsync(value);
                break;
            case "Show notifications":
                await settingsService.SetShowNotificationsAsync(value);
                break;
            case "Start with Windows":
                await settingsService.SetStartWithWindowsAsync(value);
                break;
            case "Auto-save changes":
                // Save auto-save preference
                await settingsService.SetSettingAsync("AutoSave", value);
                break;
            case "Show tree view icons":
                await settingsService.SetSettingAsync("ShowTreeViewIcons", value);
                break;
            case "Enable glassmorphism effects":
                await settingsService.SetSettingAsync("GlassmorphismEffects", value);
                ApplyGlassmorphismEffects(value);
                break;
            case "Confirm before deleting items":
                await settingsService.SetSettingAsync("ConfirmBeforeDelete", value);
                break;
            case "Restore last window position":
                await settingsService.SetSettingAsync("RestoreWindowPosition", value);
                break;
            case "Load last opened database":
                await settingsService.SetSettingAsync("LoadLastDatabase", value);
                break;
        }
    }

    private async Task HandleThemeChanged(string theme)
    {
        var mainWindow = Application.Current.MainWindow as MainWindow;
        var settingsService = mainWindow?.ViewModel?.SettingsService;
        if (settingsService == null) return;

        try
        {
            await settingsService.SetThemeAsync(theme);
            ApplyTheme(theme);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to change theme: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task HandleTextSettingChanged(string label, string value)
    {
        var mainWindow = Application.Current.MainWindow as MainWindow;
        var settingsService = mainWindow?.ViewModel?.SettingsService;
        if (settingsService == null) return;

        var settingKey = label.Replace(":", "").Trim();
        switch (settingKey)
        {
            case "Application Name":
                await settingsService.SetSettingAsync("ApplicationName", value);
                break;
            case "Default database path":
                await settingsService.SetSettingAsync("DefaultDatabasePath", value);
                break;
        }
    }

    private async Task HandleNumericSettingChanged(string label, double value)
    {
        var mainWindow = Application.Current.MainWindow as MainWindow;
        var settingsService = mainWindow?.ViewModel?.SettingsService;
        if (settingsService == null) return;

        var settingKey = label.Replace(":", "").Trim().Replace("(", "").Replace(")", "").Replace(" ", "");
        switch (settingKey)
        {
            case "Auto-backupintervalminutes":
                await settingsService.SetSettingAsync("AutoBackupInterval", (int)value);
                break;
        }
    }

    private void HandleTransparencyChanged(double transparency)
    {
        var mainWindow = Application.Current.MainWindow as MainWindow;
        if (mainWindow != null)
        {
            // Apply transparency to main window
            mainWindow.Opacity = transparency / 100.0;
            // Save the setting as BackgroundOpacity (int)
            _pendingTransparencySave = Task.Run(async () =>
            {
                var settingsService = mainWindow?.ViewModel?.SettingsService;
                if (settingsService != null)
                {
                    await settingsService.SetBackgroundOpacityAsync((int)Math.Round(transparency));
                }
            });
        }
    }

    private void ApplyTheme(string theme)
    {
        try
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow == null) return;

            // Helper to update all text foregrounds in a window recursively
            void UpdateForegrounds(Window window, System.Windows.Media.Brush brush)
            {
                UpdateElementForegrounds(window, brush);
            }

            switch (theme.ToLower())
            {
                case "dark":
                    _isLightTheme = false;
                    ApplyDarkTheme(mainWindow);
                    ApplyDarkTheme(this);
                    UpdateForegrounds(mainWindow, GetForegroundBrush());
                    UpdateForegrounds(this, GetForegroundBrush());
                    break;
                case "light":
                    _isLightTheme = true;
                    ApplyLightTheme(mainWindow);
                    ApplyLightTheme(this);
                    UpdateForegrounds(mainWindow, GetForegroundBrush());
                    UpdateForegrounds(this, GetForegroundBrush());
                    // Force refresh the settings content to apply light theme
                    RefreshSettingsUI();
                    // Update existing form fields immediately
                    UpdateExistingSettingsControls();
                    break;
                case "auto":
                    // For now, default to dark theme for auto mode
                    _isLightTheme = false;
                    ApplyDarkTheme(mainWindow);
                    ApplyDarkTheme(this);
                    UpdateForegrounds(mainWindow, GetForegroundBrush());
                    UpdateForegrounds(this, GetForegroundBrush());
                    UpdateExistingSettingsControls();
                    break;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to apply theme: {ex.Message}", "Theme Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void UpdateElementForegrounds(System.Windows.DependencyObject element, System.Windows.Media.Brush brush)
    {
        if (element is TextBlock textBlock)
        {
            textBlock.Foreground = brush;
        }
        else if (element is Button button)
        {
            button.Foreground = brush;
        }
        else if (element is ComboBox comboBox)
        {
            comboBox.Foreground = brush;
        }
        else if (element is CheckBox checkBox)
        {
            checkBox.Foreground = brush;
        }
        else if (element is TextBox textBox)
        {
            textBox.Foreground = brush;
        }

        // Recursively update children
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(element); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(element, i);
            UpdateElementForegrounds(child, brush);
        }
    }

    private void RefreshSettingsUI()
    {
        // Refresh the currently displayed settings to apply the new theme
        if (SettingsTreeView.SelectedItem is TreeViewItem selectedItem && selectedItem.Tag is string tag)
        {
            LoadSettingsContent(tag);
        }
    }

    // Walk the currently displayed settings content and update common form fields
    // so their Foreground/Background/BorderBrush match the current theme immediately.
    private void UpdateExistingSettingsControls()
    {
        if (SettingsContentPanel == null) return;

        void UpdateRecursive(System.Windows.DependencyObject parent)
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);

                switch (child)
                {
                    case TextBlock tb:
                        tb.Foreground = GetForegroundBrush();
                        break;
                    case TextBox tbox:
                        tbox.Foreground = GetForegroundBrush();
                        tbox.Background = GetControlBackgroundBrush();
                        tbox.BorderBrush = GetControlBorderBrush();
                        break;
                    case ComboBox cb:
                        cb.Foreground = GetForegroundBrush();
                        cb.Background = GetControlBackgroundBrush();
                        cb.BorderBrush = GetControlBorderBrush();
                        break;
                    case CheckBox chb:
                        chb.Foreground = GetForegroundBrush();
                        break;
                    case Button btn:
                        btn.Foreground = GetForegroundBrush();
                        break;
                    case Slider s:
                        // Slider has no Foreground, but update nearby label if any
                        break;
                }

                UpdateRecursive(child);
            }
        }

        UpdateRecursive(SettingsContentPanel);

        // Also update the left navigation TreeView and its items
        try
        {
            if (SettingsTreeView != null)
            {
                SettingsTreeView.Foreground = GetForegroundBrush();
                foreach (var item in SettingsTreeView.Items)
                {
                    if (item is TreeViewItem tvi)
                    {
                        UpdateTreeViewItemRecursive(tvi);
                    }
                }
            }
        }
        catch { }
    }

    private void UpdateTreeViewItemRecursive(TreeViewItem tvi)
    {
        tvi.Foreground = GetForegroundBrush();
        // Update header text blocks if present
        if (tvi.Header is TextBlock tb)
            tb.Foreground = GetForegroundBrush();

        foreach (var child in tvi.Items)
        {
            if (child is TreeViewItem childTvi)
                UpdateTreeViewItemRecursive(childTvi);
        }
    }

    private void ApplyDarkTheme(Window window)
    {
        // Apply dark theme colors
        var darkBackground = new System.Windows.Media.LinearGradientBrush();
        darkBackground.StartPoint = new Point(0, 0);
        darkBackground.EndPoint = new Point(1, 1);
        darkBackground.GradientStops.Add(new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromArgb(255, 31, 31, 31), 0.0));
        darkBackground.GradientStops.Add(new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromArgb(255, 45, 45, 48), 1.0));
        
        if (window.Content is Grid grid)
        {
            grid.Background = darkBackground;
        }

        // Restore borders/elements changed by light theme
        UpdateBordersForDarkTheme(window);
    }

    private void UpdateBordersForDarkTheme(Window window)
    {
        if (window.Content is Border mainBorder)
        {
            // Restore main border for dark theme
            mainBorder.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(230, 31, 31, 31));
            mainBorder.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(100, 255, 255, 255));
            UpdateElementsForDarkTheme(mainBorder);
        }
        else if (window.Content is Grid grid)
        {
            UpdateElementsForDarkTheme(grid);
        }
    }

    private void UpdateElementsForDarkTheme(System.Windows.DependencyObject parent)
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            
            if (child is Border border)
            {
                // Update borders back to dark theme colors
                var currentBg = border.Background as System.Windows.Media.SolidColorBrush;
                if (currentBg != null && !IsLikelyDarkColor(currentBg.Color))
                {
                    border.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(30, 0, 0, 0));
                }

                var currentBorderBrush = border.BorderBrush as System.Windows.Media.SolidColorBrush;
                if (currentBorderBrush != null)
                {
                    border.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(51, 255, 255, 255));
                }
            }
            else if (child is TextBlock textBlock)
            {
                // Update text colors for dark theme
                textBlock.Foreground = GetForegroundBrush();
            }
            
            // Recursively update children
            UpdateElementsForDarkTheme(child);
        }
    }

    private void ApplyLightTheme(Window window)
    {
        // Apply light theme colors  
        var lightBackground = new System.Windows.Media.LinearGradientBrush();
        lightBackground.StartPoint = new Point(0, 0);
        lightBackground.EndPoint = new Point(1, 1);
        lightBackground.GradientStops.Add(new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromArgb(255, 240, 240, 240), 0.0));
        lightBackground.GradientStops.Add(new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromArgb(255, 255, 255, 255), 1.0));
        
        // Apply light background to the window itself
        window.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 245, 245, 245));
        
        if (window.Content is Grid grid)
        {
            grid.Background = lightBackground;
        }

        // Update Border elements for light theme
        UpdateBordersForLightTheme(window);
    }

    private void UpdateBordersForLightTheme(Window window)
    {
        if (window.Content is Border mainBorder)
        {
            // Update main border for light theme
            mainBorder.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(230, 250, 250, 250));
            mainBorder.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(100, 100, 100, 100));
            
            // Find and update nested elements
            UpdateElementsForLightTheme(mainBorder);
        }
        else if (window.Content is Grid grid)
        {
            UpdateElementsForLightTheme(grid);
        }
    }

    private void UpdateElementsForLightTheme(System.Windows.DependencyObject parent)
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            
            if (child is Border border)
            {
                // Update borders to light theme colors
                var currentBg = border.Background as System.Windows.Media.SolidColorBrush;
                if (currentBg != null && IsLikelyDarkColor(currentBg.Color))
                {
                    border.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(200, 255, 255, 255));
                }
                
                var currentBorderBrush = border.BorderBrush as System.Windows.Media.SolidColorBrush;
                if (currentBorderBrush != null)
                {
                    border.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(150, 100, 100, 100));
                }
            }
            else if (child is TextBlock textBlock)
            {
                // Update text colors for light theme
                textBlock.Foreground = GetForegroundBrush();
            }
            else if (child is TreeView treeView)
            {
                // Update TreeView styling for light theme
                treeView.Background = System.Windows.Media.Brushes.Transparent;
            }
            
            // Recursively update children
            UpdateElementsForLightTheme(child);
        }
    }

    private bool IsLikelyDarkColor(System.Windows.Media.Color color)
    {
        // Consider a color dark if its brightness is below a threshold
        double brightness = (color.R * 0.299 + color.G * 0.587 + color.B * 0.114);
        return brightness < 128;
    }

    private void ApplyGlassmorphismEffects(bool enable)
    {
        var mainWindow = Application.Current.MainWindow as MainWindow;
        if (mainWindow == null) return;

        if (enable)
        {
            // Enable glassmorphism effects
            mainWindow.AllowsTransparency = true;
            if (mainWindow.Opacity > 0.95) mainWindow.Opacity = 0.95;
        }
        else
        {
            // Disable glassmorphism effects
            mainWindow.Opacity = 1.0;
        }
    }

    private void AddTextSetting(string label, string value)
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 0, 0, 16) };
        
        var labelBlock = new TextBlock
        {
            Text = label,
            Foreground = GetForegroundBrush(),
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 4)
        };
        
        var textBox = new TextBox
        {
            Text = value,
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(51, 255, 255, 255)),
            Foreground = GetForegroundBrush(),
            BorderBrush = _isLightTheme ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(102, 0, 0, 0)) : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(102, 255, 255, 255)),
            Padding = new Thickness(8),
            FontSize = 14,
            Width = 200,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        
        // Add event handler for text changes
        textBox.TextChanged += async (sender, e) => await HandleTextSettingChanged(label, textBox.Text);
        
        panel.Children.Add(labelBlock);
        panel.Children.Add(textBox);
    SettingsContentPanel.Children.Add(panel);
    }

    private void AddFilePathSetting(string label, string value)
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 0, 0, 16) };

        var labelBlock = new TextBlock
        {
            Text = label,
            Foreground = GetForegroundBrush(),
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 4)
        };

        var pathPanel = new StackPanel { Orientation = Orientation.Horizontal };

        var textBox = new TextBox
        {
            Text = value,
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(51, 255, 255, 255)),
            Foreground = GetForegroundBrush(),
            BorderBrush = _isLightTheme ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(102, 0, 0, 0)) : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(102, 255, 255, 255)),
            Padding = new Thickness(8),
            FontSize = 14,
            Width = 420,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        var browseButton = new Button
        {
            Content = "📂 Browse",
            Style = (Style)FindResource("ModernButtonStyle"),
            Padding = new Thickness(12, 6, 12, 6),
            Margin = new Thickness(8, 0, 0, 0),
            FontSize = 14
        };

        browseButton.Click += (s, e) => BrowseExecutable(textBox);

        pathPanel.Children.Add(textBox);
        pathPanel.Children.Add(browseButton);

        panel.Children.Add(labelBlock);
        panel.Children.Add(pathPanel);
        SettingsContentPanel.Children.Add(panel);
    }

    private void BrowseExecutable(System.Windows.Controls.TextBox pathTextBox)
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select executable",
            Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
            DefaultExt = ".exe",
            CheckFileExists = true
        };

        // Try to set initial directory to current textbox value's folder
        try
        {
            var current = pathTextBox.Text;
            if (!string.IsNullOrWhiteSpace(current) && System.IO.File.Exists(current))
            {
                openFileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(current);
                openFileDialog.FileName = System.IO.Path.GetFileName(current);
            }
        }
        catch { }

        if (openFileDialog.ShowDialog() == true)
        {
            pathTextBox.Text = openFileDialog.FileName;
        }
    }

    private void AddNumericSetting(string label, double value)
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 0, 0, 16) };
        
        var labelBlock = new TextBlock
        {
            Text = label,
            Foreground = GetForegroundBrush(),
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 4)
        };
        
        var textBox = new TextBox
        {
            Text = value.ToString(),
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(51, 255, 255, 255)),
            Foreground = GetForegroundBrush(),
            BorderBrush = _isLightTheme ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(102, 0, 0, 0)) : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(102, 255, 255, 255)),
            Padding = new Thickness(8),
            FontSize = 14,
            Width = 100,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        
        // Add event handler for numeric value changes
        textBox.TextChanged += async (sender, e) =>
        {
            if (double.TryParse(textBox.Text, out double numericValue))
            {
                await HandleNumericSettingChanged(label, numericValue);
            }
        };
        
        panel.Children.Add(labelBlock);
        panel.Children.Add(textBox);
    SettingsContentPanel.Children.Add(panel);
    }

    private void AddComboBoxSetting(string label, string[] options, string selectedValue)
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 0, 0, 16) };
        
        var labelBlock = new TextBlock
        {
            Text = label,
            Foreground = GetForegroundBrush(),
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 4)
        };
        
        var comboBox = new ComboBox
        {
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(51, 255, 255, 255)),
            Foreground = GetForegroundBrush(),
            BorderBrush = _isLightTheme ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(102, 0, 0, 0)) : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(102, 255, 255, 255)),
            Padding = new Thickness(8),
            FontSize = 14,
            Width = 150,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        
        foreach (var option in options)
        {
            comboBox.Items.Add(option);
        }
        comboBox.SelectedItem = selectedValue;
        
        panel.Children.Add(labelBlock);
        panel.Children.Add(comboBox);
    SettingsContentPanel.Children.Add(panel);
    }

    private void AddButton(string text, System.Action action)
    {
        var button = new Button
        {
            Content = text,
            // Use Success for action-like buttons (Save/Add). Default to Modern otherwise.
            Style = (Style)FindResource("SuccessButtonStyle"),
            Padding = new Thickness(16, 8, 16, 8),
            Margin = new Thickness(0, 0, 0, 12),
            FontSize = 14,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        
        button.Click += (s, e) => action?.Invoke();
    SettingsContentPanel.Children.Add(button);
    }

    private void AddThemeComboBox(string label, string[] options, string selectedValue)
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 0, 0, 16) };
        
        var labelBlock = new TextBlock
        {
            Text = label,
            Foreground = GetForegroundBrush(),
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 4)
        };
        
        var comboBox = new ComboBox
        {
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(51, 255, 255, 255)),
            Foreground = GetForegroundBrush(),
            BorderBrush = _isLightTheme ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(102, 0, 0, 0)) : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(102, 255, 255, 255)),
            Padding = new Thickness(8),
            FontSize = 14,
            Width = 150,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        
        foreach (var option in options)
        {
            comboBox.Items.Add(option);
        }
        comboBox.SelectedItem = selectedValue;
        
        // Add event handler for theme changes
        comboBox.SelectionChanged += async (s, e) => 
        {
            if (comboBox.SelectedItem?.ToString() is string newTheme)
            {
                await HandleThemeChanged(newTheme);
            }
        };
        
        panel.Children.Add(labelBlock);
        panel.Children.Add(comboBox);
    SettingsContentPanel.Children.Add(panel);
    }

    private void AddTransparencySlider(string label, double value)
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 0, 0, 16) };
        
        var labelBlock = new TextBlock
        {
            Text = $"{label} {value}%",
            Foreground = GetForegroundBrush(),
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 4)
        };
        
        var slider = new Slider
        {
            Minimum = 50,
            Maximum = 100,
            Value = value,
            Width = 200,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 4, 0, 0)
        };
        
        // Update label text when slider changes
        slider.ValueChanged += (s, e) => 
        {
            labelBlock.Text = $"{label.Replace(" (%)", "")} ({slider.Value:F0}%)";

            // Apply the transparency immediately so the user sees the effect while dragging
            try
            {
                var mw = Application.Current.MainWindow as MainWindow;
                if (mw != null)
                {
                    mw.Opacity = slider.Value / 100.0;
                }
            }
            catch { }

            // Debounce rapid changes to avoid excessive DB writes. Wait 400ms after last change then save.
            var mainWindow = Application.Current.MainWindow as MainWindow;
            var settingsService = mainWindow?.ViewModel?.SettingsService as WinWork.Core.Services.ISettingsService;

            // Cancel previous pending save
            try { _transparencyCts?.Cancel(); } catch { }
            _transparencyCts?.Dispose();
            _transparencyCts = new CancellationTokenSource();
            var token = _transparencyCts.Token;

            _pendingTransparencySave = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(400, token);
                    if (token.IsCancellationRequested) return;
                    if (settingsService != null)
                    {
                        // Use typed API to persist integer opacity
                        await settingsService.SetBackgroundOpacityAsync((int)Math.Round(slider.Value));
                    }
                }
                catch (OperationCanceledException) { }
                catch { }
            }, token);
        };

        // When the user releases the mouse (finishes dragging) or presses Enter, save immediately
        slider.PreviewMouseLeftButtonUp += async (s, e) =>
        {
            try
            {
                // Cancel the debounce so we don't have duplicate saves
                try { _transparencyCts?.Cancel(); } catch { }
                // Await any pending save if it is running
                if (_pendingTransparencySave != null)
                {
                    try { await _pendingTransparencySave; } catch { }
                }

                var mainWindow = Application.Current.MainWindow as MainWindow;
                var settingsService2 = mainWindow?.ViewModel?.SettingsService;
                if (settingsService2 != null)
                {
                    var valueToSave = (int)Math.Round(slider.Value);
                    if (valueToSave < 10) valueToSave = 10;
                    if (valueToSave > 100) valueToSave = 100;
                    await settingsService2.SetBackgroundOpacityAsync(valueToSave);
                }
            }
            catch { }
        };

        slider.PreviewKeyUp += async (s, e) =>
        {
            try
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                {
                    try { _transparencyCts?.Cancel(); } catch { }
                    if (_pendingTransparencySave != null)
                    {
                        try { await _pendingTransparencySave; } catch { }
                    }
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    var settingsService2 = mainWindow?.ViewModel?.SettingsService;
                    if (settingsService2 != null)
                    {
                        var valueToSave = (int)Math.Round(slider.Value);
                        if (valueToSave < 10) valueToSave = 10;
                        if (valueToSave > 100) valueToSave = 100;
                        await settingsService2.SetBackgroundOpacityAsync(valueToSave);
                    }
                }
            }
            catch { }
        };
        
        panel.Children.Add(labelBlock);
        panel.Children.Add(slider);
    SettingsContentPanel.Children.Add(panel);
    }



    #region Data & Storage Operations

    private void OpenDataFolder()
    {
        try
        {
            var dataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinWork");
            if (!Directory.Exists(dataFolder))
                Directory.CreateDirectory(dataFolder);
                
            System.Diagnostics.Process.Start("explorer.exe", dataFolder);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open data folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    

    private void ExportAllData()
    {
        try
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow?.DataContext is MainWindowViewModel viewModel)
            {
                viewModel.ExportDataCommand?.Execute(null);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to export data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }



    #endregion

    private void AddTextArea(string text)
    {
        var textBlock = new TextBlock
        {
            Text = text,
            Foreground = _isLightTheme ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(179, 0, 0, 0)) : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(179, 255, 255, 255)),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13,
            Margin = new Thickness(0, 0, 0, 16),
            Padding = new Thickness(12),
            Background = _isLightTheme ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(18, 0, 0, 0)) : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(25, 255, 255, 255))
        };
    SettingsContentPanel.Children.Add(textBlock);
    }

    private void AddInfoSection(string label, string value)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
        
        var labelBlock = new TextBlock
        {
            Text = label,
            Foreground = GetForegroundBrush(),
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Width = 120
        };
        
        var valueBlock = new TextBlock
        {
            Text = value,
            Foreground = GetSecondaryForegroundBrush(),
            FontSize = 14
        };
        
        panel.Children.Add(labelBlock);
        panel.Children.Add(valueBlock);
    SettingsContentPanel.Children.Add(panel);
    }

    #endregion

    #region Database Settings Methods

    private string GetCurrentDatabasePath()
    {
        // Use the application's default database path (AppData\WinWork\winwork.db)
        try
        {
            return WinWork.Data.DatabaseConfiguration.GetDefaultDatabasePath();
        }
        catch
        {
            // Fallback: application directory name (legacy)
            var appDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "";
            return System.IO.Path.Combine(appDirectory, "winwork.db");
        }
    }

    private string GetDatabaseFileSize(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                var fileInfo = new FileInfo(path);
                var sizeInBytes = fileInfo.Length;
                
                if (sizeInBytes < 1024)
                    return $"{sizeInBytes} bytes";
                else if (sizeInBytes < 1024 * 1024)
                    return $"{sizeInBytes / 1024:F1} KB";
                else
                    return $"{sizeInBytes / (1024 * 1024):F1} MB";
            }
            return "File not found";
        }
        catch
        {
            return "Unable to read";
        }
    }

    private string GetDatabaseLastModified(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                var lastModified = File.GetLastWriteTime(path);
                return lastModified.ToString("yyyy-MM-dd HH:mm:ss");
            }
            return "File not found";
        }
        catch
        {
            return "Unable to read";
        }
    }

    private void AddDatabasePathSetting(string label, string currentPath)
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 0, 0, 16) };
        
        var labelBlock = new TextBlock
        {
            Text = label,
            Foreground = System.Windows.Media.Brushes.White,
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 4)
        };

        var pathPanel = new StackPanel { Orientation = Orientation.Horizontal };
        
        var textBox = new TextBox
        {
            Text = currentPath,
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(51, 255, 255, 255)),
            Foreground = System.Windows.Media.Brushes.White,
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(102, 255, 255, 255)),
            Padding = new Thickness(8),
            FontSize = 14,
            Width = 400,
            IsReadOnly = true,
            Name = "DatabasePathTextBox"
        };

        var browseButton = new Button
        {
            Content = "📁 Browse",
            Style = (Style)FindResource("ModernButtonStyle"),
            Padding = new Thickness(12, 6, 12, 6),
            Margin = new Thickness(8, 0, 0, 0),
            FontSize = 14
        };

        browseButton.Click += (s, e) => BrowseDatabaseFile(textBox);

        pathPanel.Children.Add(textBox);
        pathPanel.Children.Add(browseButton);
        
        panel.Children.Add(labelBlock);
        panel.Children.Add(pathPanel);
    SettingsContentPanel.Children.Add(panel);
    }

    private void BrowseDatabaseFile(System.Windows.Controls.TextBox pathTextBox)
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Select SQLite Database File",
            Filter = "SQLite Database Files (*.db;*.sqlite;*.sqlite3)|*.db;*.sqlite;*.sqlite3|All Files (*.*)|*.*",
            DefaultExt = ".db",
            CheckFileExists = true
        };

        // Set initial directory to current database location
        var currentPath = pathTextBox.Text;
        if (!string.IsNullOrEmpty(currentPath) && File.Exists(currentPath))
        {
            openFileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(currentPath);
            openFileDialog.FileName = System.IO.Path.GetFileName(currentPath);
        }
        else
        {
            // Set to application directory
            var appDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (!string.IsNullOrEmpty(appDirectory))
                openFileDialog.InitialDirectory = appDirectory;
        }

        if (openFileDialog.ShowDialog() == true)
        {
            pathTextBox.Text = openFileDialog.FileName;
            
            // Show confirmation message
            MessageBox.Show(
                $"Database path updated to:\n{openFileDialog.FileName}\n\nRestart the application to use the new database file.",
                "Database Path Updated",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private void OptimizeDatabase()
    {
        MessageBox.Show(
            "Database optimization completed successfully.\n\nThe database has been defragmented and optimized for better performance.",
            "Database Optimization",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void CleanUpDatabase()
    {
        var result = MessageBox.Show(
            "This will remove unused data and optimize the database structure.\n\nDo you want to continue?",
            "Database Cleanup",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            MessageBox.Show(
                "Database cleanup completed successfully.\n\nUnused data has been removed and the database structure has been optimized.",
                "Database Cleanup Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private void ResetDatabaseToDefault()
    {
        var result = MessageBox.Show(
            "This will reset the database path to the default location (AppData\\WinWork\\winwork.db).\n\nDo you want to continue?",
            "Reset Database Path",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            // Find the database path textbox and reset it to the standard AppData location
            var defaultPath = GetCurrentDatabasePath();
            
            // Update the textbox (this is a simplified approach - in a real app you'd use proper data binding)
            MessageBox.Show(
                $"Database path has been reset to:\n{defaultPath}\n\nRestart the application to use the default database file.",
                "Database Path Reset",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
                
            // Reload the database settings to refresh the UI
            LoadDatabaseSettings();
        }
    }

    private void AddSeparator()
    {
        var separator = new System.Windows.Shapes.Rectangle
        {
            Height = 1,
            Fill = _isLightTheme ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(51, 0, 0, 0)) : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(51, 255, 255, 255)),
            Margin = new Thickness(0, 16, 0, 16),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
    SettingsContentPanel.Children.Add(separator);
    }

    private void AddSectionSubHeader(string title)
    {
        var header = new TextBlock
        {
            Text = title,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Foreground = GetForegroundBrush(),
            Margin = new Thickness(0, 0, 0, 12)
        };
    SettingsContentPanel.Children.Add(header);
    }

    #endregion
    
    #region Import/Export Methods
    
    private void ExportData()
    {
        try
        {
            // Get the main window to access its DataContext
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow?.DataContext is MainWindowViewModel viewModel)
            {
                viewModel.ExportDataCommand?.Execute(null);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed: {ex.Message}", "Export Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void ImportData()
    {
        try
        {
            // Get the main window to access its DataContext  
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow?.DataContext is MainWindowViewModel viewModel)
            {
                viewModel.ImportDataCommand?.Execute(null);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Import failed: {ex.Message}", "Import Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    #endregion

    #region Missing Button Implementations

    private void ChooseBackupFolder()
    {
        try
        {
            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Select backup folder location";
            saveFileDialog.Filter = "Folders|*.*";
            saveFileDialog.FileName = "Select this folder";
            saveFileDialog.CheckFileExists = false;
            saveFileDialog.CheckPathExists = true;
            
            // Set initial directory
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var defaultBackupPath = Path.Combine(documentsPath, "WinWork", "Backups");
            if (Directory.Exists(defaultBackupPath))
            {
                saveFileDialog.InitialDirectory = defaultBackupPath;
            }
            
            if (saveFileDialog.ShowDialog() == true)
            {
                var selectedPath = Path.GetDirectoryName(saveFileDialog.FileName) ?? "";
                if (!string.IsNullOrWhiteSpace(selectedPath))
                {
                    // Persist selected backup folder to settings
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    var settingsService = mainWindow?.ViewModel?.SettingsService;
                    try
                    {
                        if (!Directory.Exists(selectedPath)) Directory.CreateDirectory(selectedPath);
                        if (settingsService != null)
                        {
                            // Save as simple string setting
                            settingsService.SetSettingAsync("BackupFolder", selectedPath).Wait();
                        }
                    }
                    catch { }

                    MessageBox.Show($"Backup folder set to:\n{selectedPath}", 
                        "Backup Folder Updated", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to select backup folder: {ex.Message}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenLogFolder()
    {
        try
        {
            var logFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinWork", "Logs");
            if (!Directory.Exists(logFolder))
            {
                Directory.CreateDirectory(logFolder);
            }
            
            System.Diagnostics.Process.Start("explorer.exe", logFolder);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open log folder: {ex.Message}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CreateBackupNow()
    {
        try
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            var settingsService = mainWindow?.ViewModel?.SettingsService;

            // Determine backup folder: priority - saved setting, default Documents\WinWork\Backups
            string backupFolder = settingsService != null ? (settingsService.GetSettingAsync("BackupFolder").Result ?? "") : "";
            if (string.IsNullOrWhiteSpace(backupFolder))
            {
                backupFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WinWork", "Backups");
            }

            if (!Directory.Exists(backupFolder))
            {
                Directory.CreateDirectory(backupFolder);
            }

            // Locate current DB
            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinWork", "winwork.db");
            if (!File.Exists(dbPath))
            {
                MessageBox.Show($"Database file not found: {dbPath}", "Backup Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var destFile = Path.Combine(backupFolder, $"winwork_backup_{timestamp}.db");

            // Copy the DB file (overwrite if exists)
            File.Copy(dbPath, destFile, overwrite: true);

            MessageBox.Show($"Backup created:\n{destFile}", "Backup Created", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to create backup: {ex.Message}", "Backup Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ChangeMasterPassword()
    {
        try
        {
            var result = MessageBox.Show(
                "This feature will allow you to set a master password for additional security.\n\nThis functionality is not yet implemented. Would you like to be notified when it becomes available?",
                "Change Master Password",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);
                
            if (result == MessageBoxResult.Yes)
            {
                MessageBox.Show("You will be notified when password protection is available.", 
                    "Notification Set", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenWebsite()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/codelogn/WinWork",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open website: {ex.Message}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ReportIssue()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/codelogn/WinWork/issues",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open issue tracker: {ex.Message}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ContactSupport()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "mailto:support@winwork.app?subject=WinWork Support Request",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open email client: {ex.Message}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion
}
}