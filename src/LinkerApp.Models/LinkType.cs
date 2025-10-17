namespace LinkerApp.Models;

/// <summary>
/// Types of links supported by the application
/// </summary>
public enum LinkType
{
    /// <summary>
    /// A folder that contains other links
    /// </summary>
    Folder = 0,
    
    /// <summary>
    /// Web URL (HTTP/HTTPS)
    /// </summary>
    WebUrl = 1,
    
    /// <summary>
    /// Local file path
    /// </summary>
    FilePath = 2,
    
    /// <summary>
    /// Application executable
    /// </summary>
    Application = 3,
    
    /// <summary>
    /// Folder path (Explorer)
    /// </summary>
    FolderPath = 4,
    
    /// <summary>
    /// Windows Store App
    /// </summary>
    WindowsStoreApp = 5,
    
    /// <summary>
    /// System location (Control Panel, Settings, etc.)
    /// </summary>
    SystemLocation = 6,
    
    /// <summary>
    /// Text notes/memo
    /// </summary>
    Notes = 7
}