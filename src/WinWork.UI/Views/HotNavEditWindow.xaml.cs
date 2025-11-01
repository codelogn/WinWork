using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Microsoft.VisualBasic;
using WinWork.Models;

namespace WinWork.UI.Views;

public class HotNavRootRow : INotifyPropertyChanged, INotifyDataErrorInfo
{
    private string _path = string.Empty;
    public int Id { get; set; }
    public int HotNavId { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string Path
    {
        get => _path;
        set
        {
            if (_path != value)
            {
                _path = value;
                Validate();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Path)));
            }
        }
    }

    private readonly Dictionary<string, List<string>> _errors = new();

    public bool HasErrors => _errors.Any();

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public System.Collections.IEnumerable GetErrors(string? propertyName)
    {
        if (propertyName == null) return _errors.SelectMany(kv => kv.Value);
        return _errors.TryGetValue(propertyName, out var list) ? list : Array.Empty<string>();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Validate()
    {
        _errors.Clear();
        if (string.IsNullOrWhiteSpace(Path))
        {
            _errors[nameof(Path)] = new List<string> { "Path cannot be empty" };
        }
        else if (!System.IO.Directory.Exists(Path))
        {
            _errors[nameof(Path)] = new List<string> { "Directory does not exist" };
        }
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Path)));
    }
}

public partial class HotNavEditWindow : Window
{
    public HotNav HotNav { get; private set; }
    private List<HotNavRootRow> _rows = new();

    // Safe accessors for named controls (use FindName so file can be edited without generated fields)
    private System.Windows.Controls.DataGrid? RootsDataGridCtrl => this.FindName("RootsDataGrid") as System.Windows.Controls.DataGrid;
    private System.Windows.Controls.TextBox? NameTextBoxCtrl => this.FindName("NameTextBox") as System.Windows.Controls.TextBox;
    private System.Windows.Controls.CheckBox? IncludeFilesCheckBoxCtrl => this.FindName("IncludeFilesCheckBox") as System.Windows.Controls.CheckBox;
    private System.Windows.Controls.TextBox? MaxDepthTextBoxCtrl => this.FindName("MaxDepthTextBox") as System.Windows.Controls.TextBox;

    public HotNavEditWindow(HotNav hotNav)
    {
        InitializeComponent();
        HotNav = hotNav ?? throw new ArgumentNullException(nameof(hotNav));
        Loaded += HotNavEditWindow_Loaded;
    }

    private void HotNavEditWindow_Loaded(object sender, RoutedEventArgs e)
    {
    if (NameTextBoxCtrl != null) NameTextBoxCtrl.Text = HotNav.Name ?? string.Empty;
    if (IncludeFilesCheckBoxCtrl != null) IncludeFilesCheckBoxCtrl.IsChecked = HotNav.IncludeFiles;
    if (MaxDepthTextBoxCtrl != null) MaxDepthTextBoxCtrl.Text = HotNav.MaxDepth?.ToString() ?? string.Empty;

        _rows = (HotNav.Roots?.OrderBy(r => r.SortOrder).Select(r => new HotNavRootRow
        {
            Id = r.Id,
            HotNavId = r.HotNavId,
            Path = r.Path,
            SortOrder = r.SortOrder,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        }).ToList()) ?? new List<HotNavRootRow>();

        // ensure validation on load
        foreach (var row in _rows) row.Path = row.Path;

        if (RootsDataGridCtrl != null) RootsDataGridCtrl.ItemsSource = _rows;
    }

    private void AddRoot_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new System.Windows.Forms.FolderBrowserDialog();
        var res = dlg.ShowDialog();
        if (res == System.Windows.Forms.DialogResult.OK)
        {
            var max = _rows.Any() ? _rows.Max(r => r.SortOrder) : 0;
            var newRow = new HotNavRootRow { Path = dlg.SelectedPath, SortOrder = max + 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            _rows.Add(newRow);
            RefreshGrid();
            if (RootsDataGridCtrl != null) RootsDataGridCtrl.SelectedItem = newRow;
        }
    }

    private void EditRoot_Click(object sender, RoutedEventArgs e)
    {
        if (RootsDataGridCtrl != null && RootsDataGridCtrl.SelectedItem is HotNavRootRow sel)
        {
            var newPath = Interaction.InputBox("Edit root path:", "Edit Root", sel.Path);
            if (!string.IsNullOrWhiteSpace(newPath) && !string.Equals(newPath, sel.Path, StringComparison.OrdinalIgnoreCase))
            {
                sel.Path = newPath;
                sel.UpdatedAt = DateTime.UtcNow;
                RefreshGrid();
                RootsDataGridCtrl.SelectedItem = sel;
            }
        }
    }

    private void BrowseRoot_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Find the data context (HotNavRootRow) for the button that was clicked
            if (sender is System.Windows.Controls.Button btn)
            {
                if (btn.DataContext is HotNavRootRow row)
                {
                    var dlg = new System.Windows.Forms.FolderBrowserDialog();
                    var res = dlg.ShowDialog();
                    if (res == System.Windows.Forms.DialogResult.OK)
                    {
                        row.Path = dlg.SelectedPath;
                        row.UpdatedAt = DateTime.UtcNow;
                        RefreshGrid();
                        if (RootsDataGridCtrl != null) RootsDataGridCtrl.SelectedItem = row;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to pick folder: {ex.Message}");
        }
    }

    private void MoveUp_Click(object sender, RoutedEventArgs e)
    {
        if (RootsDataGridCtrl != null && RootsDataGridCtrl.SelectedItem is HotNavRootRow sel)
        {
            var idx = _rows.IndexOf(sel);
            if (idx > 0)
            {
                _rows.RemoveAt(idx);
                _rows.Insert(idx - 1, sel);
                ReassignOrders();
                RefreshGrid();
                RootsDataGridCtrl.SelectedItem = sel;
            }
        }
    }

    private void MoveDown_Click(object sender, RoutedEventArgs e)
    {
        if (RootsDataGridCtrl != null && RootsDataGridCtrl.SelectedItem is HotNavRootRow sel)
        {
            var idx = _rows.IndexOf(sel);
            if (idx >= 0 && idx < _rows.Count - 1)
            {
                _rows.RemoveAt(idx);
                _rows.Insert(idx + 1, sel);
                ReassignOrders();
                RefreshGrid();
                RootsDataGridCtrl.SelectedItem = sel;
            }
        }
    }

    private void RemoveRoot_Click(object sender, RoutedEventArgs e)
    {
        if (RootsDataGridCtrl != null && RootsDataGridCtrl.SelectedItem is HotNavRootRow sel)
        {
            _rows.Remove(sel);
            ReassignOrders();
            RefreshGrid();
        }
    }

    private void ReassignOrders()
    {
        for (int i = 0; i < _rows.Count; i++) _rows[i].SortOrder = i + 1;
    }

    private void RefreshGrid()
    {
        if (RootsDataGridCtrl != null)
        {
            RootsDataGridCtrl.ItemsSource = null;
            RootsDataGridCtrl.ItemsSource = _rows;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    // Allow dragging the window by the custom title bar
    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        try { this.DragMove(); } catch { }
    }

    // Close button behavior maps to cancel
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Cancel_Click(sender, e);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        // commit any pending edits in the DataGrid
    RootsDataGridCtrl?.CommitEdit();
    RootsDataGridCtrl?.CommitEdit();

        // validate rows
        var invalid = _rows.FirstOrDefault(r => r.HasErrors);
        if (invalid != null)
        {
            MessageBox.Show($"Cannot save; invalid root: {invalid.Path}", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (NameTextBoxCtrl != null) HotNav.Name = NameTextBoxCtrl.Text?.Trim() ?? HotNav.Name;
        if (IncludeFilesCheckBoxCtrl != null) HotNav.IncludeFiles = IncludeFilesCheckBoxCtrl.IsChecked == true;
        if (MaxDepthTextBoxCtrl != null && int.TryParse(MaxDepthTextBoxCtrl.Text, out var md))
            HotNav.MaxDepth = md;
        else
            HotNav.MaxDepth = null;

        // copy rows back to HotNav.Roots
        HotNav.Roots = _rows.Select(r => new HotNavRoot
        {
            Id = r.Id,
            HotNavId = r.HotNavId,
            Path = r.Path,
            SortOrder = r.SortOrder,
            CreatedAt = r.CreatedAt == default ? DateTime.UtcNow : r.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        }).ToList();

        DialogResult = true;
        Close();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (this.WindowState == WindowState.Maximized)
        {
            this.WindowState = WindowState.Normal;
            if (sender is System.Windows.Controls.Button btn)
                btn.Tag = "🗖";
        }
        else
        {
            this.WindowState = WindowState.Maximized;
            if (sender is System.Windows.Controls.Button btn)
                btn.Tag = "🗗";
        }
    }
}

