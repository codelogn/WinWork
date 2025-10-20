using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using WinWork.Models;

namespace WinWork.UI.ViewModels;

/// <summary>
/// ViewModel for the Tag Management dialog
/// </summary>
public class TagManagementViewModel : ViewModelBase
{
    private string _newTagName = string.Empty;
    private string _selectedColorHex = "#4CAF50";
    private TagViewModel? _selectedTag;
    private bool _isEditMode;

    public ObservableCollection<TagViewModel> Tags { get; }
    public ObservableCollection<ColorOption> ColorOptions { get; }

    public ICommand AddTagCommand { get; }
    public ICommand EditTagCommand { get; }
    public ICommand DeleteTagCommand { get; }
    public ICommand SaveEditCommand { get; }
    public ICommand CancelEditCommand { get; }
    public ICommand CloseCommand { get; }

    public string NewTagName
    {
        get => _newTagName;
        set => SetProperty(ref _newTagName, value);
    }

    public string SelectedColorHex
    {
        get => _selectedColorHex;
        set => SetProperty(ref _selectedColorHex, value);
    }

    public TagViewModel? SelectedTag
    {
        get => _selectedTag;
        set => SetProperty(ref _selectedTag, value);
    }

    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }

    // Events
    public event EventHandler<TagEventArgs>? TagAdded;
    public event EventHandler<TagEventArgs>? TagUpdated;
    public event EventHandler<TagEventArgs>? TagDeleted;
    public event EventHandler? CloseRequested;

    public TagManagementViewModel()
    {
        Tags = new ObservableCollection<TagViewModel>();
        ColorOptions = new ObservableCollection<ColorOption>
        {
            new("#F44336", "Red"),
            new("#E91E63", "Pink"),
            new("#9C27B0", "Purple"),
            new("#673AB7", "Deep Purple"),
            new("#3F51B5", "Indigo"),
            new("#2196F3", "Blue"),
            new("#03A9F4", "Light Blue"),
            new("#00BCD4", "Cyan"),
            new("#009688", "Teal"),
            new("#4CAF50", "Green"),
            new("#8BC34A", "Light Green"),
            new("#CDDC39", "Lime"),
            new("#FFEB3B", "Yellow"),
            new("#FFC107", "Amber"),
            new("#FF9800", "Orange"),
            new("#FF5722", "Deep Orange"),
            new("#795548", "Brown"),
            new("#9E9E9E", "Grey"),
            new("#607D8B", "Blue Grey")
        };

        // Initialize commands
        AddTagCommand = new RelayCommand(AddTag, CanAddTag);
        EditTagCommand = new RelayCommand<TagViewModel>(EditTag);
        DeleteTagCommand = new RelayCommand<TagViewModel>(DeleteTag);
        SaveEditCommand = new RelayCommand(SaveEdit, CanSaveEdit);
        CancelEditCommand = new RelayCommand(CancelEdit);
        CloseCommand = new RelayCommand(Close);
    }

    public void LoadTags(IEnumerable<Tag> tags)
    {
        Tags.Clear();
        foreach (var tag in tags)
        {
            Tags.Add(new TagViewModel(tag));
        }
    }

    private bool CanAddTag()
    {
        return !string.IsNullOrWhiteSpace(_newTagName) && !IsEditMode;
    }

    private void AddTag()
    {
        if (!CanAddTag()) return;

        var tag = new Tag
        {
            Name = _newTagName.Trim(),
            Color = _selectedColorHex
        };

        TagAdded?.Invoke(this, new TagEventArgs(tag, false));
        
        // Reset form
        NewTagName = string.Empty;
        SelectedColorHex = "#4CAF50";
    }

    private void EditTag(TagViewModel? tagViewModel)
    {
        if (tagViewModel == null) return;

        SelectedTag = tagViewModel;
        NewTagName = tagViewModel.Name;
        SelectedColorHex = tagViewModel.Tag.Color;
        IsEditMode = true;
    }

    private bool CanSaveEdit()
    {
        return !string.IsNullOrWhiteSpace(_newTagName) && IsEditMode && SelectedTag != null;
    }

    private void SaveEdit()
    {
        if (!CanSaveEdit() || SelectedTag == null) return;

        SelectedTag.Tag.Name = _newTagName.Trim();
        SelectedTag.Tag.Color = _selectedColorHex;

        TagUpdated?.Invoke(this, new TagEventArgs(SelectedTag.Tag, true));
        
        CancelEdit();
    }

    private void CancelEdit()
    {
        IsEditMode = false;
        SelectedTag = null;
        NewTagName = string.Empty;
        SelectedColorHex = "#4CAF50";
    }

    private void DeleteTag(TagViewModel? tagViewModel)
    {
        if (tagViewModel == null) return;

        TagDeleted?.Invoke(this, new TagEventArgs(tagViewModel.Tag, true));
    }

    private void Close()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    public void RefreshTag(Tag updatedTag)
    {
        var existingTag = Tags.FirstOrDefault(t => t.Tag.Id == updatedTag.Id);
        if (existingTag != null)
        {
            existingTag.Tag.Name = updatedTag.Name;
            existingTag.Tag.Color = updatedTag.Color;
            // Trigger property change notification
            existingTag.OnPropertyChanged(nameof(existingTag.Name));
            existingTag.OnPropertyChanged(nameof(existingTag.Color));
        }
    }

    public void AddNewTag(Tag newTag)
    {
        Tags.Add(new TagViewModel(newTag));
    }

    public void RemoveTag(Tag tag)
    {
        var tagToRemove = Tags.FirstOrDefault(t => t.Tag.Id == tag.Id);
        if (tagToRemove != null)
        {
            Tags.Remove(tagToRemove);
        }
    }
}

/// <summary>
/// Represents a color option for tags
/// </summary>
public class ColorOption
{
    public string HexValue { get; }
    public string Name { get; }
    public SolidColorBrush Brush { get; }

    public ColorOption(string hexValue, string name)
    {
        HexValue = hexValue;
        Name = name;
        
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hexValue);
            Brush = new SolidColorBrush(color);
        }
        catch
        {
            Brush = new SolidColorBrush(Colors.Gray);
        }
    }
}

/// <summary>
/// Event arguments for tag operations
/// </summary>
public class TagEventArgs : EventArgs
{
    public Tag Tag { get; }
    public bool IsExistingTag { get; }

    public TagEventArgs(Tag tag, bool isExistingTag)
    {
        Tag = tag;
        IsExistingTag = isExistingTag;
    }
}
