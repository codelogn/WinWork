namespace LinkerApp.Models;

/// <summary>
/// Junction table for many-to-many relationship between Links and Tags
/// </summary>
public class LinkTag
{
    public int LinkId { get; set; }
    public int TagId { get; set; }
    
    /// <summary>
    /// When this tag was applied to the link
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Link Link { get; set; } = null!;
    public virtual Tag Tag { get; set; } = null!;
}