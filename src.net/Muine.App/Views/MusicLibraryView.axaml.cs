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

    public event EventHandler<ArtistViewModel>? ArtistDoubleClicked;
    public event EventHandler<AlbumViewModel>? AlbumDoubleClicked;
    public event EventHandler<AlbumViewModel>? AddAlbumToPlaylistRequested;

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
        if (sender is ListBox listBox && listBox.SelectedItem is AlbumViewModel album)
        {
            AlbumDoubleClicked?.Invoke(this, album);
        }
    }

    private void OnAddAlbumToPlaylistClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.DataContext is AlbumViewModel album)
        {
            AddAlbumToPlaylistRequested?.Invoke(this, album);
        }
    }
}
