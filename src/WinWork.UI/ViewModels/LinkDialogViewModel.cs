using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WinWork.Models;
using WinWork.UI.Utils;

namespace WinWork.UI.ViewModels;

/// <summary>
/// ViewModel for the Add/Edit Link dialog
/// </summary>
public class LinkDialogViewModel : ViewModelBase
{
    private string _name = string.Empty;
    private string _url = string.Empty;
    private string _description = string.Empty;
    private string _notes = string.Empty;
    private string _tagsString = string.Empty;
    private LinkType _selectedLinkType = LinkType.WebUrl;
    private LinkTypeItem? _selectedLinkTypeItem;
    private bool _isEditMode;
    private Link? _originalLink;
    private LinkTreeItemViewModel? _parentItem;
    private LinkTreeItemViewModel? _selectedParent;

    public ObservableCollection<LinkTypeItem> LinkTypes { get; }
    public ObservableCollection<LinkTreeItemViewModel> AvailableParents { get; }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand BrowseFileCommand { get; }
    public ICommand BrowseFolderCommand { get; }
    public ICommand BrowseApplicationCommand { get; }


    public string DialogTitle 
    {
        get
        {
            if (_isEditMode)
                return "Edit Item";
            
            string itemType = _selectedLinkType switch
            {
                LinkType.Folder => "Folder",
                LinkType.Notes => "Note",
                _ => "Link"
            };
            
            if (_parentItem != null)
                return $"Add New {itemType} to '{_parentItem.Name}'";
            
            return $"Add New {itemType}";
        }
    }
    
    public string Name
    {
        get => _name;
        set 
        { 
            if (SetProperty(ref _name, value))
            {
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string Url
    {
        get => _url;
        set 
        { 
            if (SetProperty(ref _url, value))
            {
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public string Notes
    {
        get => _notes;
        set 
        { 
            if (SetProperty(ref _notes, value))
            {
                // Trigger validation update when Notes changes
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string TagsString
    {
        get => _tagsString;
        set => SetProperty(ref _tagsString, value);
    }

    public LinkType SelectedLinkType
    {
        get => _selectedLinkType;
        set
        {
            Console.WriteLine($"DEBUG: SelectedLinkType setter called with value: {value}");
            if (SetProperty(ref _selectedLinkType, value))
            {
                Console.WriteLine($"DEBUG: SelectedLinkType changed to {value}");
                
                // Update the selected item to match the new type (avoid circular reference)
                var matchingItem = LinkTypes.FirstOrDefault(x => x.Type == value);
                if (matchingItem != _selectedLinkTypeItem)
                {
                    _selectedLinkTypeItem = matchingItem;
                    OnPropertyChanged(nameof(SelectedLinkTypeItem));
                }
                
                UpdateUrlPlaceholder();
                OnPropertyChanged(nameof(IsFolderType));
                OnPropertyChanged(nameof(IsNotesType));
                OnPropertyChanged(nameof(RequiresUrl));
                OnPropertyChanged(nameof(DialogTitle));
                Console.WriteLine($"DEBUG: IsNotesType is now {IsNotesType}");
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public LinkTypeItem? SelectedLinkTypeItem
    {
        get => _selectedLinkTypeItem;
        set
        {
            Console.WriteLine($"DEBUG: SelectedLinkTypeItem setter called with value: {value?.DisplayName ?? "null"}");
            if (SetProperty(ref _selectedLinkTypeItem, value))
            {
                if (value != null && value.Type != _selectedLinkType)
                {
                    Console.WriteLine($"DEBUG: Setting SelectedLinkType to {value.Type} from SelectedLinkTypeItem");
                    _selectedLinkType = value.Type; // Set directly to avoid circular reference
                    
                    // Manually trigger the property change notifications
                    OnPropertyChanged(nameof(SelectedLinkType));
                    UpdateUrlPlaceholder();
                    OnPropertyChanged(nameof(IsFolderType));
                    OnPropertyChanged(nameof(IsNotesType));
                    OnPropertyChanged(nameof(RequiresUrl));
                    OnPropertyChanged(nameof(DialogTitle));
                    Console.WriteLine($"DEBUG: IsNotesType is now {IsNotesType}");
                    System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                }
            }
        }
    }

    public string UrlPlaceholder { get; private set; } = "https://example.com";

    public bool IsFolderType => SelectedLinkType == LinkType.Folder;
    
    public bool IsNotesType 
    { 
        get 
        { 
            bool result = SelectedLinkType == LinkType.Notes;
            Console.WriteLine($"DEBUG: IsNotesType getter called - SelectedLinkType={SelectedLinkType}, result={result}");
            return result;
        } 
    }
    
    public bool RequiresUrl => SelectedLinkType != LinkType.Folder && SelectedLinkType != LinkType.Notes;

    public bool IsEditMode
    {
        get => _isEditMode;
        private set 
        { 
            SetProperty(ref _isEditMode, value);
            Console.WriteLine($"DEBUG: IsEditMode set to {value}");
            OnPropertyChanged(nameof(DeleteButtonVisibility));
        }
    }

    public System.Windows.Visibility DeleteButtonVisibility
    {
        get 
        {
            var visibility = IsEditMode ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            Console.WriteLine($"DEBUG: DeleteButtonVisibility returning {visibility} (IsEditMode={IsEditMode})");
            return visibility;
        }
    }

    public LinkTreeItemViewModel? SelectedParent
    {
        get => _selectedParent;
        set => SetProperty(ref _selectedParent, value);
    }

    // Events
    public event EventHandler<LinkSaveEventArgs>? LinkSaved;
    public event EventHandler? DialogCancelled;
    public event EventHandler<LinkDeleteEventArgs>? LinkDeleted;

    public LinkDialogViewModel()
    {
        LinkTypes = new ObservableCollection<LinkTypeItem>
        {
            new(LinkType.Folder, "📁 Folder", "Organize items into groups"),
            new(LinkType.WebUrl, "🌐 Web URL", "Website or web page"),
            new(LinkType.FilePath, "📄 File", "Local file or document"),
            new(LinkType.Application, "💻 Application", "Executable program"),
            new(LinkType.Notes, "📝 Notes", "Text notes and memos")
        };

        AvailableParents = new ObservableCollection<LinkTreeItemViewModel>();

        // Initialize commands
        SaveCommand = new RelayCommand(() => {
            Save();
        }, () => {
            bool result = CanSave();
            return result;
        });
        CancelCommand = new RelayCommand(Cancel);
        DeleteCommand = new RelayCommand(Delete, CanDelete);
        BrowseFileCommand = new RelayCommand(BrowseFile);
        BrowseFolderCommand = new RelayCommand(BrowseFolder);
        BrowseApplicationCommand = new RelayCommand(BrowseApplication);

        UpdateUrlPlaceholder();
        
        // Initialize default selection for ComboBox
        SelectedLinkTypeItem = LinkTypes.FirstOrDefault(x => x.Type == _selectedLinkType);
    }

    public void SetInitialType(LinkType linkType)
    {
        SelectedLinkType = linkType;
        if (linkType == LinkType.Folder)
        {
            Name = "New Folder";
        }
    }

    public void SetEditMode(Link link)
    {
        var startMessage = "DEBUG: SetEditMode called";
        FileLogger.Log(startMessage);
        
        var linkTypeMessage = $"DEBUG: Link type = {link.Type}";
        FileLogger.Log(linkTypeMessage);
        
        var linkIdMessage = $"DEBUG: Link ID = {link.Id}";
        FileLogger.Log(linkIdMessage);
        
        var linkNameMessage = $"DEBUG: Link Name = '{link.Name}'";
        FileLogger.Log(linkNameMessage);
        
        var linkNotesMessage = $"DEBUG: Link Notes = '{link.Notes ?? "null"}'";
        FileLogger.Log(linkNotesMessage);
        
        _originalLink = link;
        IsEditMode = true;
        
        Name = link.Name;
        Url = link.Url ?? string.Empty;
        Description = link.Description ?? string.Empty;
        Notes = link.Notes ?? string.Empty;
        
        var settingTypeMessage = $"DEBUG: Setting SelectedLinkType to {link.Type}";
        FileLogger.Log(settingTypeMessage);
        SelectedLinkType = link.Type;
        
        // Set the selected item for ComboBox binding
        SelectedLinkTypeItem = LinkTypes.FirstOrDefault(x => x.Type == link.Type);
        var selectedItemMessage = $"DEBUG: SelectedLinkTypeItem set to: {SelectedLinkTypeItem?.DisplayName}";
        FileLogger.Log(selectedItemMessage);
        
        var finalTypeMessage = $"DEBUG: SelectedLinkType is now {SelectedLinkType}";
        FileLogger.Log(finalTypeMessage);
        
        var isNotesMessage = $"DEBUG: IsNotesType is now {IsNotesType}";
        FileLogger.Log(isNotesMessage);
        
        var notesPropertyMessage = $"DEBUG: Notes property is now '{Notes}'";
        FileLogger.Log(notesPropertyMessage);
        
        var editModeMessage = $"DEBUG: IsEditMode is now {IsEditMode}";
        FileLogger.Log(editModeMessage);

        // Load tags as comma-separated string
        if (link.LinkTags != null && link.LinkTags.Any())
        {
            TagsString = string.Join(", ", link.LinkTags.Select(lt => lt.Tag?.Name).Where(n => !string.IsNullOrEmpty(n)));
        }
        else
        {
            TagsString = string.Empty;
        }
    }
    
    public void SetParentContext(LinkTreeItemViewModel? parentItem)
    {
        Console.WriteLine($"SetParentContext: Setting parent = {parentItem?.Name ?? "null"} (ID: {parentItem?.Link?.Id ?? 0})");
        _parentItem = parentItem;
        SelectedParent = parentItem;
        OnPropertyChanged(nameof(DialogTitle));
        Console.WriteLine($"SetParentContext: Dialog title updated to: {DialogTitle}");
    }

    public void SetAvailableParents(IEnumerable<LinkTreeItemViewModel> allFolders)
    {
        try
        {
            AvailableParents.Clear();
            
            // Add "Root Level" option (null parent)
            var rootOption = new LinkTreeItemViewModel(new Link 
            { 
                Id = 0, 
                Name = "🏠 Root Level", 
                Type = LinkType.Folder 
            });
            AvailableParents.Add(rootOption);
            
            // Add all folders except the one being edited (to prevent circular references)
            var foldersToAdd = allFolders?.Where(f => f?.Link != null && 
                                                      f.Link.Type == LinkType.Folder && 
                                                      f.Link.Id != _originalLink?.Id) ?? Enumerable.Empty<LinkTreeItemViewModel>();
            
            foreach (var folder in foldersToAdd)
            {
                if (folder != null)
                {
                    AvailableParents.Add(folder);
                }
            }
            
            Console.WriteLine($"SetAvailableParents: Added {AvailableParents.Count} parent options");
            
            // Set selected parent based on existing link's ParentId (for edit mode)
            if (IsEditMode && _originalLink != null)
            {
                if (_originalLink.ParentId == null)
                {
                    SelectedParent = rootOption; // Root level
                }
                else
                {
                    SelectedParent = AvailableParents.FirstOrDefault(p => p?.Link?.Id == _originalLink.ParentId);
                }
                Console.WriteLine($"SetAvailableParents: Set selected parent to {SelectedParent?.Name ?? "null"} for edit mode");
            }
            else
            {
                // For new items, default to root level if no parent context was set
                if (SelectedParent == null)
                {
                    SelectedParent = rootOption;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR in SetAvailableParents: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }



    private void UpdateUrlPlaceholder()
    {
        UrlPlaceholder = _selectedLinkType switch
        {
            LinkType.WebUrl => "https://example.com",
            LinkType.FilePath => @"C:\path\to\file.txt",
            LinkType.FolderPath => @"C:\path\to\folder",
            LinkType.Application => @"C:\Program Files\App\app.exe",
            LinkType.WindowsStoreApp => "ms-windows-store://pdp/?productid=...",
            LinkType.SystemLocation => "ms-settings:display",
            _ => "Enter URL or path..."
        };
        OnPropertyChanged(nameof(UrlPlaceholder));
    }

    private bool CanSave()
    {
        
        // Name is always required for all types
        if (string.IsNullOrWhiteSpace(_name)) 
        {
            return false;
        }
        
        // Type-specific validation
        switch (_selectedLinkType)
        {
            case LinkType.Folder:
                // Folders only need a name
                return true;
                
            case LinkType.Notes:
                // Notes require both name and notes content
                bool hasNotes = !string.IsNullOrWhiteSpace(_notes);
                return hasNotes;
                
            default:
                // All other link types require a URL
                bool hasUrl = !string.IsNullOrWhiteSpace(_url);
                return hasUrl;
        }
    }

    private void Save()
    {
        
        if (!CanSave()) 
        {
            return;
        }

        var link = _originalLink ?? new Link();
        
        link.Name = _name.Trim();
        link.Url = _url.Trim();
        link.Description = string.IsNullOrWhiteSpace(_description) ? null : _description.Trim();
        link.Notes = string.IsNullOrWhiteSpace(_notes) ? null : _notes.Trim();
        link.Type = _selectedLinkType;
        
        // Set parent ID based on selected parent (including for edits)
        if (SelectedParent != null && SelectedParent.Link.Id > 0)
        {
            link.ParentId = SelectedParent.Link.Id;
            Console.WriteLine($"Save: Setting ParentId = {link.ParentId} for link '{link.Name}' (Parent: {SelectedParent.Name})");
        }
        else
        {
            link.ParentId = null; // Root level
            Console.WriteLine($"Save: Setting ParentId = null (root level) for link '{link.Name}'");
        }
        
        if (!IsEditMode)
        {
            link.CreatedAt = DateTime.UtcNow;
        }
        link.UpdatedAt = DateTime.UtcNow;

        LinkSaved?.Invoke(this, new LinkSaveEventArgs(link, TagsString, IsEditMode));
    }

    private void Cancel()
    {
        DialogCancelled?.Invoke(this, EventArgs.Empty);
    }

    private bool CanDelete()
    {
        return IsEditMode && _originalLink != null;
    }

    private void Delete()
    {
        if (_originalLink != null)
        {
            LinkDeleted?.Invoke(this, new LinkDeleteEventArgs(_originalLink));
        }
    }

    private void BrowseFile()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select File",
            Filter = "All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            Url = dialog.FileName;
            if (string.IsNullOrWhiteSpace(_name))
            {
                Name = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
            }
        }
    }

    private void BrowseFolder()
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
                Url = dialog.FileName;
                if (string.IsNullOrWhiteSpace(_name))
                {
                    Name = System.IO.Path.GetFileName(dialog.FileName);
                }
            }
        }
        catch (Exception ex)
        {
            // Fallback to simple text input
        }
    }

    private void BrowseApplication()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Application",
            Filter = "Executable Files (*.exe)|*.exe|Batch Files (*.bat)|*.bat|Command Files (*.cmd)|*.cmd|All Files (*.*)|*.*",
            FilterIndex = 1
        };

        if (dialog.ShowDialog() == true)
        {
            Url = dialog.FileName;
            if (string.IsNullOrWhiteSpace(_name))
            {
                Name = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
            }
        }
    }


}

/// <summary>
/// Represents a link type option in the UI
/// </summary>
public class LinkTypeItem
{
    public LinkType Type { get; }
    public string DisplayName { get; }
    public string Description { get; }

    public LinkTypeItem(LinkType type, string displayName, string description)
    {
        Type = type;
        DisplayName = displayName;
        Description = description;
    }
}

/// <summary>
/// Event arguments for link save operation
/// </summary>
public class LinkSaveEventArgs : EventArgs
{
    public Link Link { get; }
    public string TagsString { get; }
    public bool IsEditMode { get; }

    public LinkSaveEventArgs(Link link, string tagsString, bool isEditMode)
    {
        Link = link;
        TagsString = tagsString;
        IsEditMode = isEditMode;
    }
}

public class LinkDeleteEventArgs : EventArgs
{
    public Link Link { get; }

    public LinkDeleteEventArgs(Link link)
    {
        Link = link;
    }
}

/// <summary>
/// Generic relay command with parameter
/// </summary>
public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;

    public void Execute(object? parameter) => _execute((T?)parameter);
}
