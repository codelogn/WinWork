using System.Drawing;
using System.Windows.Forms;
using System.Drawing;
using System.Windows.Forms;
using WinWork.Core.Interfaces;
using WinWork.Models;

namespace WinWork.Core.Services;

public class HotNavsTrayService : IDisposable
{
    private readonly IHotNavService _hotNavService;
    private NotifyIcon? _notifyIcon;
    private Action? _openManager;

    public HotNavsTrayService(IHotNavService hotNavService)
    {
        _hotNavService = hotNavService;
    }

    /// <summary>
    /// Initialize the tray icon. Provide an optional action to open the manager window (UI responsibility).
    /// </summary>
    public async Task InitializeAsync(Icon icon, Action? openManager = null)
    {
        if (_notifyIcon != null)
            return;

        _openManager = openManager;

        _notifyIcon = new NotifyIcon
        {
            Icon = icon,
            Text = "Hot Navs",
            Visible = true
        };

        _notifyIcon.MouseClick += async (s, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                await ShowMenuAsync();
            }
            else if (e.Button == MouseButtons.Right)
            {
                ShowContextMenu();
            }
        };
    }

    private async Task ShowMenuAsync()
    {
        if (_notifyIcon == null)
            return;

        var menu = new ContextMenuStrip();
        var hotnavs = await _hotNavService.GetAllAsync();
        foreach (var hv in hotnavs)
        {
            var hvItem = new ToolStripMenuItem(hv.Name);
            hvItem.Tag = hv;

            // When clicked, build a submenu of roots and lazy-expand into directory tree
            hvItem.DropDownOpening += async (s, e) =>
            {
                if (hvItem.DropDownItems.Count == 0)
                {
                    // Populate roots
                    foreach (var root in hv.Roots.OrderBy(r => r.SortOrder))
                    {
                        var rootItem = await CreatePathMenuItemAsync(root.Path, hv.IncludeFiles, hv.MaxDepth ?? 16);
                        hvItem.DropDownItems.Add(rootItem);
                    }
                }
            };

            // Provide a placeholder click to open first root if any
            hvItem.Click += (s, e) =>
            {
                var first = hv.Roots.OrderBy(r => r.SortOrder).FirstOrDefault();
                if (first != null)
                {
                    try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(first.Path) { UseShellExecute = true }); } catch { }
                }
            };

            menu.Items.Add(hvItem);
        }

        menu.Items.Add(new ToolStripSeparator());
        var settingsItem = new ToolStripMenuItem("Hot Navs Manager");
        settingsItem.Click += (s, e) => { _openManager?.Invoke(); };
        menu.Items.Add(settingsItem);

        var exitItem = new ToolStripMenuItem("Exit Hot Navs");
        exitItem.Click += (s, e) => { _notifyIcon?.Dispose(); };
        menu.Items.Add(exitItem);

        // Show menu next to tray icon
        var pos = Cursor.Position;
        menu.Show(pos);
    }

    private void ShowContextMenu()
    {
        _ = ShowMenuAsync();
    }

    private async Task<ToolStripMenuItem> CreatePathMenuItemAsync(string path, bool includeFiles, int maxDepth)
    {
        var item = new ToolStripMenuItem(Path.GetFileName(path) != string.Empty ? Path.GetFileName(path) : path);
        try
        {
            if (!Directory.Exists(path))
            {
                item.Enabled = false;
                item.ToolTipText = "Path not found or inaccessible";
                return item;
            }

            // Add 'Open in Explorer' at top
            var openItem = new ToolStripMenuItem("Open in Explorer");
            openItem.Click += (s, e) => { try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true }); } catch { } };
            item.DropDownItems.Add(openItem);
            item.DropDownItems.Add(new ToolStripSeparator());

            // Enumerate children lazily: add placeholder items that populate on DropDownOpening
            var dirs = Directory.EnumerateDirectories(path).OrderBy(d => d).ToList();
            var files = includeFiles ? Directory.EnumerateFiles(path).OrderBy(f => f).ToList() : new List<string>();

            foreach (var d in dirs)
            {
                var sub = new ToolStripMenuItem(Path.GetFileName(d)) { Tag = new { Path = d, Depth = 1 } };
                sub.DropDownOpening += async (s, e) => await PopulateSubMenuAsync(sub, d, includeFiles, 1, maxDepth);
                item.DropDownItems.Add(sub);
            }

            if (includeFiles)
            {
                foreach (var f in files)
                {
                    var fileItem = new ToolStripMenuItem(Path.GetFileName(f));
                    fileItem.Click += (s, e) => { try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(f) { UseShellExecute = true }); } catch { } };
                    item.DropDownItems.Add(fileItem);
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            item.Enabled = false;
            item.ToolTipText = "Access denied";
        }
        catch
        {
            item.Enabled = false;
        }

        return item;
    }

    private async Task PopulateSubMenuAsync(ToolStripMenuItem menuItem, string path, bool includeFiles, int currentDepth, int maxDepth)
    {
        try
        {
            if (!Directory.Exists(path))
                return;

            // If already populated, skip
            if (menuItem.DropDownItems.Count > 0)
                return;

            if (currentDepth >= maxDepth)
            {
                var open = new ToolStripMenuItem("Open in Explorer");
                open.Click += (s, e) => { try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true }); } catch { } };
                menuItem.DropDownItems.Add(open);
                return;
            }

            var dirs = Directory.EnumerateDirectories(path).OrderBy(d => d).ToList();
            var files = includeFiles ? Directory.EnumerateFiles(path).OrderBy(f => f).ToList() : new List<string>();

            foreach (var d in dirs)
            {
                var child = new ToolStripMenuItem(Path.GetFileName(d));
                child.DropDownOpening += async (s, e) => await PopulateSubMenuAsync(child, d, includeFiles, currentDepth + 1, maxDepth);
                menuItem.DropDownItems.Add(child);
            }

            if (includeFiles)
            {
                foreach (var f in files)
                {
                    var fi = new ToolStripMenuItem(Path.GetFileName(f));
                    fi.Click += (s, e) => { try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(f) { UseShellExecute = true }); } catch { } };
                    menuItem.DropDownItems.Add(fi);
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            menuItem.Enabled = false;
            menuItem.ToolTipText = "Access denied";
        }
        catch { menuItem.Enabled = false; }
    }

    public void Dispose()
    {
        _notifyIcon?.Dispose();
        _notifyIcon = null;
    }
}
