using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Muine.Core.Models;
using Muine.Core.Services;

namespace Muine.App.ViewModels;

public partial class MetadataEditorViewModel : ViewModelBase
{
    private readonly MetadataService _metadataService;
    private readonly MusicDatabaseService _databaseService;
    private readonly CoverArtService _coverArtService;
    private Song? _originalSong;

    [ObservableProperty]
    private string _filename = string.Empty;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _artistsString = string.Empty;

    [ObservableProperty]
    private string _album = string.Empty;

    [ObservableProperty]
    private string _year = string.Empty;

    [ObservableProperty]
    private int _trackNumber;

    [ObservableProperty]
    private string? _coverImagePath;

    [ObservableProperty]
    private bool _hasChanges;

    public MetadataEditorViewModel(
        MetadataService metadataService,
        MusicDatabaseService databaseService,
        CoverArtService coverArtService)
    {
        _metadataService = metadataService;
        _databaseService = databaseService;
        _coverArtService = coverArtService;
    }

    public void LoadSong(Song song)
    {
        _originalSong = song;
        Filename = song.Filename;
        Title = song.Title;
        ArtistsString = string.Join(", ", song.Artists);
        Album = song.Album;
        Year = song.Year;
        TrackNumber = song.TrackNumber;
        CoverImagePath = song.CoverImagePath;
        HasChanges = false;
    }

    partial void OnTitleChanged(string value) => HasChanges = true;
    partial void OnArtistsStringChanged(string value) => HasChanges = true;
    partial void OnAlbumChanged(string value) => HasChanges = true;
    partial void OnYearChanged(string value) => HasChanges = true;
    partial void OnTrackNumberChanged(int value) => HasChanges = true;
    partial void OnCoverImagePathChanged(string? value) => HasChanges = true;

    [RelayCommand]
    private async Task SelectCoverImageAsync(IStorageProvider? storageProvider)
    {
        if (storageProvider == null) return;

        try
        {
            var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select Cover Art Image",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Image Files")
                    {
                        Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.bmp", "*.gif" }
                    }
                }
            });

            if (files.Count > 0)
            {
                CoverImagePath = files[0].Path.LocalPath;
            }
        }
        catch
        {
            // Error handled in UI
        }
    }

    [RelayCommand]
    private async Task<bool> SaveAsync()
    {
        if (_originalSong == null) return false;

        try
        {
            // Parse artists
            var artists = ArtistsString.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim())
                .ToArray();

            // Update the song object
            _originalSong.Title = Title;
            _originalSong.Artists = artists;
            _originalSong.Album = Album;
            _originalSong.Year = Year;
            _originalSong.TrackNumber = TrackNumber;
            _originalSong.CoverImagePath = CoverImagePath;

            // Save to database
            await _databaseService.SaveSongAsync(_originalSong);

            // NOTE: Currently only saves to database. 
            // Writing metadata back to audio file tags would require:
            // 1. A WriteMetadata method in MetadataService using TagLib
            // 2. Proper error handling for file write permissions
            // 3. Backup of original file tags before modification
            // This is intentionally left for future enhancement to keep changes minimal.

            HasChanges = false;
            return true;
        }
        catch
        {
            return false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        HasChanges = false;
    }
}
