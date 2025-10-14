using LinkerApp.Models;

namespace LinkerApp.Data.Repositories;

/// <summary>
/// Repository interface for Tag entities
/// </summary>
public interface ITagRepository
{
    Task<IEnumerable<Tag>> GetAllAsync();
    Task<Tag?> GetByIdAsync(int id);
    Task<Tag?> GetByNameAsync(string name);
    Task<IEnumerable<Tag>> SearchAsync(string searchTerm);
    Task<Tag> CreateAsync(Tag tag);
    Task<Tag> UpdateAsync(Tag tag);
    Task<bool> DeleteAsync(int id);
    Task<IEnumerable<Tag>> GetTagsForLinkAsync(int linkId);
    Task<bool> AddTagToLinkAsync(int linkId, int tagId);
    Task<bool> RemoveTagFromLinkAsync(int linkId, int tagId);
}