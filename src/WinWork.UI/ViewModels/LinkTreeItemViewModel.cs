using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using WinWork.Models;

namespace WinWork.UI.ViewModels;

/// <summary>
/// ViewModel for individual tree view items in the hierarchical link structure
/// </summary>
public class LinkTreeItemViewModel : INotifyPropertyChanged
{
    public LinkTreeItemViewModel? Parent { get; set; }
    private bool _isExpanded;
    private bool _isSelected;

    public Link Link { get; }
    public ObservableCollection<LinkTreeItemViewModel> Children { get; }
    public ObservableCollection<TagViewModel> Tags { get; }

    public string Name => Link.Name;
    public string Url => Link.Url;
    public string Description => Link.Description ?? string.Empty;

    public string Icon => GetIconForLinkType(Link.Type);
    public SolidColorBrush IconBackground => GetIconBackground(Link.Type);

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    // Commands
    public ICommand AddLinkCommand { get; }
    public ICommand AddFolderCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand CopyUrlCommand { get; }
    public ICommand OpenInBrowserCommand { get; }

    public LinkTreeItemViewModel(Link link, IEnumerable<LinkTreeItemViewModel>? children = null)
    {
        Link = link;
        Children = new ObservableCollection<LinkTreeItemViewModel>(children ?? Enumerable.Empty<LinkTreeItemViewModel>());
        Tags = new ObservableCollection<TagViewModel>();

        // Initialize commands
        AddLinkCommand = new RelayCommand(() => OnAddLink());
        AddFolderCommand = new RelayCommand(() => OnAddFolder());
        EditCommand = new RelayCommand(() => OnEdit());
        DeleteCommand = new RelayCommand(() => OnDelete());
        CopyUrlCommand = new RelayCommand(() => OnCopyUrl());
        OpenInBrowserCommand = new RelayCommand(() => OnOpenInBrowser());

        // Load tags
        LoadTags();
    }

    private void LoadTags()
    {
        if (Link.LinkTags != null)
        {
            foreach (var linkTag in Link.LinkTags)
            {
                if (linkTag.Tag != null)
                {
                    Tags.Add(new TagViewModel(linkTag.Tag));
                }
            }
        }
    }

    private static string GetIconForLinkType(LinkType type) => type switch
    {
        LinkType.WebUrl => "🌐",
        LinkType.FilePath => "📄",
        LinkType.Folder => "📁",
        LinkType.FolderPath => "📁",
        LinkType.Application => "⚙️",
        LinkType.WindowsStoreApp => "📱",
        LinkType.SystemLocation => "🖥️",
        _ => "🔗"
    };

    private static SolidColorBrush GetIconBackground(LinkType type) => new(type switch
    {
        LinkType.WebUrl => Color.FromRgb(0x4A, 0x90, 0xE2),      // Blue
        LinkType.FilePath => Color.FromRgb(0x7E, 0xD3, 0x21),     // Green
        LinkType.Folder => Color.FromRgb(0xFF, 0xB3, 0x00),   // Orange
        LinkType.FolderPath => Color.FromRgb(0xFF, 0xB3, 0x00),   // Orange
        LinkType.Application => Color.FromRgb(0x9C, 0x27, 0xB0), // Purple
        LinkType.WindowsStoreApp => Color.FromRgb(0x9C, 0x27, 0xB0), // Purple
        LinkType.SystemLocation => Color.FromRgb(0x60, 0x7D, 0x8B),   // Blue Grey
        _ => Color.FromRgb(0x75, 0x75, 0x75)                  // Grey
    });

    // Command handlers (to be implemented by the main view model)
    public event EventHandler? AddLinkRequested;
    public event EventHandler? AddFolderRequested;
    public event EventHandler? EditRequested;
    public event EventHandler? DeleteRequested;
    public event EventHandler? CopyUrlRequested;
    public event EventHandler? OpenInBrowserRequested;

    private void OnAddLink() => AddLinkRequested?.Invoke(this, EventArgs.Empty);
    private void OnAddFolder() => AddFolderRequested?.Invoke(this, EventArgs.Empty);
    private void OnEdit() => EditRequested?.Invoke(this, EventArgs.Empty);
    private void OnDelete() => DeleteRequested?.Invoke(this, EventArgs.Empty);
    private void OnCopyUrl() => CopyUrlRequested?.Invoke(this, EventArgs.Empty);
    private void OnOpenInBrowser() => OpenInBrowserRequested?.Invoke(this, EventArgs.Empty);

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion
}

/// <summary>
/// ViewModel for tags displayed in the tree view
/// </summary>
public class TagViewModel
{
    public Tag Tag { get; }

    public string Name => Tag.Name;
    public SolidColorBrush Color
    {
        get
        {
            try
            {
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(Tag.Color);
                return new SolidColorBrush(color);
            }
            catch
            {
                return new SolidColorBrush(System.Windows.Media.Colors.Gray);
            }
        }
    }

    public TagViewModel(Tag tag)
    {
        Tag = tag;
    }

    public void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

/// <summary>
/// Simple relay command implementation
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();
}

public class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _executeAsync;
    private readonly Func<bool>? _canExecute;
    private bool _isExecuting;

    public AsyncRelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
    {
        _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke() ?? true);

    public async void Execute(object? parameter)
    {
        if (_isExecuting) return;
        
        try
        {
            _isExecuting = true;
            CommandManager.InvalidateRequerySuggested();
            await _executeAsync();
        }
        catch (Exception ex)
        {
            // Log or handle the exception appropriately
            // Ignore async command errors silently
        }
        finally
        {
            _isExecuting = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
