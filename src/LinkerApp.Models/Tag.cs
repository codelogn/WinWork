namespace LinkerApp.Models;

/// <summary>
/// Represents a tag that can be applied to links
/// </summary>
public class Tag
{
    public int Id { get; set; }
    
    /// <summary>
    /// Name of the tag
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Color of the tag in hex format (e.g., #FF5733)
    /// </summary>
    public string Color { get; set; } = "#808080";
    
    /// <summary>
    /// Optional description for the tag
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last modified timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<LinkTag> LinkTags { get; set; } = new List<LinkTag>();
}