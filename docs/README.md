# WinWork Documentation

## Overview
WinWork is a WPF application for managing links, folders, and notes. It uses MVVM architecture and Entity Framework Core for data persistence.
## Key Features
- Unified add/edit dialog for all item types (Links, Folders, Notes)
- Notes type (freeform text, no URL required)
- Tagging system with color-coded tags
- Hierarchical folders and subfolders
- File-based logging (logs/debug, auto-cleanup >3 days)
- SQLite database (see database.md)
- Entity Framework Core migrations

## Architecture
- **MVVM Pattern**: Separation of UI, logic, and data
- **Dialogs**: Single dialog for adding/editing all item types
- **Logging**: All debug output written to logs/debug, cleaned up automatically
- **Database**: See [database.md](database.md) for schema and technical details

## Recent Changes
- Unified dialog for add/edit
- Notes type added
- Debug window removed, file-based logging only
- Database schema updated for Notes
# WinWork - Universal Link Management Application

A modern, feature-rich link management application built with .NET 9 and WPF, designed to organize, search, and open any type of link including web URLs, file paths, folders, applications, and system locations.

## 🚀 Features

### Core Link Management
- **Hierarchical Organization** - Tree view with folders and subfolders
- **Universal Link Support** - Web URLs, file paths, folders, Windows applications, system locations
- **Drag & Drop** - Intuitive organization with drag and drop support
- **Context Menus** - Right-click actions for quick access

### Advanced Features
- **Real-time Search** - Instant filtering as you type
- **Tag Management** - Color-coded tags with custom colors
- **Import/Export** - Browser bookmarks import, multiple export formats
- **System Integration** - System tray and global hotkeys support
- **Modern UI** - Glassmorphism effects and transparent windows

### Technical Highlights
- **.NET 9** with Windows-specific optimizations
- **Entity Framework Core** with SQLite database
- **MVVM Architecture** with proper separation of concerns
- **Dependency Injection** throughout the application
- **Modern WPF** with custom styling and animations
  - Transparent buttons with hover effects
  - Contemporary Windows design language
  - Dark/Light theme support
  
- **Tree View Navigation**
  - Hierarchical folder structure (unlimited nesting)
  - Drag & drop support for reorganizing
  - Context menus for quick actions
  - Expand/collapse functionality
  - Icons for different item types

- **Link Management**
  - Double-click to open links (external browser for URLs, default app for files)
  - Quick preview of link details
  - Search and filter functionality
  - Bulk operations (move, delete, edit)

### Backend (Data Management)
- **SQLite Database**
  - Local database storage
  - Fast querying and indexing
  - Portable database file
  
- **Data Model**
  - **Links Table**: ID, Name, URL/URI, Type, ParentID, Tags, Description, CreatedDate, LastModified
  - **Tags Table**: ID, TagName, Color
  - **LinkTags Table**: LinkID, TagID (many-to-many relationship)

- **Link Types**
  - Folder (container for other items)
  - Web Link (HTTP/HTTPS URLs)
  - File Link (local file paths)
  - Application Link (executable paths)

### Additional Features
- **System Integration**
  - System tray integration with context menu
  - Global hotkey for quick access (e.g., Ctrl+Alt+L)
  - Overlay window that appears over any application
  - Auto-hide and minimize to tray functionality

- **Tag System**
  - Color-coded tags for visual organization
  - Filter by tags with multi-select
  - Tag management interface with drag & drop
  
- **Quick Search & Access**
  - Instant search with real-time filtering
  - Search by name, URL, tags, or description
  - Recent links history and favorites
  - Fuzzy search for quick navigation
  
- **Universal Link Opening**
  - Web URLs (HTTP/HTTPS) - opens in default browser
  - File paths - opens with associated application
  - Folder paths - opens in File Explorer
  - Windows Store apps - launches UWP applications
  - System locations (Control Panel, Settings, etc.)
  
- **Import/Export**
  - Import from browser bookmarks (Chrome, Firefox, Edge)
  - Export to various formats (HTML, JSON)
  - Backup and restore functionality

## Technical Stack
- **Frontend**: WPF with modern transparent styling and glass effects
- **Backend**: C# .NET 8 (latest LTS)
- **Database**: SQLite with Entity Framework Core
- **System Integration**: Global hotkeys, system tray, overlay windows
- **UI Framework**: WPF with Fluent Design and custom animations
- **Icons**: Segoe Fluent Icons and custom SVG icons
- **Effects**: Acrylic backgrounds, blur effects, smooth transitions

## Architecture
```
┌─────────────────────┐
│   Presentation      │
│   (WPF Views)       │
├─────────────────────┤
│   Business Logic    │
│   (ViewModels)      │
├─────────────────────┤
│   Data Access       │
│   (Entity Framework)│
├─────────────────────┤
│   Database          │
│   (SQLite)          │
└─────────────────────┘
```

## Project Structure
```
WinWork/
├── src/
│   ├── WinWork.UI/          # WPF Application
│   ├── WinWork.Core/        # Business Logic
│   ├── WinWork.Data/        # Data Access Layer
│   └── WinWork.Models/      # Data Models
├── tests/
├── docs/
└── database/
```
