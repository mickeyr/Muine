using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Muine.App.ViewModels;
using Muine.Core.Models;

namespace Muine.App.Views;

public partial class MusicLibraryView : UserControl
{
    public MusicLibraryView()
    {
        InitializeComponent();
    }

    public event EventHandler<AlbumViewModel>? AddAlbumToPlaylistRequested;
    public event EventHandler<Song>? SongDoubleClicked;
    public event EventHandler<Song>? AddSongToPlaylistRequested;
    public event EventHandler<Song>? EditMetadataRequested;

    private void OnArtistDoubleClick(object? sender, RoutedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is ArtistViewModel artist && DataContext is MusicLibraryViewModel viewModel)
        {
            // Navigate to albums view for this artist
            viewModel.SelectArtistCommand.Execute(artist);
        }
    }

    private void OnAlbumDoubleClick(object? sender, RoutedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is AlbumViewModel album && DataContext is MusicLibraryViewModel viewModel)
        {
            // Navigate to songs view for this album
            viewModel.SelectAlbumCommand.Execute(album);
        }
    }

    private void OnAddAlbumToPlaylistClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.DataContext is AlbumViewModel album)
        {
            AddAlbumToPlaylistRequested?.Invoke(this, album);
        }
    }

    private void OnSongDoubleClick(object? sender, RoutedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is Song song)
        {
            SongDoubleClicked?.Invoke(this, song);
        }
    }

    private void OnAddSongToPlaylistClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.DataContext is Song song)
        {
            AddSongToPlaylistRequested?.Invoke(this, song);
        }
    }

    private void OnEditMetadataClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.DataContext is Song song)
        {
            EditMetadataRequested?.Invoke(this, song);
        }
    }
}
