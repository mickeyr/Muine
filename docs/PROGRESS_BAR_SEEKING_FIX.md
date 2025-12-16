# Progress Bar Seeking Fix - Technical Summary

## Problem
When dragging the playback progress bar slider, the song did not properly skip to the new position. Two issues were identified:
1. VLC "Invalid PCR value" errors were logged during seeking
2. The slider would jump back to the current playback position while being dragged

```
[00007f08380600a0] main input error: Invalid PCR value in ES_OUT_SET_(GROUP_)PCR !
```

## Root Causes

### Issue 1: Continuous Seeking
The original implementation used `PropertyChanged` event on the slider to detect seeking:
- The slider had two-way binding to `CurrentPosition` 
- Every time the position updated during playback, it triggered the `PropertyChanged` event
- The code tried to filter out programmatic updates using `slider.IsPointerOver && viewModel.IsPlaying`
- This approach was unreliable and caused the slider to seek while still being updated from playback
- Continuous seeking during playback caused the VLC "Invalid PCR" errors

### Issue 2: Slider Jumping During Drag
Even after fixing the continuous seeking issue:
- The slider still had two-way binding to `CurrentPosition`
- During drag, playback position updates would try to reset the slider value
- The `_isUserSeeking` flag prevented the ViewModel property update but not the binding update
- This caused the slider to jump back to the current position while being dragged

## Solution

### Phase 1: Fix Continuous Seeking
Replaced the `PropertyChanged` approach with explicit pointer event handling:

1. **Added seeking state tracking** in `MainWindowViewModel`:
   - Added `_isUserSeeking` private field to track when user is actively seeking
   - Modified `OnPlaybackPositionChanged` to skip position updates while `_isUserSeeking` is true
   - Added public `BeginSeeking()` method to set the flag when user starts dragging
   - Added public `EndSeeking(double position)` method to perform the actual seek when user releases

2. **Updated UI event handling** in `MainWindow.axaml.cs`:
   - Removed the problematic `PropertyChanged` logic
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

### Phase 2: Fix Slider Jumping During Drag
Changed the binding mode to prevent playback updates from affecting user interaction:

1. **Changed binding mode** in `MainWindow.axaml`:
   - Changed from `Value="{Binding CurrentPosition}"` (TwoWay by default)
   - To `Value="{Binding CurrentPosition, Mode=OneWay}"`
   - This allows the slider to receive position updates but not send them back during drag

2. **Added seek preview** in `MainWindow.axaml.cs`:
   - Added `OnSliderPointerMoved` event handler
   - Calls `viewModel.UpdateSeekPreview(slider.Value)` while dragging

3. **Added UpdateSeekPreview method** in `MainWindowViewModel`:
   - Updates the time display to show the target seek position while dragging
   - Only active when `_isUserSeeking` is true
   - Provides visual feedback of where the seek will go

## Benefits
- **Single seek operation**: Seeking only happens once when the user releases the slider, not continuously during dragging
- **No slider jumping**: OneWay binding prevents playback updates from resetting the slider during drag
- **Better UX**: Slider is enabled whenever a song is loaded, allowing seeking while paused
- **Seek preview**: Shows target time while dragging for better user feedback
- **Click to seek**: Can click anywhere on the slider to jump to that position
- **Eliminates VLC errors**: The "Invalid PCR" errors are eliminated because we no longer seek continuously

## Files Modified
1. `src/Muine.App/ViewModels/MainWindowViewModel.cs`
   - Added `_isUserSeeking` flag
   - Added `CanSeek` property
   - Added `BeginSeeking()`, `UpdateSeekPreview()`, and `EndSeeking()` methods
   - Modified `OnPlaybackPositionChanged()` to respect seeking state
   - Modified `OnCurrentSongChanged()` to notify CanSeek changes

2. `src/Muine.App/Views/MainWindow.axaml`
   - Changed slider binding from TwoWay to OneWay
   - Added PointerPressed, PointerMoved, and PointerReleased event handlers
   - Changed IsEnabled from IsPlaying to CanSeek

3. `src/Muine.App/Views/MainWindow.axaml.cs`
   - Added `OnSliderPointerPressed`, `OnSliderPointerMoved`, and `OnSliderPointerReleased` handlers

## Testing
- All existing tests pass
- Build succeeds with no warnings or errors
- The fix follows Avalonia UI best practices for handling slider interaction
