# Copilot Instructions for Muine Music Player

## Project Overview

Muine is a music player being migrated from legacy Mono/GTK# stack to modern .NET 10 with Avalonia UI for cross-platform support (Linux, Windows, macOS).

**Migration Status**: ~40% complete (see MIGRATION_STATUS.md for details)

## Project Structure

```
Muine/
├── src.net/                    # New .NET 10 codebase
│   ├── Muine.App/              # Avalonia UI application (MVVM pattern)
│   │   ├── Views/              # XAML views
│   │   ├── ViewModels/         # View models
│   │   └── Assets/             # UI resources
│   └── Muine.Core/             # Core business logic library
│       ├── Models/             # Data models (Song, Album)
│       └── Services/           # Services (Metadata, Database, Scanner)
├── tests/                      # xUnit test project
│   ├── Models/                 # Model tests
│   └── Services/               # Service tests
├── src/                        # Legacy Mono/GTK# code (reference only)
├── README.NET10.md             # .NET 10 migration documentation
└── MIGRATION_STATUS.md         # Detailed migration status
```

## Architecture & Design Patterns

### Core Principles

1. **Separation of Concerns**: UI (Avalonia) is completely separate from business logic (Core)
2. **MVVM Pattern**: Use for all UI components in Muine.App
3. **Async/Await**: All I/O operations must be asynchronous
4. **Dependency Injection**: Use for service management
5. **Modern C# Idioms**: Properties, null-safety, pattern matching, records where appropriate

### Code Style

- **Models**: Use POCOs with auto-properties, computed properties for derived values
- **Services**: Async methods, implement IDisposable where needed
- **ViewModels**: Inherit from ViewModelBase, use CommunityToolkit.Mvvm attributes
- **Naming**: PascalCase for public members, camelCase with underscore for private fields
- **Null Safety**: Enable nullable reference types, use `string?` for nullable strings

## Technology Stack

### Core Dependencies
- **.NET**: 10.0
- **UI Framework**: Avalonia 11.3.9
- **MVVM**: CommunityToolkit.Mvvm 8.2.1
- **Metadata**: TagLib-Sharp 2.3.0
- **Database**: Microsoft.Data.Sqlite 10.0.1
- **Testing**: xUnit 2.9.3

### Audio Backend (To Be Implemented)
- Consider: LibVLCSharp, NAudio, or PortAudio
- Must support: Play/Pause/Stop, Volume, Seek, ReplayGain

## Build & Test Commands

```bash
# Build solution
dotnet build

# Run tests
dotnet test

# Run application
cd src.net/Muine.App
dotnet run

# Build for release
dotnet build -c Release
```

## Coding Guidelines

### Models

Models should be simple POCOs with computed properties:

```csharp
public class Song
{
    public int Id { get; set; }
    public string Filename { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string[] Artists { get; set; } = Array.Empty<string>();
    
    // Computed properties
    public bool HasAlbum => !string.IsNullOrEmpty(Album);
    public string DisplayName => !string.IsNullOrEmpty(Title) 
        ? Title 
        : Path.GetFileNameWithoutExtension(Filename);
}
```

### Services

Services should be async, disposable, and follow dependency injection:

```csharp
public class MusicDatabaseService : IDisposable
{
    private readonly SqliteConnection _connection;
    
    public MusicDatabaseService(string databasePath)
    {
        _connectionString = $"Data Source={databasePath}";
    }
    
    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection(_connectionString);
        await _connection.OpenAsync();
    }
    
    public async Task<List<Song>> GetAllSongsAsync()
    {
        // Implementation
    }
    
    public void Dispose() => _connection?.Dispose();
}
```

### ViewModels

Use MVVM Community Toolkit for ViewModels:

```csharp
public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _currentSong = "No song playing";
    
    [RelayCommand]
    private async Task PlayAsync()
    {
        // Play logic
    }
}
```

### Tests

Write comprehensive tests for all business logic:

```csharp
public class SongTests
{
    [Fact]
    public void Song_HasAlbum_ShouldReturnTrueWhenAlbumIsSet()
    {
        var song = new Song { Album = "Test Album" };
        Assert.True(song.HasAlbum);
    }
}
```

## Common Tasks

### Adding a New Model

1. Create in `src.net/Muine.Core/Models/`
2. Use modern C# features (init, required, etc.)
3. Add computed properties for derived values
4. Create unit tests in `tests/Models/`

### Adding a New Service

1. Create in `src.net/Muine.Core/Services/`
2. Make all I/O operations async
3. Implement IDisposable if managing resources
4. Add integration tests in `tests/Services/`
5. Consider dependency injection

### Adding UI Features

1. Create XAML view in `src.net/Muine.App/Views/`
2. Create ViewModel in `src.net/Muine.App/ViewModels/`
3. Use data binding in XAML
4. Implement commands using RelayCommand
5. Keep UI logic out of code-behind

## Migration Notes

### What's Been Replaced

| Legacy Component | Modern Replacement | Notes |
|-----------------|-------------------|-------|
| Mono runtime | .NET 10 | Full cross-platform support |
| GTK# 2.x | Avalonia 11.3.9 | Modern XAML-based UI |
| GDBM database | SQLite | Better performance, ACID compliance |
| GConf | JSON/XML config | To be implemented |
| GStreamer 0.10 | TBD | LibVLCSharp or NAudio recommended |

### Legacy Code Reference

The original Mono/GTK# code in `src/` directory is kept for reference only:
- **DO NOT** modify files in `src/` directory
- Use it as reference for understanding original functionality
- Port logic to new services in `src.net/Muine.Core/Services/`

### Key Differences

1. **No GTK# Dependencies**: Use Avalonia controls instead
2. **No GLib/GObject**: Use standard .NET async patterns
3. **No Mono.Posix**: Use System.IO and .NET cross-platform APIs
4. **No P/Invoke to libmuine.so**: Reimplement in managed C#

## Testing Requirements

### Unit Test Coverage

All new code should have unit tests:
- Models: Test all properties and computed values
- Services: Test all public methods
- ViewModels: Test commands and property changes

### Integration Tests

Test interactions between components:
- Database operations with real SQLite
- File scanning with test directories
- Metadata reading with test audio files

### Test Naming Convention

```csharp
[Fact]
public void MethodName_Condition_ExpectedBehavior()
{
    // Arrange
    // Act
    // Assert
}
```

## Security Considerations

- **No SQL Injection**: Use parameterized queries
- **Path Traversal**: Validate all file paths
- **Exception Handling**: Don't expose sensitive information in errors
- **Async Patterns**: Use `GetAwaiter().GetResult()` not `.Wait()` in constructors

## Performance Guidelines

- Use async I/O for all file operations
- Implement caching for album artwork
- Use pagination for large lists
- Background scanning for music libraries
- Dispose of resources properly

## Known Issues & TODOs

See MIGRATION_STATUS.md for:
- Current phase status
- Known issues
- Planned features
- Next steps priority order

## Getting Help

- **Migration Status**: See MIGRATION_STATUS.md
- **Build Instructions**: See README.NET10.md
- **Original Code**: Reference `src/` directory
- **Dependencies**: Check .csproj files

## Important Reminders

1. **Always run tests** after making changes
2. **Update MIGRATION_STATUS.md** when completing phases
3. **Write tests first** for new features (TDD)
4. **Keep UI separate** from business logic
5. **Use async/await** for I/O operations
6. **Document complex logic** with XML comments
7. **Follow existing patterns** in the codebase
