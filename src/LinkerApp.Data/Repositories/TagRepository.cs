using Microsoft.EntityFrameworkCore;
using LinkerApp.Models;

namespace LinkerApp.Data.Repositories;

/// <summary>
/// Repository implementation for Tag entities
/// </summary>
public class TagRepository : ITagRepository
{
    private readonly LinkerAppDbContext _context;

    public TagRepository(LinkerAppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Tag>> GetAllAsync()
    {
        return await _context.Tags
            .Include(t => t.LinkTags)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<Tag?> GetByIdAsync(int id)
    {
        return await _context.Tags
            .Include(t => t.LinkTags)
                .ThenInclude(lt => lt.Link)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Tag?> GetByNameAsync(string name)
    {
        return await _context.Tags
            .Include(t => t.LinkTags)
                .ThenInclude(lt => lt.Link)
            .FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower());
    }

    public async Task<IEnumerable<Tag>> SearchAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return Enumerable.Empty<Tag>();

        searchTerm = searchTerm.ToLower();
        
        return await _context.Tags
            .Include(t => t.LinkTags)
            .Where(t => t.Name.ToLower().Contains(searchTerm) ||
                       (t.Description != null && t.Description.ToLower().Contains(searchTerm)))
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<Tag> CreateAsync(Tag tag)
    {
        tag.CreatedAt = DateTime.UtcNow;
        tag.UpdatedAt = DateTime.UtcNow;
        
        _context.Tags.Add(tag);
        await _context.SaveChangesAsync();
        return tag;
    }

    public async Task<Tag> UpdateAsync(Tag tag)
    {
        tag.UpdatedAt = DateTime.UtcNow;
        _context.Tags.Update(tag);
        await _context.SaveChangesAsync();
        return tag;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var tag = await _context.Tags.FindAsync(id);
        if (tag == null)
            return false;

        _context.Tags.Remove(tag);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<Tag>> GetTagsForLinkAsync(int linkId)
    {
        return await _context.Tags
            .Where(t => t.LinkTags.Any(lt => lt.LinkId == linkId))
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<bool> AddTagToLinkAsync(int linkId, int tagId)
    {
        // Check if the relationship already exists
        var exists = await _context.LinkTags
            .AnyAsync(lt => lt.LinkId == linkId && lt.TagId == tagId);
        
        if (exists)
            return false;

        var linkTag = new LinkTag
        {
            LinkId = linkId,
            TagId = tagId,
            CreatedAt = DateTime.UtcNow
        };

        _context.LinkTags.Add(linkTag);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveTagFromLinkAsync(int linkId, int tagId)
    {
        var linkTag = await _context.LinkTags
            .FirstOrDefaultAsync(lt => lt.LinkId == linkId && lt.TagId == tagId);
        
        if (linkTag == null)
            return false;

        _context.LinkTags.Remove(linkTag);
        await _context.SaveChangesAsync();
        return true;
    }
}