# Muine Migration Status - .NET 10 with Avalonia

## Overview
This document tracks the progress of migrating Muine music player from Mono/GTK# to .NET 10 with Avalonia UI.

**Status**: Playback Implemented ✅  
**Last Updated**: December 10, 2025  
**Completion**: ~55% (3 of 5 major phases complete)

## Phase Status

### ✅ Phase 1: Project Structure (COMPLETE)
- Modern .NET 10 solution structure
- Three projects: Muine.App, Muine.Core, Muine.Tests
- Avalonia 11.3.9 UI framework
- Proper separation of concerns
- Build system working perfectly

### ✅ Phase 2: Core Business Logic (COMPLETE)
All core services and models implemented with comprehensive tests:

**Models:**
- `Song.cs`: Modern POCO for song metadata
- `Album.cs`: Album grouping model

**Services:**
- `MetadataService.cs`: Reads audio file metadata using TagLib-Sharp
- `MusicDatabaseService.cs`: SQLite database CRUD operations
- `LibraryScannerService.cs`: Directory scanning and music import

**Tests:** 46 tests, all passing
- Song model: 8 tests
- Album model: 6 tests
- Database service: 3 tests
- Metadata service: 9 tests
- Cover art service: 5 tests
- Library scanner: 8 tests
- Playback service: 7 tests

### ✅ Phase 3: Audio Backend (COMPLETE)
**Status**: LibVLCSharp integration complete

**Completed:**
- ✅ LibVLCSharp cross-platform audio library integration
- ✅ PlaybackService with play/pause/stop/seek functionality
- ✅ Volume control
- ✅ Position/duration tracking for progress display
- ✅ PlaybackState management (Playing, Paused, Stopped)
- ✅ ReplayGain support from metadata
- ✅ Event-driven architecture for state changes
- ✅ Comprehensive unit tests (7 tests)
- ✅ Graceful handling when LibVLC is not available

**Technical Details:**
- Uses LibVLCSharp 3.9.0 with native libraries for Windows/Mac/Linux
- Timer-based position updates (100ms interval)
- Automatic ReplayGain application from song metadata
- Disposed properly to release resources

### 🟡 Phase 4: UI Implementation (PARTIAL)
**Status**: Playback UI complete, other features pending

**Completed:**
- ✅ Main window XAML layout
- ✅ Player controls UI with functional bindings
- ✅ Play/Pause/Stop buttons with commands
- ✅ Progress slider with position tracking
- ✅ Time display (current/total duration)
- ✅ Volume slider
- ✅ Song selection and double-click to play
- ✅ Current song display
- ✅ Menu structure
- ✅ Music import functionality (folder and files)
- ✅ Song list display

**Needed:**
- Additional dialogs (About)
- Cover art display in player
- Playlist visualization and management
- Album sidebar implementation
- Previous/Next track functionality

### 🔴 Phase 5: Configuration & Platform Integration (NOT STARTED)
**Priority**: MEDIUM

Needed:
- Configuration service (replace GConf)
- Settings storage (JSON/XML)
- Platform integration (media keys, notifications)
- Plugin system modernization

## Quick Start for Developers

### Build & Test
```bash
cd /home/runner/work/Muine/Muine
dotnet build
dotnet test
```

### Run Application
```bash
cd src.net/Muine.App
dotnet run
```

### Project Structure
```
Muine/
├── src.net/
│   ├── Muine.App/              # Avalonia UI
│   │   ├── Views/              # XAML views
│   │   └── ViewModels/         # View models (basic scaffolding)
│   └── Muine.Core/             # Business logic
│       ├── Models/             # Data models
│       │   ├── Song.cs
│       │   └── Album.cs
│       └── Services/           # Services
│           ├── MetadataService.cs
│           ├── MusicDatabaseService.cs
│           └── LibraryScannerService.cs
└── tests/                      # Unit tests
    ├── Models/
    └── Services/
```

## Next Steps (Priority Order)

1. **Playlist Management** (Phase 4)
   - Implement playlist queue
   - Add/remove songs functionality
   - Previous/Next track navigation
   - Save/load playlists

2. **Album View** (Phase 4)
   - Populate album sidebar
   - Group songs by album
   - Album selection and playback

3. **Cover Art Display** (Phase 4)
   - Display cover art in player
   - Implement cover art downloading (MusicBrainz/Amazon)
   - Album art grid view

4. **Configuration System** (Phase 5)
   - Create settings service
   - Implement preferences dialog
   - Store user settings (volume, last played, etc.)

5. **Platform Integration** (Phase 5)
   - Media keys support
   - System notifications
   - System tray integration

## Technical Decisions Made

### Why SQLite?
- Cross-platform
- No external server needed
- Better performance than GDBM
- Wide .NET support
- ACID compliance

### Why Avalonia?
- True cross-platform (Linux, Windows, macOS)
- XAML-based (familiar to WPF/UWP developers)
- Active development and community
- Good performance
- Modern UI capabilities

### Why TagLib-Sharp?
- Already used in original Muine
- Cross-platform
- Supports multiple formats
- Well-maintained
- Familiar API

## Known Issues
None currently. All tests passing, code review clean, security scan passed.

## Dependencies
```xml
<!-- Core Library -->
<PackageReference Include="TagLibSharp" Version="2.3.0" />
<PackageReference Include="Microsoft.Data.Sqlite" Version="10.0.1" />
<PackageReference Include="LibVLCSharp" Version="3.9.0" />
<PackageReference Include="VideoLAN.LibVLC.Windows" Version="3.0.21" />
<PackageReference Include="VideoLAN.LibVLC.Mac" Version="3.0.21" />

<!-- Application -->
<PackageReference Include="Avalonia" Version="11.3.9" />
<PackageReference Include="Avalonia.Desktop" Version="11.3.9" />
<PackageReference Include="Avalonia.Themes.Fluent" Version="11.3.9" />
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.1" />

<!-- Testing -->
<PackageReference Include="xunit" Version="2.9.3" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
```

## Performance Notes
- Database operations are async to avoid UI blocking
- File scanning uses async I/O
- Large music libraries should be scanned in background
- Consider implementing caching for album artwork

## Testing Strategy
- Unit tests for all models and services
- Integration tests for database operations
- UI tests (pending Avalonia UI completion)
- Manual testing on Linux (primary target platform)

## Resources
- [Avalonia Documentation](https://docs.avaloniaui.net/)
- [TagLib-Sharp API](https://github.com/mono/taglib-sharp)
- [SQLite .NET Provider](https://docs.microsoft.com/en-us/dotnet/standard/data/sqlite/)
