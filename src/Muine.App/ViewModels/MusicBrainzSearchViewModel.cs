using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Muine.Core.Models;
using Muine.Core.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Muine.App.ViewModels;

/// <summary>
/// ViewModel for MusicBrainz search dialog
/// </summary>
public partial class MusicBrainzSearchViewModel : ViewModelBase
{
    private readonly MusicBrainzService _musicBrainzService;
    private readonly MetadataService _metadataService;
    private readonly MusicDatabaseService _databaseService;
    private readonly ManagedLibraryService? _managedLibraryService;

    [ObservableProperty]
    private Song _song = null!;

    [ObservableProperty]
    private string _searchArtist = string.Empty;

    [ObservableProperty]
    private string _searchTitle = string.Empty;

    [ObservableProperty]
    private string _searchAlbum = string.Empty;

    [ObservableProperty]
    private ObservableCollection<MusicBrainzMatch> _searchResults = new();

    [ObservableProperty]
    private MusicBrainzMatch? _selectedMatch;

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _dialogResult;

    public MusicBrainzSearchViewModel(
        MusicBrainzService musicBrainzService,
        MetadataService metadataService,
        MusicDatabaseService databaseService,
        ManagedLibraryService? managedLibraryService = null)
    {
        _musicBrainzService = musicBrainzService;
        _metadataService = metadataService;
        _databaseService = databaseService;
        _managedLibraryService = managedLibraryService;
    }

    /// <summary>
    /// Initialize with song - pre-populate search fields with existing tags
    /// </summary>
    public void Initialize(Song song)
    {
        Song = song;
        
        // Pre-populate search fields with existing metadata
        SearchArtist = song.Artists.Length > 0 ? song.Artists[0] : string.Empty;
        SearchTitle = song.Title ?? string.Empty;
        SearchAlbum = song.Album ?? string.Empty;
        
        // If we have enough info, do an initial search
        if (!string.IsNullOrWhiteSpace(SearchArtist) && !string.IsNullOrWhiteSpace(SearchTitle))
        {
            _ = SearchAsync();
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchArtist) || string.IsNullOrWhiteSpace(SearchTitle))
        {
            StatusMessage = "Artist and title are required for search";
            return;
        }

        IsSearching = true;
        StatusMessage = $"Searching for {SearchArtist} - {SearchTitle}...";
        SearchResults.Clear();

        try
        {
            var results = await _musicBrainzService.SearchRecordingsAsync(SearchArtist, SearchTitle, maxResults: 20);
            
            foreach (var result in results)
            {
                SearchResults.Add(result);
            }

            StatusMessage = $"Found {SearchResults.Count} results";
            
            if (SearchResults.Count == 0)
            {
                StatusMessage = "No results found. Try modifying your search terms.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Search error: {ex.Message}";
            LoggingService.Error($"MusicBrainz search failed", ex, "MusicBrainzSearchViewModel");
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    private async Task ApplySelectedMatchAsync()
    {
        if (SelectedMatch == null)
        {
            StatusMessage = "Please select a match to apply";
            return;
        }

        try
        {
            // Update song with new metadata
            Song.Artists = new[] { SelectedMatch.Artist };
            Song.Title = SelectedMatch.Title;
            Song.Album = SelectedMatch.Album ?? Song.Album;
            
            if (SelectedMatch.Year.HasValue)
            {
                Song.Year = SelectedMatch.Year.Value.ToString();
            }

            // Write updated metadata to file
            _metadataService.WriteSongMetadata(Song.Filename, Song);

            // Reorganize file if managed library is available and file is already in library
            // For YouTube temp files, skip reorganization here - it will be done during import
            var isInLibrary = !Song.Filename.Contains("/tmp/") && !Song.Filename.Contains("\\Temp\\");
            
            if (_managedLibraryService != null && isInLibrary)
            {
                var success = await _managedLibraryService.ReorganizeSongAsync(Song);
                if (success)
                {
                    LoggingService.Info($"Reorganized file after metadata update: {Song.Filename}", "MusicBrainzSearchViewModel");
                }
            }

            // Save to database only if song already has an ID (existing song)
            if (Song.Id > 0)
            {
                await _databaseService.SaveSongAsync(Song);
            }

            DialogResult = true;
            StatusMessage = "Metadata applied successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error applying metadata: {ex.Message}";
            LoggingService.Error($"Failed to apply metadata", ex, "MusicBrainzSearchViewModel");
        }
    }

    [RelayCommand]
    private void Skip()
    {
        DialogResult = false;
        StatusMessage = "Skipped";
    }
}
