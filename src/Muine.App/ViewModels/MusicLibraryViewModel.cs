using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Muine.Core.Models;
using Muine.Core.Services;
using static Muine.Core.Services.LoggingService;

namespace Muine.App.ViewModels;

public partial class MusicLibraryViewModel : ViewModelBase
{
    private readonly MusicDatabaseService _databaseService;
    private readonly YouTubeService? _youtubeService;
    private readonly BackgroundTaggingQueue? _taggingQueue;
    private readonly ManagedLibraryService? _managedLibraryService;
    private readonly MetadataService? _metadataService;
    private List<AlbumViewModel> _allAlbums = new();

    [ObservableProperty]
    private ObservableCollection<ArtistViewModel> _artists = new();

    [ObservableProperty]
    private ObservableCollection<AlbumViewModel> _albums = new();

    [ObservableProperty]
    private ObservableCollection<Song> _songs = new();

    [ObservableProperty]
    private ObservableCollection<Song> _allSongs = new();

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private ArtistViewModel? _selectedArtist;

    [ObservableProperty]
    private AlbumViewModel? _selectedAlbum;

    [ObservableProperty]
    private Song? _selectedSong;

    [ObservableProperty]
    private bool _isArtistView = true;

    [ObservableProperty]
    private bool _isAlbumView = false;

    [ObservableProperty]
    private bool _isSongView = false;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _currentFilteredArtist = string.Empty;

    [ObservableProperty]
    private string _currentFilteredAlbum = string.Empty;

    // YouTube Panel Properties
    [ObservableProperty]
    private bool _isYouTubePanelExpanded = false;

    [ObservableProperty]
    private string _youtubeSearchQuery = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Song> _youtubeSearchResults = new();

    [ObservableProperty]
    private Song? _selectedYouTubeSong;

    [ObservableProperty]
    private bool _isYouTubeSearching;

    [ObservableProperty]
    private string _youtubeStatusMessage = string.Empty;

    [ObservableProperty]
    private int _maxYouTubeResults = 20;

    // Event fired when songs are added to the library from YouTube
    public event EventHandler? SongsAddedToLibrary;
    
    // Event fired when YouTube song needs metadata review
    public event EventHandler<YouTubeSongEventArgs>? YouTubeSongNeedsMetadataReview;

    public int TotalSongs => AllSongs.Count;
    public int ArtistCount => Artists.Count;
    public int AlbumCount => _allAlbums.Count;

    public MusicLibraryViewModel(
        MusicDatabaseService databaseService,
        YouTubeService? youtubeService = null,
        BackgroundTaggingQueue? taggingQueue = null,
        ManagedLibraryService? managedLibraryService = null,
        MetadataService? metadataService = null)
    {
        _databaseService = databaseService;
        _youtubeService = youtubeService;
        _taggingQueue = taggingQueue;
        _managedLibraryService = managedLibraryService;
        _metadataService = metadataService;
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
        IsAlbumView = true;
        IsSongView = false;
        
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
    private void SelectAlbum(AlbumViewModel? album)
    {
        SelectedAlbum = album;
        IsArtistView = false;
        IsAlbumView = false;
        IsSongView = true;
        
        if (album != null)
        {
            CurrentFilteredAlbum = album.Name;
            Songs = new ObservableCollection<Song>(album.Songs.OrderBy(s => s.DiscNumber).ThenBy(s => s.TrackNumber));
        }
        else
        {
            CurrentFilteredAlbum = string.Empty;
            Songs = new ObservableCollection<Song>();
        }
    }

    [RelayCommand]
    private void ShowAllAlbums()
    {
        SelectedArtist = null;
        CurrentFilteredArtist = string.Empty;
        IsArtistView = false;
        IsAlbumView = true;
        IsSongView = false;
        Albums = new ObservableCollection<AlbumViewModel>(_allAlbums.OrderBy(a => a.Artist).ThenBy(a => a.Name));
    }

    [RelayCommand]
    private void BackToArtists()
    {
        IsArtistView = true;
        IsAlbumView = false;
        IsSongView = false;
        SelectedArtist = null;
        SelectedAlbum = null;
        CurrentFilteredArtist = string.Empty;
        CurrentFilteredAlbum = string.Empty;
    }

    [RelayCommand]
    private void BackToAlbums()
    {
        IsArtistView = false;
        IsAlbumView = true;
        IsSongView = false;
        SelectedAlbum = null;
        CurrentFilteredAlbum = string.Empty;
        
        // Restore the previous album list (filtered or all)
        if (!string.IsNullOrEmpty(CurrentFilteredArtist))
        {
            Albums = new ObservableCollection<AlbumViewModel>(
                _allAlbums.Where(a => a.Artist == CurrentFilteredArtist).OrderBy(a => a.Name));
        }
        else
        {
            Albums = new ObservableCollection<AlbumViewModel>(_allAlbums.OrderBy(a => a.Artist).ThenBy(a => a.Name));
        }
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
            else if (IsSongView && SelectedAlbum != null)
            {
                Songs = new ObservableCollection<Song>(
                    SelectedAlbum.Songs.OrderBy(s => s.DiscNumber).ThenBy(s => s.TrackNumber));
            }
            else if (IsAlbumView && !string.IsNullOrEmpty(CurrentFilteredArtist))
            {
                Albums = new ObservableCollection<AlbumViewModel>(
                    _allAlbums.Where(a => a.Artist == CurrentFilteredArtist).OrderBy(a => a.Name));
            }
            else if (IsAlbumView)
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
            else if (IsSongView && SelectedAlbum != null)
            {
                var filtered = SelectedAlbum.Songs.Where(s =>
                    s.Title.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                    s.ArtistsString.Contains(value, StringComparison.OrdinalIgnoreCase)).ToList();
                Songs = new ObservableCollection<Song>(filtered);
            }
            else if (IsAlbumView)
            {
                var filtered = Albums.Where(a => 
                    a.Name.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                    a.Artist.Contains(value, StringComparison.OrdinalIgnoreCase)).ToList();
                Albums = new ObservableCollection<AlbumViewModel>(filtered);
            }
        }
    }

    [RelayCommand]
    private void ToggleYouTubePanel()
    {
        IsYouTubePanelExpanded = !IsYouTubePanelExpanded;
        if (!IsYouTubePanelExpanded)
        {
            // Clear search when closing panel
            YoutubeSearchQuery = string.Empty;
            YoutubeSearchResults.Clear();
            YoutubeStatusMessage = string.Empty;
        }
    }

    [RelayCommand]
    private async Task SearchYouTubeAsync()
    {
        if (_youtubeService == null)
        {
            YoutubeStatusMessage = "YouTube service is not available";
            return;
        }

        if (string.IsNullOrWhiteSpace(YoutubeSearchQuery))
        {
            YoutubeStatusMessage = "Please enter a search query";
            return;
        }

        IsYouTubeSearching = true;
        YoutubeStatusMessage = $"Searching YouTube for '{YoutubeSearchQuery}'...";

        try
        {
            YoutubeSearchResults.Clear();
            var results = await _youtubeService.SearchAsync(YoutubeSearchQuery, MaxYouTubeResults);

            foreach (var song in results)
            {
                YoutubeSearchResults.Add(song);
            }

            YoutubeStatusMessage = results.Count > 0 
                ? $"Found {results.Count} results" 
                : "No results found";
        }
        catch (Exception ex)
        {
            YoutubeStatusMessage = $"Error searching: {ex.Message}";
            LoggingService.Error($"YouTube search failed", ex, "MusicLibraryViewModel");
        }
        finally
        {
            IsYouTubeSearching = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanAddYouTubeSongToLibrary))]
    private async Task AddYouTubeSongToLibraryAsync()
    {
        if (SelectedYouTubeSong == null || _youtubeService == null)
            return;

        IsYouTubeSearching = true;
        YoutubeStatusMessage = $"Preparing to download '{SelectedYouTubeSong.Title}'...";

        try
        {
            // Check if metadata is missing - YouTube songs often have incomplete metadata
            var hasMissingMetadata = 
                string.IsNullOrWhiteSpace(SelectedYouTubeSong.Artists.FirstOrDefault()) ||
                string.IsNullOrWhiteSpace(SelectedYouTubeSong.Album);
            
            if (hasMissingMetadata)
            {
                // Trigger metadata review dialog FIRST, while download happens in background
                // Create a song object for the metadata review
                var songForReview = new Song
                {
                    Title = SelectedYouTubeSong.Title,
                    Artists = SelectedYouTubeSong.Artists.Length > 0 ? SelectedYouTubeSong.Artists : new[] { "" },
                    Album = SelectedYouTubeSong.Album ?? "",
                    SourceType = SongSourceType.YouTube,
                    YouTubeId = SelectedYouTubeSong.YouTubeId,
                    YouTubeUrl = SelectedYouTubeSong.YouTubeUrl,
                    Filename = "" // Will be set after download
                };
                
                YoutubeStatusMessage = "Reviewing metadata...";
                
                // Start download in background and trigger metadata review
                YouTubeSongNeedsMetadataReview?.Invoke(this, new YouTubeSongEventArgs(songForReview, SelectedYouTubeSong.YouTubeId!));
                return;
            }
            
            // If metadata is complete, proceed with normal download flow
            YoutubeStatusMessage = $"Downloading '{SelectedYouTubeSong.Title}'...";
            
            // Download YouTube audio to temp directory
            var tempPath = await _youtubeService.DownloadToTempAsync(SelectedYouTubeSong.YouTubeId!);
            
            if (tempPath == null || !File.Exists(tempPath))
            {
                YoutubeStatusMessage = $"Failed to download '{SelectedYouTubeSong.Title}'";
                return;
            }

            YoutubeStatusMessage = $"Processing '{SelectedYouTubeSong.Title}'...";

            // If managed library service is available, import to managed library
            if (_managedLibraryService != null && _metadataService != null)
            {
                // Read metadata from downloaded file
                var song = _metadataService.ReadSongMetadata(tempPath);
                if (song != null)
                {
                    // Preserve YouTube info
                    song.SourceType = SongSourceType.YouTube;
                    song.YouTubeId = SelectedYouTubeSong.YouTubeId;
                    song.YouTubeUrl = SelectedYouTubeSong.YouTubeUrl;
                    
                    // Import to managed library (moves file from temp to library)
                    var importResult = await _managedLibraryService.ImportFileAsync(tempPath, copyInsteadOfMove: false);
                    
                    if (importResult.Success && importResult.ImportedSong != null)
                    {
                        YoutubeStatusMessage = $"Added '{SelectedYouTubeSong.Title}' to library";
                        
                        // Queue for metadata enhancement (year, cover art, etc.)
                        if (importResult.NeedsMetadataEnhancement)
                        {
                            _taggingQueue?.EnqueueSong(importResult.ImportedSong, downloadCoverArt: true);
                            LoggingService.Info($"Queued YouTube song for metadata enhancement: {importResult.ImportedSong.DisplayName}", "MusicLibraryViewModel");
                        }
                        
                        // Notify that song was added and refresh library
                        SongsAddedToLibrary?.Invoke(this, EventArgs.Empty);
                        await LoadLibraryAsync();
                    }
                    else
                    {
                        YoutubeStatusMessage = $"Error importing: {importResult.ErrorMessage}";
                        // Clean up temp file if import failed
                        if (File.Exists(tempPath))
                        {
                            try { File.Delete(tempPath); } catch { }
                        }
                    }
                }
                else
                {
                    YoutubeStatusMessage = "Failed to read metadata from downloaded file";
                    // Clean up temp file
                    if (File.Exists(tempPath))
                    {
                        try { File.Delete(tempPath); } catch { }
                    }
                }
            }
            else
            {
                // Fallback: old behavior - save directly to database
                SelectedYouTubeSong.Filename = tempPath;
                await _databaseService.SaveSongAsync(SelectedYouTubeSong);
                
                // Queue for metadata enhancement
                _taggingQueue?.EnqueueSong(SelectedYouTubeSong, downloadCoverArt: true);
                LoggingService.Info($"Queued YouTube song for metadata enhancement: {SelectedYouTubeSong.DisplayName}", "MusicLibraryViewModel");
                
                YoutubeStatusMessage = $"Added '{SelectedYouTubeSong.Title}' to library";
                
                // Notify that library has been updated
                SongsAddedToLibrary?.Invoke(this, EventArgs.Empty);
                await LoadLibraryAsync();
            }
        }
        catch (Exception ex)
        {
            YoutubeStatusMessage = $"Error adding to library: {ex.Message}";
            LoggingService.Error($"Failed to add YouTube song to library: {SelectedYouTubeSong?.Title}", ex, "MusicLibraryViewModel");
        }
        finally
        {
            IsYouTubeSearching = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanAddAllYouTubeSongsToLibrary))]
    private async Task AddAllYouTubeSongsToLibraryAsync()
    {
        if (YoutubeSearchResults.Count == 0 || _youtubeService == null)
            return;

        IsYouTubeSearching = true;
        var count = 0;
        var total = YoutubeSearchResults.Count;

        try
        {
            foreach (var song in YoutubeSearchResults.ToList())
            {
                try
                {
                    count++;
                    YoutubeStatusMessage = $"Downloading {count}/{total}: {song.Title}...";

                    // Download YouTube audio to temp directory
                    var tempPath = await _youtubeService.DownloadToTempAsync(song.YouTubeId!);
                    
                    if (tempPath == null || !File.Exists(tempPath))
                    {
                        LoggingService.Warning($"Failed to download: {song.Title}", "MusicLibraryViewModel");
                        continue;
                    }

                    // If managed library service is available, import to managed library
                    if (_managedLibraryService != null && _metadataService != null)
                    {
                        // Read metadata from downloaded file
                        var downloadedSong = _metadataService.ReadSongMetadata(tempPath);
                        if (downloadedSong != null)
                        {
                            // Preserve YouTube info
                            downloadedSong.SourceType = SongSourceType.YouTube;
                            downloadedSong.YouTubeId = song.YouTubeId;
                            downloadedSong.YouTubeUrl = song.YouTubeUrl;
                            
                            // Import to managed library
                            var importResult = await _managedLibraryService.ImportFileAsync(tempPath, copyInsteadOfMove: false);
                            
                            if (importResult.Success && importResult.ImportedSong != null)
                            {
                                // Queue for metadata enhancement if needed
                                if (importResult.NeedsMetadataEnhancement)
                                {
                                    _taggingQueue?.EnqueueSong(importResult.ImportedSong, downloadCoverArt: true);
                                }
                            }
                            else
                            {
                                LoggingService.Warning($"Failed to import: {song.Title} - {importResult.ErrorMessage}", "MusicLibraryViewModel");
                                if (File.Exists(tempPath))
                                {
                                    try { File.Delete(tempPath); } catch { }
                                }
                            }
                        }
                        else
                        {
                            LoggingService.Warning($"Failed to read metadata: {song.Title}", "MusicLibraryViewModel");
                            if (File.Exists(tempPath))
                            {
                                try { File.Delete(tempPath); } catch { }
                            }
                        }
                    }
                    else
                    {
                        // Fallback: old behavior
                        song.Filename = tempPath;
                        await _databaseService.SaveSongAsync(song);
                        _taggingQueue?.EnqueueSong(song, downloadCoverArt: true);
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.Error($"Failed to add YouTube song: {song.Title}", ex, "MusicLibraryViewModel");
                }
            }

            YoutubeStatusMessage = $"Added {count} songs to library";
            
            // Notify that library has been updated
            SongsAddedToLibrary?.Invoke(this, EventArgs.Empty);
            await LoadLibraryAsync();
        }
        catch (Exception ex)
        {
            YoutubeStatusMessage = $"Error adding to library: {ex.Message}";
            LoggingService.Error($"Failed to add YouTube songs to library", ex, "MusicLibraryViewModel");
        }
        finally
        {
            IsYouTubeSearching = false;
        }
    }

    [RelayCommand]
    private void ClearYouTubeResults()
    {
        YoutubeSearchResults.Clear();
        SelectedYouTubeSong = null;
        YoutubeStatusMessage = "Results cleared";
    }

    private bool CanAddYouTubeSongToLibrary() => SelectedYouTubeSong != null && !IsYouTubeSearching;
    private bool CanAddAllYouTubeSongsToLibrary() => YoutubeSearchResults.Count > 0 && !IsYouTubeSearching;
    
    /// <summary>
    /// Complete the import after metadata has been reviewed and updated
    /// Downloads the file, applies metadata, and imports to library
    /// </summary>
    public async Task CompleteYouTubeImportAsync(Song song, string youtubeId)
    {
        if (_managedLibraryService == null || _metadataService == null || _youtubeService == null)
            return;
            
        try
        {
            YoutubeStatusMessage = $"Downloading '{song.Title}'...";
            
            // Download YouTube audio to temp directory
            var tempPath = await _youtubeService.DownloadToTempAsync(youtubeId);
            
            if (tempPath == null || !File.Exists(tempPath))
            {
                YoutubeStatusMessage = $"Failed to download '{song.Title}'";
                IsYouTubeSearching = false;
                return;
            }
            
            YoutubeStatusMessage = $"Applying metadata to '{song.Title}'...";
            
            // Write the updated metadata to the downloaded file
            song.Filename = tempPath;
            _metadataService.WriteSongMetadata(tempPath, song);
            
            YoutubeStatusMessage = $"Importing '{song.Title}' to library...";
            
            // Import to managed library (moves file from temp to library)
            var importResult = await _managedLibraryService.ImportFileAsync(tempPath, copyInsteadOfMove: false);
            
            if (importResult.Success && importResult.ImportedSong != null)
            {
                YoutubeStatusMessage = $"Added '{song.Title}' to library";
                
                // Queue for optional metadata enhancement (year, cover art, etc.)
                if (importResult.NeedsMetadataEnhancement)
                {
                    _taggingQueue?.EnqueueSong(importResult.ImportedSong, downloadCoverArt: true);
                    LoggingService.Info($"Queued YouTube song for metadata enhancement: {importResult.ImportedSong.DisplayName}", "MusicLibraryViewModel");
                }
                
                // Notify that song was added
                SongsAddedToLibrary?.Invoke(this, EventArgs.Empty);
                await LoadLibraryAsync();
            }
            else
            {
                YoutubeStatusMessage = $"Error importing: {importResult.ErrorMessage}";
                // Clean up temp file if import failed
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { }
                }
            }
        }
        catch (Exception ex)
        {
            YoutubeStatusMessage = $"Error completing import: {ex.Message}";
            LoggingService.Error($"Failed to complete YouTube import", ex, "MusicLibraryViewModel");
        }
        finally
        {
            IsYouTubeSearching = false;
        }
    }

    partial void OnSelectedYouTubeSongChanged(Song? value)
    {
        AddYouTubeSongToLibraryCommand.NotifyCanExecuteChanged();
    }

    partial void OnYoutubeSearchResultsChanged(ObservableCollection<Song> value)
    {
        AddAllYouTubeSongsToLibraryCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsYouTubeSearchingChanged(bool value)
    {
        AddYouTubeSongToLibraryCommand.NotifyCanExecuteChanged();
        AddAllYouTubeSongsToLibraryCommand.NotifyCanExecuteChanged();
    }
}
