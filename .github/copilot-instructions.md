# Muine - Copilot Instructions

## Project Overview

Muine is an innovative music player for GNOME desktop, featuring a simple, intuitive interface designed to allow users to easily construct playlists from albums and/or single songs. The application focuses on being a music player, not a comprehensive music management application.

## Technology Stack

- **Primary Language**: C# (Mono runtime)
- **UI Framework**: GTK# >= 2.12.9, GTK+ >= 2.8
- **Audio Playback**: GStreamer 0.10 (default) or xine-lib >= 1.0.0rc3b
- **Metadata**: TagLib-Sharp >= 2.0.3
- **Build System**: GNU Autotools (autoconf, automake, libtool)
- **Native Code**: C (for bindings, glue code in `libmuine/`)
- **Minimum Mono Version**: 1.1
- **Plugin System**: Custom plugin architecture

## Repository Structure

```
/src/           - Main C# application code
/libmuine/      - Native C libraries and bindings
/PluginLib/     - Plugin API and interfaces
/DBusLib/       - D-Bus integration
/plugins/       - Built-in plugins (TrayIcon, NotificationArea, Inotify)
/data/          - Application data files (icons, UI definitions)
/doc/           - Documentation
/po/            - Internationalization/localization files
```

## Code Style and Conventions

### C# Code
- Use tabs for indentation (consistent with existing code)
- Follow standard C# naming conventions (PascalCase for public members, camelCase for private)
- Include GPL license headers at the top of all source files (see existing files for template)
- Use `using` statements grouped by: System, external libraries (Gtk, GLib, Gdk), then Mono.Unix
- Keep classes focused and cohesive

### C Code
- Used primarily in `libmuine/` for native bindings and GStreamer/multimedia integration
- Follow GTK+/GNOME C coding style
- Use descriptive variable and function names

## Building the Project

```bash
# First time setup
./configure
make

# Clean build
make clean
make
```

### Build Dependencies
- Mono >= 1.1
- Gtk# >= 2.12.9
- Gtk+ >= 2.8
- TagLib-Sharp >= 2.0.3
- gdbm
- GStreamer 0.10 or xine-lib >= 1.0.0rc3b
- intltool >= 0.37.1 (for make dist, 0.4.0+ recommended)

## Key Features to Preserve

- **Audio Format Support**: Ogg/Vorbis, FLAC, AAC, MP3
- **Album Cover Fetching**: MusicBrainz and Amazon integration
- **Embedded Album Images**: ID3v2 tag support
- **ReplayGain**: Audio normalization support
- **Multiple Artists**: Support for multiple artist and performer tags per song
- **Plugin System**: Extensible architecture via PluginLib
- **Internationalization**: Multi-language support via gettext/intltool

## Plugin Development

Plugins are built using the interfaces defined in `PluginLib/`:
- `IPlayer.cs` - Player control interface
- `ISong.cs` - Song metadata interface
- `Plugin.cs` - Base plugin class

See existing plugins in `/plugins/` for examples:
- `TrayIcon.cs` - System tray integration
- `NotificationArea.cs` - Notification area support
- `InotifyPlugin.cs` - File system monitoring

## Testing

- Manual testing is the primary validation method for this project
- Test changes against actual music files in various formats (Ogg, FLAC, MP3, AAC)
- Verify UI changes in a running GNOME desktop environment
- Test plugin loading and functionality

## Important Files

- `src/Global.cs` - Global application state and entry point
- `src/Player.cs` - Core playback functionality
- `src/Database.cs` - Music library database
- `src/PlaylistWindow.cs` - Main UI window
- `configure.in` - Autotools configuration
- `muine.spec.in` - RPM package specification

## Best Practices

1. **Minimal Changes**: Make surgical, focused changes to address specific issues
2. **Backward Compatibility**: Maintain compatibility with Mono 1.1+ and older GTK# versions
3. **Localization**: Wrap user-facing strings with Catalog.GetString() for i18n support
4. **Memory Management**: Be mindful of proper disposal of GTK# widgets and GStreamer resources
5. **Error Handling**: Use appropriate error dialogs (see `ErrorDialog.cs`) for user-facing errors
6. **Plugin Compatibility**: Changes to PluginLib interfaces may break existing plugins
7. **Native Interop**: Exercise caution when modifying P/Invoke declarations and native bindings

## Common Tasks

### Adding New UI Strings
- Wrap strings with `Catalog.GetString("Your string")`
- Update `po/POTFILES.in` if adding new source files with translatable strings

### Modifying Build Configuration
- Edit `configure.in` for build system changes
- Run `autoreconf` or `./configure` to regenerate build files
- Update `Makefile.am` files for new source files

### Adding Dependencies
- Update minimum version requirements in `configure.in`
- Document in README.md
- Update `muine.spec.in` for RPM packaging

## License

This project is licensed under the GNU General Public License v2 (GPL-2.0). All new code contributions must maintain this license and include the appropriate GPL header.
