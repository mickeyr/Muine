using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Muine.Core.Models;
using Muine.Core.Services;

namespace Muine.App.ViewModels;

public partial class MusicLibraryViewModel : ViewModelBase
{
    private readonly MusicDatabaseService _databaseService;

    [ObservableProperty]
    private ObservableCollection<ArtistViewModel> _artists = new();

    [ObservableProperty]
    private ObservableCollection<Song> _allSongs = new();

    [ObservableProperty]
    private ObservableCollection<Song> _filteredSongs = new();

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private Song? _selectedSong;

    [ObservableProperty]
    private bool _isGroupedView = true;

    [ObservableProperty]
    private bool _isLoading;

    public int TotalSongs => AllSongs.Count;
    public int ArtistCount => Artists.Count;

    public MusicLibraryViewModel(MusicDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task LoadLibraryAsync()
    {
        IsLoading = true;
        try
        {
            var songsGrouped = await _databaseService.GetSongsGroupedByArtistAndAlbumAsync();
            var allSongs = await _databaseService.GetAllSongsAsync();
            
            AllSongs = new ObservableCollection<Song>(allSongs);
            FilteredSongs = new ObservableCollection<Song>(allSongs);
            
            var artistViewModels = new List<ArtistViewModel>();

            foreach (var kvp in songsGrouped.OrderBy(x => x.Key))
            {
                var artistVm = new ArtistViewModel
                {
                    Name = kvp.Key,
                    Albums = new ObservableCollection<AlbumViewModel>()
                };

                foreach (var album in kvp.Value)
                {
                    var albumVm = new AlbumViewModel
                    {
                        Name = album.Name,
                        Artist = kvp.Key,
                        Year = album.Year,
                        CoverImagePath = album.CoverImagePath,
                        Songs = new ObservableCollection<Song>(album.Songs.OrderBy(s => s.DiscNumber).ThenBy(s => s.TrackNumber))
                    };

                    artistVm.Albums.Add(albumVm);
                }

                artistViewModels.Add(artistVm);
            }

            Artists = new ObservableCollection<ArtistViewModel>(artistViewModels);
            OnPropertyChanged(nameof(TotalSongs));
            OnPropertyChanged(nameof(ArtistCount));
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            FilteredSongs = new ObservableCollection<Song>(AllSongs);
        }
        else
        {
            var results = await _databaseService.SearchSongsAsync(SearchQuery);
            FilteredSongs = new ObservableCollection<Song>(results);
        }
    }

    partial void OnSearchQueryChanged(string value)
    {
        _ = SearchAsync();
    }

    [RelayCommand]
    private void ToggleView()
    {
        IsGroupedView = !IsGroupedView;
    }
}
