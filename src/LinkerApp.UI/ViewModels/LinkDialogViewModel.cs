using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LinkerApp.Models;

namespace LinkerApp.UI.ViewModels;

/// <summary>
/// ViewModel for the Add/Edit Link dialog
/// </summary>
public class LinkDialogViewModel : ViewModelBase
{
    private string _name = string.Empty;
    private string _url = string.Empty;
    private string _description = string.Empty;
    private LinkType _selectedLinkType = LinkType.WebUrl;
    private bool _isEditMode;
    private Link? _originalLink;
    private LinkTreeItemViewModel? _parentItem;
    private LinkTreeItemViewModel? _selectedParent;

    public ObservableCollection<LinkTypeItem> LinkTypes { get; }
    public ObservableCollection<TagViewModel> AvailableTags { get; }
    public ObservableCollection<TagViewModel> SelectedTags { get; }
    public ObservableCollection<LinkTreeItemViewModel> AvailableParents { get; }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand BrowseFileCommand { get; }
    public ICommand BrowseFolderCommand { get; }
    public ICommand AddTagCommand { get; }
    public ICommand RemoveTagCommand { get; }

    public string DialogTitle 
    {
        get
        {
            if (_isEditMode)
                return "Edit Link";
            
            if (_parentItem != null)
                return $"Add New {(_selectedLinkType == LinkType.Folder ? "Folder" : "Link")} to '{_parentItem.Name}'";
            
            return $"Add New {(_selectedLinkType == LinkType.Folder ? "Folder" : "Link")}";
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

    public LinkType SelectedLinkType
    {
        get => _selectedLinkType;
        set
        {
            if (SetProperty(ref _selectedLinkType, value))
            {
                UpdateUrlPlaceholder();
                OnPropertyChanged(nameof(IsFolderType));
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string UrlPlaceholder { get; private set; } = "https://example.com";

    public bool IsFolderType => SelectedLinkType == LinkType.Folder;

    public bool IsEditMode
    {
        get => _isEditMode;
        private set => SetProperty(ref _isEditMode, value);
    }

    public LinkTreeItemViewModel? SelectedParent
    {
        get => _selectedParent;
        set => SetProperty(ref _selectedParent, value);
    }

    // Events
    public event EventHandler<LinkSaveEventArgs>? LinkSaved;
    public event EventHandler? DialogCancelled;

    public LinkDialogViewModel()
    {
        LinkTypes = new ObservableCollection<LinkTypeItem>
        {
            new(LinkType.WebUrl, "🌐 Web URL", "Website or web page"),
            new(LinkType.FilePath, "📄 File", "Local file or document"),
            new(LinkType.FolderPath, "📁 Folder", "Local folder or directory"),
            new(LinkType.Application, "⚙️ Application", "Executable program"),
            new(LinkType.WindowsStoreApp, "📱 Store App", "Windows Store application"),
            new(LinkType.SystemLocation, "🖥️ System", "System location or setting")
        };

        AvailableTags = new ObservableCollection<TagViewModel>();
        SelectedTags = new ObservableCollection<TagViewModel>();
        AvailableParents = new ObservableCollection<LinkTreeItemViewModel>();

        // Initialize commands
        SaveCommand = new RelayCommand(Save, CanSave);
        CancelCommand = new RelayCommand(Cancel);
        BrowseFileCommand = new RelayCommand(BrowseFile);
        BrowseFolderCommand = new RelayCommand(BrowseFolder);
        AddTagCommand = new RelayCommand<TagViewModel>(AddTag);
        RemoveTagCommand = new RelayCommand<TagViewModel>(RemoveTag);

        UpdateUrlPlaceholder();
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
        _originalLink = link;
        IsEditMode = true;
        
        Name = link.Name;
        Url = link.Url ?? string.Empty;
        Description = link.Description ?? string.Empty;
        SelectedLinkType = link.Type;

        // Load selected tags
        SelectedTags.Clear();
        if (link.LinkTags != null)
        {
            foreach (var linkTag in link.LinkTags)
            {
                if (linkTag.Tag != null)
                {
                    SelectedTags.Add(new TagViewModel(linkTag.Tag));
                }
            }
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

    public void LoadAvailableTags(IEnumerable<Tag> tags)
    {
        AvailableTags.Clear();
        foreach (var tag in tags)
        {
            AvailableTags.Add(new TagViewModel(tag));
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
        // Name is always required
        if (string.IsNullOrWhiteSpace(_name)) 
        {
            System.Diagnostics.Debug.WriteLine($"CanSave: Name is empty or whitespace: '{_name}'");
            return false;
        }
        
        // For folders, URL is optional
        if (_selectedLinkType == LinkType.Folder) 
        {
            System.Diagnostics.Debug.WriteLine("CanSave: Folder type, returning true");
            return true;
        }
        
        // For other types, URL is required
        bool hasUrl = !string.IsNullOrWhiteSpace(_url);
        System.Diagnostics.Debug.WriteLine($"CanSave: Type {_selectedLinkType}, URL '{_url}', hasUrl = {hasUrl}");
        return hasUrl;
    }

    private void Save()
    {
        System.Diagnostics.Debug.WriteLine("LinkDialogViewModel.Save() called");
        System.Diagnostics.Debug.WriteLine($"CanSave() = {CanSave()}");
        System.Diagnostics.Debug.WriteLine($"Name = '{_name}', URL = '{_url}', Type = {_selectedLinkType}");
        
        if (!CanSave()) 
        {
            System.Diagnostics.Debug.WriteLine("CanSave() returned false, exiting Save()");
            return;
        }

        var link = _originalLink ?? new Link();
        
        link.Name = _name.Trim();
        link.Url = _url.Trim();
        link.Description = string.IsNullOrWhiteSpace(_description) ? null : _description.Trim();
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

        var selectedTagIds = SelectedTags.Select(t => t.Tag.Id).ToList();

        System.Diagnostics.Debug.WriteLine("Invoking LinkSaved event");
        LinkSaved?.Invoke(this, new LinkSaveEventArgs(link, selectedTagIds, IsEditMode));
        System.Diagnostics.Debug.WriteLine("LinkSaved event invoked");
    }

    private void Cancel()
    {
        DialogCancelled?.Invoke(this, EventArgs.Empty);
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
            System.Diagnostics.Debug.WriteLine($"Error browsing folder: {ex.Message}");
            // Fallback to simple text input
        }
    }

    private void AddTag(TagViewModel? tag)
    {
        if (tag != null && !SelectedTags.Any(t => t.Tag.Id == tag.Tag.Id))
        {
            SelectedTags.Add(tag);
        }
    }

    private void RemoveTag(TagViewModel? tag)
    {
        if (tag != null)
        {
            SelectedTags.Remove(tag);
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
    public List<int> SelectedTagIds { get; }
    public bool IsEditMode { get; }

    public LinkSaveEventArgs(Link link, List<int> selectedTagIds, bool isEditMode)
    {
        Link = link;
        SelectedTagIds = selectedTagIds;
        IsEditMode = isEditMode;
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