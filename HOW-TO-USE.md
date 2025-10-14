# LinkerApp - How to Use Guide

## Table of Contents
- [Getting Started](#getting-started)
- [Adding Links and Folders](#adding-links-and-folders)
- [Managing Tags](#managing-tags)
- [Organizing Content](#organizing-content)
- [Using Links](#using-links)
- [Search and Filter](#search-and-filter)
- [Settings and Customization](#settings-and-customization)
- [Keyboard Shortcuts](#keyboard-shortcuts)
- [Tips and Tricks](#tips-and-tricks)
- [Troubleshooting](#troubleshooting)

## Getting Started

### First Launch
When you first launch LinkerApp, you'll see:
- A clean interface with a tree view on the left
- Three default folders: "Bookmarks", "Development Tools", and "Productivity"
- A toolbar with buttons for common actions
- Pre-configured tags for organizing your links

### Main Interface Elements
- **Tree View**: Shows your links and folders in a hierarchical structure
- **Toolbar**: Quick access to frequently used functions
- **Status Bar**: Displays information about selected items
- **Context Menus**: Right-click anywhere for additional options

## Adding Links and Folders

### Adding a New Link
1. **Right-click** in the tree view where you want to add the link
2. Select **"Add Link"** from the context menu
3. Fill in the link details:
   - **Name**: Display name for the link
   - **URL**: Web address or file path
   - **Description**: Optional description
   - **Type**: Choose from URL, Application, File, or Folder
   - **Icon**: Optional custom icon path
   - **Tags**: Assign one or more tags

### Adding a New Folder
1. **Right-click** in the tree view where you want to add the folder
2. Select **"Add Folder"** from the context menu
3. Enter the folder name and description
4. The folder will be created and you can start adding items to it

### Link Types
- **URL**: Web addresses (http://, https://)
- **Application**: Desktop applications (.exe files)
- **File**: Documents, images, or other files
- **Folder**: Container for organizing other links

## Managing Tags

### Opening Tag Manager
1. Click the **"Manage Tags"** button in the toolbar
2. The Tag Management dialog will open

### Creating Tags
1. In the Tag Manager, click **"Add Tag"**
2. Enter tag details:
   - **Name**: Unique tag name
   - **Color**: Choose a color for visual identification
   - **Description**: Optional description of the tag's purpose

### Default Tags
LinkerApp comes with these pre-configured tags:
- **Work** (Blue): Work-related links
- **Personal** (Green): Personal links and bookmarks
- **Development** (Red): Development tools and resources
- **Frequently Used** (Orange): Most accessed links
- **Learning** (Purple): Educational resources and tutorials

### Editing Tags
1. Select a tag from the list
2. Click **"Edit"** to modify its properties
3. Change the name, color, or description as needed

### Deleting Tags
1. Select the tag you want to remove
2. Click **"Delete"**
3. Confirm the deletion (links will keep their other tags)

## Organizing Content

### Creating Folder Hierarchies
- **Drag and drop** folders into other folders to create nested structures
- Use folders to group related links by project, category, or priority
- There's no limit to how deep you can nest folders

### Moving Links and Folders
1. **Click and drag** items to move them
2. **Drop** them onto a folder to move them inside
3. **Drop** them between items to reorder at the same level
4. Use **Cut** and **Paste** from context menus for precise placement

### Sorting Options
- Links maintain their sort order automatically
- **Right-click** and select sorting options:
  - Sort by Name (A-Z or Z-A)
  - Sort by Date Added
  - Sort by Usage Frequency
  - Custom manual ordering

## Using Links

### Opening Links
- **Double-click** any link to open it:
  - URLs open in your default web browser
  - Applications launch directly
  - Files open with their default application
  - Folders expand/collapse in the tree view

### Single-Click Selection
- **Single-click** to select and view link details
- Selected link information appears in the status bar
- Use arrow keys to navigate between items

### Context Menu Options
**Right-click** any link for these options:
- **Open**: Launch the link
- **Edit**: Modify link properties
- **Copy**: Copy link to clipboard
- **Cut**: Cut link for moving
- **Paste**: Paste copied/cut items
- **Delete**: Remove the link
- **Properties**: View detailed information

## Search and Filter

### Quick Search
1. Use the search box in the toolbar
2. Type any part of a link name, URL, or description
3. Results are filtered in real-time
4. Clear the search to see all links again

### Filter by Tags
1. Click on tag names to filter by specific tags
2. Use multiple tags to narrow down results
3. Click "Show All" to clear tag filters

### Advanced Search Features
- Search is case-insensitive
- Searches through names, URLs, and descriptions
- Partial word matching supported
- Regular expressions supported for power users

## Settings and Customization

### Application Settings
Access settings through **File → Settings** or the toolbar button:

#### Appearance
- **Theme**: Choose between Light and Dark themes
- **Font Size**: Adjust text size for better visibility
- **Icon Size**: Change icon dimensions in the tree view

#### Behavior
- **Global Hotkey**: Set a system-wide shortcut to show/hide LinkerApp
- **Minimize to Tray**: Hide to system tray instead of closing
- **Start with Windows**: Launch automatically on system startup
- **Confirm Deletions**: Show confirmation dialogs before deleting items

#### Backup and Data
- **Auto Backup**: Automatically backup your data
- **Backup Interval**: Set how often backups are created
- **Backup Location**: Choose where backup files are stored
- **Import/Export**: Backup or transfer your links to other systems

## Keyboard Shortcuts

### Navigation
- **Ctrl+F**: Focus search box
- **F2**: Rename selected item
- **Delete**: Delete selected item
- **Ctrl+C**: Copy selected item
- **Ctrl+X**: Cut selected item
- **Ctrl+V**: Paste item
- **Enter**: Open selected link
- **Escape**: Clear selection/close dialogs

### File Operations
- **Ctrl+N**: Add new link
- **Ctrl+Shift+N**: Add new folder
- **Ctrl+O**: Open selected link
- **Ctrl+E**: Edit selected item
- **Ctrl+D**: Duplicate selected item

### Application
- **Ctrl+S**: Save changes
- **Ctrl+,**: Open settings
- **F1**: Show help
- **Alt+F4**: Exit application
- **Ctrl+Q**: Quick exit

### Custom Hotkeys
Set your own global hotkey in settings to:
- Show/hide LinkerApp from anywhere
- Quick-add a link from clipboard
- Open frequently used folders

## Tips and Tricks

### Productivity Tips
1. **Use descriptive names**: Make link names clear and searchable
2. **Organize by workflow**: Group links by how you use them, not just by category
3. **Tag consistently**: Use the same tags across similar items
4. **Regular cleanup**: Remove or update outdated links periodically

### Advanced Features
1. **Bulk operations**: Select multiple items with Ctrl+Click for batch operations
2. **Quick add from clipboard**: Copy a URL, then use Ctrl+Shift+V to quick-add
3. **Backup before major changes**: Create manual backups before reorganizing
4. **Use folders as shortcuts**: Create folders for different projects or contexts

### Integration Tips
1. **Browser bookmarks**: Import your existing browser bookmarks
2. **Desktop shortcuts**: Add frequently used applications to LinkerApp
3. **Project management**: Create folders for each project with relevant links
4. **Team sharing**: Export link collections to share with colleagues

## Troubleshooting

### Common Issues

#### "No such table" Error
- **Cause**: Database not properly initialized
- **Solution**: Restart the application - it will automatically create the database

#### Links Don't Open
- **Cause**: Invalid URL or missing application
- **Solution**: Edit the link and verify the URL/path is correct

#### Application Won't Start
- **Cause**: Database corruption or missing dependencies
- **Solutions**:
  1. Check the detailed error message (now copyable)
  2. Try running as administrator
  3. Clear the database file and restart (will lose data)
  4. Reinstall the application

#### Performance Issues
- **Cause**: Too many items or complex folder structures
- **Solutions**:
  1. Organize links into smaller folders
  2. Use tags instead of deep folder hierarchies
  3. Regular cleanup of unused items
  4. Check available disk space

### Data Recovery
If you experience data loss:
1. Check the backup folder (set in Settings)
2. Look for automatic backup files (.bak extension)
3. Restore from the most recent backup
4. Import previously exported link collections

### Getting Help
1. Check this guide for common tasks
2. Look in the application's Help menu
3. Check the README.md for technical information
4. Review error messages for specific guidance

### Log Files
For troubleshooting, check these locations:
- **Windows**: `%APPDATA%\LinkerApp\logs\`
- **Database**: `%APPDATA%\LinkerApp\linkerapp.db`
- **Backups**: `%APPDATA%\LinkerApp\backups\`

---

## Quick Start Checklist

- [ ] Launch LinkerApp for the first time
- [ ] Explore the default folders and tags
- [ ] Add your first link using right-click → "Add Link"
- [ ] Create a custom tag in Tag Manager
- [ ] Organize links into folders by dragging and dropping
- [ ] Set up your preferred settings (theme, hotkey, etc.)
- [ ] Configure automatic backups
- [ ] Test opening different types of links
- [ ] Set up a global hotkey for quick access

Congratulations! You're now ready to efficiently manage all your links and shortcuts with LinkerApp.