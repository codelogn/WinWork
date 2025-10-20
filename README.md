# WinWork - Universal Link Management Application# WinWork - Universal Link Management Application



<div align="center">A modern, feature-rich link management application built with .NET 9 and WPF, designed to organize, search, and open any type of link including web URLs, file paths, folders, applications, and system locations.



[![.NET 9](https://img.shields.io/badge/.NET-9-512BD4?style=flat-square)](https://dotnet.microsoft.com/)## 🚀 Features

[![WPF](https://img.shields.io/badge/WPF-Windows-0078D4?style=flat-square)](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)

[![SQLite](https://img.shields.io/badge/SQLite-Database-003B57?style=flat-square)](https://sqlite.org/)### Core Link Management

[![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)](LICENSE)- **Hierarchical Organization** - Tree view with folders and subfolders

- **Universal Link Support** - Web URLs, file paths, folders, Windows applications, system locations

**A modern, feature-rich link management application for Windows**- **Drag & Drop** - Intuitive organization with drag and drop support

- **Context Menus** - Right-click actions for quick access

[Features](#-features) • [Installation](#-installation) • [Quick Start](#-quick-start) • [Documentation](#-documentation) • [Contributing](#-contributing)

### Advanced Features

</div>- **Real-time Search** - Instant filtering as you type

- **Tag Management** - Color-coded tags with custom colors

---- **Import/Export** - Browser bookmarks import, multiple export formats

- **System Integration** - System tray and global hotkeys support

## 🚀 Features- **Modern UI** - Glassmorphism effects and transparent windows



### **Core Functionality**### Technical Highlights

- **🗂️ Hierarchical Organization** - Organize links in folders and subfolders with unlimited nesting- **.NET 9** with Windows-specific optimizations

- **🔗 Universal Link Support** - Web URLs, file paths, folders, applications, Windows Store apps, and system locations- **Entity Framework Core** with SQLite database

- **🏷️ Advanced Tagging** - Color-coded tags with custom colors for powerful categorization- **MVVM Architecture** with proper separation of concerns

- **🔍 Real-time Search** - Instant filtering and searching across all links and descriptions- **Dependency Injection** throughout the application

- **🎯 Context Menus** - Right-click actions for quick access and management- **Modern WPF** with custom styling and animations

  - Transparent buttons with hover effects

### **User Experience**  - Contemporary Windows design language

- **🎨 Modern UI** - Clean, professional interface with dark theme support  - Dark/Light theme support

- **📱 Resizable Dialogs** - Flexible window sizing with proper minimum constraints  

- **🖱️ Drag & Drop** - Intuitive organization with drag and drop support (planned)- **Tree View Navigation**

- **⌨️ Keyboard Shortcuts** - Efficient navigation and management  - Hierarchical folder structure (unlimited nesting)

- **🔄 Auto-Refresh** - Real-time updates when data changes  - Drag & drop support for reorganizing

  - Context menus for quick actions

### **Technical Highlights**  - Expand/collapse functionality

- **⚡ .NET 9** - Built with the latest .NET framework for optimal performance  - Icons for different item types

- **🏗️ MVVM Architecture** - Clean separation of concerns and testable code

- **💾 SQLite Database** - Lightweight, serverless database with Entity Framework Core- **Link Management**

- **🔧 Dependency Injection** - Modern application architecture with Microsoft.Extensions.Hosting  - Double-click to open links (external browser for URLs, default app for files)

- **🖼️ ComboBox Visibility** - Enhanced dropdown styling with proper text visibility  - Quick preview of link details

- **💿 Persistent Data** - Your data persists between application sessions  - Search and filter functionality

  - Bulk operations (move, delete, edit)

---

### Backend (Data Management)

## 🛠️ Installation- **SQLite Database**

  - Local database storage

### **Prerequisites**  - Fast querying and indexing

- **Windows 10/11** (x64)  - Portable database file

- **.NET 9 Runtime** ([Download here](https://dotnet.microsoft.com/download/dotnet/9.0))  

- **Data Model**

### **Option 1: Download Release** *(Coming Soon)*  - **Links Table**: ID, Name, URL/URI, Type, ParentID, Tags, Description, CreatedDate, LastModified

1. Download the latest release from [Releases](../../releases)  - **Tags Table**: ID, TagName, Color

2. Extract the ZIP file  - **LinkTags Table**: LinkID, TagID (many-to-many relationship)

3. Run `WinWork.UI.exe`

- **Link Types**

### **Option 2: Build from Source**  - Folder (container for other items)

```bash  - Web Link (HTTP/HTTPS URLs)

# Clone the repository  - File Link (local file paths)

git clone https://github.com/yourusername/WinWork.git  - Application Link (executable paths)

cd WinWork

### Additional Features

# Restore dependencies- **System Integration**

dotnet restore  - System tray integration with context menu

  - Global hotkey for quick access (e.g., Ctrl+Alt+L)

# Build the application  - Overlay window that appears over any application

dotnet build  - Auto-hide and minimize to tray functionality



# Run the application- **Tag System**

dotnet run --project src/WinWork.UI  - Color-coded tags for visual organization

```  - Filter by tags with multi-select

  - Tag management interface with drag & drop

---  

- **Quick Search & Access**

## ⚡ Quick Start  - Instant search with real-time filtering

  - Search by name, URL, tags, or description

### **First Launch**  - Recent links history and favorites

1. **Launch WinWork** - The application will create a local SQLite database  - Fuzzy search for quick navigation

2. **Default Structure** - You'll see pre-created folders: "📁 Bookmarks" and "📁 Development Tools"  

3. **Add Your First Link** - Click the **"+"** button or right-click to add a new link- **Universal Link Opening**

  - Web URLs (HTTP/HTTPS) - opens in default browser

### **Adding Links**  - File paths - opens with associated application

1. **Click "+" button** or **right-click** in the tree view  - Folder paths - opens in File Explorer

2. **Fill in details**:  - Windows Store apps - launches UWP applications

   - **Name**: Display name (e.g., "GitHub")  - System locations (Control Panel, Settings, etc.)

   - **Type**: Auto-detected (Web URL, Application, File, Folder)  

   - **URL/Path**: The actual link destination- **Import/Export**

   - **Parent Folder**: Choose where to organize it  - Import from browser bookmarks (Chrome, Firefox, Edge)

   - **Description**: Optional notes  - Export to various formats (HTML, JSON)

3. **Browse Button**: Use 🔧 browse button for applications, 📁 for folders, 📄 for files  - Backup and restore functionality

4. **Save** - Your link is ready to use!

## Technical Stack

### **Using Links**- **Frontend**: WPF with modern transparent styling and glass effects

- **Double-click** any link to open it- **Backend**: C# .NET 8 (latest LTS)

- **Edit**: Click once to select, then edit in the right panel- **Database**: SQLite with Entity Framework Core

- **Organize**: Use folders to keep everything organized- **System Integration**: Global hotkeys, system tray, overlay windows

- **Search**: Type in the search bar to find links instantly- **UI Framework**: WPF with Fluent Design and custom animations

- **Icons**: Segoe Fluent Icons and custom SVG icons

---- **Effects**: Acrylic backgrounds, blur effects, smooth transitions



## 📚 Documentation## Architecture

```

| Document | Description |┌─────────────────────┐

|----------|-------------|│   Presentation      │

| **[User Manual](docs/USER_MANUAL.md)** | Complete guide for end users |│   (WPF Views)       │

| **[Development Guide](docs/DEVELOPMENT.md)** | Setup and development instructions |├─────────────────────┤

| **[API Documentation](docs/API.md)** | Technical API reference |│   Business Logic    │

| **[Project Status](docs/PROJECT_STATUS.md)** | Current status and roadmap |│   (ViewModels)      │

| **[How to Use Guide](docs/HOW-TO-USE.md)** | Step-by-step usage tutorials |├─────────────────────┤

| **[Recent Improvements](docs/RECENT_IMPROVEMENTS.md)** | Latest updates and fixes |│   Data Access       │

│   (Entity Framework)│

---├─────────────────────┤

│   Database          │

## 🏗️ Architecture│   (SQLite)          │

└─────────────────────┘

``````

WinWork/

├── src/## Project Structure

│   ├── WinWork.UI/          # 🎨 WPF Application & ViewModels```

│   ├── WinWork.Core/        # ⚙️ Business Logic & ServicesWinWork/

│   ├── WinWork.Data/        # 💾 Entity Framework & Repositories├── src/

│   └── WinWork.Models/      # 📋 Domain Models & Entities│   ├── WinWork.UI/          # WPF Application

├── docs/                      # 📖 All Documentation│   ├── WinWork.Core/        # Business Logic

├── tests/                     # 🧪 Unit & Integration Tests│   ├── WinWork.Data/        # Data Access Layer

└── WinWork.sln              # 🛠️ Visual Studio Solution│   └── WinWork.Models/      # Data Models

```├── tests/

├── docs/

**Tech Stack:**└── database/

- **Frontend**: WPF with MVVM pattern```
- **Backend**: .NET 9 with Entity Framework Core
- **Database**: SQLite with automatic migrations
- **DI Container**: Microsoft.Extensions.Hosting

---

## ✨ Recent Updates

### **v1.2.0 - October 2025**
- ✅ **Fixed ComboBox Visibility** - All dropdowns now have proper white text on dark backgrounds
- ✅ **Enhanced Dialog Size** - Link/Folder dialogs are now larger (650x800) and resizable
- ✅ **Browse Buttons** - Added browse buttons for applications (🔧), files (📄), and folders (📁)
- ✅ **Application Arguments Support** - Apps with command-line arguments now work (e.g., `"git-bash.exe --cd-to-home"`)
- ✅ **Database Persistence** - Data now persists properly between app sessions
- ✅ **Entity Framework Fix** - Resolved tracking issues when saving edits

### **v1.1.0 - Earlier Updates**
- ✅ **Toast Notifications** - Success/error feedback system
- ✅ **Real-time Tree Refresh** - UI updates immediately after changes
- ✅ **Tag Management** - Complete tagging system with colors
- ✅ **Search Enhancement** - Improved filtering and search capabilities

---

## 🤝 Contributing

We welcome contributions! Here's how to get started:

### **Development Setup**
```bash
# Clone and setup
git clone https://github.com/yourusername/WinWork.git
cd WinWork

# Install tools
dotnet tool install --global dotnet-ef

# Setup development environment
dotnet restore
dotnet build
```

### **Contributing Guidelines**
1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feature/amazing-feature`)
3. **Commit** your changes (`git commit -m 'Add amazing feature'`)
4. **Push** to the branch (`git push origin feature/amazing-feature`)
5. **Open** a Pull Request

See [Development Guide](docs/DEVELOPMENT.md) for detailed information.

---

## 📄 License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

---

## 🆘 Support

- 📖 **Documentation**: Check the [docs/](docs/) folder
- 🐛 **Bug Reports**: [Create an issue](../../issues)
- 💡 **Feature Requests**: [Create an issue](../../issues)
- 💬 **Questions**: [Discussions](../../discussions)

---

## 🌟 Acknowledgments

- **Microsoft** for .NET 9 and WPF
- **Entity Framework Team** for the excellent ORM
- **SQLite** for the lightweight database
- **Community** for feedback and contributions

---

<div align="center">

**⭐ Star this repository if you find it helpful!**

Made with ❤️ for the Windows community

</div>
