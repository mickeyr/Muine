using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Muine.Core.Models;
using Muine.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Muine.App.ViewModels;

/// <summary>
/// ViewModel for reviewing songs with missing or poor metadata
/// </summary>
public partial class MetadataReviewViewModel : ViewModelBase
{
    private readonly MusicDatabaseService _databaseService;
    private readonly MusicBrainzService _musicBrainzService;
    private readonly MetadataService _metadataService;
    private readonly ManagedLibraryService? _managedLibraryService;

    [ObservableProperty]
    private ObservableCollection<Song> _songsNeedingReview = new();

    [ObservableProperty]
    private Song? _selectedSong;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public MetadataReviewViewModel(
        MusicDatabaseService databaseService,
        MusicBrainzService musicBrainzService,
        MetadataService metadataService,
        ManagedLibraryService? managedLibraryService = null)
    {
        _databaseService = databaseService;
        _musicBrainzService = musicBrainzService;
        _metadataService = metadataService;
        _managedLibraryService = managedLibraryService;
    }

    /// <summary>
    /// Load songs with missing critical metadata from database
    /// </summary>
    public async Task LoadSongsNeedingReviewAsync()
    {
        IsLoading = true;
        StatusMessage = "Loading songs needing review...";

        try
        {
            var allSongs = await _databaseService.GetAllSongsAsync();
            
            // Filter songs with missing critical metadata
            var needingReview = allSongs.Where(song =>
                (song.Artists.Length == 0 || string.IsNullOrWhiteSpace(song.Artists[0])) ||
                string.IsNullOrWhiteSpace(song.Title) ||
                string.IsNullOrWhiteSpace(song.Album)
            ).ToList();

            SongsNeedingReview.Clear();
            foreach (var song in needingReview)
            {
                SongsNeedingReview.Add(song);
            }

            StatusMessage = $"{SongsNeedingReview.Count} songs need metadata review";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading songs: {ex.Message}";
            LoggingService.Error("Failed to load songs needing review", ex, "MetadataReviewViewModel");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Add songs to the review list (called from import completion)
    /// </summary>
    public void AddSongsForReview(IEnumerable<Song> songs)
    {
        foreach (var song in songs)
        {
            if (!SongsNeedingReview.Any(s => s.Id == song.Id))
            {
                SongsNeedingReview.Add(song);
            }
        }
    }

    /// <summary>
    /// Remove a song from the review list after metadata is fixed
    /// </summary>
    public void RemoveSongFromReview(Song song)
    {
        var toRemove = SongsNeedingReview.FirstOrDefault(s => s.Id == song.Id);
        if (toRemove != null)
        {
            SongsNeedingReview.Remove(toRemove);
        }
    }

    [RelayCommand]
    private async Task SearchMusicBrainzAsync()
    {
        if (SelectedSong == null)
            return;

        // This will be handled by opening a MusicBrainz search dialog
        // The dialog will be created separately
    }

    [RelayCommand]
    private async Task RefreshListAsync()
    {
        await LoadSongsNeedingReviewAsync();
    }
}
