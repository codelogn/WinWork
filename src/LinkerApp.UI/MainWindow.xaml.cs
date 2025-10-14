using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using LinkerApp.UI.ViewModels;
using LinkerApp.UI.Controls;
using LinkerApp.UI.Views;
using LinkerApp.Core.Interfaces;
using LinkerApp.Models;
using System.ComponentModel;
using System.Drawing;

namespace LinkerApp.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        // Subscribe to dialog requests
        viewModel.LinkDialogRequested += OnLinkDialogRequested;
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

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized 
            ? WindowState.Normal 
            : WindowState.Maximized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    #endregion

    #region Action Handlers

    private void AddLink_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.AddLinkCommand.Execute(null);
        }
    }

    private void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("AddFolder_Click triggered");
        if (DataContext is MainWindowViewModel viewModel)
        {
            System.Diagnostics.Debug.WriteLine("ViewModel found, executing command");
            viewModel.AddFolderCommand.Execute(null);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("ViewModel not found!");
        }
    }

    #endregion

    #region Tree View Event Handlers

    private async void LinkTree_ItemDoubleClicked(object sender, LinkTreeItemViewModel e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.HandleItemDoubleClick(e);
        }
    }

    private void LinkTree_ItemRightClicked(object sender, LinkTreeItemViewModel e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SelectedLink = e;
        }
    }

    private async void LinkTree_ItemDropped(object sender, Controls.DragDropEventArgs e)
    {
        // Handle drag and drop operations
        if (e.DropData?.GetDataPresent(typeof(LinkTreeItemViewModel)) == true)
        {
            var draggedItem = e.DropData.GetData(typeof(LinkTreeItemViewModel)) as LinkTreeItemViewModel;
            if (draggedItem != null && DataContext is MainWindowViewModel viewModel)
            {
                // TODO: Implement drag and drop logic
            }
        }
    }

    #endregion

    #region Dialog Management

    private async void OnLinkDialogRequested(object? sender, LinkDialogRequestEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel) return;

        try
        {
            var dialogViewModel = new LinkDialogViewModel();
            
            // Load available tags
            var tags = await viewModel.GetAllTagsAsync();
            dialogViewModel.LoadAvailableTags(tags);

            // Set edit mode if editing existing link
            if (e.LinkToEdit != null)
            {
                dialogViewModel.SetEditMode(e.LinkToEdit);
            }
            // Set initial type if specified
            else if (e.InitialType.HasValue)
            {
                dialogViewModel.SetInitialType(e.InitialType.Value);
            }
            
            // Set parent context if provided
            if (e.ParentItem != null)
            {
                dialogViewModel.SetParentContext(e.ParentItem);
            }
            
            // Set available parents (all folders)
            var allFolders = GetAllFolders(viewModel.RootLinks);
            dialogViewModel.SetAvailableParents(allFolders);

            var dialog = new LinkDialog(dialogViewModel)
            {
                Owner = this
            };

            // Subscribe to the save event
            LinkSaveEventArgs? saveArgs = null;
            dialogViewModel.LinkSaved += (s, e) => saveArgs = e;

            if (dialog.ShowDialog() == true && saveArgs != null)
            {
                // Handle successful save
                await viewModel.HandleLinkSaved(saveArgs.Link, saveArgs.SelectedTagIds, saveArgs.IsEditMode);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening link dialog: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region Tag Management

    private async void ManageTags_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel) return;

        try
        {
            var tagViewModel = new TagManagementViewModel();
            
            // Load existing tags
            var tags = await viewModel.GetAllTagsAsync();
            tagViewModel.LoadTags(tags);

            var dialog = new Views.TagManagementDialog(tagViewModel)
            {
                Owner = this
            };

            // Subscribe to tag events
            tagViewModel.TagAdded += async (s, args) => await HandleTagAdded(args, viewModel);
            tagViewModel.TagUpdated += async (s, args) => await HandleTagUpdated(args, viewModel);
            tagViewModel.TagDeleted += async (s, args) => await HandleTagDeleted(args, viewModel);

            dialog.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening tag management: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task HandleTagAdded(TagEventArgs args, MainWindowViewModel viewModel)
    {
        try
        {
            var createdTag = await viewModel.CreateTagAsync(args.Tag);
            // Show success message
            if (DataContext is MainWindowViewModel mainViewModel)
            {
                mainViewModel.DisplaySuccessMessage($"Tag '{args.Tag.Name}' created successfully!");
            }
        }
        catch (Exception ex)
        {
            if (DataContext is MainWindowViewModel mainViewModel)
            {
                mainViewModel.DisplayErrorMessage($"Failed to create tag: {ex.Message}");
            }
        }
    }

    private async Task HandleTagUpdated(TagEventArgs args, MainWindowViewModel viewModel)
    {
        try
        {
            await viewModel.UpdateTagAsync(args.Tag);
            // Show success message
            if (DataContext is MainWindowViewModel mainViewModel)
            {
                mainViewModel.DisplaySuccessMessage($"Tag '{args.Tag.Name}' updated successfully!");
            }
        }
        catch (Exception ex)
        {
            if (DataContext is MainWindowViewModel mainViewModel)
            {
                mainViewModel.DisplayErrorMessage($"Failed to update tag: {ex.Message}");
            }
        }
    }

    private async Task HandleTagDeleted(TagEventArgs args, MainWindowViewModel viewModel)
    {
        try
        {
            var result = MessageBox.Show($"Are you sure you want to delete the tag '{args.Tag.Name}'?", 
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                await viewModel.DeleteTagAsync(args.Tag.Id);
                // Show success message
                if (DataContext is MainWindowViewModel mainViewModel)
                {
                    mainViewModel.DisplaySuccessMessage($"Tag '{args.Tag.Name}' deleted successfully!");
                }
            }
        }
        catch (Exception ex)
        {
            if (DataContext is MainWindowViewModel mainViewModel)
            {
                mainViewModel.DisplayErrorMessage($"Failed to delete tag: {ex.Message}");
            }
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.LoadLinksAsync();
            viewModel.DisplaySuccessMessage("Data refreshed successfully!");
        }
    }

    private void TreeViewItem_RightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is TreeViewItem item)
        {
            item.IsSelected = true;
            e.Handled = true;
        }
    }

    private void AddSubFolder_Click(object sender, RoutedEventArgs e)
    {
        Console.WriteLine("AddSubFolder_Click: Event triggered");
        if (DataContext is MainWindowViewModel viewModel && sender is MenuItem menuItem)
        {
            Console.WriteLine("AddSubFolder_Click: ViewModel and MenuItem found");
            
            // Get the TreeView from the context menu's placement target
            var treeView = (menuItem.Parent as ContextMenu)?.PlacementTarget as TreeView;
            var selectedItem = treeView?.SelectedItem as LinkTreeItemViewModel;
            Console.WriteLine($"AddSubFolder_Click: TreeView = {(treeView != null ? "found" : "null")}");
            Console.WriteLine($"AddSubFolder_Click: Selected item = {selectedItem?.Name ?? "null"}");
            Console.WriteLine($"AddSubFolder_Click: Selected item ID = {selectedItem?.Link?.Id ?? 0}");
            
            // Show dialog for creating a folder with the selected parent
            _ = Task.Run(async () => await viewModel.ShowLinkDialog(null, LinkType.Folder, selectedItem));
        }
        else
        {
            Console.WriteLine($"AddSubFolder_Click: ViewModel = {(DataContext is MainWindowViewModel ? "found" : "null")}, MenuItem = {(sender is MenuItem ? "found" : "null")}");
        }
    }

    private void AddSubLink_Click(object sender, RoutedEventArgs e)
    {
        Console.WriteLine("AddSubLink_Click: Event triggered");
        if (DataContext is MainWindowViewModel viewModel && sender is MenuItem menuItem)
        {
            Console.WriteLine("AddSubLink_Click: ViewModel and MenuItem found");
            
            // Get the TreeView from the context menu's placement target
            var treeView = (menuItem.Parent as ContextMenu)?.PlacementTarget as TreeView;
            var selectedItem = treeView?.SelectedItem as LinkTreeItemViewModel;
            Console.WriteLine($"AddSubLink_Click: TreeView = {(treeView != null ? "found" : "null")}");
            Console.WriteLine($"AddSubLink_Click: Selected item = {selectedItem?.Name ?? "null"}");
            Console.WriteLine($"AddSubLink_Click: Selected item ID = {selectedItem?.Link?.Id ?? 0}");
            
            // Show dialog for creating a link with the selected parent
            _ = Task.Run(async () => await viewModel.ShowLinkDialog(null, LinkType.WebUrl, selectedItem));
        }
        else
        {
            Console.WriteLine($"AddSubLink_Click: ViewModel = {(DataContext is MainWindowViewModel ? "found" : "null")}, MenuItem = {(sender is MenuItem ? "found" : "null")}");
        }
    }



    private void TreeViewItem_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is TreeViewItem item)
        {
            // Debug output for TreeViewItem loading
            var dataContext = item.DataContext as LinkTreeItemViewModel;
            Console.WriteLine($"TreeViewItem_Loaded: {dataContext?.Name ?? "Unknown"} - " +
                            $"HasChildren: {(dataContext?.Children?.Count > 0)} - " +
                            $"IsExpanded: {item.IsExpanded}");
            
            // Force expansion for folders with children
            if (dataContext?.Children?.Count > 0)
            {
                item.IsExpanded = true;
                Console.WriteLine($"TreeViewItem_Loaded: Forced expansion for {dataContext.Name}");
            }
        }
    }
    
    /// <summary>
    /// Recursively gets all folders from the tree structure
    /// </summary>
    private static List<LinkTreeItemViewModel> GetAllFolders(IEnumerable<LinkTreeItemViewModel> items)
    {
        var folders = new List<LinkTreeItemViewModel>();
        
        foreach (var item in items)
        {
            if (item.Link.Type == LinkType.Folder)
            {
                folders.Add(item);
            }
            
            // Recursively add folders from children
            if (item.Children?.Count > 0)
            {
                folders.AddRange(GetAllFolders(item.Children));
            }
        }
        
        return folders;
    }

    #endregion
}