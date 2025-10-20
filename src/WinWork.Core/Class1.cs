using WinWork.Models;

namespace WinWork.Core.Services;

/// <summary>
/// Service interface for link management operations
/// </summary>
public interface ILinkService
{
    Task<IEnumerable<Link>> GetAllLinksAsync();
    Task<Link?> GetLinkAsync(int id);
    Task<IEnumerable<Link>> GetRootLinksAsync();
    Task<IEnumerable<Link>> GetChildLinksAsync(int parentId);
    Task<IEnumerable<Link>> SearchLinksAsync(string searchTerm);
    Task<IEnumerable<Link>> GetLinksByTagAsync(int tagId);
    Task<IEnumerable<Link>> GetMostAccessedLinksAsync(int count = 10);
    Task<IEnumerable<Link>> GetRecentLinksAsync(int count = 10);
    Task<Link> CreateLinkAsync(Link link);
    Task<Link> UpdateLinkAsync(Link link);
    Task<bool> DeleteLinkAsync(int id);
    Task<bool> MoveLinkAsync(int linkId, int? newParentId, int newSortOrder);
    Task OpenLinkAsync(int linkId);
    Task<bool> ValidateLinkAsync(Link link);
}
