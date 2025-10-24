
## Latest Features & Changes (v1.2.0)
- Notes type (freeform text, no URL required)
- Tagging system with color-coded tags
- Hierarchical folders and subfolders (unlimited nesting)
- "Parent Folder" renamed to "Parent Item" (can be any type)
- All item types can be parents
- Context menu with Open/Edit/Add/Delete
- Real-time search includes Notes field
- Loading spinner for async operations
- Export includes timestamp
# API Documentation

## Service Layer Architecture

### ILinkService
Main service for link management operations.

#### Methods

##### GetAllLinksAsync()
```csharp
Task<IEnumerable<Link>> GetAllLinksAsync()
```
Retrieves all links from the database.

**Returns:** Collection of all links

##### GetLinkByIdAsync(int id)
```csharp
Task<Link?> GetLinkByIdAsync(int id)
```
Retrieves a specific link by ID.

**Parameters:**
- `id` - The link ID

**Returns:** Link entity or null if not found

##### GetChildLinksAsync(int? parentId)
```csharp
Task<IEnumerable<Link>> GetChildLinksAsync(int? parentId)
```
Retrieves child links for a given parent ID.

**Parameters:**
- `parentId` - Parent link ID, null for root links

**Returns:** Collection of child links

##### CreateLinkAsync(Link link)
```csharp
Task<Link> CreateLinkAsync(Link link)
```
Creates a new link in the database.

**Parameters:**
- `link` - Link entity to create

**Returns:** Created link with assigned ID

##### UpdateLinkAsync(Link link)
```csharp
Task<Link> UpdateLinkAsync(Link link)
```
Updates an existing link.

**Parameters:**
- `link` - Link entity with updates

**Returns:** Updated link entity

##### DeleteLinkAsync(int id)
```csharp
Task<bool> DeleteLinkAsync(int id)
```
Deletes a link and all its children.

**Parameters:**
- `id` - Link ID to delete

**Returns:** True if successful

##### SearchLinksAsync(string searchTerm)
```csharp
Task<IEnumerable<Link>> SearchLinksAsync(string searchTerm)
```
Searches links by name, URL, or description.

**Parameters:**
- `searchTerm` - Search string

**Returns:** Matching links



##### CreateTagAsync(Tag tag)
```csharp
##### UpdateTagAsync(Tag tag)
```csharp
```
Updates an existing tag.

**Parameters:**
- `tag` - Tag entity with updates

**Returns:** Updated tag entity

##### DeleteTagAsync(int id)
```csharp
Task<bool> DeleteTagAsync(int id)
```
Deletes a tag and removes all associations.

**Parameters:**
- `id` - Tag ID to delete

**Returns:** True if successful

### ILinkOpenerService
Service for opening different types of links.

#### Methods

##### OpenAsync(Link link)
```csharp
Task<bool> OpenAsync(Link link)
```
Opens a link using the appropriate application.

**Parameters:**
- `link` - Link to open

**Returns:** True if successful

##### ValidateLink(string url, LinkType type)
```csharp
bool ValidateLink(string url, LinkType type)
```
Validates if a URL is valid for the given link type.

**Parameters:**
- `url` - URL to validate
- `type` - Expected link type

**Returns:** True if valid

### IImportExportService
Service for importing and exporting links.

#### Methods

##### ImportChromeBookmarksAsync(string filePath)
```csharp
Task<IEnumerable<Link>> ImportChromeBookmarksAsync(string filePath)
```
Imports bookmarks from Chrome bookmarks file.

**Parameters:**
- `filePath` - Path to Chrome bookmarks JSON file

**Returns:** Collection of imported links

##### ExportToJsonAsync(IEnumerable<Link> links, string filePath)
```csharp
Task<bool> ExportToJsonAsync(IEnumerable<Link> links, string filePath)
```
Exports links to JSON format.

**Parameters:**
- `links` - Links to export
- `filePath` - Output file path

**Returns:** True if successful

## Models

### Link Entity
```csharp
public class Link
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public LinkType Type { get; set; }
    public int? ParentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public Link? Parent { get; set; }
    public ICollection<Link> Children { get; set; } = [];
    public ICollection<Tag> Tags { get; set; } = [];
}
```

### Tag Entity
```csharp
public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#007ACC";
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public ICollection<Link> Links { get; set; } = [];
}
```

### LinkType Enumeration
```csharp
public enum LinkType
{
    Folder = 0,
    WebUrl = 1,
    FilePath = 2,
    FolderPath = 3,
    Application = 4,
    WindowsStoreApp = 5,
    SystemLocation = 6
}
```

## ViewModels

### MainWindowViewModel
Main window view model with link tree and operations.

#### Properties
- `RootLinks` - Observable collection of root links
- `SelectedLink` - Currently selected link
- `SearchText` - Search filter text
- `IsLoading` - Loading state indicator

#### Commands
 - `AddLinkCommand` - Add new item
- `EditLinkCommand` - Edit selected link
- `DeleteLinkCommand` - Delete selected link
- `RefreshCommand` - Reload links

### LinkDialogViewModel
Dialog for adding/editing links.

#### Properties
- `Name` - Link name
- `Url` - Link URL/path
- `Description` - Link description
- `LinkType` - Type of link
- `AvailableTags` - Available tags for selection
- `SelectedTags` - Tags assigned to link

#### Commands
- `SaveCommand` - Save link changes
- `CancelCommand` - Cancel dialog
- `BrowseFileCommand` - Browse for file path
- `BrowseFolderCommand` - Browse for folder path

### TagManagementViewModel
Dialog for managing tags.

#### Properties
- `Tags` - Observable collection of tags
- `SelectedTag` - Currently selected tag
- `NewTagName` - Name for new tag
- `NewTagColor` - Color for new tag

#### Commands
- `AddTagCommand` - Add new tag
- `EditTagCommand` - Edit selected tag  
- `DeleteTagCommand` - Delete selected tag
- `SaveCommand` - Save changes
