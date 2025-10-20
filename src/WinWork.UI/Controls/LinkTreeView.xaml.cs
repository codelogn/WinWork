using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WinWork.Models;
using WinWork.UI.ViewModels;

namespace WinWork.UI.Controls;

/// <summary>
/// Interactive tree view control for hierarchical link management
/// </summary>
public partial class LinkTreeView : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty = 
        DependencyProperty.Register(nameof(ItemsSource), typeof(ObservableCollection<LinkTreeItemViewModel>), 
            typeof(LinkTreeView), new PropertyMetadata(null));

    public ObservableCollection<LinkTreeItemViewModel> ItemsSource
    {
        get => (ObservableCollection<LinkTreeItemViewModel>)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(nameof(SelectedItem), typeof(LinkTreeItemViewModel),
            typeof(LinkTreeView), new PropertyMetadata(null));

    public LinkTreeItemViewModel? SelectedItem
    {
        get => (LinkTreeItemViewModel?)GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    // Events
    public event EventHandler<LinkTreeItemViewModel>? ItemDoubleClicked;
    public event EventHandler<LinkTreeItemViewModel>? ItemRightClicked;
    public event EventHandler<DragDropEventArgs>? ItemDropped;

    public LinkTreeView()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void TreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (TreeView.SelectedItem is LinkTreeItemViewModel item)
        {
            ItemDoubleClicked?.Invoke(this, item);
        }
    }

    private void TreeView_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (TreeView.SelectedItem is LinkTreeItemViewModel item)
        {
            ItemRightClicked?.Invoke(this, item);
        }
    }

    private void TreeView_Drop(object sender, DragEventArgs e)
    {
        var dropArgs = new DragDropEventArgs
        {
            DropData = e.Data,
            TargetItem = TreeView.SelectedItem as LinkTreeItemViewModel
        };
        ItemDropped?.Invoke(this, dropArgs);
    }

    private void TreeView_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = DragDropEffects.Move;
    }

    private void TreeViewItem_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && sender is TreeViewItem item)
        {
            if (item.DataContext is LinkTreeItemViewModel linkItem)
            {
                DragDrop.DoDragDrop(item, linkItem, DragDropEffects.Move);
            }
        }
    }

    private void AddLink_Click(object sender, RoutedEventArgs e)
    {
        // Find the MainWindowViewModel from the parent window
        var mainWindow = Window.GetWindow(this);
        if (mainWindow?.DataContext is MainWindowViewModel mainViewModel)
        {
            mainViewModel.AddLinkCommand?.Execute(null);
        }
    }

    private void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        // Find the MainWindowViewModel from the parent window
        var mainWindow = Window.GetWindow(this);
        if (mainWindow?.DataContext is MainWindowViewModel mainViewModel)
        {
            mainViewModel.AddFolderCommand?.Execute(null);
        }
    }
}

public class DragDropEventArgs : EventArgs
{
    public IDataObject? DropData { get; set; }
    public LinkTreeItemViewModel? TargetItem { get; set; }
}
