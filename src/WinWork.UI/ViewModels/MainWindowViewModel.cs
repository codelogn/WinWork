using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WinWork.Core.Services;
using WinWork.Core.Interfaces;
using WinWork.Models;
using WinWork.UI.Utils;

namespace WinWork.UI.ViewModels;

/// <summary>
/// Options for handling import operations
/// </summary>
public enum DuplicateHandling
{
    Skip,
    Rename,
    UpdateExisting
}

public class ImportOptions
{
    public bool CreateContainer { get; set; } = true;
    public DuplicateHandling HandleDuplicates { get; set; } = DuplicateHandling.Skip;
}

/// <summary>
/// Base class for ViewModels with INotifyPropertyChanged implementation
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
            return false;

        backingStore = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

/// <summary>
/// ViewModel for the main window
/// </summary>
public class MainWindowViewModel : ViewModelBase
{
    private readonly WinWork.Core.Interfaces.ILinkService _linkService;
    private readonly ITagService _tagService;
    private readonly ISettingsService _settingsService;
    private readonly ILinkOpenerService _linkOpenerService;
    private readonly IImportExportService _importExportService;

    // Expose services to allow other UI windows to reuse them
    public WinWork.Core.Interfaces.ILinkService LinkService => _linkService;
    public ITagService TagService => _tagService;
    public ISettingsService SettingsService => _settingsService;
    public ILinkOpenerService LinkOpenerService => _linkOpenerService;

    // UI state backing fields
    private string _title = "WinWork";
    private bool _isLoading;
    private string _searchText = string.Empty;
    private LinkTreeItemViewModel? _selectedLink;
    private string _statusMessage = string.Empty;
    private bool _showSuccessMessage;
    private bool _showErrorMessage;
    
    // Edit form properties
    private string _editName = string.Empty;
    private string _editUrl = string.Empty;
    private string _editDescription = string.Empty;
    private string _editTags = string.Empty;
    private string _editNotes = string.Empty;
    private LinkType _editType = LinkType.WebUrl;
    private string _editCommand = string.Empty;
    private string _editTerminalType = string.Empty;
    private bool _editIsHotclick = false;
    private bool _isEditingItem = false;
    private Link? _originalLinkForEdit;
    private LinkTreeItemViewModel? _editParentItem;

    public ObservableCollection<LinkTreeItemViewModel> RootLinks { get; }
    public ObservableCollection<LinkTypeItem> LinkTypes { get; }
    public ObservableCollection<LinkTreeItemViewModel> AvailableParentItems { get; }
    public ObservableCollection<string> TerminalOptions { get; }

    // Commands
    public ICommand AddLinkCommand { get; }
    public ICommand AddFolderCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand EditLinkCommand { get; }
    public ICommand SaveEditCommand { get; }
    public ICommand CancelEditCommand { get; }
    public ICommand DeleteEditCommand { get; }
    public ICommand TestEditCommand { get; }
    // Inline edit browse commands
    public ICommand BrowseFileForEditCommand { get; }
    public ICommand BrowseFolderForEditCommand { get; }
    public ICommand BrowseApplicationForEditCommand { get; }
    public ICommand ExportDataCommand { get; }
    public ICommand ImportDataCommand { get; }

    public MainWindowViewModel(
        WinWork.Core.Interfaces.ILinkService linkService,
        ITagService tagService,
        ISettingsService settingsService,
        ILinkOpenerService linkOpenerService,
        IImportExportService importExportService)
    {
        _linkService = linkService;
        _tagService = tagService;
        _settingsService = settingsService;
        _linkOpenerService = linkOpenerService;
        _importExportService = importExportService;

        RootLinks = new ObservableCollection<LinkTreeItemViewModel>();
    AvailableParentItems = new ObservableCollection<LinkTreeItemViewModel>();
        // Use the centralized provider so all UI places show the same LinkType options
        LinkTypes = new ObservableCollection<LinkTypeItem>(LinkTypeProvider.DefaultLinkTypes);
        TerminalOptions = new ObservableCollection<string>
        {
            "PowerShell",
            "Git Bash",
            "CMD"
        };

        // Initialize commands
        AddLinkCommand = new AsyncRelayCommand(AddLinkAsync);
        AddFolderCommand = new AsyncRelayCommand(AddFolderAsync);
        RefreshCommand = new AsyncRelayCommand(LoadLinksAsync);
        EditLinkCommand = new RelayCommand<LinkTreeItemViewModel>(EditLink);
        SaveEditCommand = new AsyncRelayCommand(SaveEditAsync, CanSaveEdit);
        CancelEditCommand = new RelayCommand(CancelEdit);
        DeleteEditCommand = new AsyncRelayCommand(DeleteEditAsync, CanDeleteEdit);
    TestEditCommand = new RelayCommand(async () => await RunTestForEditAsync(), () => CanRunTestForEdit());
        ExportDataCommand = new AsyncRelayCommand(ExportDataAsync);
        ImportDataCommand = new AsyncRelayCommand(ImportDataAsync);
    BrowseFileForEditCommand = new RelayCommand(BrowseFileForEdit);
    BrowseFolderForEditCommand = new RelayCommand(BrowseFolderForEdit);
    BrowseApplicationForEditCommand = new RelayCommand(BrowseApplicationForEdit);
        
        _ = LoadDataAsync();
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                _ = PerformSearchAsync(value);
            }
        }
    }

    // Helper property used by MainWindow.xaml to show inline browse controls for edit panel
    public bool ShowBrowseControlsEdit =>
        (EditType == LinkType.FilePath || EditType == LinkType.FolderPath || EditType == LinkType.Application)
        && (EditType != LinkType.Folder && EditType != LinkType.Notes && EditType != LinkType.Terminal);

    public LinkTreeItemViewModel? SelectedLink
    {
        get => _selectedLink;
        set 
        { 
            if (SetProperty(ref _selectedLink, value))
            {
                LoadSelectedItemForEdit();
            }
        }
    }

    // Edit form properties
    public string EditName
    {
        get => _editName;
        set => SetProperty(ref _editName, value);
    }

    public string EditUrl
    {
        get => _editUrl;
        set => SetProperty(ref _editUrl, value);
    }

    private void BrowseFileForEdit()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select File",
            Filter = "All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            EditUrl = dialog.FileName;
            if (string.IsNullOrWhiteSpace(EditName))
            {
                EditName = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
            }
        }
    }

    private void BrowseFolderForEdit()
    {
        try
        {
            using var dialog = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog
            {
                Title = "Select Folder",
                IsFolderPicker = true
            };

            if (dialog.ShowDialog() == Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
            {
                EditUrl = dialog.FileName;
                if (string.IsNullOrWhiteSpace(EditName))
                {
                    EditName = System.IO.Path.GetFileName(dialog.FileName);
                }
            }
        }
        catch
        {
            // ignore
        }
    }

    private void BrowseApplicationForEdit()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Application",
            Filter = "Executable Files (*.exe)|*.exe|Batch Files (*.bat)|*.bat|Command Files (*.cmd)|*.cmd|All Files (*.*)|*.*",
            FilterIndex = 1
        };

        if (dialog.ShowDialog() == true)
        {
            EditUrl = dialog.FileName;
            if (string.IsNullOrWhiteSpace(EditName))
            {
                EditName = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
            }
        }
    }

    public string EditCommand
    {
        get => _editCommand;
        set
        {
            if (SetProperty(ref _editCommand, value))
            {
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    private bool CanRunTestForEdit()
    {
        return !string.IsNullOrWhiteSpace(EditCommand) && EditType == LinkType.Terminal;
    }

    private async Task RunTestForEditAsync()
    {
        try
        {
            // Construct a temporary Link to pass to the opener
            var tempLink = new Link
            {
                Type = LinkType.Terminal,
                TerminalType = EditTerminalType,
                Command = EditCommand
            };

            await _linkOpenerService.OpenAsync(tempLink);
        }
        catch (Exception ex)
        {
            DisplayErrorMessage($"Failed to run test: {ex.Message}");
        }
    }

    public string EditTerminalType
    {
        get => _editTerminalType;
        set => SetProperty(ref _editTerminalType, value);
    }

    public string EditDescription
    {
        get => _editDescription;
        set => SetProperty(ref _editDescription, value);
    }

    public string EditTags
    {
        get => _editTags;
        set => SetProperty(ref _editTags, value);
    }

    public string EditNotes
    {
        get => _editNotes;
        set => SetProperty(ref _editNotes, value);
    }

    public LinkType EditType
    {
        get => _editType;
        set
        {
            if (SetProperty(ref _editType, value))
            {
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                // Notify dependent computed properties so the UI updates (e.g., ShowBrowseControlsEdit)
                OnPropertyChanged(nameof(ShowBrowseControlsEdit));
            }
        }
    }

    public bool EditIsHotclick
    {
        get => _editIsHotclick;
        set => SetProperty(ref _editIsHotclick, value);
    }

    public bool IsEditingItem
    {
        get => _isEditingItem;
        set => SetProperty(ref _isEditingItem, value);
    }

    public LinkTreeItemViewModel? EditParentItem
    {
        get => _editParentItem;
        set => SetProperty(ref _editParentItem, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool ShowSuccessMessage
    {
        get => _showSuccessMessage;
        set => SetProperty(ref _showSuccessMessage, value);
    }

    public bool ShowErrorMessage
    {
        get => _showErrorMessage;
        set => SetProperty(ref _showErrorMessage, value);
    }

    private async Task LoadDataAsync()
    {
        IsLoading = true;
        
        try
        {
            await LoadLinksAsync();
        }
        catch (Exception ex)
        {
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task LoadLinksAsync()
    {
        try
        {
            var rootLinks = await _linkService.GetRootLinksAsync();
            
            // Ensure UI updates happen on UI thread
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                RootLinks.Clear();
            });
            
            foreach (var link in rootLinks)
            {
                var viewModel = await CreateLinkTreeItemAsync(link);
                
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    RootLinks.Add(viewModel);
                });
            }
        }
        catch (Exception ex)
        {
        }
    }

    private async Task<LinkTreeItemViewModel> CreateLinkTreeItemAsync(Link link)
    {
        var children = await _linkService.GetChildLinksAsync(link.Id);
        
        var childViewModels = new List<LinkTreeItemViewModel>();
        
        foreach (var child in children)
        {
            var childViewModel = await CreateLinkTreeItemAsync(child);
            childViewModel.Parent = null; // will set below
            childViewModels.Add(childViewModel);
        }
        
        var treeItem = new LinkTreeItemViewModel(link, childViewModels);
        
        // Hook up expansion state change event
        treeItem.ExpansionStateChanged += OnTreeItemExpansionChanged;
        
        // Assign parent for all children (events are already hooked up from recursive calls)
        foreach (var childVM in treeItem.Children)
        {
            childVM.Parent = treeItem;
        }
        
        return treeItem;
    }

    // Command implementations
    private async Task AddLinkAsync()
    {
        await ShowLinkDialog();
    }

    public async Task AddLinkAsync(LinkTreeItemViewModel? parentItem)
    {
        Console.WriteLine($"AddLinkAsync: Received parentItem = {parentItem?.Name ?? "null"} (ID: {parentItem?.Link?.Id ?? 0})");
        await ShowLinkDialog(parentItem: parentItem);
    }

    private async Task AddFolderAsync()
    {
        await ShowLinkDialog(initialType: LinkType.Folder);
    }

    private void EditLink(LinkTreeItemViewModel? linkTreeItem)
    {
        if (linkTreeItem?.Link != null)
        {
            FileLogger.Log($"EditLink called for link: {linkTreeItem.Link.Name}, Type: {linkTreeItem.Link.Type}, ID: {linkTreeItem.Link.Id}");
            // Log current LinkTypes known to the MainWindow (should include Terminal)
            try
            {
                FileLogger.Log($"LinkTypes count: {LinkTypes?.Count}");
                foreach (var lt in LinkTypes ?? Enumerable.Empty<LinkTypeItem>())
                {
                    FileLogger.Log($"LinkType entry: Type={lt.Type}, Display='{lt.DisplayName}'");
                }
            }
            catch (Exception ex)
            {
                FileLogger.Log($"Error logging LinkTypes: {ex.Message}");
            }
            // Pass parent item for correct selection in edit dialog
            _ = ShowLinkDialog(linkTreeItem.Link, null, linkTreeItem.Parent);
        }
        else
        {
            FileLogger.Log("EditLink called but linkTreeItem or Link is null");
        }
    }

    public async Task<bool> DeleteLinkAsync(int linkId)
    {
        try
        {
            IsLoading = true;
            return await _linkService.DeleteLinkAsync(linkId);
        }
        catch (Exception ex)
        {
            DisplayErrorMessage($"Failed to delete link: {ex.Message}");
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Moves a link to a new parent (for drag & drop functionality)
    /// </summary>
    public async Task<bool> MoveLinkAsync(int linkId, int? newParentId, int? newSortOrder = null)
    {
        try
        {
            IsLoading = true;

            int sortOrderToUse;
            if (newSortOrder.HasValue)
            {
                sortOrderToUse = newSortOrder.Value;
            }
            else
            {
                // Determine new sort order: append to end of target parent
                var maxOrder = await _linkService.GetMaxSortOrderAsync(newParentId);
                sortOrderToUse = maxOrder + 1;
            }

            var success = await _linkService.MoveLinkAsync(linkId, newParentId, sortOrderToUse);
            if (!success)
            {
                DisplayErrorMessage($"Failed to move link (data layer move failed)");
                return false;
            }

            // Refresh the tree to reflect changes
            await LoadLinksAsync();

            DisplaySuccessMessage($"Moved item successfully");
            return true;
        }
        catch (Exception ex)
        {
            DisplayErrorMessage($"Failed to move link: {ex.Message}");
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void DisplaySuccessMessage(string message)
    {
        StatusMessage = message;
        ShowSuccessMessage = true;
        ShowErrorMessage = false;
        
        // Auto-hide after 3 seconds
        Task.Delay(3000).ContinueWith(_ =>
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                ShowSuccessMessage = false;
                StatusMessage = string.Empty;
            });
        });
    }

    public void DisplayErrorMessage(string message)
    {
        StatusMessage = message;
        ShowErrorMessage = true;
        ShowSuccessMessage = false;
        
        // Auto-hide after 5 seconds
        Task.Delay(5000).ContinueWith(_ =>
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                ShowErrorMessage = false;
                StatusMessage = string.Empty;
            });
        });
    }

    public async Task HandleItemDoubleClick(LinkTreeItemViewModel item)
    {
        try
        {
            await _linkOpenerService.OpenAsync(item.Link);
        }
        catch (Exception ex)
        {
        }
    }

    // Dialog methods
    public async Task ShowLinkDialog(Link? linkToEdit = null, LinkType? initialType = null, LinkTreeItemViewModel? parentItem = null)
    {
        try
        {
            FileLogger.Log($"ShowLinkDialog called - linkToEdit: {linkToEdit?.Name ?? "null"} (ID: {linkToEdit?.Id ?? 0}), Type: {linkToEdit?.Type}, initialType: {initialType}, parentItem: {parentItem?.Name ?? "null"}");
            
            if (linkToEdit != null)
            {
                FileLogger.Log($"Link details - Name: '{linkToEdit.Name}', Type: {linkToEdit.Type}, URL: '{linkToEdit.Url}', Notes: '{linkToEdit.Notes}'");
            }
            
            // This will be called from the UI layer
            LinkDialogRequested?.Invoke(this, new LinkDialogRequestEventArgs(linkToEdit, initialType, parentItem));
        }
        catch (Exception ex)
        {
            FileLogger.Log($"Error in ShowLinkDialog: {ex.Message}");
        }
    }

    public async Task HandleLinkSaved(Link link, string tagsString, bool isEditMode)
    {
        try
        {
            string actionType = link.Type == LinkType.Folder ? "folder" : 
                              link.Type == LinkType.Notes ? "note" : "link";
            string action = isEditMode ? "updated" : "created";

            if (isEditMode)
            {
                Console.WriteLine($"HandleLinkSaved: Updating existing {actionType} - ID: {link.Id}, Name: '{link.Name}', Type: {link.Type}");
                await _linkService.UpdateLinkAsync(link);
                Console.WriteLine($"HandleLinkSaved: Update completed successfully for {actionType} '{link.Name}'");
            }
            else
            {
                Console.WriteLine($"HandleLinkSaved: Creating new {actionType} - Name: '{link.Name}', Type: {link.Type}");
                await _linkService.CreateLinkAsync(link);
                Console.WriteLine($"HandleLinkSaved: Creation completed successfully for {actionType} '{link.Name}'");
            }

            // Handle tags after saving the link
            await UpdateLinkTagsAsync(link.Id, tagsString);

            await LoadLinksAsync();
            DisplaySuccessMessage($"{char.ToUpper(actionType[0])}{actionType.Substring(1)} '{link.Name}' {action} successfully!");
        }
        catch (ArgumentException argEx)
        {
            // Show a friendly error message for validation errors
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                System.Windows.MessageBox.Show(argEx.Message, "Invalid Input", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR in HandleLinkSaved: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");

            string actionType = link.Type == LinkType.Folder ? "folder" : 
                              link.Type == LinkType.Notes ? "note" : "link";

            // Show detailed error information
            string errorDetails = $"Exception Type: {ex.GetType().Name}\n" +
                                $"Message: {ex.Message}\n" +
                                $"Link Details:\n" +
                                $"  - Name: {link.Name}\n" +
                                $"  - Type: {link.Type}\n" +
                                $"  - ParentId: {link.ParentId}\n" +
                                $"  - URL: {link.Url}\n" +
                                $"  - IsEditMode: {isEditMode}\n" +
                                $"Stack Trace:\n{ex.StackTrace}";

            // Use App's ShowErrorDialog for better error display
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                App.ShowErrorDialog($"Failed to Save {char.ToUpper(actionType[0])}{actionType.Substring(1)}", 
                                  $"An error occurred while saving the {actionType} '{link.Name}'.", 
                                  errorDetails);
            });
        }
    }

    public async Task<IEnumerable<Tag>> GetAllTagsAsync()
    {
        return await _tagService.GetAllTagsAsync();
    }

    public async Task<Tag> CreateTagAsync(Tag tag)
    {
        return await _tagService.CreateTagAsync(tag);
    }

    public async Task<Tag> UpdateTagAsync(Tag tag)
    {
        return await _tagService.UpdateTagAsync(tag);
    }

    public async Task<bool> DeleteTagAsync(int tagId)
    {
        return await _tagService.DeleteTagAsync(tagId);
    }

    // Search functionality
    private async Task PerformSearchAsync(string searchTerm)
    {
        IsLoading = true;
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                // Show all links
                await LoadLinksAsync();
            }
            else
            {
                // Search links
                var searchResults = await _linkService.SearchLinksAsync(searchTerm);
                RootLinks.Clear();
                foreach (var link in searchResults)
                {
                    var viewModel = await CreateLinkTreeItemAsync(link);
                    RootLinks.Add(viewModel);
                }
            }
        }
        catch (Exception ex)
        {
        }
        finally
        {
            IsLoading = false;
        }
    }

    // Edit form methods
    private void LoadSelectedItemForEdit()
    {
        if (_selectedLink?.Link != null)
        {
            _originalLinkForEdit = _selectedLink.Link;
            EditName = _selectedLink.Link.Name;
            EditUrl = _selectedLink.Link.Url ?? string.Empty;
            EditDescription = _selectedLink.Link.Description ?? string.Empty;
            EditTags = ConvertTagsToString(_selectedLink.Link.LinkTags);
            EditNotes = _selectedLink.Link.Notes ?? string.Empty;
            EditType = _selectedLink.Link.Type;
            EditIsHotclick = _selectedLink.Link.IsHotclick;
            // If terminal, load terminal-specific fields
            EditTerminalType = _selectedLink.Link.TerminalType ?? string.Empty;
            EditCommand = _selectedLink.Link.Command ?? string.Empty;
            IsEditingItem = true;
            
            // Populate available parents and set current parent
            Console.WriteLine($"DEBUG: LoadSelectedItemForEdit - Selected item: {_selectedLink.Link.Name} (ID: {_selectedLink.Link.Id})");
            Console.WriteLine($"DEBUG: LoadSelectedItemForEdit - Item's ParentId: {_selectedLink.Link.ParentId}");
            PopulateAvailableParents();
            EditParentItem = FindParentInAvailableParents(_selectedLink.Link.ParentId);
            Console.WriteLine($"DEBUG: LoadSelectedItemForEdit - Final EditParentItem: {EditParentItem?.Link.Name} (ID: {EditParentItem?.Link.Id})");
        }
        else
        {
            ClearEditForm();
        }
    }

    private void ClearEditForm()
    {
        _originalLinkForEdit = null;
        EditName = string.Empty;
        EditUrl = string.Empty;
        EditDescription = string.Empty;
        EditTags = string.Empty;
        EditNotes = string.Empty;
        EditType = LinkType.WebUrl;
        EditIsHotclick = false;
        EditParentItem = null;
        EditCommand = string.Empty;
        EditTerminalType = string.Empty;
        IsEditingItem = false;
    }

    private void PopulateAvailableParents()
    {
    AvailableParentItems.Clear();
        
        // Add "No Parent" option
        AvailableParentItems.Add(new LinkTreeItemViewModel(new Link 
        { 
            Id = 0, 
            Name = "🏠 Root Level", 
            Type = LinkType.Folder 
        }));
        
        // Add all items as potential parents with hierarchical indentation
        foreach (var rootItem in RootLinks)
        {
            AddItemsToAvailableParentItems(rootItem, 0);
        }
    }

    private void AddItemsToAvailableParentItems(LinkTreeItemViewModel item, int level)
    {
        // Don't add the item being edited as a potential parent of itself
        if (_originalLinkForEdit != null && item.Link.Id == _originalLinkForEdit.Id)
        {
            Console.WriteLine($"DEBUG: Excluding item being edited: {item.Link.Name} (ID: {item.Link.Id})");
            return;
        }
            
        // Don't add descendants of the item being edited (prevent circular references)
        if (_originalLinkForEdit != null && IsDescendantOf(item, _originalLinkForEdit.Id))
        {
            Console.WriteLine($"DEBUG: Excluding descendant of edited item: {item.Link.Name} (ID: {item.Link.Id})");
            return;
        }

        var indentation = new string(' ', level * 3); // 3 spaces per level
        var prefix = level > 0 ? $"{indentation}+- " : "";

        var displayItem = new LinkTreeItemViewModel(new Link
        {
            Id = item.Link.Id,
            Name = $"{prefix}{item.Link.Name}",
            Type = item.Link.Type,
            Url = item.Link.Url,
            Description = item.Link.Description,
            ParentId = item.Link.ParentId,
            SortOrder = item.Link.SortOrder,
            CreatedAt = item.Link.CreatedAt,
            UpdatedAt = item.Link.UpdatedAt
        });
        AvailableParentItems.Add(displayItem);

        foreach (var child in item.Children)
        {
            AddItemsToAvailableParentItems(child, level + 1);
        }
    }

    private bool IsDescendantOf(LinkTreeItemViewModel item, int ancestorId)
    {
        // Check if 'item' is a descendant of 'ancestorId'
        // We need to walk up the parent chain of 'item' to see if we find 'ancestorId'
        var current = item;
        while (current != null && current.Link.ParentId != null)
        {
            if (current.Link.ParentId == ancestorId)
            {
                Console.WriteLine($"DEBUG: {item.Link.Name} (ID: {item.Link.Id}) IS a descendant of {ancestorId}");
                return true;
            }
            // Find the parent in the tree to continue walking up
            current = FindItemInTree(current.Link.ParentId.Value);
        }
        
        Console.WriteLine($"DEBUG: {item.Link.Name} (ID: {item.Link.Id}) is NOT a descendant of {ancestorId}");
        return false;
    }

    private LinkTreeItemViewModel? FindParentInAvailableParents(int? parentId)
    {
        Console.WriteLine($"DEBUG: FindParentInAvailableParents called with parentId: {parentId}");
        Console.WriteLine($"DEBUG: AvailableParentItems count: {AvailableParentItems.Count}");
        Console.WriteLine($"DEBUG: Available parent IDs: {string.Join(", ", AvailableParentItems.Select(p => p.Link.Id))}");
        
        if (parentId == null)
        {
            Console.WriteLine("DEBUG: ParentId is null, returning Root Level");
            return AvailableParentItems.FirstOrDefault(p => p.Link.Id == 0); // Root level
        }
        
        // First try to find the parent in available parents
        var result = AvailableParentItems.FirstOrDefault(p => p.Link.Id == parentId);
        
        if (result != null)
        {
            Console.WriteLine($"DEBUG: Found parent directly: {result.Link.Name} (ID: {result.Link.Id})");
            return result;
        }
        
        Console.WriteLine($"DEBUG: Parent ID {parentId} not found in available parents");
        
        // If parent not found in available parents, it might be excluded to prevent circular reference
        // Let's find the actual parent in the tree and see if we can find a safe alternative
        var actualParent = FindItemInTree(parentId.Value);
        if (actualParent != null)
        {
            Console.WriteLine($"DEBUG: Found actual parent in tree: {actualParent.Link.Name} (ID: {actualParent.Link.Id}, ParentId: {actualParent.Link.ParentId})");
            
            // If the actual parent exists, try to find its parent (grandparent) as the selection
            // This provides a more intelligent fallback than always going to root
            if (actualParent.Link.ParentId != null)
            {
                var grandParent = AvailableParentItems.FirstOrDefault(p => p.Link.Id == actualParent.Link.ParentId);
                if (grandParent != null)
                {
                    Console.WriteLine($"DEBUG: Using grandparent as fallback: {grandParent.Link.Name} (ID: {grandParent.Link.Id})");
                    return grandParent;
                }
                else
                {
                    Console.WriteLine($"DEBUG: Grandparent with ID {actualParent.Link.ParentId} not found in available parents");
                }
            }
            else
            {
                Console.WriteLine("DEBUG: Actual parent has no parent (is root level)");
            }
        }
        else
        {
            Console.WriteLine($"DEBUG: Could not find parent with ID {parentId} in tree");
        }
        
        // Last resort: default to Root Level
        Console.WriteLine("DEBUG: Defaulting to Root Level");
        return AvailableParentItems.FirstOrDefault(p => p.Link.Id == 0);
    }
    
    private LinkTreeItemViewModel? FindItemInTree(int itemId)
    {
        foreach (var rootItem in RootLinks)
        {
            var found = FindItemInSubtree(rootItem, itemId);
            if (found != null)
                return found;
        }
        return null;
    }
    
    private LinkTreeItemViewModel? FindItemInSubtree(LinkTreeItemViewModel item, int itemId)
    {
        if (item.Link.Id == itemId)
            return item;
            
        foreach (var child in item.Children)
        {
            var found = FindItemInSubtree(child, itemId);
            if (found != null)
                return found;
        }
        
        return null;
    }

    private async Task SaveEditAsync()
    {
        if (_originalLinkForEdit == null) return;

        try
        {
            // Update the existing entity's properties directly to avoid tracking issues
            _originalLinkForEdit.Name = EditName.Trim();
            _originalLinkForEdit.Url = EditType == LinkType.Folder || EditType == LinkType.Notes ? null : EditUrl.Trim();
            _originalLinkForEdit.Description = string.IsNullOrWhiteSpace(EditDescription) ? null : EditDescription.Trim();
            _originalLinkForEdit.Notes = EditType == LinkType.Notes ? (string.IsNullOrWhiteSpace(EditNotes) ? null : EditNotes.Trim()) : null;
            _originalLinkForEdit.Type = EditType;
            _originalLinkForEdit.IsHotclick = EditIsHotclick;
            if (EditType == LinkType.Terminal)
            {
                _originalLinkForEdit.Command = string.IsNullOrWhiteSpace(EditCommand) ? null : EditCommand.Trim();
                _originalLinkForEdit.TerminalType = string.IsNullOrWhiteSpace(EditTerminalType) ? null : EditTerminalType.Trim();
            }
            else
            {
                // Clear terminal fields when not terminal
                _originalLinkForEdit.Command = null;
                _originalLinkForEdit.TerminalType = null;
            }
            _originalLinkForEdit.ParentId = EditParentItem?.Link.Id == 0 ? null : EditParentItem?.Link.Id;
            _originalLinkForEdit.UpdatedAt = DateTime.UtcNow;

            await _linkService.UpdateLinkAsync(_originalLinkForEdit);

            // Handle tags after updating the link
            await UpdateLinkTagsAsync(_originalLinkForEdit.Id, EditTags);
            await LoadLinksAsync(); // Refresh the tree
            DisplaySuccessMessage("Link updated successfully!");
        }
        catch (Exception ex)
        {
            DisplayErrorMessage($"Failed to update link: {ex.Message}");
        }
    }

    private bool CanSaveEdit()
    {
        if (_originalLinkForEdit == null) return false;
        if (string.IsNullOrWhiteSpace(EditName)) return false;
        
        // Type-specific validation
        switch (EditType)
        {
            case LinkType.Folder:
                // Folders only need a name
                return true;
                
            case LinkType.Notes:
                // Notes require both name and notes content
                return !string.IsNullOrWhiteSpace(EditNotes);
                
            case LinkType.Terminal:
                // Terminal requires a command
                return !string.IsNullOrWhiteSpace(EditCommand);

            default:
                // All other link types require a URL
                return !string.IsNullOrWhiteSpace(EditUrl);
        }
    }

    private void CancelEdit()
    {
        LoadSelectedItemForEdit(); // Reset to original values
    }

    private async Task DeleteEditAsync()
    {
        if (_originalLinkForEdit == null) return;

        try
        {
            // Capture the name before deletion since the object might become invalid
            var linkName = _originalLinkForEdit.Name;
            var linkId = _originalLinkForEdit.Id;

            Console.WriteLine($"DEBUG: About to delete link '{linkName}' with ID {linkId}");

            // Show confirmation dialog
            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to delete '{linkName}'?\n\nThis action cannot be undone.",
                "Confirm Delete",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning,
                System.Windows.MessageBoxResult.No);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                Console.WriteLine("DEBUG: User confirmed deletion, calling DeleteLinkRecursiveAsync...");
                var deletedItems = await _linkService.DeleteLinkRecursiveAsync(linkId);

                Console.WriteLine("DEBUG: DeleteLinkRecursiveAsync completed, clearing edit form...");
                ClearEditForm(); // Clear the edit form first

                Console.WriteLine("DEBUG: Edit form cleared, refreshing tree...");
                await LoadLinksAsync(); // Refresh the tree

                // Build summary string
                var summary = "The following items were deleted:\n\n" + string.Join("\n", deletedItems.Select(x => $"- {x.Name} ({x.Type})"));

                System.Windows.MessageBox.Show(summary, "Delete Summary", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

                Console.WriteLine("DEBUG: Tree refreshed, showing success message...");
                DisplaySuccessMessage($"{deletedItems.Count} item(s) deleted successfully!");
                Console.WriteLine("DEBUG: Delete operation completed successfully");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DEBUG: Exception in DeleteEditAsync: {ex}");
            DisplayErrorMessage($"Failed to delete link: {ex.Message}");
        }
    }

    private bool CanDeleteEdit()
    {
        return _originalLinkForEdit != null && IsEditingItem;
    }

    private string ConvertTagsToString(ICollection<LinkTag>? linkTags)
    {
        if (linkTags == null || !linkTags.Any())
            return string.Empty;

        return string.Join(", ", linkTags.Select(lt => lt.Tag.Name));
    }

    private async Task<List<int>> ConvertStringToTagIds(string tagsString)
    {
        if (string.IsNullOrWhiteSpace(tagsString))
            return new List<int>();

        var tagNames = tagsString.Split(',')
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var tagIds = new List<int>();
        var allTags = await _tagService.GetAllTagsAsync();
        
        foreach (var tagName in tagNames)
        {
            var existingTag = allTags.FirstOrDefault(t => 
                string.Equals(t.Name, tagName, StringComparison.OrdinalIgnoreCase));

            if (existingTag != null)
            {
                tagIds.Add(existingTag.Id);
            }
            else
            {
                // Create new tag if it doesn't exist
                var newTag = new Tag
                {
                    Name = tagName,
                    Color = GenerateRandomTagColor()
                };
                var createdTag = await _tagService.CreateTagAsync(newTag);
                tagIds.Add(createdTag.Id);
            }
        }

        return tagIds;
    }

    private async Task UpdateLinkTagsAsync(int linkId, string tagsString)
    {
        // Get current tags for the link
        var currentTags = await _tagService.GetTagsForLinkAsync(linkId);
        var currentTagIds = currentTags.Select(t => t.Id).ToHashSet();

        // Get new tag IDs from the string
        var newTagIds = (await ConvertStringToTagIds(tagsString)).ToHashSet();

        // Remove tags that are no longer needed
        foreach (var tagId in currentTagIds.Except(newTagIds))
        {
            await _tagService.RemoveTagFromLinkAsync(linkId, tagId);
        }

        // Add new tags
        foreach (var tagId in newTagIds.Except(currentTagIds))
        {
            await _tagService.AddTagToLinkAsync(linkId, tagId);
        }
    }

    private string GenerateRandomTagColor()
    {
        var colors = new[]
        {
            "#E74C3C", "#3498DB", "#2ECC71", "#F39C12", "#9B59B6",
            "#1ABC9C", "#E67E22", "#34495E", "#E91E63", "#009688"
        };
        
        var random = new Random();
        return colors[random.Next(colors.Length)];
    }

    private async Task ExportDataAsync()
    {
        var exportStartTime = DateTime.Now;
        try
        {
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Export WinWork Data",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json",
                FileName = $"WinWork_Export_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.json"
            };

            if (saveDialog.ShowDialog() == true)
            {
                // Get all data and convert to clean export format (avoiding circular references)
                var allLinks = await _linkService.GetAllLinksAsync();
                var allTags = await _tagService.GetAllTagsAsync();
                
                // Create clean export models without circular references
                var exportLinks = allLinks.Select(link => new
                {
                    Id = link.Id,
                    Name = link.Name,
                    Url = link.Url,
                    Type = link.Type.ToString(),
                    ParentId = link.ParentId,
                    Description = link.Description,
                    Notes = link.Notes,
                    SortOrder = link.SortOrder,
                    IconPath = link.IconPath,
                    CreatedAt = link.CreatedAt,
                    UpdatedAt = link.UpdatedAt,
                    LastAccessedAt = link.LastAccessedAt,
                    AccessCount = link.AccessCount,
                    IsExpanded = link.IsExpanded,
                    IsSelected = link.IsSelected,
                    TagIds = link.LinkTags?.Select(lt => lt.TagId).ToList() ?? new List<int>()
                }).ToList();

                var exportTags = allTags.Select(tag => new
                {
                    Id = tag.Id,
                    Name = tag.Name,
                    Color = tag.Color
                }).ToList();

                var exportData = new
                {
                    ExportDate = DateTime.UtcNow,
                    ExportDateLocal = DateTime.Now,
                    ExportTimezone = TimeZoneInfo.Local.DisplayName,
                    Version = "1.0",
                    Application = "WinWork",
                    Links = exportLinks,
                    Tags = exportTags,
                    Statistics = new
                    {
                        TotalLinks = exportLinks.Count,
                        TotalTags = exportTags.Count,
                        ExportDurationMs = (DateTime.Now - exportStartTime).TotalMilliseconds
                    }
                };

                var jsonOptions = new System.Text.Json.JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                };
                
                var json = System.Text.Json.JsonSerializer.Serialize(exportData, jsonOptions);
                await System.IO.File.WriteAllTextAsync(saveDialog.FileName, json);

                var exportDuration = DateTime.Now - exportStartTime;
                var exportTimeFormatted = exportStartTime.ToString("yyyy-MM-dd HH:mm:ss");
                DisplaySuccessMessage($"Data exported successfully to {System.IO.Path.GetFileName(saveDialog.FileName)}!\n" +
                                    $"📊 {exportLinks.Count} links, {exportTags.Count} tags\n" +
                                    $"🕒 Exported on {exportTimeFormatted}\n" +
                                    $"⚡ Completed in {exportDuration.TotalMilliseconds:F0}ms");
            }
        }
        catch (Exception ex)
        {
            FileLogger.Log($"Export error: {ex.Message}");
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                App.ShowErrorDialog("Export Failed", 
                    "An error occurred while exporting data.", 
                    ex.ToString());
            });
        }
    }

    private async Task ImportDataAsync()
    {
        try
        {
            var openDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Import WinWork Data",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json"
            };

            if (openDialog.ShowDialog() == true)
            {
                var json = await System.IO.File.ReadAllTextAsync(openDialog.FileName);
                
                // Basic validation
                if (string.IsNullOrWhiteSpace(json))
                {
                    throw new InvalidOperationException("File is empty or invalid.");
                }

                // Parse the JSON to validate structure
                using var jsonDoc = System.Text.Json.JsonDocument.Parse(json);
                var root = jsonDoc.RootElement;

                if (!root.TryGetProperty("links", out var linksElement) || 
                    !root.TryGetProperty("tags", out var tagsElement))
                {
                    throw new InvalidOperationException("Invalid import file format. Expected WinWork export format with 'links' and 'tags' properties.");
                }

                // Show import options dialog
                var importOptions = await ShowImportOptionsDialog();
                if (importOptions == null)
                    return;

                IsLoading = true;
                StatusMessage = "Importing data...";

                // Import tags first
                var importedTagsCount = 0;
                if (tagsElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var tagElement in tagsElement.EnumerateArray())
                    {
                        if (tagElement.TryGetProperty("name", out var nameElement) &&
                            tagElement.TryGetProperty("color", out var colorElement))
                        {
                            var tagName = nameElement.GetString();
                            var tagColor = colorElement.GetString();

                            if (!string.IsNullOrEmpty(tagName))
                            {
                                // Check if tag already exists
                                var existingTags = await _tagService.GetAllTagsAsync();
                                var existingTag = existingTags.FirstOrDefault(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase));
                                
                                if (existingTag == null)
                                {
                                    var newTag = new Tag
                                    {
                                        Name = tagName,
                                        Color = tagColor ?? "#007ACC"
                                    };
                                    await _tagService.CreateTagAsync(newTag);
                                    importedTagsCount++;
                                }
                                else if (importOptions.HandleDuplicates == DuplicateHandling.UpdateExisting)
                                {
                                    // Update existing tag color if different
                                    if (existingTag.Color != tagColor)
                                    {
                                        existingTag.Color = tagColor ?? "#007ACC";
                                        await _tagService.UpdateTagAsync(existingTag);
                                    }
                                }
                            }
                        }
                    }
                }

                // Create container folder if needed
                Link? containerFolder = null;
                if (importOptions.CreateContainer)
                {
                    var fileName = System.IO.Path.GetFileNameWithoutExtension(openDialog.FileName);
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH-mm");
                    var containerName = $"📁 Import: {fileName} ({timestamp})";
                    
                    containerFolder = new Link
                    {
                        Name = containerName,
                        Description = $"Imported data from {fileName} on {DateTime.Now:yyyy-MM-dd HH:mm}",
                        Type = LinkType.Folder,
                        ParentId = null,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    containerFolder = await _linkService.CreateLinkAsync(containerFolder);
                }

                // Import links with smart ID mapping
                var importedLinksCount = 0;
                if (linksElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    var importedLinks = new List<(Link ImportedLink, int OriginalId, int? OriginalParentId)>();
                    var oldIdToNewId = new Dictionary<int, int>(); // Maps original IDs to new IDs

                    // First pass: Create all links and build ID mapping
                    foreach (var linkElement in linksElement.EnumerateArray())
                    {
                        var link = new Link();
                        
                        // Get original ID for mapping
                        var originalId = linkElement.TryGetProperty("id", out var idEl) ? idEl.GetInt32() : 0;
                        
                        if (linkElement.TryGetProperty("title", out var titleEl))
                            link.Name = titleEl.GetString() ?? "";
                        else if (linkElement.TryGetProperty("name", out var nameEl))
                            link.Name = nameEl.GetString() ?? "";
                        
                        if (linkElement.TryGetProperty("url", out var urlEl))
                            link.Url = urlEl.GetString();
                        
                        if (linkElement.TryGetProperty("description", out var descEl))
                            link.Description = descEl.GetString();
                        
                        if (linkElement.TryGetProperty("notes", out var notesEl))
                            link.Notes = notesEl.GetString();
                        
                        if (linkElement.TryGetProperty("linkType", out var linkTypeEl))
                        {
                            var typeStr = linkTypeEl.GetString();
                            link.Type = typeStr?.ToLower() switch
                            {
                                "website" => LinkType.WebUrl,
                                "folder" => LinkType.Folder,
                                "application" => LinkType.Application,
                                "document" => LinkType.FilePath,
                                "file" => LinkType.FilePath,
                                _ => LinkType.WebUrl
                            };
                        }
                        else if (linkElement.TryGetProperty("type", out var typeEl))
                        {
                            if (Enum.TryParse<LinkType>(typeEl.GetString(), true, out var linkType))
                                link.Type = linkType;
                        }
                        
                        if (linkElement.TryGetProperty("sortOrder", out var sortEl))
                            link.SortOrder = sortEl.GetInt32();

                        // Handle naming conflicts
                        if (importOptions.HandleDuplicates == DuplicateHandling.Rename)
                        {
                            link.Name = await GetUniqueNameAsync(link.Name, link.Type);
                        }

                        // Set container as parent for root-level items if using container
                        var originalParentId = 0;
                        if (linkElement.TryGetProperty("parentId", out var parentEl) && 
                            parentEl.ValueKind != System.Text.Json.JsonValueKind.Null)
                        {
                            originalParentId = parentEl.GetInt32();
                        }

                        // If no parent and using container, set container as parent
                        if (originalParentId == 0 && containerFolder != null)
                        {
                            link.ParentId = containerFolder.Id;
                        }

                        link.CreatedAt = DateTime.UtcNow;
                        link.UpdatedAt = DateTime.UtcNow;

                        // Create the link
                        var createdLink = await _linkService.CreateLinkAsync(link);
                        importedLinks.Add((createdLink, originalId, originalParentId == 0 ? null : originalParentId));
                        importedLinksCount++;

                        // Build ID mapping
                        if (originalId > 0)
                        {
                            oldIdToNewId[originalId] = createdLink.Id;
                        }

                        // Handle tags
                        if (linkElement.TryGetProperty("tags", out var tagsEl) && tagsEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            var tagNames = new List<string>();
                            foreach (var tagEl in tagsEl.EnumerateArray())
                            {
                                if (tagEl.ValueKind == System.Text.Json.JsonValueKind.String)
                                {
                                    var tagName = tagEl.GetString();
                                    if (!string.IsNullOrEmpty(tagName))
                                        tagNames.Add(tagName);
                                }
                            }
                            
                            if (tagNames.Any())
                            {
                                await UpdateLinkTagsAsync(createdLink.Id, string.Join(", ", tagNames));
                            }
                        }
                    }

                    // Second pass: Update parent relationships using new IDs
                    foreach (var (importedLink, originalId, originalParentId) in importedLinks)
                    {
                        if (originalParentId.HasValue && oldIdToNewId.ContainsKey(originalParentId.Value))
                        {
                            var newParentId = oldIdToNewId[originalParentId.Value];
                            importedLink.ParentId = newParentId;
                            await _linkService.UpdateLinkAsync(importedLink);
                        }
                    }
                }

                await LoadLinksAsync();
                
                var containerMessage = importOptions.CreateContainer ? $" All items were placed under '{containerFolder?.Name}'." : "";
                DisplaySuccessMessage($"Import completed! Imported {importedLinksCount} links and {importedTagsCount} tags.{containerMessage}");
                FileLogger.Log($"Successfully imported {importedLinksCount} links and {importedTagsCount} tags from {openDialog.FileName}");
            }
        }
        catch (Exception ex)
        {
            FileLogger.Log($"Import error: {ex.Message}");
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                App.ShowErrorDialog("Import Failed", 
                    "An error occurred while importing data.", 
                    ex.ToString());
            });
        }
        finally
        {
            IsLoading = false;
            StatusMessage = string.Empty;
        }
    }

    private async Task<ImportOptions?> ShowImportOptionsDialog()
    {
        var options = new ImportOptions();
        
        var dialog = new System.Windows.Window
        {
            Title = "Import Options",
            Width = 500,
            Height = 400,
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
            Owner = System.Windows.Application.Current.MainWindow,
            ResizeMode = System.Windows.ResizeMode.NoResize
        };

        var stackPanel = new System.Windows.Controls.StackPanel
        {
            Margin = new System.Windows.Thickness(20)
        };

        // Title
        var title = new System.Windows.Controls.TextBlock
        {
            Text = "Choose Import Options",
            FontSize = 16,
            FontWeight = System.Windows.FontWeights.Bold,
            Margin = new System.Windows.Thickness(0, 0, 0, 20)
        };
        stackPanel.Children.Add(title);

        // Container option
        var containerCheckBox = new System.Windows.Controls.CheckBox
        {
            Content = "📁 Create import container folder",
            IsChecked = true,
            Margin = new System.Windows.Thickness(0, 0, 0, 10)
        };
        var containerDesc = new System.Windows.Controls.TextBlock
        {
            Text = "   Groups all imported items under a timestamped folder to keep them organized",
            FontSize = 11,
            Foreground = System.Windows.Media.Brushes.Gray,
            Margin = new System.Windows.Thickness(0, 0, 0, 15),
            TextWrapping = System.Windows.TextWrapping.Wrap
        };
        stackPanel.Children.Add(containerCheckBox);
        stackPanel.Children.Add(containerDesc);

        // Duplicate handling
        var duplicateLabel = new System.Windows.Controls.TextBlock
        {
            Text = "Handle duplicate names:",
            FontWeight = System.Windows.FontWeights.SemiBold,
            Margin = new System.Windows.Thickness(0, 0, 0, 5)
        };
        stackPanel.Children.Add(duplicateLabel);

        var skipRadio = new System.Windows.Controls.RadioButton
        {
            Content = "🚫 Skip duplicates",
            IsChecked = true,
            GroupName = "duplicates",
            Margin = new System.Windows.Thickness(20, 0, 0, 5)
        };
        var renameRadio = new System.Windows.Controls.RadioButton
        {
            Content = "📝 Rename duplicates (add number suffix)",
            GroupName = "duplicates",
            Margin = new System.Windows.Thickness(20, 0, 0, 5)
        };
        var updateRadio = new System.Windows.Controls.RadioButton
        {
            Content = "🔄 Update existing items",
            GroupName = "duplicates",
            Margin = new System.Windows.Thickness(20, 0, 0, 15)
        };

        stackPanel.Children.Add(skipRadio);
        stackPanel.Children.Add(renameRadio);
        stackPanel.Children.Add(updateRadio);

        // Buttons
        var buttonPanel = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            Margin = new System.Windows.Thickness(0, 20, 0, 0)
        };

        var importButton = new System.Windows.Controls.Button
        {
            Content = "📥 Import",
            Width = 80,
            Height = 30,
            Margin = new System.Windows.Thickness(0, 0, 10, 0),
            IsDefault = true
        };

        var cancelButton = new System.Windows.Controls.Button
        {
            Content = "❌ Cancel",
            Width = 80,
            Height = 30,
            IsCancel = true
        };

        buttonPanel.Children.Add(importButton);
        buttonPanel.Children.Add(cancelButton);
        stackPanel.Children.Add(buttonPanel);

        dialog.Content = stackPanel;

        bool? result = null;
        importButton.Click += (s, e) => { result = true; dialog.Close(); };
        cancelButton.Click += (s, e) => { result = false; dialog.Close(); };

        dialog.ShowDialog();

        if (result == true)
        {
            options.CreateContainer = containerCheckBox.IsChecked == true;
            options.HandleDuplicates = skipRadio.IsChecked == true ? DuplicateHandling.Skip :
                                     renameRadio.IsChecked == true ? DuplicateHandling.Rename :
                                     DuplicateHandling.UpdateExisting;
            return options;
        }

        return null;
    }

    private async Task<string> GetUniqueNameAsync(string baseName, LinkType linkType)
    {
        var allLinks = await _linkService.GetAllLinksAsync();
        var existingNames = allLinks.Where(l => l.Type == linkType)
                                  .Select(l => l.Name)
                                  .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!existingNames.Contains(baseName))
            return baseName;

        var counter = 1;
        string uniqueName;
        do
        {
            uniqueName = $"{baseName} ({counter})";
            counter++;
        } while (existingNames.Contains(uniqueName));

        return uniqueName;
    }

    private async void OnTreeItemExpansionChanged(object? sender, bool isExpanded)
    {
        if (sender is LinkTreeItemViewModel treeItem)
        {
            try
            {
                // Update the Link in the database
                await _linkService.UpdateLinkAsync(treeItem.Link);
                Console.WriteLine($"Tree expansion state saved: {treeItem.Name} = {isExpanded}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving tree expansion state: {ex.Message}");
            }
        }
    }

    // Events
    public event EventHandler<LinkDialogRequestEventArgs>? LinkDialogRequested;
}

/// <summary>
/// Event arguments for requesting link dialog
/// </summary>
public class LinkDialogRequestEventArgs : EventArgs
{
    public Link? LinkToEdit { get; }
    public LinkType? InitialType { get; }
    public LinkTreeItemViewModel? ParentItem { get; }

    public LinkDialogRequestEventArgs(Link? linkToEdit, LinkType? initialType = null, LinkTreeItemViewModel? parentItem = null)
    {
        LinkToEdit = linkToEdit;
        InitialType = initialType;
        ParentItem = parentItem;
    }
}
