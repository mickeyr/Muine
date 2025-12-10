using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Muine.App.ViewModels;
using Muine.Core.Models;

namespace Muine.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
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

    private async void OnLibraryAlbumDoubleClick(object? sender, AlbumViewModel album)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.AddAlbumToPlaylist(album.Songs);
            viewModel.SelectedTabIndex = 1; // Switch to playlist tab
            
            // Auto-start playback if not already playing
            if (!viewModel.IsPlaying)
            {
                await viewModel.PlayFromPlaylistCommand.ExecuteAsync(null);
            }
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

    private void OnSliderPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Slider.ValueProperty && sender is Slider slider && DataContext is MainWindowViewModel viewModel)
        {
            // Only seek if the user is manually changing the slider, not if it's updating from playback
            if (slider.IsPointerOver && viewModel.IsPlaying)
            {
                viewModel.SeekCommand.Execute(slider.Value);
            }
        }
    }
}