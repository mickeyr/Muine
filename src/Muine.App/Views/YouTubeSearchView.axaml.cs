using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Muine.App.ViewModels;
using Muine.Core.Models;
using System;

namespace Muine.App.Views;

public partial class YouTubeSearchView : UserControl
{
    public event EventHandler<Song>? SongDoubleClicked;

    public YouTubeSearchView()
    {
        InitializeComponent();
        
        // Handle double-click on song to play
        this.AttachedToVisualTree += (s, e) =>
        {
            if (ResultsGrid != null)
            {
                ResultsGrid.DoubleTapped += (sender, args) =>
                {
                    if (DataContext is YouTubeSearchViewModel vm && vm.SelectedSong != null)
                    {
                        SongDoubleClicked?.Invoke(this, vm.SelectedSong);
                    }
                };
            }
        };
    }

    private void OnSearchBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is YouTubeSearchViewModel vm)
        {
            _ = vm.SearchCommand.ExecuteAsync(null);
        }
    }

    private void OnResultDoubleClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is YouTubeSearchViewModel vm && vm.SelectedSong != null)
        {
            SongDoubleClicked?.Invoke(this, vm.SelectedSong);
        }
    }

    private void OnPlayClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is YouTubeSearchViewModel vm && vm.SelectedSong != null)
        {
            SongDoubleClicked?.Invoke(this, vm.SelectedSong);
        }
    }

    private async void OnAddToLibraryClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is YouTubeSearchViewModel vm)
        {
            await vm.AddToLibraryCommand.ExecuteAsync(null);
        }
    }

    private void OnCopyUrlClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is YouTubeSearchViewModel vm && vm.SelectedSong != null)
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null && !string.IsNullOrEmpty(vm.SelectedSong.YouTubeUrl))
            {
                _ = clipboard.SetTextAsync(vm.SelectedSong.YouTubeUrl);
            }
        }
    }
}
