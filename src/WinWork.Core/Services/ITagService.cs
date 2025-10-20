using WinWork.Models;

namespace WinWork.Core.Services;

/// <summary>
/// Service interface for tag management operations
/// </summary>
public interface ITagService
{
    Task<IEnumerable<Tag>> GetAllTagsAsync();
    Task<Tag?> GetTagAsync(int id);
    Task<Tag?> GetTagByNameAsync(string name);
    Task<IEnumerable<Tag>> SearchTagsAsync(string searchTerm);
    Task<IEnumerable<Tag>> GetTagsForLinkAsync(int linkId);
    Task<Tag> CreateTagAsync(Tag tag);
    Task<Tag> UpdateTagAsync(Tag tag);
    Task<bool> DeleteTagAsync(int id);
    Task<bool> AddTagToLinkAsync(int linkId, int tagId);
    Task<bool> RemoveTagFromLinkAsync(int linkId, int tagId);
    Task<bool> ValidateTagAsync(Tag tag);
}
