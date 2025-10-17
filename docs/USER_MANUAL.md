
---

## 🚀 Getting Started
- **Runtime**: .NET 9 Runtime ([Download here](https://dotnet.microsoft.com/download/dotnet/9.0))
- **Storage**: ~50MB for application + your database size
- **Memory**: 100MB+ RAM recommended

### **Installation**
3. **Run `LinkerApp.UI.exe`** to launch the application

- ✅ **Database Creation**: Automatic SQLite database setup (`linker.db`)
- ✅ **Schema Initialization**: All database tables created automatically
- ✅ **Sample Data**: Pre-created folders "📁 Bookmarks" and "📁 Development Tools" with sample links
- ✅ **Ready to Use**: Interface loads with example structure for immediate use

---

## 🖥️ Main Interface

### **Layout Overview**
```
┌─────────────────────────────────────────────────────┐
│  [🔍 Search Box]  [+] [🏷️] [📁] [🔄]              │
├─────────────────┬───────────────────────────────────┤
│                 │                                   │
│   Tree View     │         Edit Panel                │
│   (Left Panel)  │        (Right Panel)              │
│                 │                                   │
│  📁 Bookmarks   │  ┌─ Edit Item ─────────────────┐  │
│    🔗 Google    │  │ Name: [____________]         │  │
│    🔗 GitHub    │  │ Type: [Dropdown ▼]          │  │
│  📁 Dev Tools   │  │ URL:  [____________] [🔧]    │  │
│    🔗 VS Code   │  │ Desc: [____________]         │  │
│  🔗 Stack O.    │  │        [Save] [Cancel]       │  │
│                 │  └─────────────────────────────┘  │
└─────────────────┴───────────────────────────────────┘
```

### **Key Components**

#### **🔍 Search Bar**
- **Real-time filtering** as you type
- **Searches**: Names, URLs, descriptions, and tags
- **Clear**: Empty the search to see all links
- **Case insensitive** for easier searching

#### **🛠️ Toolbar Buttons**
- **[+] Add Link**: Create new link or folder
- **[🏷️] Manage Tags**: Tag creation and management
- **[📁] Add Folder**: Quick folder creation
- **[🔄] Refresh**: Reload data from database

#### **🌳 Tree View (Left Panel)**
- **Hierarchical Display**: Folders and links in organized tree structure
- **Expandable Folders**: Click arrows to expand/collapse
- **Type Icons**: Visual indicators for different link types
- **Context Menus**: Right-click for additional actions
- **Single Click**: Select item for editing
- **Double Click**: Open/launch the selected link

#### **✏️ Edit Panel (Right Panel)**
- **Dynamic Content**: Shows edit form when item is selected
- **Real-time Validation**: Immediate feedback on required fields
- **Resizable Interface**: Panel adjusts to content
- **Save/Cancel**: Clear action buttons

---

## 🔗 Managing Links

### **Adding a New Link**

#### **Method 1: Quick Add**
1. **Click the [+] button** in toolbar
2. **Choose link type** from the dropdown
3. **Fill in details** (see form fields below)
4. **Click "Save"**

#### **Method 2: Context Menu**
1. **Right-click** where you want to add the link
2. **Select "Add Link Here"** or "Add Folder Here"
3. **Complete the form**
4. **Save your changes**

### **Link Dialog Fields**

#### **📝 Required Fields**
- **Name*** (Display name): What you'll see in the tree
- **Type**: Automatically detected, but can be changed
- **URL/Path*** (Destination): Where the link goes

#### **📋 Optional Fields**
- **Description**: Notes about this link
- **Parent Folder**: Where to organize this link
- **Tags**: Categorization labels (color-coded)

### **🎯 Supported Link Types**

| Type | Description | Examples |
|------|-------------|----------|
| **🌐 Web URL** | Internet websites | `https://github.com` |
| **📄 File Path** | Local files | `C:\Documents\report.pdf` |
| **📁 Folder Path** | Local directories | `C:\Users\Username\Documents` |
| **⚙️ Application** | Executable programs | `C:\Program Files\Git\git-bash.exe` |
| **📱 Windows Store App** | Store applications | `ms-windows-store://...` |
| **🖥️ System Location** | Windows locations | `shell:desktop`, `ms-settings:display` |

### **🔧 Application Links with Arguments**
LinkerApp supports complex application launches:
```
"C:\Program Files\Git\git-bash.exe" --cd-to-home
notepad.exe C:\temp\notes.txt
code --new-window --goto package.json:25
```

---

## 📁 Browse Buttons & File Selection

### **🔧 Browse Application Button**
- **When**: Appears when Type = "Application"
- **Function**: Opens file dialog filtered for executables
- **Filters**: .exe, .bat, .cmd files (with "All Files" fallback)
- **Result**: Automatically populates URL/Path field

### **📁 Browse Folder Button**  
- **When**: Appears when Type = "Folder Path"
- **Function**: Opens Windows folder picker dialog
- **Result**: Selects directory path for the link

### **📄 Browse File Button**
- **When**: Appears when Type = "File Path" 
- **Function**: Opens file selection dialog
- **Filters**: All files (*.*) with type detection
- **Result**: Sets path to selected file

### **💡 Pro Tips**
- **Manual Entry**: You can still type paths manually even with browse buttons
- **Copy-Paste**: Drag files from Explorer or copy-paste paths
- **Arguments**: Add command-line arguments after browsing for executables
- **Validation**: Browse buttons ensure valid paths, but manual entry allows flexibility

---

## 📂 Organizing with Folders

### **Creating Folders**
1. **Use [📁] button** or **right-click → "Add Folder Here"**
2. **Set Name**: Descriptive folder name
3. **Choose Parent**: Where to place this folder (optional)
4. **Save**: Folder appears in tree immediately

### **Folder Features**
- **Unlimited Nesting**: Folders within folders, any depth
- **Visual Hierarchy**: Tree lines show parent-child relationships
- **Drag Targets**: Future drag & drop support
- **Context Actions**: Right-click for folder-specific options

### **Best Practices**
- **Categorize by Purpose**: "Work", "Personal", "Development"
- **Group by Type**: "Applications", "Documents", "Websites"
- **Use Descriptive Names**: Clear, meaningful folder names
- **Avoid Deep Nesting**: 2-3 levels maximum for usability

---

## 🏷️ Tag Management

### **Creating Tags**
1. **Click [🏷️] Manage Tags** button
2. **Enter tag name** in the input field
3. **Choose color** using the color picker
4. **Click "Add Tag"** to create

### **Assigning Tags to Links**
1. **Edit a link** (click to select, edit in right panel)
2. **In Tags section**: Select from available tags
3. **Multiple tags**: Links can have multiple tags
4. **Save changes** to apply tags

### **Tag Colors & Organization**
- **Visual Coding**: Each tag has a unique color
- **Quick Recognition**: Spot categorized links instantly  
- **Filtering**: Future search by tag functionality
- **Customization**: Choose colors that make sense to you

---

## ✏️ Editing & Updates

### **Edit Any Link**
1. **Single-click** a link in the tree view
2. **Edit panel appears** on the right side
3. **Modify any fields**: Name, URL, description, type
4. **Click "Save"** to apply changes
5. **Click "Cancel"** to discard changes

### **Real-time Validation**
- **Required Fields**: Marked with * and validated
- **URL Validation**: Checks for proper format
- **File Existence**: Validates file paths when possible
- **Immediate Feedback**: Error messages appear instantly

### **Data Persistence**
- **Auto-Save**: Changes saved to SQLite database
- **Session Persistence**: Data survives app restarts
- **No Data Loss**: Robust error handling protects your data

---

## 🔍 Search & Navigation

### **Real-time Search**
- **Type anywhere**: Search box always accessible
- **Instant Results**: Filtering happens as you type
- **Multi-field Search**: Searches names, URLs, descriptions
- **Case Insensitive**: "GitHub" matches "github", "GITHUB"

### **Opening Links**
- **Double-click**: Primary method to launch links
- **Default Applications**: Uses Windows file associations
- **Web Links**: Open in your default browser
- **Applications**: Launch directly with any arguments

### **Navigation Tips**
- **Expand/Collapse**: Click arrows to show/hide folder contents
- **Clear Search**: Empty search box to see all links
- **Refresh Data**: Use [🔄] button if data seems stale

---

## ⌨️ Keyboard Shortcuts

### **Currently Available**
- **Enter**: Save when editing (in text fields)
- **Escape**: Cancel current edit operation
- **Tab**: Navigate between form fields
- **Space**: Expand/collapse selected tree item

### **Planned Shortcuts** (Future Updates)
- **Ctrl+N**: New link
- **Ctrl+F**: Focus search box  
- **Delete**: Delete selected link
- **F2**: Rename selected item

---

## 🛠️ Troubleshooting

### **Common Issues & Solutions**

#### **🚫 "Application won't start"**
- **Check .NET 9 Runtime**: Must be installed
- **Run as Administrator**: Try elevated permissions
- **Check Windows version**: Requires Windows 10/11

#### **📄 "Database errors on startup"**
- **File Permissions**: Ensure write access to app folder
- **Antivirus**: Temporarily disable to test
- **Disk Space**: Ensure adequate free space

#### **🔗 "Links won't open"**
- **File Paths**: Verify files still exist at specified paths
- **Applications**: Check if executable paths are correct
- **Permissions**: Some system locations require admin rights

#### **🎨 "Can't see dropdown text"**
- **This is fixed** in version 1.2.0+
- **Update**: Get the latest version of LinkerApp
- **Theme**: Ensure you're not using high contrast mode

### **Performance Tips**
- **Regular Cleanup**: Remove broken or unused links
- **Organize Folders**: Use folders to keep tree manageable
- **Limit Nesting**: Avoid excessive folder depth
- **Database Maintenance**: Restart app occasionally to refresh

### **Getting Help**
- **Documentation**: Check other files in `/docs/` folder
- **Issues**: Report bugs on the project repository
- **Community**: Join discussions for tips and tricks

---

## 🎓 Advanced Usage

### **Power User Features**
- **Batch Operations**: Select multiple items (planned)
- **Import/Export**: Backup and restore data (planned)
- **Custom Hotkeys**: Global shortcuts (planned)
- **System Integration**: Tray icon, context menus (planned)

### **Data Management**
- **Database Location**: `linker.db` in application folder
- **Backup Strategy**: Copy database file for backup
- **Migration**: Move database file to transfer data
- **Reset**: Delete database file to start fresh

---

*This manual covers LinkerApp version 1.2.0. For the latest updates and features, check the project documentation and release notes.*

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