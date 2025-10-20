# Development Notes

## Architecture
- MVVM pattern
- Entity Framework Core with SQLite
- Unified dialog for add/edit (Links, Folders, Notes)
- FileLogger for debug output (logs/debug, auto-cleanup)

## Database
- See docs/database.md for schema and technical details
- Migrations managed via EF Core
- Notes type added to schema

## Logging
- All debug output written to logs/debug
- Old logs (>3 days) cleaned up automatically
# WinWork Development Guide

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

## Prerequisites

### Required Software
- **.NET 9 SDK** or later
- **Visual Studio 2022** (17.8+) or **Visual Studio Code** with C# extension
- **Git** for version control
- **Entity Framework Core CLI** tools

### Development Environment Setup

1. **Install .NET 9 SDK**
   ```bash
   # Download from https://dotnet.microsoft.com/download/dotnet/9.0
   # Verify installation
   dotnet --version
   ```

2. **Install Entity Framework Tools**
   ```bash
   dotnet tool install --global dotnet-ef
   ```

3. **Clone and Setup Project**
   ```bash
   git clone <repository-url>
   cd WinWork
   dotnet restore
   ```

## Project Structure

```
WinWork/
├── src/
│   ├── WinWork.UI/          # WPF Application (Main UI)
│   ├── WinWork.Core/        # Business Logic Services
│   ├── WinWork.Data/        # Data Access Layer & EF Context
│   └── WinWork.Models/      # Data Models & Entities
├── tests/                     # Unit & Integration Tests
├── docs/                      # Documentation
└── database/                  # Database files (local development)
```

## Development Commands

### Building the Solution

```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build src/WinWork.UI

# Build in Release mode
dotnet build --configuration Release
```

### Running the Application

```bash
# Run from solution root
dotnet run --project src/WinWork.UI

# Or navigate to UI project
cd src/WinWork.UI
dotnet run
```

### Database Management

```bash
# Navigate to Data project for EF commands
cd src/WinWork.Data

# Create new migration
dotnet ef migrations add <MigrationName>

# Update database
dotnet ef database update

# Remove last migration
dotnet ef migrations remove

# Generate SQL script
dotnet ef migrations script

# Drop database (development only)
dotnet ef database drop
```

### Testing

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/WinWork.Tests
```

### Package Management

```bash
# Add package to specific project
dotnet add src/WinWork.UI package <PackageName>

# Remove package
dotnet remove src/WinWork.UI package <PackageName>

# Update packages
dotnet list package --outdated
dotnet add package <PackageName> --version <Version>
```

## Development Workflow

### 1. Setting Up Development Database

The application uses SQLite with a local database file. On first run:

```bash
# Ensure you're in the Data project directory
cd src/WinWork.Data

# Create/update database with latest migrations
dotnet ef database update
```

Database file location: `%APPDATA%/WinWork/WinWork.db`

### 2. Making Database Schema Changes

1. Modify models in `WinWork.Models`
2. Update DbContext if needed in `WinWork.Data`
3. Create migration:
   ```bash
   cd src/WinWork.Data
   dotnet ef migrations add YourMigrationName
   ```
4. Apply migration:
   ```bash
   dotnet ef database update
   ```

### 3. Adding New Features

1. **Models**: Add/modify in `WinWork.Models`
2. **Data Access**: Add repositories in `WinWork.Data/Repositories`
3. **Business Logic**: Add services in `WinWork.Core/Services`
4. **UI**: Add views/controls in `WinWork.UI`

### 4. Debugging

- **Database**: Use SQLite browser or VS Code SQLite extension
- **Logging**: Check console output (debug builds have detailed EF logging)
- **UI**: Use WPF debugging tools in Visual Studio

## Building for Distribution

### Development Build

```bash
# Build with debug symbols
dotnet build --configuration Debug
```

### Release Build

```bash
# Clean build for release
dotnet clean
dotnet build --configuration Release --no-restore

# Create self-contained executable
dotnet publish src/WinWork.UI --configuration Release --self-contained true --runtime win-x64
```

### Publishing Options

```bash
# Framework-dependent (requires .NET runtime on target machine)
dotnet publish src/WinWork.UI --configuration Release --runtime win-x64

# Self-contained (includes runtime, larger file size)
dotnet publish src/WinWork.UI --configuration Release --self-contained true --runtime win-x64

# Single file (everything in one executable)
dotnet publish src/WinWork.UI --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true

# Trimmed (smaller size, advanced)
dotnet publish src/WinWork.UI --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
```

## Environment Configuration

### Development Settings

The app automatically creates default settings on first run. For development, you can:

1. **Reset Database**: Delete `%APPDATA%/WinWork/WinWork.db`
2. **Clean Settings**: Delete the entire `%APPDATA%/WinWork/` folder
3. **Custom Database Path**: Modify connection string in `DatabaseConfiguration.cs`

### Production Deployment

1. **Database**: SQLite database is portable - include in app directory or use AppData
2. **Settings**: Application settings are stored in the database
3. **Dependencies**: Ensure target machines have required Visual C++ redistributables

## Troubleshooting

### Common Issues

1. **"dotnet-ef not found"**
   ```bash
   dotnet tool install --global dotnet-ef
   ```

2. **Migration fails**
   ```bash
   # Check if database is locked
   # Close application and try again
   dotnet ef database update --force
   ```

3. **Package restore fails**
   ```bash
   dotnet clean
   dotnet restore --force
   ```

4. **UI doesn't start**
   - Check target framework is `net9.0-windows`
   - Verify WPF workload is installed
   - Check for missing dependencies

### Development Database Reset

```bash
# Full reset (lose all data)
cd src/WinWork.Data
dotnet ef database drop
dotnet ef database update

# Or delete the database file manually
# Windows: %APPDATA%/WinWork/WinWork.db
```

## Code Style & Guidelines

### Naming Conventions
- **Pascal Case**: Classes, methods, properties, public fields
- **Camel Case**: Private fields, parameters, local variables
- **Interfaces**: Prefix with 'I' (e.g., `ILinkService`)

### Project Dependencies
- `UI` → `Core` → `Data` → `Models`
- No circular references
- Use dependency injection for services

### Database Guidelines
- Always create migrations for schema changes
- Use meaningful migration names
- Test migrations on clean database
- Include seed data for default values

## Performance Tips

### Development
- Use `dotnet watch run` for hot reload during UI development
- Enable detailed logging in debug mode
- Use profiling tools for performance issues

### Production
- Use Release configuration
- Consider AOT compilation for faster startup
- Monitor memory usage with large datasets
- Optimize database queries with indexes

## Contributing

1. Create feature branch from `main`
2. Make changes following coding standards
3. Add/update tests for new functionality
4. Update documentation if needed
5. Submit pull request

## Useful Resources

- [.NET 9 Documentation](https://docs.microsoft.com/dotnet/)
- [WPF Documentation](https://docs.microsoft.com/dotnet/desktop/wpf/)
- [Entity Framework Core](https://docs.microsoft.com/ef/)
- [SQLite Documentation](https://www.sqlite.org/docs.html)
