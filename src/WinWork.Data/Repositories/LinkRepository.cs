using Microsoft.EntityFrameworkCore;
using WinWork.Models;

namespace WinWork.Data.Repositories;

/// <summary>
/// Repository implementation for Link entities
/// </summary>
public class LinkRepository : ILinkRepository
{
    private readonly WinWorkDbContext _context;

    public LinkRepository(WinWorkDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Link>> GetAllAsync()
    {
        return await _context.Links
            .Include(l => l.Children)
            .Include(l => l.LinkTags)
                .ThenInclude(lt => lt.Tag)
            .OrderBy(l => l.ParentId)
            .ThenBy(l => l.SortOrder)
            .ToListAsync();
    }

    public async Task<Link?> GetByIdAsync(int id)
    {
        return await _context.Links
            .Include(l => l.Children.OrderBy(c => c.SortOrder))
            .Include(l => l.LinkTags)
                .ThenInclude(lt => lt.Tag)
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<IEnumerable<Link>> GetRootLinksAsync()
    {
        System.Diagnostics.Debug.WriteLine("LinkRepository.GetRootLinksAsync started");
        
        var totalCount = await _context.Links.CountAsync();
        System.Diagnostics.Debug.WriteLine($"Total links in database: {totalCount}");
        
        var rootCount = await _context.Links.Where(l => l.ParentId == null).CountAsync();
        System.Diagnostics.Debug.WriteLine($"Root links in database: {rootCount}");
        
        var result = await _context.Links
            .Include(l => l.Children.OrderBy(c => c.SortOrder))
            .Include(l => l.LinkTags)
                .ThenInclude(lt => lt.Tag)
            .Where(l => l.ParentId == null)
            .OrderBy(l => l.SortOrder)
            .ToListAsync();
            
        System.Diagnostics.Debug.WriteLine($"GetRootLinksAsync returning {result.Count} items");
        foreach (var link in result)
        {
            System.Diagnostics.Debug.WriteLine($"  - {link.Name} (Type: {link.Type}, ParentId: {link.ParentId})");
        }
        
        return result;
    }

    public async Task<IEnumerable<Link>> GetChildrenAsync(int parentId)
    {
        return await _context.Links
            .Include(l => l.Children.OrderBy(c => c.SortOrder))
            .Include(l => l.LinkTags)
                .ThenInclude(lt => lt.Tag)
            .Where(l => l.ParentId == parentId)
            .OrderBy(l => l.SortOrder)
            .ToListAsync();
    }

    public async Task<IEnumerable<Link>> SearchAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return Enumerable.Empty<Link>();

        searchTerm = searchTerm.ToLower();
        
        return await _context.Links
            .Include(l => l.LinkTags)
                .ThenInclude(lt => lt.Tag)
            .Where(l => l.Name.ToLower().Contains(searchTerm) ||
                       (l.Description != null && l.Description.ToLower().Contains(searchTerm)) ||
                       (l.Url != null && l.Url.ToLower().Contains(searchTerm)) ||
                       (l.Notes != null && l.Notes.ToLower().Contains(searchTerm)) ||
                       l.LinkTags.Any(lt => lt.Tag.Name.ToLower().Contains(searchTerm)))
            .OrderByDescending(l => l.AccessCount)
            .ThenByDescending(l => l.LastAccessedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Link>> GetByTagAsync(int tagId)
    {
        return await _context.Links
            .Include(l => l.LinkTags)
                .ThenInclude(lt => lt.Tag)
            .Where(l => l.LinkTags.Any(lt => lt.TagId == tagId))
            .OrderByDescending(l => l.AccessCount)
            .ToListAsync();
    }

    public async Task<IEnumerable<Link>> GetMostAccessedAsync(int count = 10)
    {
        return await _context.Links
            .Include(l => l.LinkTags)
                .ThenInclude(lt => lt.Tag)
            .Where(l => l.Type != LinkType.Folder)
            .OrderByDescending(l => l.AccessCount)
            .ThenByDescending(l => l.LastAccessedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IEnumerable<Link>> GetRecentlyAccessedAsync(int count = 10)
    {
        return await _context.Links
            .Include(l => l.LinkTags)
                .ThenInclude(lt => lt.Tag)
            .Where(l => l.Type != LinkType.Folder && l.LastAccessedAt != null)
            .OrderByDescending(l => l.LastAccessedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<Link> CreateAsync(Link link)
    {
        System.Diagnostics.Debug.WriteLine($"CreateAsync called for link: {link.Name} (Type: {link.Type})");
        
        link.CreatedAt = DateTime.UtcNow;
        link.UpdatedAt = DateTime.UtcNow;
        
        // Set sort order if not specified
        if (link.SortOrder == 0)
        {
            link.SortOrder = await GetMaxSortOrderAsync(link.ParentId) + 1;
        }

        _context.Links.Add(link);
        await _context.SaveChangesAsync();
        
        var totalCount = await _context.Links.CountAsync();
        System.Diagnostics.Debug.WriteLine($"Link '{link.Name}' created successfully! Total links now: {totalCount}");
        
        return link;
    }

    public async Task<Link> UpdateAsync(Link link)
    {
        link.UpdatedAt = DateTime.UtcNow;
        
        // Check if the entity is already being tracked
        var trackedEntity = _context.Entry(link);
        if (trackedEntity.State == Microsoft.EntityFrameworkCore.EntityState.Detached)
        {
            // Only call Update if the entity is not being tracked
            _context.Links.Update(link);
        }
        // If already tracked, EF will automatically detect changes
        
        await _context.SaveChangesAsync();
        return link;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var link = await _context.Links.FindAsync(id);
        if (link == null)
            return false;

        _context.Links.Remove(link);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task UpdateAccessInfoAsync(int id)
    {
        var link = await _context.Links.FindAsync(id);
        if (link == null)
            return;

        link.LastAccessedAt = DateTime.UtcNow;
        link.AccessCount++;
        link.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task<bool> MoveAsync(int linkId, int? newParentId, int newSortOrder)
    {
        var link = await _context.Links.FindAsync(linkId);
        if (link == null)
            return false;

        // Update other items' sort orders if necessary
        if (newParentId != link.ParentId || newSortOrder != link.SortOrder)
        {
            // Adjust sort orders in the target parent
            var siblings = await _context.Links
                .Where(l => l.ParentId == newParentId && l.Id != linkId)
                .OrderBy(l => l.SortOrder)
                .ToListAsync();

            for (int i = 0; i < siblings.Count; i++)
            {
                var adjustedOrder = i >= newSortOrder ? i + 2 : i + 1;
                if (siblings[i].SortOrder != adjustedOrder)
                {
                    siblings[i].SortOrder = adjustedOrder;
                    siblings[i].UpdatedAt = DateTime.UtcNow;
                }
            }

            link.ParentId = newParentId;
            link.SortOrder = newSortOrder;
            link.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        return true;
    }

    public async Task<int> GetMaxSortOrderAsync(int? parentId)
    {
        var maxOrder = await _context.Links
            .Where(l => l.ParentId == parentId)
            .MaxAsync(l => (int?)l.SortOrder);

        return maxOrder ?? 0;
    }
}
