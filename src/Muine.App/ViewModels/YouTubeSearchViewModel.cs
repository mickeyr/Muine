using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Muine.Core.Models;
using Muine.Core.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using static Muine.Core.Services.LoggingService;

namespace Muine.App.ViewModels;

public partial class YouTubeSearchViewModel : ViewModelBase
{
    private readonly YouTubeService _youtubeService;
    private readonly MusicDatabaseService _databaseService;

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

    public YouTubeSearchViewModel(YouTubeService youtubeService, MusicDatabaseService databaseService)
    {
        _youtubeService = youtubeService;
        _databaseService = databaseService;
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
        StatusMessage = $"Adding '{SelectedSong.Title}' to library...";

        try
        {
            // Save the YouTube song to the database
            await _databaseService.SaveSongAsync(SelectedSong);
            StatusMessage = $"Added '{SelectedSong.Title}' to library";
            
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

        try
        {
            foreach (var song in SearchResults)
            {
                await _databaseService.SaveSongAsync(song);
                count++;
                StatusMessage = $"Adding to library... ({count}/{SearchResults.Count})";
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
