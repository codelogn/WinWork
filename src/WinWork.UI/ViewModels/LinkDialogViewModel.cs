using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows;
using WinWork.Models;
using WinWork.UI.Utils;

namespace WinWork.UI.ViewModels;

/// <summary>
/// ViewModel for the Add/Edit Link dialog
/// </summary>
public class LinkDialogViewModel : ViewModelBase
{
    public bool CanSave => CanSaveInternal();
    private string _name = string.Empty;
    private string _url = string.Empty;
    private string _description = string.Empty;
    private string _notes = string.Empty;
    private string _command = string.Empty;
    private string _tagsString = string.Empty;
    private LinkType _selectedLinkType = LinkType.WebUrl;
    // SelectedLinkTypeItem removed: selection is driven by SelectedLinkType enum
    private bool _isEditMode;
    private Link? _originalLink;
    private LinkTreeItemViewModel? _parentItem;
    private LinkTreeItemViewModel? _selectedParent;
    private string _terminalShell = string.Empty;
    private string _terminalType = string.Empty;
    private bool _isHotclick = false;
    private string _validationMessage = string.Empty;

    public ObservableCollection<LinkTypeItem> LinkTypes { get; }
    public ObservableCollection<LinkTreeItemViewModel> AvailableParents { get; }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand BrowseFileCommand { get; }
    public ICommand BrowseFolderCommand { get; }
    public ICommand BrowseApplicationCommand { get; }
    public ICommand TestCommand { get; private set; }


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

    public string Command
    {
        get => _command;
        set
        {
            if (SetProperty(ref _command, value))
            {
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string TerminalShell
    {
        get => _terminalShell;
        set => SetProperty(ref _terminalShell, value);
    }

    public string TerminalType
    {
        get => _terminalType;
        set => SetProperty(ref _terminalType, value);
    }

    // Normalize a potentially malformed terminal type string stored in DB
    private string NormalizeTerminalType(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        var s = raw.Trim();

        // e.g. "System.Windows.Controls.ComboBoxItem: Git Bash" -> take text after colon
        var idx = s.IndexOf(':');
        if (idx >= 0 && idx < s.Length - 1)
        {
            var after = s.Substring(idx + 1).Trim();
            if (!string.IsNullOrEmpty(after)) s = after;
        }

        // Strip quotes
        s = s.Trim('"', '\'');

        // Canonicalize common values
        if (s.Equals("powershell", StringComparison.OrdinalIgnoreCase) || s.Equals("power shell", StringComparison.OrdinalIgnoreCase)) return "PowerShell";
        if (s.IndexOf("git", StringComparison.OrdinalIgnoreCase) >= 0 && s.IndexOf("bash", StringComparison.OrdinalIgnoreCase) >= 0) return "Git Bash";
        if (s.Equals("cmd", StringComparison.OrdinalIgnoreCase) || s.Equals("command", StringComparison.OrdinalIgnoreCase)) return "CMD";

        var match = TerminalOptions.FirstOrDefault(t => string.Equals(t, s, StringComparison.OrdinalIgnoreCase));
        if (match != null) return match;

        return s;
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
                
                // Keep LinkTypes in sync for any UI display; selection logic is driven by SelectedLinkType
                // No SelectedLinkTypeItem field to update here
                
                UpdateUrlPlaceholder();
                OnPropertyChanged(nameof(IsFolderType));
                OnPropertyChanged(nameof(IsNotesType));
                OnPropertyChanged(nameof(IsTerminalType));
                // Notify visibility properties so UI shows/hides panels immediately when type changes
                OnPropertyChanged(nameof(TerminalPanelVisibility));
                OnPropertyChanged(nameof(UrlPanelVisibility));
                OnPropertyChanged(nameof(RequiresUrl));
                OnPropertyChanged(nameof(DialogTitle));
                Console.WriteLine($"DEBUG: IsNotesType is now {IsNotesType}");
                // If user switched to Terminal while in the Add form, ensure a default terminal type is selected
                if (IsTerminalType && string.IsNullOrWhiteSpace(TerminalType))
                {
                    try
                    {
                        if (_uiSettingsService != null)
                        {
                            var def = _uiSettingsService.GetDefaultTerminalAsync().GetAwaiter().GetResult();
                            if (!string.IsNullOrWhiteSpace(def)) TerminalType = def;
                        }
                    }
                    catch
                    {
                        // ignore
                    }
                    if (string.IsNullOrWhiteSpace(TerminalType))
                        TerminalType = "PowerShell";
                    OnPropertyChanged(nameof(TerminalType));
                }
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool IsTerminalType => SelectedLinkType == LinkType.Terminal;
    public Visibility TerminalPanelVisibility => IsTerminalType ? Visibility.Visible : Visibility.Collapsed;
    public Visibility UrlPanelVisibility => (RequiresUrl && !IsTerminalType) ? Visibility.Visible : Visibility.Collapsed;

    // SelectedLinkTypeItem removed. The UI ComboBox should bind SelectedValue to SelectedLinkType (enum).

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

    public bool IsHotclick
    {
        get => _isHotclick;
        set => SetProperty(ref _isHotclick, value);
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        set => SetProperty(ref _validationMessage, value);
    }

    // Events
    public event EventHandler<LinkSaveEventArgs>? LinkSaved;
    public event EventHandler? DialogCancelled;
    public event EventHandler<LinkDeleteEventArgs>? LinkDeleted;
    public event EventHandler<TerminalTestEventArgs>? TestRequested;

    private readonly WinWork.Core.Services.ISettingsService? _uiSettingsService;

    public ObservableCollection<string> TerminalOptions { get; } = new ObservableCollection<string>();

    public LinkDialogViewModel(WinWork.Core.Services.ISettingsService? settingsService = null)
    {
        _uiSettingsService = settingsService;
        // Initialize link types from a single provider so Add/Edit always use the same options
        LinkTypes = new ObservableCollection<LinkTypeItem>(LinkTypeProvider.DefaultLinkTypes);

        AvailableParents = new ObservableCollection<LinkTreeItemViewModel>();

        // Initialize commands
        SaveCommand = new RelayCommand(() => {
            Save();
        }, () => {
            bool result = CanSave;
            return result;
        });
        CancelCommand = new RelayCommand(Cancel);
        DeleteCommand = new RelayCommand(Delete, CanDelete);
        BrowseFileCommand = new RelayCommand(BrowseFile);
        BrowseFolderCommand = new RelayCommand(BrowseFolder);
        BrowseApplicationCommand = new RelayCommand(BrowseApplication);
    TestCommand = new RelayCommand(() => OnTestRequested(), () => IsTerminalType && !string.IsNullOrWhiteSpace(Command));

        UpdateUrlPlaceholder();

        // Terminal options (static list of types; paths come from app settings when launching)
        TerminalOptions.Add("PowerShell");
        TerminalOptions.Add("Git Bash");
        TerminalOptions.Add("CMD");

        // Ensure Command change reevaluates TestCommand availability
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Command) || e.PropertyName == nameof(TerminalType) || e.PropertyName == nameof(SelectedLinkType))
            {
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        };

    // Note: do not force SelectedLinkTypeItem here; SetEditMode will assign it when editing

        // If settings provided, set TerminalType default
        try
        {
            if (_uiSettingsService != null)
            {
                var def = _uiSettingsService.GetDefaultTerminalAsync().GetAwaiter().GetResult();
                if (!string.IsNullOrWhiteSpace(def))
                {
                    TerminalType = def;
                }
            }
        }
        catch
        {
            // ignore settings errors; fall back to PowerShell
            if (string.IsNullOrWhiteSpace(TerminalType)) TerminalType = "PowerShell";
        }
    }

    public void SetInitialType(LinkType linkType)
    {
        SelectedLinkType = linkType;
        OnPropertyChanged(nameof(IsTerminalType));
        if (linkType == LinkType.Folder)
        {
            Name = "New Folder";
        }
        // If starting a new Terminal, ensure terminal fields have sensible defaults
        if (linkType == LinkType.Terminal)
        {
            // Use configured default if available, otherwise fall back to PowerShell
            if (string.IsNullOrWhiteSpace(TerminalType))
            {
                try
                {
                    if (_uiSettingsService != null)
                    {
                        var def = _uiSettingsService.GetDefaultTerminalAsync().GetAwaiter().GetResult();
                        if (!string.IsNullOrWhiteSpace(def)) TerminalType = def;
                    }
                }
                catch
                {
                    // ignore
                }
                if (string.IsNullOrWhiteSpace(TerminalType)) TerminalType = "PowerShell";
            }
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
        
        // First set edit state and selected type so UI bindings react immediately
        _originalLink = link;
        IsEditMode = true;

        // Set the SelectedLinkType early to drive UI visibility and ComboBox selection
        var settingTypeMessage = $"DEBUG: Setting SelectedLinkType to {link.Type}";
        FileLogger.Log(settingTypeMessage);
        SelectedLinkType = link.Type;

        // Now populate fields that depend on type
        Name = link.Name;
        Url = link.Url ?? string.Empty;
        // For Terminal items, TerminalType is stored per-item (do not reuse Url)
        TerminalType = NormalizeTerminalType(link.TerminalType ?? link.Url ?? string.Empty);
        Description = link.Description ?? string.Empty;
        Notes = link.Notes ?? string.Empty;
        Command = link.Command ?? string.Empty;
        IsHotclick = link.IsHotclick;

        // Ensure visibility properties and terminal selection bind correctly
        OnPropertyChanged(nameof(IsTerminalType));
        OnPropertyChanged(nameof(TerminalPanelVisibility));
        OnPropertyChanged(nameof(UrlPanelVisibility));

    // Determine the display name for the selected LinkType for logging
    var matchingItem = LinkTypes.FirstOrDefault(x => x.Type == link.Type);

        // If terminal, try to select the corresponding TerminalOptions entry
        if (link.Type == LinkType.Terminal && !string.IsNullOrWhiteSpace(TerminalType))
        {
            var canonical = NormalizeTerminalType(TerminalType);
            if (!string.IsNullOrWhiteSpace(canonical))
            {
                // If TerminalOptions contains it, set TerminalType to the canonical value
                var opt = TerminalOptions.FirstOrDefault(t => string.Equals(t, canonical, StringComparison.OrdinalIgnoreCase));
                if (opt != null)
                {
                    TerminalType = opt;
                }
                else
                {
                    TerminalType = canonical;
                }
                OnPropertyChanged(nameof(TerminalType));
            }
        }
    var selectedItemMessage = $"DEBUG: SelectedLinkType display = {matchingItem?.DisplayName ?? "(none)"}";
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

        int parentId = parentItem?.Link?.Id ?? 0;
        var matchingParent = AvailableParents.FirstOrDefault(p => p?.Link?.Id == parentId);
        if (matchingParent != null)
        {
            SelectedParent = matchingParent;
            Console.WriteLine($"SetParentContext: Selected parent by ID: {matchingParent.Name}");
        }
        else
        {
            // Fallback to root if not found
            var rootOption = AvailableParents.FirstOrDefault(p => p?.Link?.Id == 0);
            SelectedParent = rootOption;
            Console.WriteLine($"SetParentContext: Parent not found, selecting root level");
        }

        OnPropertyChanged(nameof(DialogTitle));
        Console.WriteLine($"SetParentContext: Final SelectedParent = {SelectedParent?.Name ?? "null"}, Dialog title updated to: {DialogTitle}");
    }

    public void SetAvailableParents(IEnumerable<LinkTreeItemViewModel> allItems)
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
            
            // Add all items except the one being edited (to prevent circular references)
            var itemsToAdd = allItems?.Where(f => f?.Link != null && 
                                                  f.Link.Id != _originalLink?.Id) ?? Enumerable.Empty<LinkTreeItemViewModel>();
            
            foreach (var item in itemsToAdd)
            {
                if (item != null)
                {
                    AvailableParents.Add(item);
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
                // But don't override if SelectedParent was already set by SetParentContext
                if (SelectedParent == null)
                {
                    SelectedParent = rootOption;
                    Console.WriteLine($"SetAvailableParents: Defaulted to root level for new item");
                }
                else
                {
                    // Respect SelectedParent set by SetParentContext
                    Console.WriteLine($"SetAvailableParents: Keeping existing SelectedParent: {SelectedParent?.Name ?? "null"}");
                }
            }
        }
    catch (Exception)
        {
            Console.WriteLine($"ERROR in SetAvailableParents");
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

    private bool CanSaveInternal()
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
                // All other link types require a URL, except Terminal which requires a command
                if (_selectedLinkType == LinkType.Terminal)
                {
                    return !string.IsNullOrWhiteSpace(_command);
                }
                bool hasUrl = !string.IsNullOrWhiteSpace(_url);
                return hasUrl;
        }
    }

    private void Save()
    {
        
    if (!CanSave) 
        {
            return;
        }

        var link = _originalLink ?? new Link();
        
        link.Name = _name.Trim();
        link.Url = _url.Trim();
        link.Description = string.IsNullOrWhiteSpace(_description) ? null : _description.Trim();
        link.Notes = string.IsNullOrWhiteSpace(_notes) ? null : _notes.Trim();
        link.Type = _selectedLinkType;
        link.IsHotclick = _isHotclick;
        if (_selectedLinkType == LinkType.Terminal)
        {
            // Store chosen terminal type/profile in TerminalType and store commands in Command
            link.TerminalType = string.IsNullOrWhiteSpace(_terminalType) ? null : _terminalType.Trim();
            // For backward compatibility, don't overwrite Url unless TerminalType is empty
            if (string.IsNullOrWhiteSpace(link.TerminalType))
            {
                link.Url = string.IsNullOrWhiteSpace(_terminalShell) ? null : _terminalShell.Trim();
            }
            link.Command = string.IsNullOrWhiteSpace(_command) ? null : _command.Trim();
        }
        else
        {
            // Ensure non-terminal types don't accidentally keep Command
            if (_originalLink == null || _originalLink.Type != LinkType.Terminal)
            {
                link.Command = null;
            }
        }
        
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
    catch (Exception)
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

    private void OnTestRequested()
    {
        // Raise an event so the UI host can execute the terminal without saving
        TestRequested?.Invoke(this, new TerminalTestEventArgs(TerminalType, Command));
    }

    public class TerminalTestEventArgs : EventArgs
    {
        public string TerminalType { get; }
        public string Command { get; }

        public TerminalTestEventArgs(string terminalType, string command)
        {
            TerminalType = terminalType ?? string.Empty;
            Command = command ?? string.Empty;
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
