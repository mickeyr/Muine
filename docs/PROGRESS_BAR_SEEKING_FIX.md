# Progress Bar Seeking Fix - Technical Summary

## Problem
When dragging the playback progress bar slider, the song did not properly skip to the new position. Instead, VLC errors were logged:
```
[00007f08380600a0] main input error: Invalid PCR value in ES_OUT_SET_(GROUP_)PCR !
```

## Root Cause
The original implementation used `PropertyChanged` event on the slider to detect seeking:
- The slider had two-way binding to `CurrentPosition` 
- Every time the position updated during playback, it triggered the `PropertyChanged` event
- The code tried to filter out programmatic updates using `slider.IsPointerOver && viewModel.IsPlaying`
- This approach was unreliable and caused the slider to seek while still being updated from playback
- Continuous seeking during playback caused the VLC "Invalid PCR" errors

## Solution
Replaced the `PropertyChanged` approach with explicit pointer event handling:

1. **Added seeking state tracking** in `MainWindowViewModel`:
   - Added `_isUserSeeking` private field to track when user is actively seeking
   - Modified `OnPlaybackPositionChanged` to skip position updates while `_isUserSeeking` is true
   - Added public `BeginSeeking()` method to set the flag when user starts dragging
   - Added public `EndSeeking(double position)` method to perform the actual seek when user releases

2. **Updated UI event handling** in `MainWindow.axaml.cs`:
   - Removed the problematic `PropertyChanged` logic from `OnSliderPropertyChanged`
   - Added `OnSliderPointerPressed` to call `viewModel.BeginSeeking()` when user starts dragging
   - Added `OnSliderPointerReleased` to call `viewModel.EndSeeking(slider.Value)` when user releases

3. **Updated XAML bindings** in `MainWindow.axaml`:
   - Changed slider events from `PropertyChanged="OnSliderPropertyChanged"` to:
     - `PointerPressed="OnSliderPointerPressed"`
     - `PointerReleased="OnSliderPointerReleased"`
   - Changed `IsEnabled="{Binding IsPlaying}"` to `IsEnabled="{Binding CanSeek}"`
   
4. **Added CanSeek property**:
   - Added `CanSeek` computed property that returns true when a song is loaded
   - This allows seeking even when paused (not just playing)
   - Added property change notification when current song changes

## Benefits
- **Single seek operation**: Seeking only happens once when the user releases the slider, not continuously during dragging
- **No interference**: Playback position updates don't interfere with user seeking
- **Better UX**: Slider is enabled whenever a song is loaded, allowing seeking while paused
- **Eliminates VLC errors**: The "Invalid PCR" errors are eliminated because we no longer seek continuously

## Files Modified
1. `src.net/Muine.App/ViewModels/MainWindowViewModel.cs`
   - Added `_isUserSeeking` flag
   - Added `CanSeek` property
   - Added `BeginSeeking()` and `EndSeeking()` methods
   - Modified `OnPlaybackPositionChanged()` to respect seeking state
   - Modified `OnCurrentSongChanged()` to notify CanSeek changes

2. `src.net/Muine.App/Views/MainWindow.axaml`
   - Changed slider event bindings from PropertyChanged to PointerPressed/PointerReleased
   - Changed IsEnabled from IsPlaying to CanSeek

3. `src.net/Muine.App/Views/MainWindow.axaml.cs`
   - Replaced `OnSliderPropertyChanged` logic with `OnSliderPointerPressed` and `OnSliderPointerReleased`

## Testing
- All existing tests pass
- Build succeeds with no warnings or errors
- The fix follows Avalonia UI best practices for handling slider interaction
