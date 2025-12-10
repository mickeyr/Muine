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

    private void OnSongDoubleClick(object? sender, RoutedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is Song song)
        {
            SongDoubleClicked?.Invoke(this, song);
        }
    }
}
