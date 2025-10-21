using WinWork.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace WinWork.Core.Interfaces
{
    public interface ILinkService
    {
    Task<IEnumerable<Link>> GetAllLinksAsync();
    Task<Link?> GetLinkAsync(int id);
    Task<IEnumerable<Link>> GetRootLinksAsync();
    Task<IEnumerable<Link>> GetChildLinksAsync(int parentId);
    Task<Link> CreateLinkAsync(Link link);
    Task<Link> UpdateLinkAsync(Link link);
    Task<bool> MoveLinkAsync(int linkId, int? newParentId, int newSortOrder);
    Task<List<(string Name, LinkType Type)>> DeleteLinkRecursiveAsync(int id);
    Task<bool> ValidateLinkAsync(Link link);
    Task OpenLinkAsync(int linkId);
    Task<IEnumerable<Link>> SearchLinksAsync(string searchTerm);
    Task<bool> DeleteLinkAsync(int id);
    }
}
