# User Manual - LinkerApp

## Getting Started

### Installation
1. Download and install **.NET 9 Runtime** from Microsoft's website
2. Download the LinkerApp executable or source code
3. Run `LinkerApp.UI.exe` or build from source using `dotnet run`

### First Launch
On first launch, LinkerApp will:
- Create a SQLite database (`linker.db`) in the application folder
- Initialize the database schema automatically
- Display an empty tree view ready for your first links

## Using LinkerApp

### Main Interface

#### Tree View (Left Panel)
- **Displays** your organized links in a hierarchical tree structure  
- **Root level** shows folders and standalone links
- **Expandable folders** contain nested links and subfolders
- **Icons** indicate different link types (web, file, folder, app)

#### Search Bar (Top)
- **Real-time search** - Results appear as you type
- **Searches** link names, URLs, and descriptions
- **Clear search** by emptying the text box to show all links

#### Toolbar Buttons
- **Add Link** - Create a new link or folder
- **Manage Tags** - Create and organize tags
- **Settings** - Application preferences (future feature)

### Managing Links

#### Adding a New Link
1. **Right-click** in the tree view or click **"Add Link"**
2. **Fill in the Link Dialog:**
   - **Name:** Display name for your link (required)
   - **URL/Path:** The actual link destination (required)
   - **Description:** Optional notes about the link
   - **Type:** Auto-detected based on URL/path
   - **Parent Folder:** Choose where to place the link
   - **Tags:** Select from existing tags or create new ones

3. **Click "Save"** to create the link

#### Supported Link Types

**Web URLs:**
- `https://www.example.com`
- `http://site.org`
- `ftp://files.example.com`

**File Paths:**
- `C:\Documents\report.pdf`
- `D:\Photos\vacation.jpg`
- `\\server\share\document.docx`

**Folder Paths:**
- `C:\Projects\`
- `D:\Downloads\`
- `\\network\shared\`

**Applications:**
- `C:\Program Files\App\program.exe`
- `notepad.exe`
- `calculator.exe`

**Windows Store Apps:**
- `ms-windows-store://pdp/?productid=...`
- Custom protocol URLs

**System Locations:**
- `shell:desktop` (Desktop folder)
- `shell:documents` (Documents folder)
- `ms-settings:display` (Display settings)
- `control.exe` (Control Panel)

#### Editing Links
1. **Right-click** on a link and select **"Edit"**
2. **Modify** any fields in the dialog
3. **Click "Save"** to apply changes

#### Organizing Links
- **Create folders** by setting Link Type to "Folder"
- **Drag and drop** links between folders (coming soon)
- **Use the Parent Folder** dropdown when adding/editing links
- **Create nested structures** with folders inside folders

#### Opening Links
- **Double-click** any link to open it
- **Web URLs** open in your default browser
- **Files** open with their associated application
- **Folders** open in Windows Explorer
- **Applications** launch directly

### Tag Management

#### Creating Tags
1. **Click "Manage Tags"** button
2. **Click "Add Tag"** 
3. **Enter tag name** and **choose a color**
4. **Click "Save"** to create the tag

#### Using Tags
- **Assign tags** when adding or editing links
- **Multiple tags** can be assigned to one link
- **Filter by tags** (coming in future update)
- **Color coding** helps visually organize links

#### Managing Existing Tags
- **Edit tags** to change name or color
- **Delete unused tags** 
- **View tag assignments** in the tag management dialog

### Search and Filter

#### Real-time Search
- **Type in search box** for instant results
- **Searches through:**
  - Link names
  - URLs and file paths  
  - Link descriptions
- **Partial matches** are highlighted
- **Case-insensitive** search

#### Advanced Features (Coming Soon)
- **Filter by tags**
- **Filter by link type**
- **Date range filtering**
- **Recent links view**

### Import and Export

#### Importing Bookmarks
LinkerApp can import from popular browsers:

**Chrome/Edge Bookmarks:**
1. **Export bookmarks** from Chrome/Edge as HTML or find the bookmarks JSON file
2. **Use File → Import** (coming soon) or use the import service
3. **Select the bookmarks file**
4. **Choose import location** in your link tree

**Firefox Bookmarks:**
1. **Export bookmarks** from Firefox as HTML
2. **Use the Firefox import option**
3. **Bookmarks maintain their folder structure**

#### Exporting Your Links
**Available formats:**
- **JSON** - Complete data including tags and metadata
- **HTML** - Browser-compatible bookmarks format
- **CSV** - Spreadsheet format for analysis

**Export process:**
1. **Select export format**
2. **Choose file location**
3. **Export maintains** hierarchical structure where possible

## Keyboard Shortcuts

### Current Shortcuts
- **Ctrl+F** - Focus search box (coming soon)
- **Delete** - Delete selected link (coming soon)
- **F2** - Rename selected link (coming soon)
- **Enter** - Open selected link (coming soon)

### Global Hotkeys (Coming Soon)
- **Configure custom shortcuts** for quick access
- **Show/hide LinkerApp** with global hotkey
- **Quick add link** from anywhere in Windows

## System Integration

### System Tray
LinkerApp can minimize to the system tray:
- **Right-click tray icon** for context menu
- **Double-click** to restore main window
- **Exit** from tray menu to close completely

### Notifications
- **Balloon tips** for important updates
- **Link opening** confirmations for potentially unsafe links
- **Import/export** status notifications

## Tips and Best Practices

### Organization Strategy
1. **Create main categories** as top-level folders (Work, Personal, Projects)
2. **Use descriptive names** for links and folders
3. **Add descriptions** for complex or important links
4. **Tag consistently** for better search and filtering
5. **Regular cleanup** - remove outdated links

### Performance Tips
- **Avoid very deep nesting** (more than 5 levels)
- **Use search** instead of browsing for large collections
- **Regular backups** via export functionality
- **Keep descriptions concise** but informative

### Security Considerations
- **Be careful** with links to executable files
- **Verify URLs** before adding suspicious links
- **Backup your database** regularly
- **Don't store sensitive information** in link descriptions

## Troubleshooting

### Common Issues

**Application won't start:**
- Verify .NET 9 Runtime is installed
- Check Windows compatibility (Windows 10/11 required)
- Run as administrator if permission issues occur

**Links won't open:**
- Verify the file or URL still exists
- Check that associated applications are installed
- Update file paths if files have moved

**Search not working:**
- Check for special characters in search terms
- Verify database isn't corrupted
- Restart application to refresh search index

**Database issues:**
- Locate `linker.db` file in application folder
- Create backup before troubleshooting
- Delete database file to reset (loses all data)

### Getting Help
- **Check documentation** in the `docs/` folder
- **Review error messages** for specific guidance
- **Submit issues** via the project repository
- **Check compatibility** requirements

### Performance Issues
- **Large numbers of links** may slow tree view
- **Frequent searches** are optimized for performance
- **Database file** grows with usage (normal)
- **Memory usage** increases with many open folders

## Advanced Usage

### Database Location
- **Default location:** Application folder
- **Backup regularly** by copying `linker.db` file
- **Portable usage** - copy entire application folder
- **Multiple databases** - run from different folders

### Customization Options
- **Window size and position** are remembered
- **Tree view expansion state** is preserved
- **Search history** (coming soon)
- **Theme customization** (coming soon)

### Integration with Other Tools
- **Browser integration** via bookmark import
- **File manager** integration for folder links
- **Command line** access (future feature)
- **API access** for external applications (future feature)

---

## Version Information

**Current Version:** 1.0.0  
**Release Date:** October 2025  
**Platform:** Windows 10/11 with .NET 9  
**License:** MIT License  

For the latest updates and documentation, visit the project repository.