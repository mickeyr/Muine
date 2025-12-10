# Muine Music Player - .NET 10 Migration

This is a modern port of the Muine music player to .NET 10 with Avalonia UI.

## Project Structure

```
Muine/
├── src.net/
│   ├── Muine.App/          # Avalonia UI application
│   └── Muine.Core/         # Core business logic library
│       ├── Models/         # Data models (Song, Album)
│       └── Services/       # Services (Metadata, Database)
├── tests/                  # Unit tests
│   └── Models/             # Model tests
└── Muine.sln              # Solution file
```

## Requirements

- .NET 10 SDK
- Linux, Windows, or macOS

## Building

```bash
dotnet restore
dotnet build
```

## Running Tests

```bash
dotnet test
```

## Running the Application

```bash
cd src.net/Muine.App
dotnet run
```

## Features Migrated

### ✅ Completed
- [x] .NET 10 project structure
- [x] Modern C# models for Song and Album
- [x] TagLib-Sharp integration for metadata reading
- [x] SQLite database integration
- [x] Music library scanning (folders and files)
- [x] Cover art extraction and caching
- [x] Avalonia UI application
- [x] Audio playback engine (LibVLCSharp)
- [x] Play/Pause/Stop controls
- [x] Progress tracking with seek
- [x] Volume control
- [x] ReplayGain support
- [x] Unit tests for all services (46 tests passing)

### 🚧 In Progress
- [ ] Playlist queue management
- [ ] Previous/Next track navigation
- [ ] Album view and grouping

### 📋 Planned
- [ ] Cover art display in UI
- [ ] Cover art downloading (MusicBrainz/Amazon)
- [ ] Configuration system (replacing GConf)
- [ ] Plugin system for .NET 10
- [ ] Keyboard shortcuts
- [ ] Media key integration

## Technology Stack

- **Framework**: .NET 10
- **UI**: Avalonia 11.3.9 (cross-platform XAML)
- **Audio Metadata**: TagLib-Sharp 2.3.0
- **Database**: SQLite (via Microsoft.Data.Sqlite 10.0.1)
- **Audio Playback**: LibVLCSharp 3.9.0 with VLC 3.0.21
- **MVVM**: CommunityToolkit.Mvvm 8.2.1
- **Testing**: xUnit

## Differences from Original

The original Muine was built on:
- Mono runtime
- GTK# 2.x for UI
- GStreamer 0.10 for audio
- GDBM for database
- GConf for configuration

The new version uses:
- .NET 10 runtime (cross-platform)
- Avalonia UI (modern XAML-based)
- Cross-platform audio library (TBD)
- SQLite for database
- JSON/XML configuration

## License

GPL v2 - Same as original Muine
