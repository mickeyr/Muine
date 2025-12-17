# Muine Music Player

**A modern, cross-platform music player built with .NET 10 and Avalonia UI**

Muine is an innovative music player, featuring a simple, intuitive interface.
It is designed to allow users to easily construct a playlist from albums and/or
single songs. Its goal is to be simply a music player, not to become a robust
music management application.

Originally written for the GNOME desktop using Mono and GTK#, Muine has been
modernized to use .NET 10 with Avalonia UI for true cross-platform support
on Linux, Windows, and macOS.

## Features

### Current Features
- ✅ Ogg/Vorbis, FLAC, AAC and MP3 music playback support
- ✅ Support for embedded album images in ID3v2 tags
- ✅ ReplayGain support
- ✅ Support for multiple artist and performer tags per song
- ✅ Music library scanning (folders and files)
- ✅ SQLite database for efficient music management
- ✅ Play/Pause/Stop controls with seek functionality
- ✅ Volume control
- ✅ Keyboard media controls with MPRIS support
  - Global media keys on Linux (MPRIS D-Bus)
  - Taskbar "now playing" integration on Linux
  - Window-focus media keys on Windows/macOS

### Planned Features
- 🚧 Playlist queue management
- 🚧 Previous/Next track navigation
- 🚧 Album view and grouping
- 📋 Cover art display in UI
- 📋 Automatic album cover fetching via MusicBrainz and Amazon
- 📋 Configuration system
- 📋 Plugin system
- 📋 Keyboard shortcuts

## Requirements

- .NET 10 SDK or later
- Linux, Windows, or macOS
- VLC media player (for audio playback)

## Building

```bash
dotnet restore
dotnet build
```

## Running Tests

```bash
dotnet test
```

73 tests currently passing ✅

## Running the Application

```bash
cd src/Muine.App
dotnet run
```

## Project Structure

```
Muine/
├── src/
│   ├── Muine.App/          # Avalonia UI application (MVVM)
│   │   ├── Views/          # XAML views
│   │   ├── ViewModels/     # View models
│   │   └── Assets/         # UI resources
│   └── Muine.Core/         # Core business logic library
│       ├── Models/         # Data models (Song, Album, Playlist)
│       └── Services/       # Services (Metadata, Database, Playback)
├── tests/                  # xUnit test project
│   ├── Models/             # Model tests
│   └── Services/           # Service tests
├── docs/                   # Additional documentation
└── Muine.sln              # Solution file
```

## Technology Stack

- **Framework**: .NET 10
- **UI**: Avalonia 11.3.9 (cross-platform XAML-based UI)
- **Audio Metadata**: TagLib-Sharp 2.3.0
- **Database**: SQLite via Microsoft.Data.Sqlite 10.0.1
- **Audio Playback**: LibVLCSharp 3.9.0 with VLC 3.0.21
- **Linux Integration**: Tmds.DBus 0.15.0 (MPRIS media control)
- **MVVM**: CommunityToolkit.Mvvm 8.2.1
- **Testing**: xUnit 2.9.3

## Migration from Legacy Codebase

The original Muine was built on:
- Mono runtime
- GTK# 2.x for UI
- GStreamer 0.10 for audio
- GDBM for database
- GConf for configuration

The modernized version uses:
- .NET 10 runtime (cross-platform)
- Avalonia UI (modern XAML-based)
- LibVLCSharp for audio playback
- SQLite for database
- JSON/XML configuration (planned)

For detailed migration status, see [MIGRATION_STATUS.md](MIGRATION_STATUS.md).

## Contributing

Contributions are welcome! Please ensure all tests pass before submitting a pull request.

## License

GPL v2 - Same as original Muine

Originally written by Jorn Baayen, now maintained by the community.
