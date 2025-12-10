using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Muine.Core.Models;

namespace Muine.App.Views;

public partial class MusicLibraryView : UserControl
{
    public MusicLibraryView()
    {
        InitializeComponent();
    }

    public event EventHandler<Song>? SongDoubleClicked;
    public event EventHandler<Song>? AddToPlaylistRequested;
    public event EventHandler<Song>? EditMetadataRequested;

    private void OnSongDoubleClick(object? sender, RoutedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is Song song)
        {
            SongDoubleClicked?.Invoke(this, song);
        }
    }

    private void OnAddToPlaylistClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.DataContext is Song song)
        {
            AddToPlaylistRequested?.Invoke(this, song);
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
