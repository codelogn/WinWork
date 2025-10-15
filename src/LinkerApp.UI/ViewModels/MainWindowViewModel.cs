using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LinkerApp.Core.Services;
using LinkerApp.Models;

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
    private LinkType _editType = LinkType.WebUrl;
    private bool _isEditingItem = false;
    private Link? _originalLinkForEdit;

    public ObservableCollection<LinkTreeItemViewModel> RootLinks { get; }
    public ObservableCollection<LinkTypeItem> LinkTypes { get; }

    // Commands
    public ICommand AddLinkCommand { get; }
    public ICommand AddFolderCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand SaveEditCommand { get; }
    public ICommand CancelEditCommand { get; }

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
        LinkTypes = new ObservableCollection<LinkTypeItem>
        {
            new LinkTypeItem(LinkType.WebUrl, "🌐 Web Link", "HTTP/HTTPS website"),
            new LinkTypeItem(LinkType.FilePath, "📄 File Path", "Local file"),
            new LinkTypeItem(LinkType.Folder, "📁 Folder", "Container for other items"),
            new LinkTypeItem(LinkType.FolderPath, "📁 Folder Path", "Windows Explorer folder"),
            new LinkTypeItem(LinkType.Application, "⚙️ Application", "Executable program"),
            new LinkTypeItem(LinkType.WindowsStoreApp, "📱 Windows Store App", "UWP/Store application"),
            new LinkTypeItem(LinkType.SystemLocation, "🖥️ System Location", "Control Panel, Settings, etc.")
        };

        // Initialize commands
        AddLinkCommand = new AsyncRelayCommand(AddLinkAsync);
        AddFolderCommand = new AsyncRelayCommand(AddFolderAsync);
        RefreshCommand = new AsyncRelayCommand(LoadLinksAsync);
        SaveEditCommand = new AsyncRelayCommand(SaveEditAsync, CanSaveEdit);
        CancelEditCommand = new RelayCommand(CancelEdit);
        
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
            // This will be called from the UI layer
            LinkDialogRequested?.Invoke(this, new LinkDialogRequestEventArgs(linkToEdit, initialType, parentItem));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing link dialog: {ex.Message}");
        }
    }

    public async Task HandleLinkSaved(Link link, List<int> tagIds, bool isEditMode)
    {
        try
        {
            string actionType = link.Type == LinkType.Folder ? "folder" : "link";
            string action = isEditMode ? "updated" : "created";
            
            if (isEditMode)
            {
                await _linkService.UpdateLinkAsync(link);
            }
            else
            {
                await _linkService.CreateLinkAsync(link);
            }

            // TODO: Update tags when tag management is implemented
            // For now, we'll skip tag assignment

            await LoadLinksAsync();
            DisplaySuccessMessage($"{char.ToUpper(actionType[0])}{actionType.Substring(1)} '{link.Name}' {action} successfully!");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving link: {ex.Message}");
            Console.WriteLine($"ERROR in HandleLinkSaved: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            
            string actionType = link.Type == LinkType.Folder ? "folder" : "link";
            
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
            EditType = _selectedLink.Link.Type;
            IsEditingItem = true;
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
        EditType = LinkType.WebUrl;
        IsEditingItem = false;
    }

    private async Task SaveEditAsync()
    {
        if (_originalLinkForEdit == null) return;

        try
        {
            // Update the existing entity's properties directly to avoid tracking issues
            _originalLinkForEdit.Name = EditName.Trim();
            _originalLinkForEdit.Url = EditType == LinkType.Folder ? null : EditUrl.Trim();
            _originalLinkForEdit.Description = string.IsNullOrWhiteSpace(EditDescription) ? null : EditDescription.Trim();
            _originalLinkForEdit.Type = EditType;
            _originalLinkForEdit.UpdatedAt = DateTime.UtcNow;

            await _linkService.UpdateLinkAsync(_originalLinkForEdit);
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
        if (EditType != LinkType.Folder && string.IsNullOrWhiteSpace(EditUrl)) return false;
        return true;
    }

    private void CancelEdit()
    {
        LoadSelectedItemForEdit(); // Reset to original values
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