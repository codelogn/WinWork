using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using LinkerApp.Data;
using LinkerApp.Core;
using LinkerApp.UI.Services;
using LinkerApp.UI.ViewModels;
using LinkerApp.Core.Interfaces;
using System.Runtime.InteropServices;

namespace LinkerApp.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool AllocConsole();

    protected override async void OnStartup(StartupEventArgs e)
    {
        try
        {
            // Allocate a console window for debugging
            AllocConsole();
            Console.WriteLine("LinkerApp Console Debug Output");
            Console.WriteLine("==============================");
            // Create and configure host
            _host = CreateHost();
            
            // Start the host
            await _host.StartAsync();

            // Initialize database synchronously before showing UI
            using (var scope = _host.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<LinkerAppDbContext>();
                await DatabaseConfiguration.InitializeDatabaseAsync(dbContext);
                
                // Ensure we have seed data for testing
                await EnsureSeedDataAsync(dbContext);
            }

            // Show main window after database is ready
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

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
                services.AddLinkerAppDatabase();
                
                // Configure core services  
                services.AddLinkerAppCore();
                
                // Configure UI services
                services.AddLinkerAppUI();
                
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
    private static async Task EnsureSeedDataAsync(LinkerAppDbContext dbContext)
    {
        try
        {
            // FORCE COMPLETE DATABASE RESET - Remove all existing data and recreate with hierarchical structure
            Console.WriteLine("EnsureSeedDataAsync: FORCE CLEARING ALL DATA TO CREATE HIERARCHICAL STRUCTURE");
            
            // Delete all existing links to start fresh
            var allLinks = await dbContext.Links.ToListAsync();
            if (allLinks.Any())
            {
                Console.WriteLine($"EnsureSeedDataAsync: Removing {allLinks.Count} existing links");
                dbContext.Links.RemoveRange(allLinks);
                await dbContext.SaveChangesAsync();
            }
            
            var remainingCount = await dbContext.Links.CountAsync();
            Console.WriteLine($"EnsureSeedDataAsync: After cleanup, {remainingCount} links remain");
            
            Console.WriteLine("EnsureSeedDataAsync: Creating seed data...");

            // Create hierarchical seed data using Entity Framework with explicit relationships
            Console.WriteLine("Creating hierarchical seed data using Entity Framework approach...");
            
            // Create root folders first
            var bookmarksFolder = new LinkerApp.Models.Link
            {
                Name = "📁 Bookmarks",
                Type = LinkerApp.Models.LinkType.Folder,
                ParentId = null,
                SortOrder = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var devToolsFolder = new LinkerApp.Models.Link
            {
                Name = "📁 Development Tools",
                Type = LinkerApp.Models.LinkType.Folder,
                ParentId = null,
                SortOrder = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Add folders and save to get IDs
            dbContext.Links.AddRange(bookmarksFolder, devToolsFolder);
            await dbContext.SaveChangesAsync();
            
            Console.WriteLine($"Created folders - Bookmarks ID: {bookmarksFolder.Id}, DevTools ID: {devToolsFolder.Id}");
            
            // Now create child links with proper ParentId
            var googleLink = new LinkerApp.Models.Link
            {
                Name = "🔗 Google",
                Url = "https://www.google.com",
                Description = "Google Search Engine",
                Type = LinkerApp.Models.LinkType.WebUrl,
                ParentId = bookmarksFolder.Id,
                SortOrder = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var githubLink = new LinkerApp.Models.Link
            {
                Name = "🔗 GitHub", 
                Url = "https://github.com",
                Description = "GitHub - Code Repository",
                Type = LinkerApp.Models.LinkType.WebUrl,
                ParentId = devToolsFolder.Id,
                SortOrder = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var vstudioLink = new LinkerApp.Models.Link
            {
                Name = "🔗 Visual Studio Code",
                Url = "https://code.visualstudio.com", 
                Description = "VS Code - Free Code Editor",
                Type = LinkerApp.Models.LinkType.WebUrl,
                ParentId = devToolsFolder.Id,
                SortOrder = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var stackOverflowLink = new LinkerApp.Models.Link
            {
                Name = "🔗 Stack Overflow",
                Url = "https://stackoverflow.com",
                Description = "Programming Q&A Community",
                Type = LinkerApp.Models.LinkType.WebUrl,
                ParentId = null,
                SortOrder = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Add child links
            dbContext.Links.AddRange(googleLink, githubLink, vstudioLink, stackOverflowLink);
            await dbContext.SaveChangesAsync();
            
            Console.WriteLine("Hierarchical seed data created with Entity Framework!");
            
            // Final verification using the folder objects
            var totalCount = await dbContext.Links.CountAsync();
            var bookmarkChildrenCount = await dbContext.Links.CountAsync(l => l.ParentId == bookmarksFolder.Id);
            var devToolChildrenCount = await dbContext.Links.CountAsync(l => l.ParentId == devToolsFolder.Id);
            var totalRootLinks = await dbContext.Links.CountAsync(l => l.ParentId == null);
            var totalChildLinks = await dbContext.Links.CountAsync(l => l.ParentId != null);
            
            Console.WriteLine($"FINAL VERIFICATION:");
            Console.WriteLine($"Total links: {totalCount}");
            Console.WriteLine($"Bookmarks folder (ID: {bookmarksFolder.Id}) has {bookmarkChildrenCount} children");
            Console.WriteLine($"DevTools folder (ID: {devToolsFolder.Id}) has {devToolChildrenCount} children");
            Console.WriteLine($"Total root links: {totalRootLinks}, Total child links: {totalChildLinks}");
            Console.WriteLine($"Google ParentId: {googleLink.ParentId}, GitHub ParentId: {githubLink.ParentId}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating seed data: {ex.Message}");
        }
    }
}

