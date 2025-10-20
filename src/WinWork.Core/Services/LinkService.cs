using WinWork.Models;
using WinWork.Data.Repositories;

namespace WinWork.Core.Services;

/// <summary>
/// Service implementation for link management operations
/// </summary>
public class LinkService : ILinkService
{
    private readonly ILinkRepository _linkRepository;
    private readonly ILinkOpenerService _linkOpenerService;

    public LinkService(ILinkRepository linkRepository, ILinkOpenerService linkOpenerService)
    {
        _linkRepository = linkRepository;
        _linkOpenerService = linkOpenerService;
    }

    public async Task<IEnumerable<Link>> GetAllLinksAsync()
    {
        return await _linkRepository.GetAllAsync();
    }

    public async Task<Link?> GetLinkAsync(int id)
    {
        return await _linkRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Link>> GetRootLinksAsync()
    {
        return await _linkRepository.GetRootLinksAsync();
    }

    public async Task<IEnumerable<Link>> GetChildLinksAsync(int parentId)
    {
        return await _linkRepository.GetChildrenAsync(parentId);
    }

    public async Task<IEnumerable<Link>> SearchLinksAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return Enumerable.Empty<Link>();

        return await _linkRepository.SearchAsync(searchTerm.Trim());
    }

    public async Task<IEnumerable<Link>> GetLinksByTagAsync(int tagId)
    {
        return await _linkRepository.GetByTagAsync(tagId);
    }

    public async Task<IEnumerable<Link>> GetMostAccessedLinksAsync(int count = 10)
    {
        return await _linkRepository.GetMostAccessedAsync(count);
    }

    public async Task<IEnumerable<Link>> GetRecentLinksAsync(int count = 10)
    {
        return await _linkRepository.GetRecentlyAccessedAsync(count);
    }

    public async Task<Link> CreateLinkAsync(Link link)
    {
        if (!await ValidateLinkAsync(link))
            throw new ArgumentException("Invalid link data", nameof(link));

        // Set default values
        if (string.IsNullOrWhiteSpace(link.Name))
            throw new ArgumentException("Link name is required", nameof(link));

        if (link.Type != LinkType.Folder && link.Type != LinkType.Notes && string.IsNullOrWhiteSpace(link.Url))
            throw new ArgumentException("URL is required for link types (except folders and notes)", nameof(link));

        return await _linkRepository.CreateAsync(link);
    }

    public async Task<Link> UpdateLinkAsync(Link link)
    {
        if (!await ValidateLinkAsync(link))
            throw new ArgumentException("Invalid link data", nameof(link));

        var existingLink = await _linkRepository.GetByIdAsync(link.Id);
        if (existingLink == null)
            throw new InvalidOperationException($"Link with ID {link.Id} not found");

        return await _linkRepository.UpdateAsync(link);
    }

    public async Task<bool> DeleteLinkAsync(int id)
    {
        var link = await _linkRepository.GetByIdAsync(id);
        if (link == null)
            return false;

        // Check if it's a folder with children
        if (link.Type == LinkType.Folder && link.Children.Any())
        {
            throw new InvalidOperationException("Cannot delete folder with children. Move or delete children first.");
        }

        return await _linkRepository.DeleteAsync(id);
    }

    public async Task<bool> MoveLinkAsync(int linkId, int? newParentId, int newSortOrder)
    {
        // Validate that we're not creating a circular reference
        if (newParentId.HasValue)
        {
            var targetParent = await _linkRepository.GetByIdAsync(newParentId.Value);
            if (targetParent == null)
                throw new ArgumentException($"Parent link with ID {newParentId} not found");

            // Check for circular reference (moving a folder into one of its descendants)
            var movingLink = await _linkRepository.GetByIdAsync(linkId);
            if (movingLink?.Type == LinkType.Folder)
            {
                if (await IsDescendant(newParentId.Value, linkId))
                    throw new InvalidOperationException("Cannot move folder into one of its descendants");
            }
        }

        return await _linkRepository.MoveAsync(linkId, newParentId, newSortOrder);
    }

    public async Task OpenLinkAsync(int linkId)
    {
        var link = await _linkRepository.GetByIdAsync(linkId);
        if (link == null)
            throw new ArgumentException($"Link with ID {linkId} not found");

        if (link.Type == LinkType.Folder)
            throw new InvalidOperationException("Cannot open a folder link");

        var opened = await _linkOpenerService.OpenAsync(link);
        if (opened)
        {
            await _linkRepository.UpdateAccessInfoAsync(linkId);
        }
    }

    public Task<bool> ValidateLinkAsync(Link link)
    {
        if (link == null)
            return Task.FromResult(false);

        if (string.IsNullOrWhiteSpace(link.Name))
            return Task.FromResult(false);

        if (link.Type != LinkType.Folder && link.Type != LinkType.Notes)
        {
            if (string.IsNullOrWhiteSpace(link.Url))
                return Task.FromResult(false);

            if (!_linkOpenerService.ValidateLink(link.Url, link.Type))
                return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    private async Task<bool> IsDescendant(int potentialDescendant, int ancestor)
    {
        var current = await _linkRepository.GetByIdAsync(potentialDescendant);
        while (current?.ParentId != null)
        {
            if (current.ParentId == ancestor)
                return true;
            current = await _linkRepository.GetByIdAsync(current.ParentId.Value);
        }
        return false;
    }
}
