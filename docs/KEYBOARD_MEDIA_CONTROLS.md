# Keyboard Media Controls

## Overview

Muine supports keyboard media controls, allowing you to control playback using dedicated media keys on your keyboard. This feature works cross-platform on Windows, Linux, and macOS through Avalonia's keyboard event abstraction.

## Supported Media Keys

The following media keys are supported:

### Playback Controls

| Key | Action | Description |
|-----|--------|-------------|
| **Media Play/Pause** | Toggle Play/Pause | Starts playback if stopped, toggles between play and pause if playing |
| **Media Stop** | Stop | Stops playback completely |
| **Media Next Track** | Next Track | Plays the next song in the playlist |
| **Media Previous Track** | Previous Track | Plays the previous song in the playlist |

### Volume Controls

| Key | Action | Description |
|-----|--------|-------------|
| **Volume Up** | Increase Volume | Increases volume by 5% (max 100%) |
| **Volume Down** | Decrease Volume | Decreases volume by 5% (min 0%) |
| **Volume Mute** | Toggle Mute | Mutes audio (stores previous volume) or unmutes (restores previous volume) |

## Platform Support

### Windows
- Media keys work when the application has focus
- Most keyboards with multimedia keys are supported
- Windows system media transport controls integration may be added in future

### Linux
- Media keys work when the application has focus
- Tested with standard multimedia keyboards
- MPRIS D-Bus integration may be added in future for global media key support

### macOS
- Media keys work when the application has focus
- System media key integration may be added in future

## Implementation Details

### Architecture

The media key handling is implemented in `MainWindow.axaml.cs`:

1. A `KeyDown` event handler is attached to the main window
2. When a media key is pressed, the handler checks the key type
3. The appropriate command from `MainWindowViewModel` is executed
4. The event is marked as handled to prevent further propagation

### Code Location

- **File**: `src/Muine.App/Views/MainWindow.axaml.cs`
- **Method**: `OnWindowKeyDown`
- **Event**: Subscribed in constructor via `this.KeyDown += OnWindowKeyDown`

### Volume Mute Implementation

The mute functionality stores the volume level before muting:
- When muting: Current volume is saved to `_volumeBeforeMute` and volume is set to 0
- When unmuting: Volume is restored from `_volumeBeforeMute` (defaults to 50 if invalid)

## Usage

1. **Start the application**: `dotnet run` from `src/Muine.App` directory
2. **Import music**: Use File → Import Music Folder
3. **Add songs to playlist**: Double-click songs in the Music Library tab
4. **Use media keys**: Press media keys on your keyboard to control playback

## Limitations

- **Application must have focus**: Currently, media keys only work when Muine is the active window
- **No global hotkeys**: The current implementation doesn't support global hotkeys (keys work even when app is not focused)

## Future Enhancements

### Planned Improvements

1. **MPRIS Integration (Linux)**
   - Support for D-Bus MPRIS interface
   - Global media key support without focus
   - Integration with desktop media controls

2. **Windows System Media Transport Controls**
   - Native Windows 10/11 media controls integration
   - Thumbnail toolbar buttons
   - Global media key support

3. **macOS MPNowPlayingInfoCenter**
   - Native macOS media controls
   - Control center integration
   - Global media key support

4. **Customizable Key Bindings**
   - Allow users to customize media key mappings
   - Support for additional keyboard shortcuts
   - Configuration UI for key bindings

## Testing

### Manual Testing

To test media key functionality:

1. Build and run the application:
   ```bash
   cd /home/runner/work/Muine/Muine
   dotnet build
   cd src/Muine.App
   dotnet run
   ```

2. Load some music and add songs to the playlist

3. Test each media key:
   - Press Play/Pause key → Should toggle playback
   - Press Stop key → Should stop playback
   - Press Next key → Should play next song
   - Press Previous key → Should play previous song
   - Press Volume Up/Down → Should adjust volume
   - Press Mute → Should mute/unmute

### Automated Testing

Currently, UI keyboard event testing is not implemented due to the complexity of testing Avalonia UI components. This feature has been manually tested and verified to work correctly.

## Troubleshooting

### Media keys not responding

1. **Check if application has focus**: Click on the Muine window to ensure it's active
2. **Verify keyboard support**: Some keyboards may not send standard media key codes
3. **Check platform**: Ensure your platform supports media keys through Avalonia

### Volume mute not working correctly

1. The mute functionality should toggle between current volume and 0
2. If volume is already 0, unmute will restore to previous volume or 50%
3. Check that volume slider is not manually set to 0 (which would affect mute behavior)

## Related Documentation

- [PLAYBACK_ARCHITECTURE.md](PLAYBACK_ARCHITECTURE.md) - Details on playback service
- [README.md](../README.md) - Main project documentation
- [MIGRATION_STATUS.md](../MIGRATION_STATUS.md) - Migration progress

## Code Examples

### Event Handler Implementation

```csharp
private async void OnWindowKeyDown(object? sender, KeyEventArgs e)
{
    if (DataContext is not MainWindowViewModel viewModel)
        return;

    switch (e.Key)
    {
        case Key.MediaPlayPause:
            viewModel.TogglePlayPauseCommand.Execute(null);
            e.Handled = true;
            break;
        // ... other cases
    }
}
```

### Command Execution

The implementation leverages existing commands in `MainWindowViewModel`:
- `TogglePlayPauseCommand` - Toggle play/pause
- `StopCommand` - Stop playback
- `PlayNextCommand` - Play next track
- `PlayPreviousCommand` - Play previous track
- `Volume` property - Direct volume manipulation

## Contributing

If you'd like to contribute to media key support:

1. Platform-specific integration (MPRIS, SMTC, MPNowPlayingInfoCenter)
2. Global hotkey support
3. Customizable key bindings
4. Automated UI testing for keyboard events

Please ensure any changes maintain cross-platform compatibility.
