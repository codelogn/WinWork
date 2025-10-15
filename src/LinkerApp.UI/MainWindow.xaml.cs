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
using System.Linq;

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
                    await viewModel.HandleLinkSaved(saveArgs.Link, saveArgs.SelectedTagIds, saveArgs.IsEditMode);
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

    private void ContextMenu_Opened(object sender, RoutedEventArgs e)
    {
        if (sender is ContextMenu contextMenu)
        {
            var treeView = FindName("LinksTreeView") as TreeView;
            var selectedItem = treeView?.SelectedItem as LinkTreeItemViewModel;
            
            var editMenuItem = contextMenu.Items.OfType<MenuItem>().FirstOrDefault(m => m.Name == "EditMenuItem");
            var copyUrlMenuItem = contextMenu.Items.OfType<MenuItem>().FirstOrDefault(m => m.Name == "CopyUrlMenuItem");
            var deleteMenuItem = contextMenu.Items.OfType<MenuItem>().FirstOrDefault(m => m.Name == "DeleteMenuItem");
            var editSeparator = contextMenu.Items.OfType<Separator>().FirstOrDefault(s => s.Name == "EditSeparator");
            var deleteSeparator = contextMenu.Items.OfType<Separator>().FirstOrDefault(s => s.Name == "DeleteSeparator");
            
            if (selectedItem != null)
            {
                // Show Edit option for both links and folders
                if (editMenuItem != null)
                    editMenuItem.Visibility = Visibility.Visible;
                
                // Show Copy URL only for web links
                if (copyUrlMenuItem != null)
                    copyUrlMenuItem.Visibility = selectedItem.Link.Type == LinkType.WebUrl ? Visibility.Visible : Visibility.Collapsed;
                
                // Show Delete option for both links and folders
                if (deleteMenuItem != null)
                    deleteMenuItem.Visibility = Visibility.Visible;
                
                // Show separators when needed
                if (editSeparator != null)
                    editSeparator.Visibility = Visibility.Visible;
                if (deleteSeparator != null)
                    deleteSeparator.Visibility = Visibility.Visible;
            }
            else
            {
                // Hide all item-specific options
                if (editMenuItem != null)
                    editMenuItem.Visibility = Visibility.Collapsed;
                if (copyUrlMenuItem != null)
                    copyUrlMenuItem.Visibility = Visibility.Collapsed;
                if (deleteMenuItem != null)
                    deleteMenuItem.Visibility = Visibility.Collapsed;
                if (editSeparator != null)
                    editSeparator.Visibility = Visibility.Collapsed;
                if (deleteSeparator != null)
                    deleteSeparator.Visibility = Visibility.Collapsed;
            }
        }
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
        var linkItem = GetLinkItemFromMenuItem(sender);
        if (linkItem != null && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.EditLinkCommand.Execute(linkItem);
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
            viewModel.AddFolderCommand.Execute(linkItem.Link.Id);
        }
    }

    private void AddItemLink_Click(object sender, RoutedEventArgs e)
    {
        var linkItem = GetLinkItemFromMenuItem(sender);
        if (linkItem != null && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.AddLinkCommand.Execute(linkItem.Link.Id);
        }
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
        if (sender is MenuItem menuItem)
        {
            // Get the context menu
            var contextMenu = menuItem.Parent as ContextMenu;
            if (contextMenu?.PlacementTarget is TreeViewItem treeViewItem)
            {
                return treeViewItem.DataContext as LinkTreeItemViewModel;
            }
        }
        return null;
    }

    private void ItemContextMenu_Opened(object sender, RoutedEventArgs e)
    {
        if (sender is ContextMenu contextMenu && contextMenu.PlacementTarget is TreeViewItem treeViewItem)
        {
            var selectedItem = treeViewItem.DataContext as LinkTreeItemViewModel;
            
            var editMenuItem = contextMenu.Items.OfType<MenuItem>().FirstOrDefault(m => m.Name == "EditMenuItem");
            var copyUrlMenuItem = contextMenu.Items.OfType<MenuItem>().FirstOrDefault(m => m.Name == "CopyUrlMenuItem");
            var addFolderMenuItem = contextMenu.Items.OfType<MenuItem>().FirstOrDefault(m => m.Name == "AddFolderMenuItem");
            var addLinkMenuItem = contextMenu.Items.OfType<MenuItem>().FirstOrDefault(m => m.Name == "AddLinkMenuItem");
            var deleteMenuItem = contextMenu.Items.OfType<MenuItem>().FirstOrDefault(m => m.Name == "DeleteMenuItem");
            
            if (selectedItem != null)
            {
                // Show Copy URL only for web links
                if (copyUrlMenuItem != null)
                    copyUrlMenuItem.Visibility = selectedItem.Link.Type == LinkType.WebUrl ? Visibility.Visible : Visibility.Collapsed;
                
                // Show Add options only for folders
                if (addFolderMenuItem != null)
                    addFolderMenuItem.Visibility = selectedItem.Link.Type == LinkType.Folder ? Visibility.Visible : Visibility.Collapsed;
                if (addLinkMenuItem != null)
                    addLinkMenuItem.Visibility = selectedItem.Link.Type == LinkType.Folder ? Visibility.Visible : Visibility.Collapsed;
            }
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