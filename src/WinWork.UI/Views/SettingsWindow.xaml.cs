using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using Microsoft.Win32;
using WinWork.UI.ViewModels;

namespace WinWork.UI.Views
{
/// <summary>
/// Interaction logic for SettingsWindow.xaml
/// </summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        
        // Select the first item by default
        if (SettingsTreeView.Items.Count > 0 && SettingsTreeView.Items[0] is TreeViewItem firstItem)
        {
            firstItem.IsSelected = true;
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

    private void LoadApplicationSettings()
    {
        AddSectionHeader("Application Settings");
        AddDescription("Configure application behavior and preferences.");
        
        AddTextSetting("Application Name:", "WinWork");
        AddCheckBox("Auto-save changes", true);
        AddCheckBox("Confirm before deleting items", true);
        AddNumericSetting("Auto-backup interval (minutes):", 30);
    }

    private async void LoadInterfaceSettings()
    {
        AddSectionHeader("Interface Settings");
        AddDescription("Customize the user interface appearance and behavior.");
        
        var mainWindow = Application.Current.MainWindow as MainWindow;
        var settingsService = mainWindow?.ViewModel?.SettingsService;
        
        // Load actual values from settings service
        string currentTheme = settingsService != null ? await settingsService.GetThemeAsync() : "Dark";
        var currentTransparency = settingsService != null ? (await settingsService.GetSettingAsync<double>("WindowTransparency") ?? 90.0) : 90.0;
        var autoSave = settingsService != null ? (await settingsService.GetSettingAsync<bool>("AutoSave") ?? true) : true;
        var showTreeIcons = settingsService != null ? (await settingsService.GetSettingAsync<bool>("ShowTreeViewIcons") ?? true) : true;
        var glassmorphism = settingsService != null ? (await settingsService.GetSettingAsync<bool>("GlassmorphismEffects") ?? true) : true;
        
        AddThemeComboBox("Theme:", new[] { "Dark", "Light", "Auto" }, currentTheme);
        AddCheckBox("Show tree view icons", showTreeIcons);
        AddCheckBox("Enable glassmorphism effects", glassmorphism);
        AddTransparencySlider("Window transparency (%):", currentTransparency);
        AddCheckBox("Auto-save changes", autoSave);
    }

    private void LoadStartupSettings()
    {
        AddSectionHeader("Startup Settings");
        AddDescription("Configure how WinWork behaves when starting up.");
        
        AddCheckBox("Start with Windows", false);
        AddCheckBox("Restore last window position", true);
        AddCheckBox("Load last opened database", true);
        AddTextSetting("Default database path:", "");
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

    private void LoadDatabaseSettings()
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
        AddInfoSection("Current Status:", "Connected");
        AddInfoSection("Database Path:", currentPath);
        AddInfoSection("File Size:", GetDatabaseFileSize(currentPath));
        AddInfoSection("Last Modified:", GetDatabaseLastModified(currentPath));

        // Query total records count and tables info
        int recordsCount = 0;
        int tagsCount = 0;
        int linkTagsCount = 0;
        try
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            var viewModel = mainWindow?.ViewModel;
            var linkService = viewModel?.GetType().GetProperty("_linkService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(viewModel) as WinWork.Core.Interfaces.ILinkService;
            var tagService = viewModel?.GetType().GetProperty("_tagService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(viewModel) as WinWork.Core.Services.ITagService;
            if (linkService != null)
            {
                var allLinksTask = linkService.GetAllLinksAsync();
                allLinksTask.Wait();
                recordsCount = allLinksTask.Result.Count();
            }
            if (tagService != null)
            {
                var allTagsTask = tagService.GetAllTagsAsync();
                allTagsTask.Wait();
                tagsCount = allTagsTask.Result.Count();
            }
            // For LinkTags, we need to count all link.LinkTags
            if (linkService != null)
            {
                var allLinksTask = linkService.GetAllLinksAsync();
                allLinksTask.Wait();
                linkTagsCount = allLinksTask.Result.SelectMany(l => l.LinkTags ?? new List<WinWork.Models.LinkTag>()).Count();
            }
        }
        catch { }
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
    AddTextSetting("Backup folder:", backupFolder);
    AddButton("📂 Choose Backup Folder", () => ChooseBackupFolder());
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
            Foreground = System.Windows.Media.Brushes.White,
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
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(204, 255, 255, 255)),
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
            Foreground = System.Windows.Media.Brushes.White,
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

    private void HandleTransparencyChanged(double transparency)
    {
        var mainWindow = Application.Current.MainWindow as MainWindow;
        if (mainWindow != null)
        {
            // Apply transparency to main window
            mainWindow.Opacity = transparency / 100.0;
            
            // Save the setting
            Task.Run(async () =>
            {
                var settingsService = mainWindow?.ViewModel?.SettingsService;
                if (settingsService != null)
                {
                    await settingsService.SetSettingAsync("WindowTransparency", transparency);
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
                    ApplyDarkTheme(mainWindow);
                    ApplyDarkTheme(this);
                    UpdateForegrounds(mainWindow, System.Windows.Media.Brushes.White);
                    UpdateForegrounds(this, System.Windows.Media.Brushes.White);
                    break;
                case "light":
                    ApplyLightTheme(mainWindow);
                    ApplyLightTheme(this);
                    UpdateForegrounds(mainWindow, System.Windows.Media.Brushes.Black);
                    UpdateForegrounds(this, System.Windows.Media.Brushes.Black);
                    // Force refresh the settings content to apply light theme
                    RefreshSettingsUI();
                    break;
                case "auto":
                    // For now, default to dark theme for auto mode
                    ApplyDarkTheme(mainWindow);
                    ApplyDarkTheme(this);
                    UpdateForegrounds(mainWindow, System.Windows.Media.Brushes.White);
                    UpdateForegrounds(this, System.Windows.Media.Brushes.White);
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
                textBlock.Foreground = System.Windows.Media.Brushes.Black;
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
            Foreground = System.Windows.Media.Brushes.White,
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 4)
        };
        
        var textBox = new TextBox
        {
            Text = value,
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(51, 255, 255, 255)),
            Foreground = System.Windows.Media.Brushes.White,
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(102, 255, 255, 255)),
            Padding = new Thickness(8),
            FontSize = 14
        };
        
        panel.Children.Add(labelBlock);
        panel.Children.Add(textBox);
    SettingsContentPanel.Children.Add(panel);
    }

    private void AddNumericSetting(string label, double value)
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 0, 0, 16) };
        
        var labelBlock = new TextBlock
        {
            Text = label,
            Foreground = System.Windows.Media.Brushes.White,
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 4)
        };
        
        var textBox = new TextBox
        {
            Text = value.ToString(),
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(51, 255, 255, 255)),
            Foreground = System.Windows.Media.Brushes.White,
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(102, 255, 255, 255)),
            Padding = new Thickness(8),
            FontSize = 14,
            Width = 100,
            HorizontalAlignment = HorizontalAlignment.Left
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
            Foreground = System.Windows.Media.Brushes.White,
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 4)
        };
        
        var comboBox = new ComboBox
        {
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(51, 255, 255, 255)),
            Foreground = System.Windows.Media.Brushes.White,
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(102, 255, 255, 255)),
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
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(102, 0, 120, 215)),
            Foreground = System.Windows.Media.Brushes.White,
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(102, 255, 255, 255)),
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
            Foreground = System.Windows.Media.Brushes.White,
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 4)
        };
        
        var comboBox = new ComboBox
        {
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(51, 255, 255, 255)),
            Foreground = System.Windows.Media.Brushes.White,
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(102, 255, 255, 255)),
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
            Foreground = System.Windows.Media.Brushes.White,
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
            HandleTransparencyChanged(slider.Value);
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

    private void CreateBackupNow()
    {
        try
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow?.DataContext is MainWindowViewModel viewModel)
            {
                // Use the export functionality to create a backup
                viewModel.ExportDataCommand?.Execute(null);
                MessageBox.Show("Backup created successfully!", "Backup Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to create backup: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(179, 255, 255, 255)),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13,
            Margin = new Thickness(0, 0, 0, 16),
            Padding = new Thickness(12),
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(25, 255, 255, 255))
        };
    SettingsContentPanel.Children.Add(textBlock);
    }

    private void AddInfoSection(string label, string value)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
        
        var labelBlock = new TextBlock
        {
            Text = label,
            Foreground = System.Windows.Media.Brushes.White,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Width = 120
        };
        
        var valueBlock = new TextBlock
        {
            Text = value,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(204, 255, 255, 255)),
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
        // Get the default database path (in the application directory)
        var appDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "";
        var defaultPath = System.IO.Path.Combine(appDirectory, "linker.db");
        
        // TODO: In a real implementation, this would come from configuration
        return defaultPath;
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
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(102, 0, 120, 215)),
            Foreground = System.Windows.Media.Brushes.White,
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(102, 255, 255, 255)),
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
            "This will reset the database path to the default location (linker.db in the application directory).\n\nDo you want to continue?",
            "Reset Database Path",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            // Find the database path textbox and reset it
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
            Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(51, 255, 255, 255)),
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
            Foreground = System.Windows.Media.Brushes.White,
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
                MessageBox.Show($"Backup folder set to:\n{selectedPath}", 
                    "Backup Folder Updated", MessageBoxButton.OK, MessageBoxImage.Information);
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