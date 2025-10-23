using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using WinWork.UI.ViewModels;
using WinWork.UI.Controls;
using WinWork.UI.Views;
using WinWork.UI.Utils;
using WinWork.Core.Interfaces;
using WinWork.Models;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System;
using System.Collections.ObjectModel;

namespace WinWork.UI
{

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    // Enable mouse wheel scrolling for left navigation area
    private void LinksTreeScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var scrollViewer = sender as ScrollViewer;
        if (scrollViewer != null)
        {
            if (e.Delta != 0)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta / 3.0);
                e.Handled = true;
            }
        }
    }
    public MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainWindowViewModel viewModel) : this()
    {
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

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.ExportDataCommand?.Execute(null);
        }
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.ImportDataCommand?.Execute(null);
        }
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new Views.SettingsWindow();
        settingsWindow.Owner = this;
        settingsWindow.ShowDialog();
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

    private void LinksTreeView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Find the TreeViewItem that was right-clicked
        var treeViewItem = FindParent<TreeViewItem>(e.OriginalSource as DependencyObject);
        if (treeViewItem != null)
        {
            // Select the item when right-clicking
            treeViewItem.Focus();
            treeViewItem.IsSelected = true;
            
            // Update the view model's selected link
            if (DataContext is MainWindowViewModel viewModel && treeViewItem.DataContext is LinkTreeItemViewModel linkItem)
            {
                viewModel.SelectedLink = linkItem;
            }
        }
    }

    // Helper method to find parent of specific type
    private static T? FindParent<T>(DependencyObject? child) where T : DependencyObject
    {
        if (child == null) return null;

        var parentObject = VisualTreeHelper.GetParent(child);
        if (parentObject == null) return null;

        if (parentObject is T parent)
            return parent;

        return FindParent<T>(parentObject);
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

    private async Task LinkTree_ItemDropped(object sender, Controls.DragDropEventArgs e)
    {
        // Handle drag and drop operations
        if (e.DropData?.GetDataPresent(typeof(LinkTreeItemViewModel)) != true) return;
        
        var draggedItem = e.DropData.GetData(typeof(LinkTreeItemViewModel)) as LinkTreeItemViewModel;
        var targetItem = e.TargetItem;
        
        if (draggedItem == null || DataContext is not MainWindowViewModel viewModel) return;
        
        // Prevent dropping on self
        if (draggedItem == targetItem) return;
        
        // Prevent dropping on own child (infinite loop prevention)
        if (IsChildOf(targetItem, draggedItem)) return;
        
        try
        {
            // Determine new parent ID based on drop target
            int? newParentId = null;
            
            if (targetItem != null)
            {
                // If dropping on a folder, make it the parent
                if (targetItem.Link.Type == LinkType.Folder)
                {
                    newParentId = targetItem.Link.Id;
                }
                // If dropping on a link, use the link's parent (make them siblings)
                else
                {
                    newParentId = targetItem.Link.ParentId;
                }
            }
            // If targetItem is null, drop at root level
            
            // Update the database
            await viewModel.MoveLinkAsync(draggedItem.Link.Id, newParentId);
            
            FileLogger.Log($"Drag & Drop: Moved '{draggedItem.Name}' to new parent ID: {newParentId}");
        }
        catch (Exception ex)
        {
            FileLogger.Log($"Drag & Drop Error: {ex.Message}");
            MessageBox.Show($"Failed to move item: {ex.Message}", "Drag & Drop Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Checks if targetItem is a child (descendant) of parentItem
    /// </summary>
    private bool IsChildOf(LinkTreeItemViewModel? targetItem, LinkTreeItemViewModel parentItem)
    {
        if (targetItem == null) return false;
        
        // Check all children recursively
        return IsChildOfRecursive(targetItem, parentItem.Children);
    }

    private bool IsChildOfRecursive(LinkTreeItemViewModel targetItem, ObservableCollection<LinkTreeItemViewModel>? children)
    {
        if (children == null) return false;
        
        foreach (var child in children)
        {
            if (child == targetItem) return true;
            if (IsChildOfRecursive(targetItem, child.Children)) return true;
        }
        
        return false;
    }

    #endregion

    #region Dialog Management

    private async void OnLinkDialogRequested(object? sender, LinkDialogRequestEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel) return;

        try
        {
            
            var dialogViewModel = new LinkDialogViewModel(ViewModel.SettingsService as WinWork.Core.Services.ISettingsService);
            
            if (e.LinkToEdit != null)
            {
                dialogViewModel.SetEditMode(e.LinkToEdit);
                var allItems = GetAllItems(viewModel.RootLinks);
                dialogViewModel.SetAvailableParents(allItems);
            }
            else
            {
                if (e.InitialType.HasValue)
                    dialogViewModel.SetInitialType(e.InitialType.Value);
                if (e.ParentItem != null)
                    dialogViewModel.SetParentContext(e.ParentItem);
                var allItems = GetAllItems(viewModel.RootLinks);
                dialogViewModel.SetAvailableParents(allItems);
            }

            var dialog = new LinkDialog(dialogViewModel)
            {
                Owner = this
            };

            // Subscribe to the save and delete events
            LinkSaveEventArgs? saveArgs = null;
            LinkDeleteEventArgs? deleteArgs = null;
            dialogViewModel.LinkSaved += (s, e) => saveArgs = e;
            dialogViewModel.LinkDeleted += (s, e) => deleteArgs = e;
            dialogViewModel.TestRequested += async (s, e) =>
            {
                try
                {
                    // Use the window's DataContext ViewModel to access services
                    if (DataContext is MainWindowViewModel mwvm)
                    {
                        var args = e as WinWork.UI.ViewModels.LinkDialogViewModel.TerminalTestEventArgs;
                        if (args != null)
                        {
                            // Use the link opener service to start the terminal
                            await mwvm.HandleItemDoubleClick(new WinWork.UI.ViewModels.LinkTreeItemViewModel(new WinWork.Models.Link { Type = WinWork.Models.LinkType.Terminal, TerminalType = args.TerminalType, Command = args.Command }));
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to launch terminal: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

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

    private void AddItem_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            // Get the context item the same way Edit and Delete do
            var linkItem = GetLinkItemFromMenuItem(sender);
            Console.WriteLine($"AddItem_Click: Retrieved linkItem = {linkItem?.Name ?? "null"} (ID: {linkItem?.Link?.Id ?? 0})");
            
            // Use the context item as the parent for the new link
            _ = viewModel.AddLinkAsync(linkItem);
        }
    }

    private async void OpenItem_Click(object sender, RoutedEventArgs e)
    {
        var linkItem = GetLinkItemFromMenuItem(sender);
        if (linkItem != null && DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.HandleItemDoubleClick(linkItem);
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

        // Get the context item the same way Edit and Delete do
        var linkItem = GetLinkItemFromMenuItem(sender);
        Console.WriteLine($"AddNew_Click: Retrieved linkItem = {linkItem?.Name ?? "null"} (ID: {linkItem?.Link?.Id ?? 0})");
        
        // Use the context item as the parent for the new link
        _ = viewModel.AddLinkAsync(linkItem);
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
            
            // First check if the MenuItem has a DataContext (new approach)
            if (menuItem.DataContext is LinkTreeItemViewModel menuDataContext)
            {
                var menuContextMessage = $"DEBUG: Found MenuItem DataContext: {menuDataContext.Name}";
                FileLogger.Log(menuContextMessage);
                return menuDataContext;
            }
            
            // Fallback to the old approach via PlacementTarget
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

    private void TreeViewItem_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && sender is TreeViewItem item)
        {
            if (item.DataContext is LinkTreeItemViewModel linkItem)
            {
                FileLogger.Log($"Drag & Drop: Starting drag operation for '{linkItem.Name}'");
                
                try
                {
                    // Start drag operation
                    DragDrop.DoDragDrop(item, linkItem, DragDropEffects.Move);
                }
                catch (Exception ex)
                {
                    FileLogger.Log($"Drag & Drop: Error starting drag operation: {ex.Message}");
                }
            }
        }
    }

    private void LinksTreeView_DragOver(object sender, DragEventArgs e)
    {
        // Allow move operations
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private async void LinksTreeView_Drop(object sender, DragEventArgs e)
    {
        try
        {
            if (e.Data.GetDataPresent(typeof(LinkTreeItemViewModel)))
            {
                var draggedItem = e.Data.GetData(typeof(LinkTreeItemViewModel)) as LinkTreeItemViewModel;
                
                // Find the target item by hit testing
                var targetElement = e.OriginalSource as DependencyObject;
                var targetTreeViewItem = FindParent<TreeViewItem>(targetElement);
                var targetItem = targetTreeViewItem?.DataContext as LinkTreeItemViewModel;
                
                // Create event args and call the existing handler
                var dropArgs = new Controls.DragDropEventArgs
                {
                    DropData = e.Data,
                    TargetItem = targetItem
                };
                
                await LinkTree_ItemDropped(sender, dropArgs);
            }
        }
        catch (Exception ex)
        {
            FileLogger.Log($"TreeView Drop Error: {ex.Message}");
        }
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
            
            // Create context menu programmatically to avoid XAML connection ID issues
            CreateContextMenuForTreeItem(item, dataContext);
            
            // Keep the old code as reference but commented out
            if (false && item.ContextMenu != null)
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
                        case "?? Edit":
                            menuItem.Click += EditItem_Click;
                            break;
                        case "?? Copy URL":
                            menuItem.Click += CopyItemUrl_Click;
                            // Hide Copy URL for non-web links
                            if (dataContext?.Link?.Type != LinkType.WebUrl)
                            {
                                menuItem.Visibility = Visibility.Collapsed;
                            }
                            break;
                        case "? Add New":
                            menuItem.Click += AddNew_Click;
                            break;
                        case "??? Delete":
                            menuItem.Click += DeleteItem_Click;
                            break;
                    }
                }
            }
        }
    }

    private void CreateContextMenuForTreeItem(TreeViewItem item, LinkTreeItemViewModel? dataContext)
    {
        if (dataContext == null) return;

        // Create context menu
        var contextMenu = new ContextMenu
        {
            Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#CC000000")),
            BorderBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#33FFFFFF")),
            BorderThickness = new Thickness(1)
        };

        // Edit menu item
        var editMenuItem = new MenuItem
        {
            Header = "✏️ Edit",
            Foreground = System.Windows.Media.Brushes.White,
            Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(1),
            DataContext = dataContext
        };
        editMenuItem.Click += EditItem_Click;
        contextMenu.Items.Add(editMenuItem);

        // Open menu item (only for items that can be opened - not folders)
        if (dataContext.Link.Type != LinkType.Folder)
        {
            var openMenuItem = new MenuItem
            {
                Header = "🚀 Open",
                Foreground = System.Windows.Media.Brushes.White,
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(1),
                DataContext = dataContext
            };
            openMenuItem.Click += OpenItem_Click;
            contextMenu.Items.Add(openMenuItem);
        }

        // Copy URL menu item (only for web URLs)
        if (dataContext.Link.Type == LinkType.WebUrl)
        {
            var copyMenuItem = new MenuItem
            {
                Header = "📋 Copy URL",
                Foreground = System.Windows.Media.Brushes.White,
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(1),
                DataContext = dataContext
            };
            copyMenuItem.Click += CopyUrl_Click;
            contextMenu.Items.Add(copyMenuItem);
        }

        // Separator
        contextMenu.Items.Add(new Separator { Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#33FFFFFF")) });

        // Add New menu item
        var addMenuItem = new MenuItem
        {
            Header = "➕ Add New",
            Foreground = System.Windows.Media.Brushes.White,
            Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(1),
            DataContext = dataContext
        };
        addMenuItem.Click += AddItem_Click;
        contextMenu.Items.Add(addMenuItem);

        // Delete menu item
        var deleteMenuItem = new MenuItem
        {
            Header = "🗑️ Delete",
            Foreground = System.Windows.Media.Brushes.White,
            Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(1),
            DataContext = dataContext
        };
        deleteMenuItem.Click += DeleteItem_Click;
        contextMenu.Items.Add(deleteMenuItem);

        // Assign context menu to item
        item.ContextMenu = contextMenu;
    }
    
    /// <summary>
    /// Recursively gets all items from the tree structure
    /// </summary>
    private static List<LinkTreeItemViewModel> GetAllItems(IEnumerable<LinkTreeItemViewModel> items)
    {
        var allItems = new List<LinkTreeItemViewModel>();
        
        foreach (var item in items)
        {
            // Add this item regardless of type
            allItems.Add(item);
            
            // Recursively add items from children
            if (item.Children?.Count > 0)
            {
                allItems.AddRange(GetAllItems(item.Children));
            }
        }
        
        return allItems;
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
}
