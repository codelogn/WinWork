# Development Guide

## Development Environment Setup

### Prerequisites
- **Visual Studio 2022** (v17.8 or later) with .NET 9 support
- **.NET 9 SDK** (9.0.0 or later)
- **Git** for version control
- **SQLite** (included with .NET)

### Recommended Extensions
- **Entity Framework Core Tools**
- **WPF Designer** (included with Visual Studio)
- **NuGet Package Manager** (included)

## Project Structure Deep Dive

### LinkerApp.Models
Contains all domain models and enums.

**Key Files:**
- `Link.cs` - Main link entity with hierarchical relationships
- `Tag.cs` - Tag entity for categorization
- `AppSettings.cs` - Application settings model
- `Enums/LinkType.cs` - Enumeration of supported link types

### LinkerApp.Data
Data access layer with Entity Framework Core.

**Key Files:**
- `LinkerDbContext.cs` - Main database context
- `Repositories/LinkRepository.cs` - Link data operations
- `Repositories/TagRepository.cs` - Tag data operations
- `Migrations/` - EF Core database migrations

### LinkerApp.Core
Business logic and service layer.

**Key Directories:**
- `Services/` - Business logic implementations
- `Interfaces/` - Service contracts
- `ServiceCollectionExtensions.cs` - DI registration

### LinkerApp.UI
WPF presentation layer with MVVM pattern.

**Key Directories:**
- `Views/` - XAML windows and user controls
- `ViewModels/` - MVVM view models
- `Controls/` - Custom WPF controls
- `Converters/` - Value converters for data binding

## Coding Standards

### C# Style Guidelines
Follow Microsoft's C# coding conventions:

```csharp
// Use PascalCase for public members
public class LinkService : ILinkService
{
    // Use camelCase for private fields
    private readonly ILinkRepository _linkRepository;
    
    // Use PascalCase for properties
    public string Name { get; set; }
    
    // Use PascalCase for methods
    public async Task<Link> CreateLinkAsync(Link link)
    {
        // Use camelCase for local variables
        var createdLink = await _linkRepository.CreateAsync(link);
        return createdLink;
    }
}
```

### XAML Guidelines
- Use proper indentation (4 spaces)
- Group related properties together
- Use meaningful names for controls
- Leverage data binding over code-behind

```xml
<Window x:Class="LinkerApp.UI.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="LinkerApp"
        Width="1200"
        Height="800">
    
    <Grid Margin="10">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>
        
        <!-- Search Bar -->
        <TextBox Grid.Row="0"
                 Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}"
                 PlaceholderText="Search links..."
                 Margin="0,0,0,10"/>
        
        <!-- Tree View -->
        <TreeView Grid.Row="1"
                  ItemsSource="{Binding RootLinks}"
                  SelectedItem="{Binding SelectedLink}"/>
    </Grid>
</Window>
```

## Database Development

### Entity Framework Migrations

**Adding a new migration:**
```bash
cd src/LinkerApp.Data
dotnet ef migrations add <MigrationName>
```

**Updating the database:**
```bash
dotnet ef database update
```

**Rolling back a migration:**
```bash
dotnet ef database update <PreviousMigrationName>
```

### Database Design Patterns

**Entity Relationships:**
- Use navigation properties for related entities
- Configure relationships in `OnModelCreating`
- Use appropriate cascade delete behaviors

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Configure Link-Tag many-to-many relationship
    modelBuilder.Entity<Link>()
        .HasMany(l => l.Tags)
        .WithMany(t => t.Links)
        .UsingEntity("LinkTags");
    
    // Configure Link hierarchy (self-referencing)
    modelBuilder.Entity<Link>()
        .HasOne(l => l.Parent)
        .WithMany(l => l.Children)
        .HasForeignKey(l => l.ParentId)
        .OnDelete(DeleteBehavior.Cascade);
}
```

## UI Development

### MVVM Pattern Implementation

**ViewModel Base Class:**
```csharp
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
            return false;
            
        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```

**Command Implementation:**
```csharp
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;
    
    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }
    
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
    
    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    
    public void Execute(object? parameter) => _execute(parameter);
}
```

### Dependency Injection in ViewModels

**Service Registration:**
```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLinkerAppServices(this IServiceCollection services)
    {
        // Register repositories
        services.AddScoped<ILinkRepository, LinkRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        
        // Register services
        services.AddScoped<ILinkService, LinkService>();
        services.AddScoped<ITagService, TagService>();
        
        // Register ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<LinkDialogViewModel>();
        
        return services;
    }
}
```

## Testing Strategy

### Unit Testing
Create unit tests for business logic in services.

**Example Test:**
```csharp
[TestClass]
public class LinkServiceTests
{
    private Mock<ILinkRepository> _mockRepository;
    private LinkService _linkService;
    
    [TestInitialize]
    public void Setup()
    {
        _mockRepository = new Mock<ILinkRepository>();
        _linkService = new LinkService(_mockRepository.Object);
    }
    
    [TestMethod]
    public async Task CreateLinkAsync_ValidLink_ReturnsCreatedLink()
    {
        // Arrange
        var link = new Link { Name = "Test", Url = "https://example.com" };
        _mockRepository.Setup(r => r.CreateAsync(link)).ReturnsAsync(link);
        
        // Act
        var result = await _linkService.CreateLinkAsync(link);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Test", result.Name);
    }
}
```

### Integration Testing
Test the full application flow including database operations.

### UI Testing
Consider using tools like:
- **White** - For WPF UI automation
- **FlaUI** - Modern UI automation framework
- **Microsoft.VisualStudio.TestTools.UITest** - Visual Studio UI testing

## Performance Optimization

### Database Performance
- Use async operations for all database calls
- Implement proper indexing on frequently queried columns
- Use `Include()` for eager loading related data
- Consider pagination for large datasets

### UI Performance
- Use virtualization for large collections (`VirtualizingPanel`)
- Implement lazy loading for tree view nodes
- Use data binding efficiently
- Minimize property change notifications

### Memory Management
- Dispose of resources properly
- Unsubscribe from events in ViewModels
- Use weak event patterns for long-lived objects

## Deployment

### Build Configuration
**Release Configuration:**
```xml
<PropertyGroup Condition="'$(Configuration)'=='Release'">
    <DebugType>none</DebugType>
    <Optimize>true</Optimize>
    <TrimMode>link</TrimMode>
    <PublishTrimmed>true</PublishTrimmed>
</PropertyGroup>
```

### Publishing Options
**Self-contained deployment:**
```bash
dotnet publish -c Release -r win-x64 --self-contained true
```

**Framework-dependent deployment:**
```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

## Troubleshooting Common Issues

### Build Issues
- **Missing .NET 9 SDK:** Install from Microsoft's website
- **Package conflicts:** Clear NuGet cache and restore
- **XAML errors:** Check for typos in property names and bindings

### Runtime Issues
- **Database connection:** Ensure SQLite files have proper permissions
- **UI freezing:** Check for blocking operations on UI thread
- **Memory leaks:** Verify event subscriptions are properly cleaned up

### Debugging Tips
- Use **Debug.WriteLine()** for console output
- Set breakpoints in ViewModels and Services
- Use **Data Binding debugging** in Visual Studio
- Monitor performance with **PerfView** or **dotMemory**