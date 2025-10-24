
## Latest Features & Changes (v1.2.0)
- Unified add/edit dialog for all item types (Links, Folders, Notes)
- Notes type (freeform text, no URL required)
- Tagging system with color-coded tags
- Hierarchical folders and subfolders (unlimited nesting)
- Browse buttons for applications, files, folders
- Application links with command-line arguments
- Context menu with Open/Edit/Add/Delete
- Real-time search includes Notes field
- Loading spinner for async operations
- "Parent Folder" renamed to "Parent Item" (can be any type)
- Export includes timestamp
# WinWork - How to Use Guide (v1.2.0)

## 📑 Table of Contents
- [🚀 Getting Started](#-getting-started)
- [➕ Adding Links and Folders](#-adding-links-and-folders)
- [🏷️ Managing Tags](#️-managing-tags)
- [📂 Organizing Content](#-organizing-content)
- [🔗 Using Links](#-using-links)
- [🔍 Search and Navigation](#-search-and-navigation)
- [🎨 Current Features](#-current-features)
- [⌨️ Keyboard Shortcuts](#️-keyboard-shortcuts)
- [💡 Tips and Best Practices](#-tips-and-best-practices)
- [🛠️ Troubleshooting](#️-troubleshooting)

---

## 🚀 Getting Started

### **First Launch Experience**
When you first launch WinWork, you'll see:
- **Clean Modern Interface** with tree view (left) and edit panel (right)
- **Sample Folders**: "📁 Bookmarks" and "📁 Development Tools" with example links
- **Resizable Panels**: Adjust layout to your preference
 - **Toolbar**: [+] Add Item, [🏷️] Manage Tags buttons
- **Search Bar**: Real-time filtering as you type

### **Main Interface Elements (Current v1.2.0)**
- **🌳 Tree View (Left)**: Hierarchical display of your links and folders
- **✏️ Edit Panel (Right)**: Dynamic form that appears when you select items
- **🔍 Search Bar (Top)**: Real-time search across all your links
 - **🛠️ Toolbar**: Essential actions - Add Item [+] and Manage Tags [🏷️]
- **📱 Responsive Design**: Resizable dialogs and panels

### **Key Improvements in v1.2.0**
- ✅ **Fixed ComboBox Visibility**: All dropdown text now clearly visible
- ✅ **Resizable Dialogs**: Link/Folder forms now 650x800 and fully resizable
- ✅ **Browse Buttons**: 🔧 File/folder/app selection made easy
- ✅ **Enhanced Application Support**: Command-line arguments now supported
- ✅ **Better UX**: Larger, more usable interface elements

## ➕ Adding Links and Folders

### **Adding a New Item (Enhanced in v1.2.0)**
1. **Click the [+] button** in the toolbar OR **right-click** in tree view
2. **Link Dialog opens** (now 650x800 and resizable!)
3. **Fill in the details:**
   - **Name*** (required): Display name for the link
   - **URL/Path*** (required): Destination - can be web address or file path
   - **Description**: Optional notes about this link  
   - **Type**: Auto-detected, but changeable via dropdown
   - **Parent Folder**: Choose organization location (dropdown)
   - **Tags**: Multi-select from available tags (dropdown)
4. **Use Browse Buttons** 🔧 for easy selection:
   - **🔧 Browse Application**: For .exe, .bat, .cmd files
   - **📁 Browse Folder**: Windows folder picker
   - **📄 Browse File**: File selection dialog
5. **Click "Save"** to create your link

### **Adding a New Folder**
1. **Right-click** in tree view where you want the folder OR use [+] button
2. **Select "Folder"** as the type
3. **Enter folder details:**
   - **Name**: Folder display name
   - **Description**: Optional folder description
   - **Parent Folder**: Where to place this folder
4. **Save** - Folder appears immediately and ready for use

### **🎯 Supported Link Types (Auto-Detected)**
| **Type** | **Examples** | **Browse Button** |
|----------|--------------|-------------------|
| **🌐 Web URL** | `https://google.com`, `http://localhost:3000` | None |
| **⚙️ Application** | `C:\Program Files\VS Code\Code.exe`, `notepad.exe` | 🔧 Browse Applications |
| **📄 File Path** | `C:\Documents\report.pdf`, `D:\Photos\vacation.jpg` | 📄 Browse Files |
| **📁 Folder Path** | `C:\Users\Name\Documents`, `\\server\share` | 📁 Browse Folders |
| **📱 Store App** | `ms-windows-store://pdp/?productid=...` | None |
| **🖥️ System** | `shell:desktop`, `ms-settings:display` | None |

### **🔧 Application Links with Arguments (New!)**
You can now save complex application launches:
```
"C:\Program Files\Git\git-bash.exe" --cd-to-home
notepad.exe C:\temp\notes.txt  
code --new-window --goto package.json:25
```
- **Browse for the .exe** first, then **add arguments manually**
- **Quotes handled automatically** for paths with spaces
- **Full command-line support** for power users

## 🏷️ Managing Tags

### **Opening Tag Manager**
1. **Click [🏷️] Manage Tags** button in the toolbar
2. **Tag Management dialog opens** with current tags listed

### **Creating New Tags**
1. **In Tag Manager**, enter tag name in the input field
2. **Choose a color** using the color picker
3. **Click "Add Tag"** to create
4. **Tag appears immediately** in the tags list

### **Default Sample Tags**
WinWork comes with sample tags you can use or modify:
- **Work**: Professional and work-related links
- **Personal**: Personal bookmarks and resources  
- **Development**: Development tools and programming resources
- **Frequently Used**: Your most accessed links
- **Learning**: Educational content and tutorials

### **Assigning Tags to Links**
1. **Edit a link** (select item, use right panel)
2. **In Tags dropdown**: Select from available tags (multi-select supported)
3. **Links can have multiple tags** for flexible organization
4. **Save changes** to apply tag assignments

### **Tag Features**
- **Color Coding**: Visual identification with custom colors
- **Multi-Selection**: Links can have multiple tags
- **Persistent**: Tags saved in database across sessions
- **Searchable**: Future search/filter by tags (planned)

## 📂 Organizing Content

### **Current Organization Features (v1.2.0)**
- **Folder Structure**: Hierarchical organization with unlimited nesting
- **Parent-Child Relationships**: Links can be placed inside folders
- **Visual Tree Display**: Clear folder/link relationships with expand/collapse
- **Dropdown Selection**: Choose parent folder when creating/editing items

### **Creating Folder Hierarchies**
1. **Create folders** using [+] button → select "Folder" type
2. **Set parent folder** in the Parent Folder dropdown
3. **Unlimited nesting depth** - folders within folders
4. **Visual indicators**: Tree lines show parent-child relationships

### **Moving Links and Folders (Current Method)**
1. **Edit any item** by selecting it (single-click)
2. **Change Parent Folder** using the dropdown in edit panel
3. **Save changes** to move item to new location
4. **Root level**: Leave Parent Folder empty

### **Organization Best Practices**
- **Use descriptive folder names**: "Work Projects", "Development Tools"
- **Group by workflow**: How you use items, not just categories
- **Limit nesting depth**: 2-3 levels maximum for usability
- **Consistent structure**: Apply same organization logic throughout

### **Future Enhancements (Planned)**
- **Drag & Drop**: Direct drag-and-drop moving
- **Sorting Options**: Custom sort orders within folders
- **Bulk Operations**: Move multiple items at once

## 🔗 Using Links

### **Opening Links (Enhanced in v1.2.0)**
- **Double-click** any link to open it with enhanced support:
  - **🌐 Web URLs**: Open in your default browser
  - **⚙️ Applications**: Launch directly with command-line arguments
  - **📄 Files**: Open with default application (Office, PDF reader, etc.)
  - **📁 Folders**: Open in Windows File Explorer
  - **📱 Store Apps**: Launch Windows Store applications
  - **🖥️ System Locations**: Open settings, shell locations

### **Enhanced Application Support**
WinWork now handles complex application launches:
```bash
# These all work perfectly now:
"C:\Program Files\Git\git-bash.exe" --cd-to-home
notepad.exe C:\temp\notes.txt
code --new-window --goto package.json:25
powershell.exe -Command "Get-Process"
```

### **Selection and Editing**
- **Single-click** to select and view/edit link details
- **Edit panel appears** on the right with all link properties
- **Real-time validation** shows errors immediately
- **Save/Cancel buttons** for clear action confirmation

### **Current Features (v1.2.0)**
- **Persistent Selection**: Selected item stays active across operations
- **Immediate Editing**: Click item → edit in right panel → save
- **Validation Feedback**: Required fields marked with * and validated
- **Error Handling**: Clear messages for invalid paths/URLs

## 🔍 Search and Navigation

### **Real-Time Search (Current v1.2.0)**
1. **Type in the search box** at the top of the interface
2. **Results filter instantly** as you type - no need to press Enter
3. **Searches multiple fields**: Names, URLs, descriptions
4. **Clear search** by emptying the text box to see all links

### **Search Features**
- **Case Insensitive**: "GitHub" matches "github", "GITHUB"  
- **Partial Matching**: "git" finds "GitHub", "GitLab", "git-bash"
- **Multi-field Search**: Searches names, URLs, AND descriptions
- **Instant Results**: No delay, filters as you type
- **Clear Reset**: Empty search box shows all links immediately

### **Navigation Tips**
- **Expand/Collapse**: Click folder arrows to show/hide contents
- **Tree Structure**: Visual hierarchy makes navigation intuitive
- **Persistent State**: Expanded folders stay open during searches
- **Quick Access**: Double-click to open, single-click to edit

### **Planned Enhancements**
- **Tag Filtering**: Filter by specific tags
- **Advanced Search**: Boolean operators, regex support
- **Recent Items**: Quick access to frequently used links
- **Global Hotkeys**: System-wide access to search

## 🎨 Current Features (v1.2.0)

### **✅ Implemented Features**
- **📊 SQLite Database**: Persistent data storage with Entity Framework Core
- **🌳 Tree View Interface**: Hierarchical display of links and folders
- **🔍 Real-time Search**: Instant filtering across all content
 - **➕ Add/Edit Items**: Create and modify items with comprehensive form
- **🏷️ Tag Management**: Create, assign, and manage colored tags
- **📂 Folder Organization**: Nested folder structure for organization
- **🔧 Browse Buttons**: Easy file/folder/application selection
- **⚙️ Enhanced Applications**: Command-line argument support
- **📱 Resizable Interface**: 650x800 dialogs, fully resizable
- **🎨 Fixed UI Issues**: Visible ComboBox text on all themes

### **🎯 Link Type Support**
- ✅ **Web URLs**: `https://`, `http://` links
- ✅ **File Paths**: Documents, images, any file type  
- ✅ **Folder Paths**: Local and network directories
- ✅ **Applications**: .exe with full argument support
- ✅ **System Locations**: `shell:`, `ms-settings:` etc.
- ✅ **Store Apps**: Windows Store application links

### **🖥️ Interface Improvements**
- ✅ **Larger Dialogs**: 650x800 default size vs previous 500x600
- ✅ **Resizable Windows**: All dialogs can be resized by user
- ✅ **Visible Dropdowns**: Fixed ComboBox text visibility issues
- ✅ **Browse Integration**: 🔧 buttons for easy file selection
- ✅ **Better UX**: Larger, more accessible interface elements

### **📋 Planned Features (Future Releases)**
- 🔄 **Global Hotkeys**: System-wide access shortcuts
- 🔄 **Drag & Drop**: Direct item moving and reordering
- 🔄 **Import/Export**: Backup and restore functionality
- 🔄 **Themes**: Light/Dark theme support
- 🔄 **System Tray**: Minimize to tray option
- 🔄 **Auto-Backup**: Scheduled data backups

## ⌨️ Keyboard Shortcuts

### **Currently Available (v1.2.0)**
- **Enter**: Save when editing in text fields
- **Escape**: Cancel current edit operation/close dialogs
- **Tab**: Navigate between form fields
- **Space**: Expand/collapse selected tree items
- **Arrow Keys**: Navigate tree view items
- **Double-click**: Open/launch selected link
- **Single-click**: Select item for editing

### **Form Navigation**
- **Tab/Shift+Tab**: Move between Name, URL, Description fields
- **Enter in text field**: Apply changes (when in input fields)
- **Escape in dialog**: Cancel and close dialog
- **Click Save/Cancel**: Complete or abort edit operations

- **Planned Keyboard Shortcuts (Future)**
- **Ctrl+N**: Add new item
- **Ctrl+F**: Focus search box
- **F2**: Rename selected item
- **Delete**: Delete selected item
- **Ctrl+S**: Save changes
- **Ctrl+O**: Open selected link
- **Alt+F4**: Exit application

### **Future Global Hotkeys**
Will be configurable in future versions:
- **Show/Hide WinWork**: Quick access from anywhere
- **Quick-Add from Clipboard**: Instant link creation
- **Focus Search**: Jump directly to search from desktop

## 💡 Tips and Best Practices

### **Organization Tips**
1. **📝 Use Descriptive Names**: Make link names clear and searchable
   - Good: "GitHub - MyProject Repository"
   - Avoid: "Link1", "App"

2. **🗂️ Organize by Workflow**: Group by how you use items, not just categories
   - "Daily Tools" vs "Applications"
   - "Current Projects" vs "All Projects"

3. **🏷️ Tag Consistently**: Use same tags across similar items
   - Consistent: "work", "personal", "development"
   - Avoid: "Work", "work-stuff", "workplace"

4. **🧹 Regular Maintenance**: Clean up periodically
   - Remove broken or outdated links
   - Update paths that have changed
   - Reorganize as your workflow evolves

### **Interface Tips (v1.2.0)**
1. **🔧 Use Browse Buttons**: Don't type long paths manually
   - Click 🔧 to browse for applications
   - Use folder picker for directories
   - Still allows manual editing after browsing

2. **📏 Resize Dialogs**: Adjust dialog size to your preference
   - Drag dialog corners to resize
   - Larger dialogs = easier form completion
   - Size preferences persist across sessions

3. **🔍 Search Efficiently**: Take advantage of real-time search
   - Type partial names: "git" finds "GitHub", "GitLab"
   - Search descriptions too, not just names
   - Clear search to return to full view

### **Application Link Pro Tips**
1. **⚙️ Complex App Commands**: Leverage the new argument support
   ```
   "C:\Program Files\Git\git-bash.exe" --cd-to-home
   code --new-window --goto package.json:25
   notepad.exe C:\temp\scratch.txt
   ```

2. **📂 Project Workflows**: Create application links for project contexts
   - VS Code with specific workspace
   - Terminal with specific directory
   - Browser with development URL

### **Data Safety**
1. **💾 Database Location**: Know where your data is stored
   - Database file: `linker.db` in application folder
   - Copy this file to backup your data
   - Move file to transfer to another machine

2. **✅ Validate Links**: Regularly check that your links still work
   - File paths can become invalid after moves
   - Applications may get updated/relocated
   - Web URLs can change or disappear

## 🛠️ Troubleshooting

### **✅ Fixed Issues (v1.2.0)**

#### **Dropdown Text Not Visible** ✅ SOLVED
- **Was**: ComboBox dropdowns showed blank text
- **Fixed**: Custom templates with proper popup backgrounds
- **Now**: All dropdown text clearly visible with white text

#### **Dialog Too Small** ✅ SOLVED  
- **Was**: 500x600 dialog cramped for complex links
- **Fixed**: Increased to 650x800 with full resize support
- **Now**: Comfortable editing with resizable windows

#### **No Browse Functionality** ✅ SOLVED
- **Was**: Had to type all file paths manually
- **Fixed**: Added 🔧 browse buttons for files/folders/apps
- **Now**: Easy selection with manual override capability

#### **Application Arguments Rejected** ✅ SOLVED
- **Was**: `git-bash.exe --cd-to-home` marked as invalid
- **Fixed**: Enhanced parsing for quoted paths and arguments
- **Now**: Full command-line argument support

---

### **Current Common Issues**

#### **🚫 "Application won't start"**
- **Cause**: Missing .NET 9 Runtime
- **Solution**: Install .NET 9 Runtime from Microsoft
- **Check**: Run `dotnet --version` in Command Prompt

#### **📄 "Database errors on startup"**  
- **Cause**: File permissions or antivirus blocking
- **Solutions**:
  1. Run as Administrator (right-click → "Run as administrator")
  2. Temporarily disable antivirus to test
  3. Ensure write permissions to application folder

#### **🔗 "Links won't open"**
- **File Paths**: Check if file still exists at that location
- **Applications**: Verify executable path is correct
- **Web URLs**: Test URL in browser first
- **Solution**: Edit link and update path/URL

#### **⚠️ "Invalid link data" errors**
- **Cause**: Usually incomplete required fields
- **Solution**: Ensure Name and URL/Path fields are filled
- **Check**: Look for * next to required field labels

---

### **Performance & Optimization**

#### **🚀 Keeping WinWork Fast**
1. **Regular Cleanup**: Remove unused or broken links
2. **Organize Efficiently**: Use folders but avoid excessive nesting
3. **Database Maintenance**: Restart app occasionally
4. **Monitor Size**: Very large link collections may slow performance

#### **📊 Current Limitations**
- **Search**: Real-time but searches all fields (can slow with many items)
- **UI Responsiveness**: Large tree structures may affect performance
- **Database**: SQLite suitable for personal use, not massive datasets

---

### **Data Management**

#### **💾 Database Location**
- **File**: `linker.db` in the application folder
- **Backup**: Copy this file to backup all your data
- **Transfer**: Move file to transfer data to another machine
- **Reset**: Delete file to start fresh (loses all data)

#### **🔄 Data Recovery**
If you lose data:
1. **Check Application Folder**: Look for `linker.db` file
2. **File Recovery**: Use Windows file recovery tools
3. **Previous Versions**: Check Windows "Previous Versions" feature
4. **Manual Backup**: Future versions will include export/import

---

### **Getting Help**

#### **📚 Documentation**
1. **This Guide**: Comprehensive usage instructions
2. **README.md**: Technical overview and setup
3. **DEVELOPMENT.md**: Technical architecture details
4. **GitHub Issues**: Report bugs and request features

#### **🐛 Reporting Issues**
When reporting problems, include:
- **Windows Version**: 10/11, build number
- **.NET Version**: Run `dotnet --version`
- **Error Messages**: Exact text of any error dialogs
- **Steps to Reproduce**: What you were doing when issue occurred
- **Link Types**: What type of link caused the problem

---

### **System Requirements**
- **OS**: Windows 10 version 1809+ or Windows 11
- **Runtime**: .NET 9 Runtime (Microsoft)
- **Memory**: 100MB+ RAM recommended
- **Storage**: 50MB+ for application + database size
- **Display**: 1024x768 minimum (1920x1080+ recommended)

---

## 🎯 Quick Start Checklist (v1.2.0)

### **Essential First Steps**
- [ ] **🚀 Launch WinWork** - First-time database setup happens automatically
- [ ] **👀 Explore Sample Data** - Check out pre-created folders and example links
- [ ] **➕ Add Your First Link** - Click [+] button, fill form, experience new larger dialog
- [ ] **🔧 Try Browse Buttons** - Use browse functionality for easy file/app selection
- [ ] **🏷️ Create Custom Tag** - Open Tag Manager, add a tag with your chosen color

### **Getting Comfortable**
- [ ] **🔍 Test Search** - Type in search box, see real-time filtering
- [ ] **📂 Organize Content** - Create folders, use Parent Folder dropdown
- [ ] **⚙️ Add Complex App** - Try an application link with command-line arguments
- [ ] **📱 Resize Interface** - Resize the dialogs to your preference
- [ ] **🔗 Test All Link Types** - Web URLs, files, folders, applications

### **Validate Core Functions**
- [ ] **💾 Data Persistence** - Close and reopen app, verify data is saved
- [ ] **✏️ Edit Links** - Select items, edit in right panel, save changes
- [ ] **🎨 Check Dropdowns** - Verify all ComboBox text is clearly visible
- [ ] **🚀 Open Links** - Double-click various link types, ensure they work
- [ ] **🔄 Test Workflow** - Complete add → organize → search → open cycle

---

## 🎉 Success!

**Congratulations!** You're now ready to efficiently manage all your links, shortcuts, and applications with WinWork v1.2.0. 

**Key Benefits You'll Enjoy:**
- 🔍 **Instant Search** across all your organized links
- 📂 **Hierarchical Organization** with unlimited folder nesting  
- 🔧 **Easy File Selection** with integrated browse buttons
- ⚙️ **Powerful Application Links** supporting command-line arguments
- 🏷️ **Flexible Tagging** with color-coded organization
- 📱 **Comfortable Interface** with resizable, larger dialogs

**For Advanced Usage:**
- Check out other documentation in the `/docs/` folder
- Review `PROJECT_STATUS.md` for current features and roadmap
- See `RECENT_IMPROVEMENTS.md` for latest changes and fixes

*Happy link managing! 🚀*
