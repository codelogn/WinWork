using Microsoft.EntityFrameworkCore;
using WinWork.Models;

namespace WinWork.Data.Repositories;

public class HotNavRepository : IHotNavRepository
{
    private readonly WinWorkDbContext _context;

    public HotNavRepository(WinWorkDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<HotNav>> GetAllAsync()
    {
        return await _context.Set<HotNav>()
            .Include(h => h.Roots.OrderBy(r => r.SortOrder))
            .OrderBy(h => h.SortOrder)
            .ToListAsync();
    }

    public async Task<HotNav?> GetByIdAsync(int id)
    {
        return await _context.Set<HotNav>()
            .Include(h => h.Roots.OrderBy(r => r.SortOrder))
            .FirstOrDefaultAsync(h => h.Id == id);
    }

    public async Task<HotNav> CreateAsync(HotNav hotNav)
    {
        hotNav.CreatedAt = DateTime.UtcNow;
        hotNav.UpdatedAt = DateTime.UtcNow;
        if (hotNav.SortOrder == 0)
            hotNav.SortOrder = await GetMaxSortOrderAsync() + 1;

        _context.Set<HotNav>().Add(hotNav);
        await _context.SaveChangesAsync();
        return hotNav;
    }

    public async Task<HotNav> UpdateAsync(HotNav hotNav)
    {
        // Avoid attaching a new instance which may conflict with an already-tracked
        // entity. Load the existing entity from the context and apply the changes
        // to its scalar properties. Roots/children are handled by the caller.
        var existing = await _context.Set<HotNav>().FirstOrDefaultAsync(h => h.Id == hotNav.Id);
        if (existing == null)
        {
            throw new KeyNotFoundException($"HotNav with Id {hotNav.Id} not found");
        }

        existing.Name = hotNav.Name;
        existing.IncludeFiles = hotNav.IncludeFiles;
        existing.MaxDepth = hotNav.MaxDepth;
        existing.SortOrder = hotNav.SortOrder;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _context.Set<HotNav>().FindAsync(id);
        if (entity == null)
            return false;
        _context.Set<HotNav>().Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<int> GetMaxSortOrderAsync()
    {
        var max = await _context.Set<HotNav>().MaxAsync(h => (int?)h.SortOrder);
        return max ?? 0;
    }
}
