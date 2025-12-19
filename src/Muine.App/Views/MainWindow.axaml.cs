using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Muine.App.ViewModels;
using Muine.Core.Models;

namespace Muine.App.Views;

public partial class MainWindow : Window
{
    private const float DefaultVolume = 50;
    
    private bool _isDraggingThumb = false;
    private Thumb? _sliderThumb;
    private Slider? _positionSlider;
    private double _lastProgrammaticValue = 0;
    private DateTime _lastPointerSeekTime = DateTime.MinValue;
    private float _volumeBeforeMute = DefaultVolume;

    public MainWindow()
    {
        InitializeComponent();
        
        // Hook into the slider's Loaded event to find and attach to the Thumb
        this.Loaded += OnWindowLoaded;
        
        // Hook up keyboard event handler for media keys
        this.KeyDown += OnWindowKeyDown;
    }

    private void OnWindowLoaded(object? sender, RoutedEventArgs e)
    {
        // Find the slider and its thumb
        _positionSlider = this.FindControl<Slider>("PositionSlider");
        if (_positionSlider != null)
        {
            // Find the Thumb within the slider's visual tree
            _sliderThumb = FindThumbInSlider(_positionSlider);
            if (_sliderThumb != null)
            {
                _sliderThumb.DragStarted += OnThumbDragStarted;
                _sliderThumb.DragDelta += OnThumbDragDelta;
                _sliderThumb.DragCompleted += OnThumbDragCompleted;
            }
        }
    }

    private Thumb? FindThumbInSlider(Slider slider)
    {
        // Search the visual tree for a Thumb control
        return slider.GetVisualDescendants().OfType<Thumb>().FirstOrDefault();
    }

    private void OnThumbDragStarted(object? sender, VectorEventArgs e)
    {
        _isDraggingThumb = true;
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.BeginSeeking();
        }
    }

    private void OnThumbDragDelta(object? sender, VectorEventArgs e)
    {
        if (_isDraggingThumb && _positionSlider != null && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.UpdateSeekPreview(_positionSlider.Value);
        }
    }

    private void OnThumbDragCompleted(object? sender, VectorEventArgs e)
    {
        if (_isDraggingThumb && _positionSlider != null && DataContext is MainWindowViewModel viewModel)
        {
            _isDraggingThumb = false;
            
            // Set timestamp BEFORE calling EndSeeking to prevent ValueChanged from detecting this as a track click
            _lastPointerSeekTime = DateTime.Now;
            
            viewModel.EndSeeking(_positionSlider.Value);
        }
    }

    private void OnSliderValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (sender is not Slider slider || DataContext is not MainWindowViewModel viewModel)
            return;

        // Ignore if we're in the middle of dragging the thumb
        if (_isDraggingThumb)
        {
            return;
        }

        // Ignore if we just completed a pointer-based seek within the last 100ms
        // This prevents double-seeking when pointer events trigger a value change
        var timeSinceLastPointerSeek = DateTime.Now - _lastPointerSeekTime;
        if (timeSinceLastPointerSeek.TotalMilliseconds < 100)
        {
            _lastProgrammaticValue = e.NewValue;
            return;
        }

        // Check if this is a small change that's likely from playback updates
        var valueDiff = Math.Abs(e.NewValue - e.OldValue);
        
        // If the change is very small (< 0.5 seconds), it's likely a playback update
        // User clicks typically make larger jumps
        if (valueDiff < 0.5)
        {
            _lastProgrammaticValue = e.NewValue;
            return;
        }

        // This is likely a track click - significant value change without drag/press events
        // This happens when clicking directly on the slider track
        
        // Perform the seek immediately for track clicks
        _lastProgrammaticValue = e.NewValue;
        viewModel.BeginSeeking();
        viewModel.EndSeeking(e.NewValue);
    }

    private async void OnImportMusicFolderClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            try
            {
                await viewModel.ImportMusicFolderCommand.ExecuteAsync(StorageProvider);
            }
            catch
            {
                // ViewModel handles error display via StatusMessage
            }
        }
    }

    private async void OnAddMusicFilesClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            try
            {
                await viewModel.AddMusicFilesCommand.ExecuteAsync(StorageProvider);
            }
            catch
            {
                // ViewModel handles error display via StatusMessage
            }
        }
    }

    private async void OnReviewMetadataClick(object? sender, RoutedEventArgs e)
    {
        var reviewWindow = new MetadataReviewWindow
        {
            DataContext = App.CreateMetadataReviewViewModel()
        };

        if (reviewWindow.DataContext is MetadataReviewViewModel viewModel)
        {
            await viewModel.LoadSongsNeedingReviewAsync();
        }

        await reviewWindow.ShowDialog(this);
        
        // Refresh library after review dialog closes
        if (DataContext is MainWindowViewModel mainViewModel)
        {
            // Trigger a UI refresh by reloading the library view
            if (mainViewModel.MusicLibraryViewModel != null)
            {
                await mainViewModel.MusicLibraryViewModel.LoadLibraryAsync();
            }
        }
    }

    private void OnLibrarySongDoubleClick(object? sender, Song song)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.AddSongToPlaylist(song);
            viewModel.SelectedTabIndex = 1; // Switch to playlist tab
            
            // Auto-start playback if not already playing
            if (!viewModel.IsPlaying)
            {
                _ = viewModel.PlayFromPlaylistCommand.ExecuteAsync(null);
            }
        }
    }

    private async void OnLibraryAlbumDoubleClick(object? sender, AlbumViewModel album)
    {
        // No longer auto-adds - now navigates to song list in the MusicLibraryView code-behind
    }

    private void OnLibraryAddSongToPlaylistRequested(object? sender, Song song)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.AddSongToPlaylist(song);
        }
    }

    private void OnLibraryAddAlbumToPlaylistRequested(object? sender, AlbumViewModel album)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.AddAlbumToPlaylist(album.Songs);
        }
    }

    private async void OnEditMetadataRequested(object? sender, Song song)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            var editorVm = viewModel.CreateMetadataEditor(song);
            var editorWindow = new MetadataEditorWindow
            {
                DataContext = editorVm
            };

            var result = await editorWindow.ShowDialog<bool?>(this);
            
            if (result == true)
            {
                await viewModel.RefreshAfterMetadataEdit();
                viewModel.StatusMessage = "Metadata updated successfully";
            }
        }
    }

    private async void OnPlaylistSongDoubleClick(object? sender, Song song)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            try
            {
                // Find the index of the song in the playlist and play it
                var index = viewModel.PlaylistViewModel.Songs.IndexOf(song);
                if (index >= 0)
                {
                    viewModel.PlaylistViewModel.MoveTo(index);
                    await viewModel.PlayFromPlaylistCommand.ExecuteAsync(null);
                }
            }
            catch
            {
                // ViewModel handles error display via StatusMessage
            }
        }
    }

    private async void OnRefreshSelectedSongClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            try
            {
                await viewModel.RefreshSelectedSongMetadataCommand.ExecuteAsync(null);
            }
            catch
            {
                // ViewModel handles error display via StatusMessage
            }
        }
    }

    private async void OnRefreshAllMetadataClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            try
            {
                await viewModel.RefreshAllMetadataCommand.ExecuteAsync(null);
            }
            catch
            {
                // ViewModel handles error display via StatusMessage
            }
        }
    }

    private async void OnRadioStationDoubleClick(object? sender, RadioStation station)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            try
            {
                await viewModel.PlayRadioStationAsync(station);
            }
            catch
            {
                // ViewModel handles error display via StatusMessage
            }
        }
    }

    private async void OnAddRadioStationRequested(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            var editorVm = viewModel.CreateRadioStationEditor();
            var editorWindow = new AddRadioStationWindow
            {
                DataContext = editorVm
            };

            await editorWindow.ShowDialog(this);
            
            if (editorWindow.WasSaved)
            {
                await viewModel.RefreshRadioStationsAsync();
            }
        }
    }

    private async void OnEditRadioStationRequested(object? sender, RadioStation station)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            var editorVm = viewModel.CreateRadioStationEditor(station);
            var editorWindow = new AddRadioStationWindow
            {
                DataContext = editorVm
            };

            await editorWindow.ShowDialog(this);
            
            if (editorWindow.WasSaved)
            {
                await viewModel.RefreshRadioStationsAsync();
            }
        }
    }

    private async void OnRadioRefreshRequested(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.RefreshRadioStationsAsync();
        }
    }

    private async void OnYouTubeSongDoubleClick(object? sender, Song song)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            try
            {
                await viewModel.PlayYouTubeSongAsync(song);
            }
            catch
            {
                // ViewModel handles error display via StatusMessage
            }
        }
    }

    private async void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
            return;

        try
        {
            // Handle media keys
            switch (e.Key)
            {
                case Key.MediaPlayPause:
                    if (viewModel.TogglePlayPauseCommand?.CanExecute(null) == true)
                    {
                        viewModel.TogglePlayPauseCommand.Execute(null);
                        e.Handled = true;
                    }
                    break;

                case Key.MediaStop:
                    if (viewModel.StopCommand?.CanExecute(null) == true)
                    {
                        viewModel.StopCommand.Execute(null);
                        e.Handled = true;
                    }
                    break;

                case Key.MediaNextTrack:
                    if (viewModel.PlayNextCommand?.CanExecute(null) == true)
                    {
                        await viewModel.PlayNextCommand.ExecuteAsync(null);
                        e.Handled = true;
                    }
                    break;

                case Key.MediaPreviousTrack:
                    if (viewModel.PlayPreviousCommand?.CanExecute(null) == true)
                    {
                        await viewModel.PlayPreviousCommand.ExecuteAsync(null);
                        e.Handled = true;
                    }
                    break;

                case Key.VolumeUp:
                    // Increase volume by 5%
                    viewModel.Volume = Math.Min(100, viewModel.Volume + 5);
                    e.Handled = true;
                    break;

                case Key.VolumeDown:
                    // Decrease volume by 5%
                    viewModel.Volume = Math.Max(0, viewModel.Volume - 5);
                    e.Handled = true;
                    break;

                case Key.VolumeMute:
                    // Toggle mute by storing/restoring volume
                    if (viewModel.Volume > 0)
                    {
                        _volumeBeforeMute = viewModel.Volume;
                        viewModel.Volume = 0;
                    }
                    else
                    {
                        viewModel.Volume = _volumeBeforeMute > 0 ? _volumeBeforeMute : DefaultVolume;
                    }
                    e.Handled = true;
                    break;
            }
        }
        catch (Exception ex)
        {
            // Log error but don't crash the application
            System.Diagnostics.Debug.WriteLine($"Error handling media key: {ex.Message}");
        }
    }

    // Keep fallback pointer handlers in case Thumb events don't work

}