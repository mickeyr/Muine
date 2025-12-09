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
- [x] Avalonia UI application scaffolding
- [x] Unit tests for core models
- [x] Basic UI layout with music player controls

### 🚧 In Progress
- [ ] SQLite database integration
- [ ] Audio playback engine (replacing GStreamer)
- [ ] Album cover management
- [ ] Playlist management
- [ ] Configuration system (replacing GConf)

### 📋 Planned
- [ ] Full UI implementation
- [ ] Plugin system for .NET 10
- [ ] Music library scanning
- [ ] ReplayGain support
- [ ] Cover art downloading
- [ ] Keyboard shortcuts

## Technology Stack

- **Framework**: .NET 10
- **UI**: Avalonia 11.3.9 (cross-platform XAML)
- **Audio Metadata**: TagLib-Sharp 2.3.0
- **Database**: SQLite (via Microsoft.Data.Sqlite 10.0.1)
- **Audio Playback**: TBD (considering LibVLCSharp, NAudio, or similar)
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
