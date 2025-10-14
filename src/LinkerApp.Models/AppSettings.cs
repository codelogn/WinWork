namespace LinkerApp.Models;

/// <summary>
/// Configuration settings for the application
/// </summary>
public class AppSettings
{
    public int Id { get; set; }
    
    /// <summary>
    /// Key for the setting
    /// </summary>
    public string Key { get; set; } = string.Empty;
    
    /// <summary>
    /// Value for the setting
    /// </summary>
    public string Value { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional description of what this setting does
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Last modified timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}