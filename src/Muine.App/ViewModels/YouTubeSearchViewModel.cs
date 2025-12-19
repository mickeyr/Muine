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

/// <summary>
/// Event args for YouTube songs that need metadata review
/// </summary>
public class YouTubeSongEventArgs : EventArgs
{
    public Song Song { get; }
    public string YouTubeId { get; }
    
    public YouTubeSongEventArgs(Song song, string youtubeId)
    {
        Song = song;
        YouTubeId = youtubeId;
    }
}

public partial class YouTubeSearchViewModel : ViewModelBase
{
    private readonly YouTubeService _youtubeService;
    private readonly MusicDatabaseService _databaseService;
    private readonly BackgroundTaggingQueue? _taggingQueue;
    private readonly ManagedLibraryService? _managedLibraryService;
    private readonly MetadataService? _metadataService;

    // Event fired when songs are added to the library
    public event EventHandler? SongsAddedToLibrary;
    
    // Event fired when YouTube song needs metadata review
    public event EventHandler<YouTubeSongEventArgs>? YouTubeSongNeedsMetadataReview;

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
        StatusMessage = $"Preparing to download '{SelectedSong.Title}'...";

        try
        {
            // Read metadata from the selected song to check if it needs review
            // YouTube songs typically have title but often missing artist/album
            var hasMissingMetadata = 
                string.IsNullOrWhiteSpace(SelectedSong.Artists.FirstOrDefault()) ||
                string.IsNullOrWhiteSpace(SelectedSong.Album);
            
            if (hasMissingMetadata)
            {
                // Trigger metadata review dialog FIRST, while download happens in background
                // Create a song object for the metadata review
                var songForReview = new Song
                {
                    Title = SelectedSong.Title,
                    Artists = SelectedSong.Artists.Length > 0 ? SelectedSong.Artists : new[] { "" },
                    Album = SelectedSong.Album ?? "",
                    SourceType = SongSourceType.YouTube,
                    YouTubeId = SelectedSong.YouTubeId,
                    YouTubeUrl = SelectedSong.YouTubeUrl,
                    Filename = "" // Will be set after download
                };
                
                StatusMessage = "Reviewing metadata...";
                
                // Start download in background and trigger metadata review
                YouTubeSongNeedsMetadataReview?.Invoke(this, new YouTubeSongEventArgs(songForReview, SelectedSong.YouTubeId!));
                return;
            }
            
            // If metadata is complete, proceed with normal download flow
            StatusMessage = $"Downloading '{SelectedSong.Title}'...";
            
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
                        
                        // Queue for metadata enhancement (year, cover art, etc.)
                        if (importResult.NeedsMetadataEnhancement)
                        {
                            _taggingQueue?.EnqueueSong(importResult.ImportedSong, downloadCoverArt: true);
                            LoggingService.Info($"Queued YouTube song for metadata enhancement: {importResult.ImportedSong.DisplayName}", "YouTubeSearchViewModel");
                        }
                        
                        // Notify that song was added
                        SongsAddedToLibrary?.Invoke(this, EventArgs.Empty);
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
    
    /// <summary>
    /// Complete the import after metadata has been reviewed and updated
    /// Downloads the file, applies metadata, and imports to library
    /// </summary>
    public async Task CompleteYouTubeImportAsync(Song song, string youtubeId)
    {
        if (_managedLibraryService == null || _metadataService == null)
            return;
            
        try
        {
            StatusMessage = $"Downloading '{song.Title}'...";
            
            // Download YouTube audio to temp directory
            var tempPath = await _youtubeService.DownloadToTempAsync(youtubeId);
            
            if (tempPath == null || !File.Exists(tempPath))
            {
                StatusMessage = $"Failed to download '{song.Title}'";
                IsSearching = false;
                return;
            }
            
            StatusMessage = $"Applying metadata to '{song.Title}'...";
            
            // Write the updated metadata to the downloaded file
            song.Filename = tempPath;
            _metadataService.WriteSongMetadata(tempPath, song);
            
            StatusMessage = $"Importing '{song.Title}' to library...";
            
            // Import to managed library (moves file from temp to library)
            var importResult = await _managedLibraryService.ImportFileAsync(tempPath, copyInsteadOfMove: false);
            
            if (importResult.Success && importResult.ImportedSong != null)
            {
                StatusMessage = $"Added '{song.Title}' to library";
                
                // Queue for optional metadata enhancement (year, cover art, etc.)
                if (importResult.NeedsMetadataEnhancement)
                {
                    _taggingQueue?.EnqueueSong(importResult.ImportedSong, downloadCoverArt: true);
                    LoggingService.Info($"Queued YouTube song for metadata enhancement: {importResult.ImportedSong.DisplayName}", "YouTubeSearchViewModel");
                }
                
                // Notify that song was added
                SongsAddedToLibrary?.Invoke(this, EventArgs.Empty);
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
        catch (Exception ex)
        {
            StatusMessage = $"Error completing import: {ex.Message}";
            LoggingService.Error($"Failed to complete YouTube import", ex, "YouTubeSearchViewModel");
        }
        finally
        {
            IsSearching = false;
        }
    }

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
