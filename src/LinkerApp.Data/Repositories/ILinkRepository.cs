using LinkerApp.Models;

namespace LinkerApp.Data.Repositories;

/// <summary>
/// Repository interface for Link entities
/// </summary>
public interface ILinkRepository
{
    Task<IEnumerable<Link>> GetAllAsync();
    Task<Link?> GetByIdAsync(int id);
    Task<IEnumerable<Link>> GetRootLinksAsync();
    Task<IEnumerable<Link>> GetChildrenAsync(int parentId);
    Task<IEnumerable<Link>> SearchAsync(string searchTerm);
    Task<IEnumerable<Link>> GetByTagAsync(int tagId);
    Task<IEnumerable<Link>> GetMostAccessedAsync(int count = 10);
    Task<IEnumerable<Link>> GetRecentlyAccessedAsync(int count = 10);
    Task<Link> CreateAsync(Link link);
    Task<Link> UpdateAsync(Link link);
    Task<bool> DeleteAsync(int id);
    Task UpdateAccessInfoAsync(int id);
    Task<bool> MoveAsync(int linkId, int? newParentId, int newSortOrder);
    Task<int> GetMaxSortOrderAsync(int? parentId);
}