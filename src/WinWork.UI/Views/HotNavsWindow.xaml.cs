using System.Windows;
using WinWork.Core.Interfaces;
using WinWork.Models;
using System.IO;
using Microsoft.VisualBasic;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace WinWork.UI.Views;

public partial class HotNavsWindow : Window
{
    private readonly IHotNavService _hotNavService;

    public HotNavsWindow(IHotNavService hotNavService)
    {
        InitializeComponent();
        _hotNavService = hotNavService;
        Loaded += HotNavsWindow_Loaded;
    }

    private async void HotNavsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Ensure database schema is present for HotNavs. Some older databases may
            // not have the new tables yet; attempt to apply migrations as a safe,
            // non-destructive fallback before querying to avoid the "no such table"
            // SqliteException that crashes the window.
            try
            {
                var services = (App.Current as App)?.Services;
                var db = services?.GetService(typeof(WinWork.Data.WinWorkDbContext)) as WinWork.Data.WinWorkDbContext;
                if (db != null)
                {
                    // Quick check: is the HotNavs table present? Query sqlite_master.
                    try
                    {
                        var conn = db.Database.GetDbConnection();
                        await conn.OpenAsync();
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='HotNavs';";
                            var result = await cmd.ExecuteScalarAsync();
                            if (result == null)
                            {
                                // Table missing; first try to apply migrations (preferred)
                                try
                                {
                                    await db.Database.MigrateAsync();
                                }
                                catch (Exception migEx)
                                {
                                    System.Diagnostics.Debug.WriteLine($"HotNavsWindow: migration attempt failed: {migEx}");
                                }

                                // Re-check; if still missing, create tables non-destructively as a fallback.
                                try
                                {
                                    using (var checkCmd = conn.CreateCommand())
                                    {
                                        checkCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='HotNavs';";
                                        var recheck = await checkCmd.ExecuteScalarAsync();
                                        if (recheck == null)
                                        {
                                            System.Diagnostics.Debug.WriteLine("HotNavsWindow: HotNavs table still missing after migrations; creating tables via SQL fallback.");
                                            using (var createCmd = conn.CreateCommand())
                                            {
                                                createCmd.CommandText = @"CREATE TABLE IF NOT EXISTS ""HotNavs"" (
    ""Id"" INTEGER PRIMARY KEY AUTOINCREMENT,
    ""Name"" TEXT NOT NULL,
    ""IncludeFiles"" INTEGER NOT NULL,
    ""MaxDepth"" INTEGER,
    ""SortOrder"" INTEGER NOT NULL,
    ""CreatedAt"" TEXT NOT NULL,
    ""UpdatedAt"" TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS ""HotNavRoots"" (
    ""Id"" INTEGER PRIMARY KEY AUTOINCREMENT,
    ""HotNavId"" INTEGER NOT NULL,
    ""Path"" TEXT NOT NULL,
    ""SortOrder"" INTEGER NOT NULL,
    ""CreatedAt"" TEXT NOT NULL,
    ""UpdatedAt"" TEXT NOT NULL,
    FOREIGN KEY(""HotNavId"") REFERENCES ""HotNavs""(""Id"") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS ""IX_HotNavRoots_HotNavId"" ON ""HotNavRoots""(""HotNavId"");";
                                                await createCmd.ExecuteNonQueryAsync();
                                            }
                                        }
                                    }
                                }
                                catch (Exception createEx)
                                {
                                    System.Diagnostics.Debug.WriteLine($"HotNavsWindow: SQL fallback create failed: {createEx}");
                                }
                            }
                        }
                    }
                    catch (Exception checkEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"HotNavsWindow: schema check failed: {checkEx}");
                    }
                    finally
                    {
                        try { await db.Database.CloseConnectionAsync(); } catch { }
                    }
                }
            }
            catch { }

            // Load HotNavs; wrap in try/catch to surface any DB or service errors
            var items = await _hotNavService.GetAllAsync();
            HotNavsListView.ItemsSource = items;
        }
        catch (Exception ex)
        {
            // Log and show an error dialog so the user can see what went wrong instead of the UI freezing silently
            try { System.Diagnostics.Debug.WriteLine($"HotNavsWindow_Loaded error: {ex}"); } catch { }
            try { App.ShowErrorDialog("HotNavs Load Error", "Failed to load Hot Navs. See details.", ex.ToString()); } catch { }
        }
    }

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dlg = new System.Windows.Forms.FolderBrowserDialog();
            var res = dlg.ShowDialog();
            if (res == System.Windows.Forms.DialogResult.OK)
            {
                var name = Interaction.InputBox("Enter name for Hot Nav:", "New Hot Nav", Path.GetFileName(dlg.SelectedPath));
                if (string.IsNullOrWhiteSpace(name)) name = Path.GetFileName(dlg.SelectedPath);

                var hv = new HotNav { Name = name, IncludeFiles = true };
                var created = await _hotNavService.CreateAsync(hv);

                // Add root via DbContext
                var services = (App.Current as App)?.Services;
                var db = services?.GetService(typeof(WinWork.Data.WinWorkDbContext)) as WinWork.Data.WinWorkDbContext;
                if (db != null)
                {
                    var ctx = new WinWork.Models.HotNavRoot { HotNavId = created.Id, Path = dlg.SelectedPath, SortOrder = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                    db.Set<WinWork.Models.HotNavRoot>().Add(ctx);
                    await db.SaveChangesAsync();
                }

                // Refresh list
                var items = await _hotNavService.GetAllAsync();
                HotNavsListView.ItemsSource = items;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to create Hot Nav: {ex.Message}");
        }
    }

    private async void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (HotNavsListView.SelectedItem is HotNav hv)
        {
            // create a deep copy to edit
                var editModel = new HotNav
            {
                Id = hv.Id,
                Name = hv.Name,
                IncludeFiles = hv.IncludeFiles,
                MaxDepth = hv.MaxDepth,
                SortOrder = hv.SortOrder,
                CreatedAt = hv.CreatedAt,
                UpdatedAt = hv.UpdatedAt,
                    Roots = hv.Roots?.Select(r => new HotNavRoot { Id = r.Id, HotNavId = r.HotNavId, Path = r.Path, SortOrder = r.SortOrder, CreatedAt = r.CreatedAt, UpdatedAt = r.UpdatedAt }).ToList() ?? new List<HotNavRoot>()
            };

            var editor = new HotNavEditWindow(editModel) { Owner = this };
            var res = editor.ShowDialog();
            if (res == true)
            {
                // persist HotNav properties
                var updated = await _hotNavService.UpdateAsync(editModel);

                // persist roots via DbContext - handle add/update/delete
                var services = (App.Current as App)?.Services;
                var db = services?.GetService(typeof(WinWork.Data.WinWorkDbContext)) as WinWork.Data.WinWorkDbContext;
                if (db != null)
                {
                    // load existing roots
                    var existing = db.Set<WinWork.Models.HotNavRoot>().Where(r => r.HotNavId == updated.Id).ToList();

                    // remove roots not present
                    var keepPaths = editModel.Roots?.Select(r => r.Path).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var ex in existing)
                    {
                        if (!keepPaths.Contains(ex.Path))
                            db.Set<WinWork.Models.HotNavRoot>().Remove(ex);
                    }

                    // add or update roots from model
                    var order = 1;
                    foreach (var r in editModel.Roots ?? Enumerable.Empty<HotNavRoot>())
                    {
                        var found = existing.FirstOrDefault(x => string.Equals(x.Path, r.Path, StringComparison.OrdinalIgnoreCase));
                        if (found == null)
                        {
                            var add = new WinWork.Models.HotNavRoot { HotNavId = updated.Id, Path = r.Path, SortOrder = order, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                            db.Set<WinWork.Models.HotNavRoot>().Add(add);
                        }
                        else
                        {
                            found.SortOrder = order;
                            found.UpdatedAt = DateTime.UtcNow;
                            db.Set<WinWork.Models.HotNavRoot>().Update(found);
                        }
                        order++;
                    }

                    await db.SaveChangesAsync();
                }

                // refresh list
                var items = await _hotNavService.GetAllAsync();
                HotNavsListView.ItemsSource = items;
            }
        }
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (HotNavsListView.SelectedItem is HotNav hv)
        {
            var ok = MessageBox.Show($"Delete '{hv.Name}'?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes;
            if (ok)
            {
                await _hotNavService.DeleteAsync(hv.Id);
                var items = await _hotNavService.GetAllAsync();
                HotNavsListView.ItemsSource = items;
            }
        }
    }

    // Allow dragging the window by the custom title bar
    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        try { this.DragMove(); } catch { }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
