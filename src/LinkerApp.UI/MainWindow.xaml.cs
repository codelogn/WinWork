using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using LinkerApp.UI.ViewModels;
using LinkerApp.UI.Controls;
using LinkerApp.UI.Views;
using LinkerApp.UI.Utils;
using LinkerApp.Core.Interfaces;
using LinkerApp.Models;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System;
using System.Collections.ObjectModel;

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

    #endregion

    #region Tree View Event Handlers

    private void LinksTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is MainWindowViewModel viewModel && e.NewValue is LinkTreeItemViewModel selectedItem)
        {
            viewModel.SelectedLink = selectedItem;
        }
    }

    private async void LinksTreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel && viewModel.SelectedLink != null)
        {
            await viewModel.HandleItemDoubleClick(viewModel.SelectedLink);
        }
    }

    private void LinksTreeView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Don't handle the event - let it continue to the context menu
    }

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

            // Subscribe to the save and delete events
            LinkSaveEventArgs? saveArgs = null;
            LinkDeleteEventArgs? deleteArgs = null;
            dialogViewModel.LinkSaved += (s, e) => saveArgs = e;
            dialogViewModel.LinkDeleted += (s, e) => deleteArgs = e;

            if (dialog.ShowDialog() == true)
            {
                if (saveArgs != null)
                {
                    // Handle successful save
                    await viewModel.HandleLinkSaved(saveArgs.Link, saveArgs.TagsString, saveArgs.IsEditMode);
                }
                else if (deleteArgs != null)
                {
                    // Handle successful delete
                    await viewModel.DeleteLinkAsync(deleteArgs.Link.Id);
                    await viewModel.LoadLinksAsync();
                }
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

    // Tag management methods kept for potential future use
    private async Task HandleTagAdded(TagEventArgs args, MainWindowViewModel viewModel)
    {
        try
        {
            var createdTag = await viewModel.CreateTagAsync(args.Tag);
            
            // Refresh the tags in the TagManagementDialog by adding the new tag
            var dialog = Application.Current.Windows.OfType<Views.TagManagementDialog>().FirstOrDefault();
            if (dialog != null)
            {
                dialog.ViewModel.AddNewTag(createdTag);
            }
            
            // Show success message
            if (DataContext is MainWindowViewModel mainViewModel)
            {
                mainViewModel.DisplaySuccessMessage($"Tag '{createdTag.Name}' created successfully!");
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
            var updatedTag = await viewModel.UpdateTagAsync(args.Tag);
            
            // Refresh the tags in the TagManagementDialog by updating the existing tag
            var dialog = Application.Current.Windows.OfType<Views.TagManagementDialog>().FirstOrDefault();
            if (dialog != null)
            {
                dialog.ViewModel.RefreshTag(updatedTag);
            }
            
            // Show success message
            if (DataContext is MainWindowViewModel mainViewModel)
            {
                mainViewModel.DisplaySuccessMessage($"Tag '{updatedTag.Name}' updated successfully!");
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
                
                // Remove the tag from the TagManagementDialog
                var dialog = Application.Current.Windows.OfType<Views.TagManagementDialog>().FirstOrDefault();
                if (dialog != null)
                {
                    dialog.ViewModel.RemoveTag(args.Tag);
                }
                
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

    private void ContextMenu_Opened(object sender, RoutedEventArgs e)
    {
        // Context menu opened - no special handling needed for now
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            var treeView = FindName("LinksTreeView") as TreeView;
            var selectedItem = treeView?.SelectedItem as LinkTreeItemViewModel;
            
            if (selectedItem != null)
            {
                viewModel.EditLinkCommand.Execute(selectedItem);
            }
        }
    }

    private void CopyUrl_Click(object sender, RoutedEventArgs e)
    {
        var treeView = FindName("LinksTreeView") as TreeView;
        var selectedItem = treeView?.SelectedItem as LinkTreeItemViewModel;
        
        if (selectedItem?.Link?.Url != null)
        {
            try
            {
                Clipboard.SetText(selectedItem.Link.Url);
                // You could add a status message or notification here
                MessageBox.Show($"URL copied to clipboard: {selectedItem.Link.Url}", "URL Copied", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy URL to clipboard: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            var treeView = FindName("LinksTreeView") as TreeView;
            var selectedItem = treeView?.SelectedItem as LinkTreeItemViewModel;
            
            if (selectedItem != null)
            {
                var itemType = selectedItem.Link.Type == LinkType.Folder ? "folder" : "link";
                var hasChildren = selectedItem.Link.Type == LinkType.Folder && selectedItem.Children?.Count > 0;
                
                string message;
                if (hasChildren)
                {
                    message = $"The folder '{selectedItem.Name}' contains {selectedItem.Children?.Count} item(s). " +
                             "You must delete or move all items from this folder before you can delete it.\n\n" +
                             "Do you want to delete all items in this folder first?";
                }
                else
                {
                    message = $"Are you sure you want to delete this {itemType}?\n\n" +
                             $"Name: {selectedItem.Name}" +
                             (selectedItem.Link.Type != LinkType.Folder ? $"\nURL: {selectedItem.Link.Url}" : "");
                }
                
                var result = MessageBox.Show(message, 
                    hasChildren ? "Folder Not Empty" : $"Delete {char.ToUpper(itemType[0])}{itemType.Substring(1)}",
                    hasChildren ? MessageBoxButton.YesNo : MessageBoxButton.YesNo, 
                    MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        if (hasChildren)
                        {
                            // Delete all children first
                            var childrenToDelete = selectedItem.Children?.ToList() ?? new List<LinkTreeItemViewModel>();
                            foreach (var child in childrenToDelete)
                            {
                                await viewModel.DeleteLinkAsync(child.Link.Id);
                            }
                        }
                        
                        // Delete the item itself
                        await viewModel.DeleteLinkAsync(selectedItem.Link.Id);
                        
                        // Refresh the tree view
                        await viewModel.LoadLinksAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to delete {itemType}: {ex.Message}", "Error", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }

    // Individual item context menu handlers
    private void EditItem_Click(object sender, RoutedEventArgs e)
    {
        var message = "DEBUG: EditItem_Click called";
        FileLogger.Log(message);
        
        var linkItem = GetLinkItemFromMenuItem(sender);
        var linkFoundMessage = $"DEBUG: linkItem found: {linkItem?.Name ?? "null"}";
        FileLogger.Log(linkFoundMessage);
        
        if (linkItem != null && DataContext is MainWindowViewModel viewModel)
        {
            var executeMessage = $"DEBUG: Calling EditLinkCommand.Execute for: {linkItem.Name}";
            FileLogger.Log(executeMessage);
            viewModel.EditLinkCommand.Execute(linkItem);
        }
        else
        {
            var failMessage = $"DEBUG: EditItem_Click failed - linkItem: {linkItem?.Name ?? "null"}, viewModel: {DataContext?.GetType().Name ?? "null"}";
            FileLogger.Log(failMessage);
        }
    }

    private void CopyItemUrl_Click(object sender, RoutedEventArgs e)
    {
        var linkItem = GetLinkItemFromMenuItem(sender);
        if (linkItem?.Link?.Url != null)
        {
            try
            {
                Clipboard.SetText(linkItem.Link.Url);
                MessageBox.Show($"URL copied to clipboard: {linkItem.Link.Url}", "URL Copied", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy URL to clipboard: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void AddItemFolder_Click(object sender, RoutedEventArgs e)
    {
        var linkItem = GetLinkItemFromMenuItem(sender);
        if (linkItem != null && DataContext is MainWindowViewModel viewModel)
        {
            // Add as sibling at the same level (use parent ID)
            var parentId = linkItem.Link.ParentId;
            viewModel.AddFolderCommand.Execute(parentId);
        }
    }

    private void AddItemLink_Click(object sender, RoutedEventArgs e)
    {
        var linkItem = GetLinkItemFromMenuItem(sender);
        if (linkItem != null && DataContext is MainWindowViewModel viewModel)
        {
            // Add as sibling at the same level (use parent ID)
            var parentId = linkItem.Link.ParentId;
            viewModel.AddLinkCommand.Execute(parentId);
        }
    }

    private void AddNew_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel) return;

        // For the navigation button, just use the AddLinkCommand which will show the unified dialog
        viewModel.AddLinkCommand.Execute(null);
    }

    private LinkTreeItemViewModel? GetParentItem(LinkTreeItemViewModel item)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            return FindParentInTree(viewModel.RootLinks, item);
        }
        return null;
    }
    
    private LinkTreeItemViewModel? FindParentInTree(ObservableCollection<LinkTreeItemViewModel> items, LinkTreeItemViewModel target)
    {
        foreach (var item in items)
        {
            if (item.Children.Contains(target))
                return item;
                
            var found = FindParentInTree(item.Children, target);
            if (found != null) return found;
        }
        return null;
    }



    private async void DeleteItem_Click(object sender, RoutedEventArgs e)
    {
        var linkItem = GetLinkItemFromMenuItem(sender);
        
        if (linkItem != null && DataContext is MainWindowViewModel viewModel)
        {
            var itemType = linkItem.Link.Type == LinkType.Folder ? "folder" : "link";
            var hasChildren = linkItem.Link.Type == LinkType.Folder && linkItem.Children?.Count > 0;
            
            string message;
            if (hasChildren)
            {
                message = $"The folder '{linkItem.Name}' contains {linkItem.Children?.Count} item(s). " +
                         "You must delete or move all items from this folder before you can delete it.\n\n" +
                         "Do you want to delete all items in this folder first?";
            }
            else
            {
                message = $"Are you sure you want to delete this {itemType}?\n\n" +
                         $"Name: {linkItem.Name}" +
                         (linkItem.Link.Type != LinkType.Folder ? $"\nURL: {linkItem.Link.Url}" : "");
            }
            
            var result = MessageBox.Show(message, 
                hasChildren ? "Folder Not Empty" : $"Delete {char.ToUpper(itemType[0])}{itemType.Substring(1)}",
                hasChildren ? MessageBoxButton.YesNo : MessageBoxButton.YesNo, 
                MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (hasChildren)
                    {
                        // Delete all children first
                        var childrenToDelete = linkItem.Children?.ToList() ?? new List<LinkTreeItemViewModel>();
                        foreach (var child in childrenToDelete)
                        {
                            await viewModel.DeleteLinkAsync(child.Link.Id);
                        }
                    }
                    
                    // Delete the item itself
                    await viewModel.DeleteLinkAsync(linkItem.Link.Id);
                    
                    // Refresh the tree view
                    await viewModel.LoadLinksAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to delete {itemType}: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private LinkTreeItemViewModel? GetLinkItemFromMenuItem(object sender)
    {
        var senderMessage = $"DEBUG: GetLinkItemFromMenuItem called with sender: {sender?.GetType().Name ?? "null"}";
        FileLogger.Log(senderMessage);
        
        if (sender is MenuItem menuItem)
        {
            var menuItemMessage = $"DEBUG: MenuItem found, parent: {menuItem.Parent?.GetType().Name ?? "null"}";
            FileLogger.Log(menuItemMessage);
            
            // Get the context menu
            var contextMenu = menuItem.Parent as ContextMenu;
            if (contextMenu?.PlacementTarget is TreeViewItem treeViewItem)
            {
                var result = treeViewItem.DataContext as LinkTreeItemViewModel;
                var resultMessage = $"DEBUG: Found TreeViewItem with DataContext: {result?.Name ?? "null"}";
                FileLogger.Log(resultMessage);
                return result;
            }
            else
            {
                var placementMessage = $"DEBUG: PlacementTarget is not TreeViewItem: {contextMenu?.PlacementTarget?.GetType().Name ?? "null"}";
                FileLogger.Log(placementMessage);
            }
        }
        return null;
    }



    private void TreeViewItem_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is TreeViewItem item)
        {
            // Debug output for TreeViewItem loading
            var dataContext = item.DataContext as LinkTreeItemViewModel;
            
            // Force expansion for folders with children
            if (dataContext?.Children?.Count > 0)
            {
                item.IsExpanded = true;
            }
            
            // Attach context menu event handlers programmatically to avoid XAML connection ID issues
            if (item.ContextMenu != null)
            {
                foreach (var menuItem in item.ContextMenu.Items.OfType<MenuItem>())
                {
                    // Remove any existing handlers first
                    menuItem.Click -= EditItem_Click;
                    menuItem.Click -= CopyItemUrl_Click;
                    menuItem.Click -= AddNew_Click;
                    menuItem.Click -= DeleteItem_Click;
                    
                    // Attach handlers based on header text
                    switch (menuItem.Header?.ToString())
                    {
                        case "✏️ Edit":
                            menuItem.Click += EditItem_Click;
                            break;
                        case "📋 Copy URL":
                            menuItem.Click += CopyItemUrl_Click;
                            // Hide Copy URL for non-web links
                            if (dataContext?.Link?.Type != LinkType.WebUrl)
                            {
                                menuItem.Visibility = Visibility.Collapsed;
                            }
                            break;
                        case "➕ Add New":
                            menuItem.Click += AddNew_Click;
                            break;
                        case "🗑️ Delete":
                            menuItem.Click += DeleteItem_Click;
                            break;
                    }
                }
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
