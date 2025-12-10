# Playback Architecture

## Overview

The Muine playback system uses LibVLCSharp for cross-platform audio playback with a clean event-driven MVVM architecture.

## Component Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    MainWindow (View)                        │
│  ┌─────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ Play Button │  │ Stop Button  │  │ Pause Button │      │
│  └─────────────┘  └──────────────┘  └──────────────┘      │
│  ┌──────────────────────────────────────────────────┐      │
│  │           Progress Slider (Seek)                 │      │
│  └──────────────────────────────────────────────────┘      │
│  ┌──────────────┐  ┌─────────────────────────────┐        │
│  │Volume Slider │  │ Time Display (0:00 / 3:45)  │        │
│  └──────────────┘  └─────────────────────────────┘        │
└─────────────────────────────────────────────────────────────┘
                           │
                           │ Data Binding
                           ▼
┌─────────────────────────────────────────────────────────────┐
│              MainWindowViewModel (ViewModel)                │
│                                                              │
│  Observable Properties:                                      │
│  • CurrentSongDisplay                                        │
│  • IsPlaying, IsPaused                                       │
│  • CurrentPosition, MaxPosition                              │
│  • TimeDisplay                                               │
│  • Volume                                                    │
│                                                              │
│  Commands:                                                   │
│  • PlaySelectedSongCommand                                   │
│  • TogglePlayPauseCommand                                    │
│  • StopCommand                                               │
│  • SeekCommand                                               │
└─────────────────────────────────────────────────────────────┘
                           │
                           │ Uses
                           ▼
┌─────────────────────────────────────────────────────────────┐
│              PlaybackService (Service)                      │
│                                                              │
│  State:                                                      │
│  • PlaybackState (Stopped/Playing/Paused)                   │
│  • CurrentSong                                               │
│  • Position, Duration                                        │
│  • Volume                                                    │
│                                                              │
│  Methods:                                                    │
│  • PlayAsync(Song)                                           │
│  • Play(), Pause(), Stop()                                   │
│  • TogglePlayPause()                                         │
│  • Seek(TimeSpan)                                            │
│                                                              │
│  Events:                                                     │
│  • StateChanged                                              │
│  • PositionChanged (every 100ms)                             │
│  • CurrentSongChanged                                        │
└─────────────────────────────────────────────────────────────┘
                           │
                           │ Uses
                           ▼
┌─────────────────────────────────────────────────────────────┐
│           LibVLC / MediaPlayer (LibVLCSharp)                │
│                                                              │
│  • Cross-platform audio engine                              │
│  • Supports all VLC formats                                 │
│  • Hardware acceleration                                     │
│  • Audio effects and filters                                │
└─────────────────────────────────────────────────────────────┘
```

## Event Flow

### Playing a Song

1. User double-clicks song in list
2. `OnSongDoubleClick` event handler calls `PlaySelectedSongCommand`
3. ViewModel calls `PlaybackService.PlayAsync(song)`
4. PlaybackService:
   - Stops current playback
   - Creates Media from song file
   - Applies ReplayGain if available
   - Starts playback via LibVLC
5. LibVLC raises `Playing` event
6. PlaybackService raises `StateChanged` event
7. ViewModel updates `IsPlaying` property
8. UI updates play button state

### Progress Tracking

1. Timer in PlaybackService fires every 100ms
2. PlaybackService queries LibVLC for current position
3. PlaybackService raises `PositionChanged` event
4. ViewModel updates `CurrentPosition` and `TimeDisplay`
5. UI updates progress slider position

### Seeking

1. User drags progress slider
2. Slider property changed event fires
3. `OnSliderPropertyChanged` calls `SeekCommand`
4. ViewModel calls `PlaybackService.Seek(position)`
5. PlaybackService updates LibVLC position
6. Position updates continue via timer

## Key Design Decisions

### Why LibVLCSharp?
- **Cross-platform**: Works on Windows, macOS, Linux
- **Robust**: Mature VLC backend with excellent format support
- **Performance**: Hardware-accelerated decoding
- **Features**: Built-in ReplayGain, equalizer, effects

### Why Event-Driven?
- **Loose coupling**: UI doesn't depend on playback internals
- **Testability**: Easy to test components in isolation
- **Extensibility**: Easy to add new UI components that respond to playback

### Why Timer-Based Updates?
- **Smooth UI**: 100ms provides smooth slider animation
- **Efficient**: Doesn't block UI thread
- **Reliable**: Not dependent on LibVLC event timing

## ReplayGain Implementation

```csharp
if (song.Gain != 0.0)
{
    var gainDb = song.Gain;
    // Convert dB to linear: 10^(dB/20)
    var linearGain = Math.Pow(10, gainDb / 20.0);
    var newVolume = (int)(100 * linearGain);
    _mediaPlayer.Volume = Math.Clamp(newVolume, 0, 200);
}
```

ReplayGain values from metadata are automatically applied to normalize volume across tracks.

## Error Handling

The PlaybackService gracefully handles:
- LibVLC not available (initialization fails, `_mediaPlayer` is null)
- Missing audio files (throws `FileNotFoundException`)
- Invalid operations (throws `InvalidOperationException`)

The UI checks `PlaybackService.IsLibVLCAvailable` to determine if playback is possible.

## Testing

The PlaybackService includes comprehensive tests that work both with and without LibVLC:
- State management tests
- Volume control tests
- Error handling tests
- Event firing tests

Tests detect LibVLC availability and adjust expectations accordingly.

## Future Enhancements

Potential areas for improvement:
1. **Playlist Queue**: Add queue management for multiple songs
2. **Crossfade**: Smooth transitions between tracks
3. **Equalizer**: Expose LibVLC's built-in equalizer
4. **Gapless Playback**: For continuous album playback
5. **Audio Visualization**: Spectrum analyzer or waveform display
