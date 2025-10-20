using WinWork.Models;

namespace WinWork.Core.Services;

/// <summary>
/// Service interface for opening various types of links
/// </summary>
public interface ILinkOpenerService
{
    Task<bool> OpenAsync(Link link);
    Task<bool> OpenAsync(string url, LinkType type);
    bool CanOpen(LinkType type);
    bool ValidateLink(string url, LinkType type);
}
