namespace WinWork.Models;

/// <summary>
/// Represents a link item in the hierarchical tree structure
/// </summary>
public class Link
{
    public int Id { get; set; }
    
    /// <summary>
    /// Display name of the link or folder
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// URL, file path, or application path
    /// </summary>
    public string? Url { get; set; }
    
    /// <summary>
    /// Type of link: Folder, WebUrl, FilePath, Application
    /// </summary>
    public LinkType Type { get; set; }
    
    /// <summary>
    /// Optional description
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Notes content for Notes type items
    /// </summary>
    public string? Notes { get; set; }
    
    /// <summary>
    /// Parent folder ID (null for root items)
    /// </summary>
    public int? ParentId { get; set; }
    
    /// <summary>
    /// Order within the parent folder
    /// </summary>
    public int SortOrder { get; set; }
    
    /// <summary>
    /// Icon path or identifier
    /// </summary>
    public string? IconPath { get; set; }
    
    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last modified timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last accessed timestamp
    /// </summary>
    public DateTime? LastAccessedAt { get; set; }
    
    /// <summary>
    /// Number of times this link has been accessed
    /// </summary>
    public int AccessCount { get; set; }
    
    /// <summary>
    /// Whether the item is expanded in the tree view
    /// </summary>
    public bool IsExpanded { get; set; }
    
    /// <summary>
    /// Whether the item is currently selected
    /// </summary>
    public bool IsSelected { get; set; }
    
    // Navigation properties
    public virtual Link? Parent { get; set; }
    public virtual ICollection<Link> Children { get; set; } = new List<Link>();
    public virtual ICollection<LinkTag> LinkTags { get; set; } = new List<LinkTag>();
}
