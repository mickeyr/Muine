using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Muine.Core.Models;
using Muine.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Muine.Core.Services.LoggingService;

namespace Muine.App.ViewModels;

public partial class YouTubeSearchViewModel : ViewModelBase
{
    private readonly YouTubeService _youtubeService;
    private readonly MusicDatabaseService _databaseService;
    private readonly BackgroundTaggingQueue? _taggingQueue;
    private readonly ManagedLibraryService? _managedLibraryService;
    private readonly MetadataService? _metadataService;

    // Event fired when songs are added to the library
    public event EventHandler? SongsAddedToLibrary;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Song> _searchResults = new();

    [ObservableProperty]
    private Song? _selectedSong;

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private int _maxResults = 20;

    public YouTubeSearchViewModel(
        YouTubeService youtubeService, 
        MusicDatabaseService databaseService, 
        BackgroundTaggingQueue? taggingQueue = null,
        ManagedLibraryService? managedLibraryService = null,
        MetadataService? metadataService = null)
    {
        _youtubeService = youtubeService;
        _databaseService = databaseService;
        _taggingQueue = taggingQueue;
        _managedLibraryService = managedLibraryService;
        _metadataService = metadataService;
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            StatusMessage = "Please enter a search query";
            return;
        }

        IsSearching = true;
        StatusMessage = $"Searching YouTube for '{SearchQuery}'...";

        try
        {
            SearchResults.Clear();
            var results = await _youtubeService.SearchAsync(SearchQuery, MaxResults);

            foreach (var song in results)
            {
                SearchResults.Add(song);
            }

            StatusMessage = results.Count > 0 
                ? $"Found {results.Count} results" 
                : "No results found";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error searching: {ex.Message}";
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanAddToLibrary))]
    private async Task AddToLibraryAsync()
    {
        if (SelectedSong == null)
            return;

        IsSearching = true;
        StatusMessage = $"Downloading '{SelectedSong.Title}'...";

        try
        {
            // Download YouTube audio to temp directory
            var tempPath = await _youtubeService.DownloadToTempAsync(SelectedSong.YouTubeId!);
            
            if (tempPath == null || !File.Exists(tempPath))
            {
                StatusMessage = $"Failed to download '{SelectedSong.Title}'";
                return;
            }

            StatusMessage = $"Processing '{SelectedSong.Title}'...";

            // If managed library service is available, import to managed library
            if (_managedLibraryService != null && _metadataService != null)
            {
                // Read metadata from downloaded file
                var song = _metadataService.ReadSongMetadata(tempPath);
                if (song != null)
                {
                    // Preserve YouTube info
                    song.SourceType = SongSourceType.YouTube;
                    song.YouTubeId = SelectedSong.YouTubeId;
                    song.YouTubeUrl = SelectedSong.YouTubeUrl;
                    
                    // Import to managed library (moves file from temp to library)
                    var importResult = await _managedLibraryService.ImportFileAsync(tempPath, copyInsteadOfMove: false);
                    
                    if (importResult.Success && importResult.ImportedSong != null)
                    {
                        StatusMessage = $"Added '{SelectedSong.Title}' to library";
                        
                        // Queue for metadata enhancement
                        if (importResult.NeedsMetadataEnhancement)
                        {
                            _taggingQueue?.EnqueueSong(importResult.ImportedSong, downloadCoverArt: true);
                            LoggingService.Info($"Queued YouTube song for metadata enhancement: {importResult.ImportedSong.DisplayName}", "YouTubeSearchViewModel");
                        }
                    }
                    else
                    {
                        StatusMessage = $"Error importing: {importResult.ErrorMessage}";
                        // Clean up temp file if import failed
                        if (File.Exists(tempPath))
                        {
                            try { File.Delete(tempPath); } catch { }
                        }
                    }
                }
                else
                {
                    StatusMessage = "Failed to read metadata from downloaded file";
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
                // Update song with temp file path
                SelectedSong.Filename = tempPath;
                await _databaseService.SaveSongAsync(SelectedSong);
                
                // Queue for metadata enhancement
                _taggingQueue?.EnqueueSong(SelectedSong, downloadCoverArt: true);
                LoggingService.Info($"Queued YouTube song for metadata enhancement: {SelectedSong.DisplayName}", "YouTubeSearchViewModel");
                
                StatusMessage = $"Added '{SelectedSong.Title}' to library";
            }
            
            // Notify that library has been updated
            SongsAddedToLibrary?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error adding to library: {ex.Message}";
            LoggingService.Error($"Failed to add YouTube song to library: {SelectedSong?.Title}", ex, "YouTubeSearch");
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanAddAllToLibrary))]
    private async Task AddAllToLibraryAsync()
    {
        if (SearchResults.Count == 0)
            return;

        IsSearching = true;
        var count = 0;
        var total = SearchResults.Count;

        try
        {
            foreach (var song in SearchResults.ToList()) // ToList to avoid collection modification issues
            {
                try
                {
                    count++;
                    StatusMessage = $"Downloading {count}/{total}: {song.Title}...";

                    // Download YouTube audio to temp directory
                    var tempPath = await _youtubeService.DownloadToTempAsync(song.YouTubeId!);
                    
                    if (tempPath == null || !File.Exists(tempPath))
                    {
                        LoggingService.Warning($"Failed to download: {song.Title}", "YouTubeSearchViewModel");
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
                                LoggingService.Warning($"Failed to import: {song.Title} - {importResult.ErrorMessage}", "YouTubeSearchViewModel");
                                // Clean up temp file
                                if (File.Exists(tempPath))
                                {
                                    try { File.Delete(tempPath); } catch { }
                                }
                            }
                        }
                        else
                        {
                            LoggingService.Warning($"Failed to read metadata: {song.Title}", "YouTubeSearchViewModel");
                            // Clean up temp file
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
                    LoggingService.Error($"Failed to add YouTube song: {song.Title}", ex, "YouTubeSearchViewModel");
                }
            }

            StatusMessage = $"Added {count} songs to library";
            
            // Notify that library has been updated
            SongsAddedToLibrary?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error adding to library: {ex.Message}";
            LoggingService.Error($"Failed to add YouTube songs to library", ex, "YouTubeSearch");
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    private void ClearResults()
    {
        SearchResults.Clear();
        SelectedSong = null;
        StatusMessage = "Results cleared";
    }

    private bool CanAddToLibrary() => SelectedSong != null && !IsSearching;
    private bool CanAddAllToLibrary() => SearchResults.Count > 0 && !IsSearching;

    partial void OnSelectedSongChanged(Song? value)
    {
        AddToLibraryCommand.NotifyCanExecuteChanged();
    }

    partial void OnSearchResultsChanged(ObservableCollection<Song> value)
    {
        AddAllToLibraryCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsSearchingChanged(bool value)
    {
        AddToLibraryCommand.NotifyCanExecuteChanged();
        AddAllToLibraryCommand.NotifyCanExecuteChanged();
    }
}
