using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Muine.Core.Models;

namespace Muine.App.ViewModels;

public partial class PlaylistViewModel : ViewModelBase
{
    private readonly Playlist _playlist;

    [ObservableProperty]
    private ObservableCollection<Song> _songs = new();

    [ObservableProperty]
    private Song? _selectedSong;

    [ObservableProperty]
    private int _currentIndex = -1;

    public int Count => Songs.Count;

    public PlaylistViewModel()
    {
        _playlist = new Playlist();
    }

    public void AddSong(Song song)
    {
        _playlist.Add(song);
        Songs.Add(song);
    }

    public void AddSongs(IEnumerable<Song> songs)
    {
        _playlist.AddRange(songs);
        foreach (var song in songs)
        {
            Songs.Add(song);
        }
    }

    [RelayCommand]
    private void RemoveSong(Song? song)
    {
        if (song != null && Songs.Contains(song))
        {
            var index = Songs.IndexOf(song);
            _playlist.RemoveAt(index);
            Songs.Remove(song);
            OnPropertyChanged(nameof(Count));
        }
    }

    [RelayCommand]
    private void ClearPlaylist()
    {
        _playlist.Clear();
        Songs.Clear();
        CurrentIndex = -1;
        OnPropertyChanged(nameof(Count));
    }

    public Song? GetNextSong()
    {
        var song = _playlist.Next();
        CurrentIndex = _playlist.CurrentIndex;
        return song;
    }

    public Song? GetPreviousSong()
    {
        var song = _playlist.Previous();
        CurrentIndex = _playlist.CurrentIndex;
        return song;
    }

    public Song? GetCurrentSong()
    {
        return _playlist.CurrentSong;
    }

    public void MoveTo(int index)
    {
        _playlist.MoveTo(index);
        CurrentIndex = _playlist.CurrentIndex;
    }

    public void MoveSong(int oldIndex, int newIndex)
    {
        _playlist.Move(oldIndex, newIndex);
        Songs.Move(oldIndex, newIndex);
        CurrentIndex = _playlist.CurrentIndex;
    }

    public bool HasNext => _playlist.HasNext;
    public bool HasPrevious => _playlist.HasPrevious;
}
