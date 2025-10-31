using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using WinWork.Data;
using WinWork.Core;
using WinWork.UI.Services;
using WinWork.UI.ViewModels;
using WinWork.Core.Services;
using System.Runtime.InteropServices;

namespace WinWork.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    // Removed AllocConsole import for production GUI-only build

    protected override async void OnStartup(StartupEventArgs e)
    {
        try
        {
            // Create and configure host
            _host = CreateHost();
            
            // Start the host
            await _host.StartAsync();

            // Initialize database before showing UI
            using (var scope = _host.Services.CreateScope())
            {
                try
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<WinWorkDbContext>();
                    
                    // Get database path for error reporting
                    var dbPath = DatabaseConfiguration.GetDefaultDatabasePath();
                    
                    try
                    {
                        await DatabaseConfiguration.InitializeDatabaseAsync(dbContext);
                    }
                    catch (Exception dbEx)
                    {
                        ShowErrorDialog(
                            "Database Initialization Error",
                            $"Failed to initialize database at {dbPath}. The application will now exit.",
                            $"Error details:\n{dbEx.Message}\n\nStack trace:\n{dbEx.StackTrace}");
                        Shutdown(1);
                        return;
                    }
                }
                catch (Exception scopeEx)
                {
                    ShowErrorDialog(
                        "Service Configuration Error",
                        "Failed to configure application services. The application will now exit.",
                        $"Error details:\n{scopeEx.Message}\n\nStack trace:\n{scopeEx.StackTrace}");
                    Shutdown(1);
                    return;
                }
            }

            // Show main window after database is ready
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            // Start auto-backup background task
            _ = Task.Run(async () =>
            {
                try
                {
                    var settingsService = _host.Services.GetRequiredService<ISettingsService>();
                    while (true)
                    {
                        try
                        {
                            var enabled = await settingsService.GetSettingAsync<bool>("AutoBackup") ?? true;
                            if (enabled)
                            {
                                // BackupInterval stored as hours (default 24)
                                var intervalHours = await settingsService.GetSettingAsync<int>("AutoBackupInterval") ?? 24;
                                // LastBackupUtc stored as ISO string
                                var lastBackupStr = await settingsService.GetSettingAsync("LastBackupUtc");
                                DateTime? lastBackup = null;
                                if (!string.IsNullOrWhiteSpace(lastBackupStr) && DateTime.TryParse(lastBackupStr, out var parsed))
                                    lastBackup = parsed.ToUniversalTime();

                                var due = false;
                                if (!lastBackup.HasValue) due = true;
                                else due = (DateTime.UtcNow - lastBackup.Value) > TimeSpan.FromHours(Math.Max(1, intervalHours));

                                if (due)
                                {
                                    // Perform backup and record last time if successful
                                    var backupResult = await WinWork.UI.Utils.BackupHelper.CreateBackupAsync();
                                    if (!string.IsNullOrWhiteSpace(backupResult))
                                    {
                                        try { await settingsService.SetSettingAsync("LastBackupUtc", DateTime.UtcNow.ToString("o")); } catch { }
                                    }
                                }
                            }
                        }
                        catch { }

                        // Sleep a moderate amount before next check
                        await Task.Delay(TimeSpan.FromMinutes(30));
                    }
                }
                catch { }
            });

            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            ShowErrorDialog("Application Startup Error", $"Application startup failed: {ex.Message}", ex.ToString());
            Shutdown(1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        
        base.OnExit(e);
    }

    private IHost CreateHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Configure database
                services.AddWinWorkDatabase();
                
                // Configure core services  
                services.AddWinWorkCore();
                
                // Configure UI services
                services.AddWinWorkUI();
                
                // Register main window
                services.AddTransient<MainWindow>();
                services.AddTransient<MainWindowViewModel>();
            })
            .Build();
    }

    /// <summary>
    /// Shows an error dialog with selectable and copyable text content
    /// </summary>
    public static void ShowErrorDialog(string title, string message, string details)
    {
        var errorWindow = new Window
        {
            Title = $"Error - {title}",
            Width = 700,
            Height = 500,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ResizeMode = ResizeMode.CanResize,
            ShowInTaskbar = false,
            Topmost = true,
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 48))
        };

        var grid = new System.Windows.Controls.Grid();
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

        // Error Icon and Message
        var headerPanel = new System.Windows.Controls.StackPanel 
        { 
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            Margin = new Thickness(15, 15, 15, 10)
        };
        
        var errorIcon = new System.Windows.Controls.TextBlock
        {
            Text = "⚠️",
            FontSize = 24,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0)
        };
        headerPanel.Children.Add(errorIcon);
        
        var messageTextBlock = new System.Windows.Controls.TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Foreground = System.Windows.Media.Brushes.White,
            FontWeight = FontWeights.Bold,
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Center
        };
        headerPanel.Children.Add(messageTextBlock);
        
        System.Windows.Controls.Grid.SetRow(headerPanel, 0);
        grid.Children.Add(headerPanel);

        // "Error Details" Label
        var detailsLabel = new System.Windows.Controls.TextBlock
        {
            Text = "Error Details (Click to select all, Ctrl+C to copy):",
            Foreground = System.Windows.Media.Brushes.LightGray,
            FontWeight = FontWeights.Medium,
            Margin = new Thickness(15, 5, 15, 5),
            FontSize = 12
        };
        System.Windows.Controls.Grid.SetRow(detailsLabel, 1);
        grid.Children.Add(detailsLabel);

        // Details in scrollable, selectable textbox
        var detailsTextBox = new System.Windows.Controls.TextBox
        {
            Text = $"Error: {message}\n\nDetails:\n{details}",
            Margin = new Thickness(15, 5, 15, 10),
            IsReadOnly = true,
            VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = new System.Windows.Media.FontFamily("Consolas, Courier New"),
            FontSize = 11,
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30)),
            Foreground = System.Windows.Media.Brushes.White,
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 100, 100)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8),
            AcceptsReturn = true,
            AcceptsTab = true
        };
        
        // Auto-select all text when clicked for easy copying
        detailsTextBox.GotFocus += (s, e) => detailsTextBox.SelectAll();
        detailsTextBox.MouseDoubleClick += (s, e) => detailsTextBox.SelectAll();
        
        System.Windows.Controls.Grid.SetRow(detailsTextBox, 2);
        grid.Children.Add(detailsTextBox);

        // Button panel
        var buttonPanel = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(15, 10, 15, 15)
        };
        
        // Copy button
        var copyButton = new System.Windows.Controls.Button
        {
            Content = "📋 Copy Error",
            Width = 100,
            Height = 32,
            Margin = new Thickness(0, 0, 10, 0),
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 122, 204)),
            Foreground = System.Windows.Media.Brushes.White,
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 100, 180)),
            BorderThickness = new Thickness(1),
            FontWeight = FontWeights.Medium
        };
        copyButton.Click += (s, e) => 
        {
            try 
            {
                System.Windows.Clipboard.SetText(detailsTextBox.Text);
                copyButton.Content = "✅ Copied!";
                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(2);
                timer.Tick += (sender, args) => 
                {
                    copyButton.Content = "📋 Copy Error";
                    timer.Stop();
                };
                timer.Start();
            }
            catch 
            {
                copyButton.Content = "❌ Failed";
            }
        };
        buttonPanel.Children.Add(copyButton);
        
        // OK button
        var okButton = new System.Windows.Controls.Button
        {
            Content = "OK",
            Width = 80,
            Height = 32,
            IsDefault = true,
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(70, 70, 70)),
            Foreground = System.Windows.Media.Brushes.White,
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 100, 100)),
            BorderThickness = new Thickness(1),
            FontWeight = FontWeights.Medium
        };
        okButton.Click += (s, e) => errorWindow.Close();
        buttonPanel.Children.Add(okButton);
        
        System.Windows.Controls.Grid.SetRow(buttonPanel, 3);
        grid.Children.Add(buttonPanel);

        errorWindow.Content = grid;
        errorWindow.ShowDialog();
    }
    
    /// <summary>
    /// Ensures the database has some seed data for testing and demo purposes
    /// </summary>
    private static async Task EnsureSeedDataAsync(WinWorkDbContext dbContext)
    {
        try
        {
            // Check if we already have data - if so, don't create seed data
            var existingCount = await dbContext.Links.CountAsync();
            // Debug: Found {existingCount} existing links
            
            if (existingCount > 0)
            {
                // Debug: Database already has data. Skipping seed data creation.
                return;
            }
            
            // Debug: "EnsureSeedDataAsync: Creating seed data..."

            // Create hierarchical seed data using Entity Framework with explicit relationships
            // Debug: "Creating hierarchical seed data using Entity Framework approach..."
            
            // Create root folders first
            var bookmarksFolder = new WinWork.Models.Link
            {
                Name = "📂 Bookmarks",
                Type = WinWork.Models.LinkType.Folder,
                ParentId = null,
                SortOrder = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var devToolsFolder = new WinWork.Models.Link
            {
                Name = "🔧 Development Tools",
                Type = WinWork.Models.LinkType.Folder,
                ParentId = null,
                SortOrder = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Add folders and save to get IDs
            dbContext.Links.AddRange(bookmarksFolder, devToolsFolder);
            await dbContext.SaveChangesAsync();
            
            // Debug: $"Created folders - Bookmarks ID: {bookmarksFolder.Id}, DevTools ID: {devToolsFolder.Id}"
            
            // Now create child links with proper ParentId
            var googleLink = new WinWork.Models.Link
            {
                Name = "🌐 Google",
                Url = "https://www.google.com",
                Description = "Google Search Engine",
                Type = WinWork.Models.LinkType.WebUrl,
                ParentId = bookmarksFolder.Id,
                SortOrder = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var githubLink = new WinWork.Models.Link
            {
                Name = "🐙 GitHub", 
                Url = "https://github.com",
                Description = "GitHub - Code Repository",
                Type = WinWork.Models.LinkType.WebUrl,
                ParentId = devToolsFolder.Id,
                SortOrder = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var vstudioLink = new WinWork.Models.Link
            {
                Name = "💻 Visual Studio Code",
                Url = "https://code.visualstudio.com", 
                Description = "VS Code - Free Code Editor",
                Type = WinWork.Models.LinkType.WebUrl,
                ParentId = devToolsFolder.Id,
                SortOrder = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var stackOverflowLink = new WinWork.Models.Link
            {
                Name = "🤔 Stack Overflow",
                Url = "https://stackoverflow.com",
                Description = "Programming Q&A Community",
                Type = WinWork.Models.LinkType.WebUrl,
                ParentId = null,
                SortOrder = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Add child links
            dbContext.Links.AddRange(googleLink, githubLink, vstudioLink, stackOverflowLink);
            await dbContext.SaveChangesAsync();
            
            // Debug: "Hierarchical seed data created with Entity Framework!"
            
            // Final verification using the folder objects
            var totalCount = await dbContext.Links.CountAsync();
            var bookmarkChildrenCount = await dbContext.Links.CountAsync(l => l.ParentId == bookmarksFolder.Id);
            var devToolChildrenCount = await dbContext.Links.CountAsync(l => l.ParentId == devToolsFolder.Id);
            var totalRootLinks = await dbContext.Links.CountAsync(l => l.ParentId == null);
            var totalChildLinks = await dbContext.Links.CountAsync(l => l.ParentId != null);
            
            // Debug: $"FINAL VERIFICATION:"
            // Debug: $"Total links: {totalCount}"
            Console.WriteLine($"Bookmarks folder (ID: {bookmarksFolder.Id}) has {bookmarkChildrenCount} children");
            Console.WriteLine($"DevTools folder (ID: {devToolsFolder.Id}) has {devToolChildrenCount} children");
            // Debug: $"Total root links: {totalRootLinks}, Total child links: {totalChildLinks}"
            // Debug: $"Google ParentId: {googleLink.ParentId}, GitHub ParentId: {githubLink.ParentId}"
        }
        catch (Exception ex)
        {
            // Ignore seed data errors silently
        }
    }
}

