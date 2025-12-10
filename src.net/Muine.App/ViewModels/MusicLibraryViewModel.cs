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
    private List<AlbumViewModel> _allAlbums = new();

    [ObservableProperty]
    private ObservableCollection<ArtistViewModel> _artists = new();

    [ObservableProperty]
    private ObservableCollection<AlbumViewModel> _albums = new();

    [ObservableProperty]
    private ObservableCollection<Song> _allSongs = new();

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private ArtistViewModel? _selectedArtist;

    [ObservableProperty]
    private AlbumViewModel? _selectedAlbum;

    [ObservableProperty]
    private bool _isArtistView = true;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _currentFilteredArtist = string.Empty;

    public int TotalSongs => AllSongs.Count;
    public int ArtistCount => Artists.Count;
    public int AlbumCount => _allAlbums.Count;

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
            
            var artistViewModels = new List<ArtistViewModel>();
            var allAlbumsList = new List<AlbumViewModel>();

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
                    allAlbumsList.Add(albumVm);
                }

                // Set cover art from first album with cover
                artistVm.UpdateCoverArt();
                artistViewModels.Add(artistVm);
            }

            Artists = new ObservableCollection<ArtistViewModel>(artistViewModels);
            _allAlbums = allAlbumsList;
            Albums = new ObservableCollection<AlbumViewModel>(allAlbumsList.OrderBy(a => a.Artist).ThenBy(a => a.Name));
            
            OnPropertyChanged(nameof(TotalSongs));
            OnPropertyChanged(nameof(ArtistCount));
            OnPropertyChanged(nameof(AlbumCount));
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SelectArtist(ArtistViewModel? artist)
    {
        SelectedArtist = artist;
        IsArtistView = false;
        
        if (artist != null)
        {
            CurrentFilteredArtist = artist.Name;
            Albums = new ObservableCollection<AlbumViewModel>(
                _allAlbums.Where(a => a.Artist == artist.Name).OrderBy(a => a.Name));
        }
        else
        {
            CurrentFilteredArtist = string.Empty;
            Albums = new ObservableCollection<AlbumViewModel>(_allAlbums.OrderBy(a => a.Artist).ThenBy(a => a.Name));
        }
    }

    [RelayCommand]
    private void ShowAllAlbums()
    {
        SelectedArtist = null;
        CurrentFilteredArtist = string.Empty;
        IsArtistView = false;
        Albums = new ObservableCollection<AlbumViewModel>(_allAlbums.OrderBy(a => a.Artist).ThenBy(a => a.Name));
    }

    [RelayCommand]
    private void BackToArtists()
    {
        IsArtistView = true;
        SelectedArtist = null;
        CurrentFilteredArtist = string.Empty;
    }

    partial void OnSearchQueryChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            // Reset to current view
            if (IsArtistView)
            {
                Artists = new ObservableCollection<ArtistViewModel>(Artists);
            }
            else if (!string.IsNullOrEmpty(CurrentFilteredArtist))
            {
                Albums = new ObservableCollection<AlbumViewModel>(
                    _allAlbums.Where(a => a.Artist == CurrentFilteredArtist).OrderBy(a => a.Name));
            }
            else
            {
                Albums = new ObservableCollection<AlbumViewModel>(_allAlbums.OrderBy(a => a.Artist).ThenBy(a => a.Name));
            }
        }
        else
        {
            // Filter based on current view
            if (IsArtistView)
            {
                var filtered = Artists.Where(a => 
                    a.Name.Contains(value, StringComparison.OrdinalIgnoreCase)).ToList();
                Artists = new ObservableCollection<ArtistViewModel>(filtered);
            }
            else
            {
                var filtered = Albums.Where(a => 
                    a.Name.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                    a.Artist.Contains(value, StringComparison.OrdinalIgnoreCase)).ToList();
                Albums = new ObservableCollection<AlbumViewModel>(filtered);
            }
        }
    }
}
