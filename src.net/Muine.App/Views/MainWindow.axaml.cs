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
    private bool _isSliderPressed = false;
    private bool _isDraggingThumb = false;
    private Thumb? _sliderThumb;
    private Slider? _positionSlider;
    private double _pressedSliderValue;
    private double _lastProgrammaticValue = 0;

    public MainWindow()
    {
        InitializeComponent();
        
        // Hook into the slider's Loaded event to find and attach to the Thumb
        this.Loaded += OnWindowLoaded;
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
                Console.WriteLine("Found slider thumb, attaching events");
                _sliderThumb.DragStarted += OnThumbDragStarted;
                _sliderThumb.DragDelta += OnThumbDragDelta;
                _sliderThumb.DragCompleted += OnThumbDragCompleted;
            }
            else
            {
                Console.WriteLine("Could not find slider thumb, using fallback approach");
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
        Console.WriteLine("OnThumbDragStarted called");
        _isSliderPressed = true;
        _isDraggingThumb = true;
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.BeginSeeking();
        }
    }

    private void OnThumbDragDelta(object? sender, VectorEventArgs e)
    {
        if (_isSliderPressed && _positionSlider != null && DataContext is MainWindowViewModel viewModel)
        {
            Console.WriteLine($"OnThumbDragDelta: Value={_positionSlider.Value}");
            viewModel.UpdateSeekPreview(_positionSlider.Value);
        }
    }

    private void OnThumbDragCompleted(object? sender, VectorEventArgs e)
    {
        Console.WriteLine("OnThumbDragCompleted called");
        if (_isSliderPressed && _positionSlider != null && DataContext is MainWindowViewModel viewModel)
        {
            _isSliderPressed = false;
            _isDraggingThumb = false;
            Console.WriteLine($"OnThumbDragCompleted: slider.Value={_positionSlider.Value}, slider.Maximum={_positionSlider.Maximum}");
            viewModel.EndSeeking(_positionSlider.Value);
        }
    }

    private void OnSliderValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (sender is not Slider slider || DataContext is not MainWindowViewModel viewModel)
            return;

        // Ignore if we're in the middle of dragging or already handling a press
        if (_isDraggingThumb || _isSliderPressed)
        {
            Console.WriteLine($"OnSliderValueChanged: Ignoring during drag/press (_isDraggingThumb={_isDraggingThumb}, _isSliderPressed={_isSliderPressed})");
            return;
        }

        // Check if this is a small change that's likely from playback updates
        var valueDiff = Math.Abs(e.NewValue - e.OldValue);
        
        // If the change is very small (< 0.5 seconds), it's likely a playback update
        // User clicks typically make larger jumps
        if (valueDiff < 0.5)
        {
            Console.WriteLine($"OnSliderValueChanged: Ignoring small change ({valueDiff}s), likely playback update");
            _lastProgrammaticValue = e.NewValue;
            return;
        }

        // This is likely a track click - significant value change without drag/press events
        // This happens when clicking directly on the slider track
        Console.WriteLine($"OnSliderValueChanged: Detected track click, oldValue={e.OldValue}, newValue={e.NewValue}, diff={valueDiff}");
        
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

    // Keep fallback pointer handlers in case Thumb events don't work
    private void OnSliderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Console.WriteLine($"OnSliderPointerPressed called (fallback)");
        if (!_isDraggingThumb && !_isSliderPressed && sender is Slider slider)
        {
            // This is a click on the track, not the thumb
            _isSliderPressed = true;
            _pressedSliderValue = slider.Value;
            Console.WriteLine($"Track click detected at value: {slider.Value}");
            
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.BeginSeeking();
            }
        }
    }

    private void OnSliderPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isSliderPressed && !_isDraggingThumb && sender is Slider slider && DataContext is MainWindowViewModel viewModel)
        {
            Console.WriteLine($"OnSliderPointerMoved: Value={slider.Value}");
            viewModel.UpdateSeekPreview(slider.Value);
        }
    }

    private void OnSliderPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        Console.WriteLine($"OnSliderPointerReleased called (fallback), _isSliderPressed={_isSliderPressed}, _isDraggingThumb={_isDraggingThumb}");
        if (_isSliderPressed && !_isDraggingThumb && sender is Slider slider && DataContext is MainWindowViewModel viewModel)
        {
            _isSliderPressed = false;
            Console.WriteLine($"OnSliderPointerReleased: slider.Value={slider.Value}, slider.Maximum={slider.Maximum}");
            viewModel.EndSeeking(slider.Value);
        }
    }

    private void OnSliderPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        Console.WriteLine($"OnSliderPointerCaptureLost called, _isSliderPressed={_isSliderPressed}, _isDraggingThumb={_isDraggingThumb}");
        if (_isSliderPressed && !_isDraggingThumb && sender is Slider slider && DataContext is MainWindowViewModel viewModel)
        {
            _isSliderPressed = false;
            Console.WriteLine($"OnSliderPointerCaptureLost: slider.Value={slider.Value}, slider.Maximum={slider.Maximum}");
            viewModel.EndSeeking(slider.Value);
        }
    }
}