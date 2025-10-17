using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LinkerApp.Core.Services;
using LinkerApp.Models;
using LinkerApp.UI.Utils;

namespace LinkerApp.UI.ViewModels;

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
    private readonly ILinkService _linkService;
    private readonly ITagService _tagService;
    private readonly ISettingsService _settingsService;
    private readonly ILinkOpenerService _linkOpenerService;
    
    private string _title = "LinkerApp";
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
    private bool _isEditingItem = false;
    private Link? _originalLinkForEdit;
    private LinkTreeItemViewModel? _editParent;

    public ObservableCollection<LinkTreeItemViewModel> RootLinks { get; }
    public ObservableCollection<LinkTypeItem> LinkTypes { get; }
    public ObservableCollection<LinkTreeItemViewModel> AvailableParents { get; }

    // Commands
    public ICommand AddLinkCommand { get; }
    public ICommand AddFolderCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand EditLinkCommand { get; }
    public ICommand SaveEditCommand { get; }
    public ICommand CancelEditCommand { get; }
    public ICommand DeleteEditCommand { get; }

    public MainWindowViewModel(
        ILinkService linkService,
        ITagService tagService,
        ISettingsService settingsService,
        ILinkOpenerService linkOpenerService)
    {
        _linkService = linkService;
        _tagService = tagService;
        _settingsService = settingsService;
        _linkOpenerService = linkOpenerService;

        RootLinks = new ObservableCollection<LinkTreeItemViewModel>();
        AvailableParents = new ObservableCollection<LinkTreeItemViewModel>();
        LinkTypes = new ObservableCollection<LinkTypeItem>
        {
            new LinkTypeItem(LinkType.Folder, "📁 Folder", "Organize items into groups"),
            new LinkTypeItem(LinkType.WebUrl, "🌐 Web URL", "Website or web page"),
            new LinkTypeItem(LinkType.FilePath, "� File", "Local file or document"),
            new LinkTypeItem(LinkType.Application, "⚙️ Application", "Executable program"),
            new LinkTypeItem(LinkType.Notes, "� Notes", "Text notes and memos")
        };

        // Initialize commands
        AddLinkCommand = new AsyncRelayCommand(AddLinkAsync);
        AddFolderCommand = new AsyncRelayCommand(AddFolderAsync);
        RefreshCommand = new AsyncRelayCommand(LoadLinksAsync);
        EditLinkCommand = new RelayCommand<LinkTreeItemViewModel>(EditLink);
        SaveEditCommand = new AsyncRelayCommand(SaveEditAsync, CanSaveEdit);
        CancelEditCommand = new RelayCommand(CancelEdit);
        DeleteEditCommand = new AsyncRelayCommand(DeleteEditAsync, CanDeleteEdit);
        
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
        set => SetProperty(ref _editType, value);
    }

    public bool IsEditingItem
    {
        get => _isEditingItem;
        set => SetProperty(ref _isEditingItem, value);
    }

    public LinkTreeItemViewModel? EditParent
    {
        get => _editParent;
        set => SetProperty(ref _editParent, value);
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
            System.Diagnostics.Debug.WriteLine($"Error loading data: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task LoadLinksAsync()
    {
        Console.WriteLine("LoadLinksAsync started");
        try
        {
            var rootLinks = await _linkService.GetRootLinksAsync();
            Console.WriteLine($"LoadLinksAsync: Got {rootLinks.Count()} root links from service");
            
            // Ensure UI updates happen on UI thread
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                RootLinks.Clear();
                Console.WriteLine("RootLinks cleared on UI thread");
            });
            
            foreach (var link in rootLinks)
            {
                Console.WriteLine($"Processing root link: {link.Name} (Type: {link.Type}, ParentId: {link.ParentId})");
                var viewModel = await CreateLinkTreeItemAsync(link);
                
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    RootLinks.Add(viewModel);
                    Console.WriteLine($"Added {link.Name} to RootLinks on UI thread with {viewModel.Children.Count} children");
                });
            }
            
            // Log final structure
            Console.WriteLine("=== FINAL TREE STRUCTURE ===");
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                foreach (var rootItem in RootLinks)
                {
                    Console.WriteLine($"ROOT: {rootItem.Name} ({rootItem.Children.Count} children)");
                    foreach (var child in rootItem.Children)
                    {
                        Console.WriteLine($"  CHILD: {child.Name} ({child.Children.Count} children)");
                        foreach (var grandChild in child.Children)
                        {
                            Console.WriteLine($"    GRANDCHILD: {grandChild.Name}");
                        }
                    }
                }
            });
            Console.WriteLine("=== END TREE STRUCTURE ===");
            
            Console.WriteLine($"LoadLinksAsync completed: RootLinks collection now has {RootLinks.Count} items");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading links: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private async Task<LinkTreeItemViewModel> CreateLinkTreeItemAsync(Link link)
    {
            Console.WriteLine($"CreateLinkTreeItemAsync: Processing link '{link.Name}' (ID: {link.Id})");
        
        var children = await _linkService.GetChildLinksAsync(link.Id);
        Console.WriteLine($"CreateLinkTreeItemAsync: Found {children.Count()} children for '{link.Name}'");
        
        var childViewModels = new List<LinkTreeItemViewModel>();
        
        foreach (var child in children)
        {
            Console.WriteLine($"  - Child: '{child.Name}' (Type: {child.Type}, ParentId: {child.ParentId})");
            var childViewModel = await CreateLinkTreeItemAsync(child);
            childViewModels.Add(childViewModel);
        }
        
        var treeItem = new LinkTreeItemViewModel(link, childViewModels);
        Console.WriteLine($"CreateLinkTreeItemAsync: Created TreeItem for '{link.Name}' with {treeItem.Children.Count} children");
        
        return treeItem;
    }

    // Command implementations
    private async Task AddLinkAsync()
    {
        await ShowLinkDialog();
    }

    private async Task AddFolderAsync()
    {
        System.Diagnostics.Debug.WriteLine("AddFolderAsync started - showing dialog for folder");
        await ShowLinkDialog(initialType: LinkType.Folder);
    }

    private void EditLink(LinkTreeItemViewModel? linkTreeItem)
    {
        if (linkTreeItem?.Link != null)
        {
            System.Diagnostics.Debug.WriteLine($"DEBUG: EditLink called for link: {linkTreeItem.Link.Name}, Type: {linkTreeItem.Link.Type}, ID: {linkTreeItem.Link.Id}");
            FileLogger.Log($"EditLink called for link: {linkTreeItem.Link.Name}, Type: {linkTreeItem.Link.Type}, ID: {linkTreeItem.Link.Id}");
            _ = ShowLinkDialog(linkTreeItem.Link);
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
            System.Diagnostics.Debug.WriteLine($"Error opening link: {ex.Message}");
        }
    }

    // Dialog methods
    public async Task ShowLinkDialog(Link? linkToEdit = null, LinkType? initialType = null, LinkTreeItemViewModel? parentItem = null)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"DEBUG: ShowLinkDialog called - linkToEdit: {linkToEdit?.Name ?? "null"}, initialType: {initialType}, parentItem: {parentItem?.Name ?? "null"}");
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
            System.Diagnostics.Debug.WriteLine($"Error showing link dialog: {ex.Message}");
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving link: {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine($"Error performing search: {ex.Message}");
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
            IsEditingItem = true;
            
            // Populate available parents and set current parent
            PopulateAvailableParents();
            EditParent = FindParentInAvailableParents(_selectedLink.Link.ParentId);
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
        EditParent = null;
        IsEditingItem = false;
    }

    private void PopulateAvailableParents()
    {
        AvailableParents.Clear();
        
        // Add "No Parent" option
        AvailableParents.Add(new LinkTreeItemViewModel(new Link 
        { 
            Id = 0, 
            Name = "📁 Root Level", 
            Type = LinkType.Folder 
        }));
        
        // Add all items as potential parents with hierarchical indentation
        foreach (var rootItem in RootLinks)
        {
            AddItemsToAvailableParents(rootItem, 0);
        }
    }

    private void AddItemsToAvailableParents(LinkTreeItemViewModel item, int level)
    {
        // Don't add the item being edited or any of its children as potential parents
        if (_originalLinkForEdit != null && IsDescendantOf(item, _originalLinkForEdit.Id))
            return;
            
        // Create a copy of the item with hierarchical display name
        var indentation = new string(' ', level * 3); // 3 spaces per level
        var prefix = level > 0 ? $"{indentation}└─ " : "";
        
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
        
        // Allow any item type to be a parent (folders, links, etc.)
        AvailableParents.Add(displayItem);
        
        foreach (var child in item.Children)
        {
            AddItemsToAvailableParents(child, level + 1);
        }
    }

    private bool IsDescendantOf(LinkTreeItemViewModel item, int ancestorId)
    {
        if (item.Link.Id == ancestorId)
            return true;
            
        return item.Children.Any(child => IsDescendantOf(child, ancestorId));
    }

    private LinkTreeItemViewModel? FindParentInAvailableParents(int? parentId)
    {
        if (parentId == null)
            return AvailableParents.FirstOrDefault(p => p.Link.Id == 0); // Root level
            
        return AvailableParents.FirstOrDefault(p => p.Link.Id == parentId);
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
            _originalLinkForEdit.ParentId = EditParent?.Link.Id == 0 ? null : EditParent?.Link.Id;
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
                Console.WriteLine("DEBUG: User confirmed deletion, calling DeleteLinkAsync...");
                await _linkService.DeleteLinkAsync(linkId);
                
                Console.WriteLine("DEBUG: DeleteLinkAsync completed, clearing edit form...");
                ClearEditForm(); // Clear the edit form first
                
                Console.WriteLine("DEBUG: Edit form cleared, refreshing tree...");
                await LoadLinksAsync(); // Refresh the tree
                
                Console.WriteLine("DEBUG: Tree refreshed, showing success message...");
                DisplaySuccessMessage($"'{linkName}' deleted successfully!");
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