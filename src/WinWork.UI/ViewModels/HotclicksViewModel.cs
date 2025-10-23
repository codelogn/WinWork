using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using WinWork.Core.Interfaces;
using WinWork.Core.Services;
using WinWork.Models;
using WinWork.UI.Utils;

namespace WinWork.UI.ViewModels
{
    public class HotclicksViewModel : ViewModelBase
    {
        private readonly WinWork.Core.Interfaces.ILinkService _linkService;
        private readonly ILinkOpenerService _linkOpenerService;
        private string _searchText = string.Empty;
        private string? _selectedTag;

        // All hotclick items grouped by tags
        private readonly Dictionary<string, List<LinkTreeItemViewModel>> _hotclickItemsByTag = new();
        
        // Collections for binding
        public ObservableCollection<string> HotclickTags { get; } = new ObservableCollection<string>();
        public ObservableCollection<LinkTreeItemViewModel> SelectedTagItems { get; } = new ObservableCollection<LinkTreeItemViewModel>();
        
        private ICollectionView? _filteredHotclickTags;
        public ICollectionView FilteredHotclickTags
        {
            get
            {
                if (_filteredHotclickTags == null)
                {
                    _filteredHotclickTags = CollectionViewSource.GetDefaultView(HotclickTags);
                    _filteredHotclickTags.Filter = FilterHotclickTags;
                }
                return _filteredHotclickTags;
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilteredHotclickTags.Refresh();
                }
            }
        }

        public string? SelectedTag
        {
            get => _selectedTag;
            set
            {
                if (SetProperty(ref _selectedTag, value))
                {
                    LoadSelectedTagItems();
                }
            }
        }

        public ICommand OpenAllCommand { get; }
        public ICommand RefreshCommand { get; }

        public HotclicksViewModel(WinWork.Core.Interfaces.ILinkService linkService, ILinkOpenerService linkOpenerService)
        {
            _linkService = linkService;
            _linkOpenerService = linkOpenerService;

            OpenAllCommand = new AsyncRelayCommand(OpenAllAsync, () => SelectedTagItems.Count > 0);
            RefreshCommand = new AsyncRelayCommand(LoadHotclickItemsAsync);

            _ = LoadHotclickItemsAsync();
        }

        private bool FilterHotclickTags(object item)
        {
            if (item is not string tagName)
                return false;

            if (string.IsNullOrWhiteSpace(SearchText))
                return true;

            return tagName.ToLowerInvariant().Contains(SearchText.ToLowerInvariant());
        }

        private void LoadSelectedTagItems()
        {
            SelectedTagItems.Clear();
            
            if (string.IsNullOrEmpty(SelectedTag) || !_hotclickItemsByTag.ContainsKey(SelectedTag))
                return;

            var items = _hotclickItemsByTag[SelectedTag];
            foreach (var item in items)
            {
                SelectedTagItems.Add(item);
            }
        }

        private async Task LoadHotclickItemsAsync()
        {
            HotclickTags.Clear();
            SelectedTagItems.Clear();
            _hotclickItemsByTag.Clear();
            
            var allLinks = await _linkService.GetAllLinksAsync();
            var hotclickLinks = allLinks.Where(l => l.IsHotclick).ToList();

            // Group hotclick items by their tags
            var tagGroups = new Dictionary<string, List<LinkTreeItemViewModel>>();
            
            foreach (var link in hotclickLinks)
            {
                var linkViewModel = new LinkTreeItemViewModel(link);
                
                // Get tags for this link (assuming tags are stored in a Tags property or similar)
                var linkTags = GetTagsFromLink(link);
                
                if (linkTags.Any())
                {
                    foreach (var tag in linkTags)
                    {
                        if (!tagGroups.ContainsKey(tag))
                            tagGroups[tag] = new List<LinkTreeItemViewModel>();
                        
                        tagGroups[tag].Add(linkViewModel);
                    }
                }
                else
                {
                    // Add to "Untagged" category if no tags
                    const string untaggedKey = "Untagged";
                    if (!tagGroups.ContainsKey(untaggedKey))
                        tagGroups[untaggedKey] = new List<LinkTreeItemViewModel>();
                    
                    tagGroups[untaggedKey].Add(linkViewModel);
                }
            }

            // Populate collections
            foreach (var tagGroup in tagGroups.OrderBy(kvp => kvp.Key))
            {
                HotclickTags.Add(tagGroup.Key);
                _hotclickItemsByTag[tagGroup.Key] = tagGroup.Value;
            }
            
            // Refresh filtered view
            FilteredHotclickTags?.Refresh();
            
            // Select first tag if available
            if (HotclickTags.Count > 0)
            {
                SelectedTag = HotclickTags[0];
            }
        }

        private List<string> GetTagsFromLink(Link link)
        {
            // Get tags from the LinkTags navigation property
            if (link.LinkTags == null || !link.LinkTags.Any())
                return new List<string>();

            return link.LinkTags
                .Where(lt => lt.Tag != null)
                .Select(lt => lt.Tag.Name)
                .Where(tagName => !string.IsNullOrWhiteSpace(tagName))
                .ToList();
        }

        private async Task OpenAllAsync()
        {
            foreach (var item in SelectedTagItems)
            {
                try
                {
                    await _linkOpenerService.OpenAsync(item.Link);
                }
                catch
                {
                    // ignore per-item errors - could be aggregated later
                }
            }
        }
    }
}
