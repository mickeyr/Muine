using System;
using System.Collections.Generic;
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

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly MetadataService _metadataService;
    private readonly MusicDatabaseService _databaseService;
    private readonly LibraryScannerService _scannerService;
    private readonly CoverArtService _coverArtService;
    private readonly PlaybackService _playbackService;

    [ObservableProperty]
    private string _statusMessage = "Ready - Muine Music Player";

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private string _scanProgress = string.Empty;

    [ObservableProperty]
    private int _totalSongs;

    [ObservableProperty]
    private ObservableCollection<Song> _songs = new();

    [ObservableProperty]
    private Song? _selectedSong;

    [ObservableProperty]
    private string _currentSongDisplay = "No song playing";

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private bool _isPaused;

    [ObservableProperty]
    private double _currentPosition;

    [ObservableProperty]
    private double _maxPosition = 100;

    [ObservableProperty]
    private string _timeDisplay = "0:00 / 0:00";

    [ObservableProperty]
    private float _volume = 50;

    [ObservableProperty]
    private MusicLibraryViewModel? _musicLibraryViewModel;

    [ObservableProperty]
    private PlaylistViewModel _playlistViewModel = new();

    [ObservableProperty]
    private int _selectedTabIndex = 0;

    public MainWindowViewModel()
    {
        // Initialize services
        _metadataService = new MetadataService();
        _coverArtService = new CoverArtService();
        _playbackService = new PlaybackService();
        
        var databasePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Muine",
            "music.db");
        
        var databaseDir = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(databaseDir) && !Directory.Exists(databaseDir))
        {
            Directory.CreateDirectory(databaseDir);
        }

        _databaseService = new MusicDatabaseService(databasePath);
        _scannerService = new LibraryScannerService(_metadataService, _databaseService, _coverArtService);
        
        // Initialize view models
        MusicLibraryViewModel = new MusicLibraryViewModel(_databaseService);
        PlaylistViewModel = new PlaylistViewModel();
        
        // Subscribe to playback events
        _playbackService.StateChanged += OnPlaybackStateChanged;
        _playbackService.PositionChanged += OnPlaybackPositionChanged;
        _playbackService.CurrentSongChanged += OnCurrentSongChanged;
        
        // Initialize database asynchronously
        _ = InitializeDatabaseAsync();
    }

    private async Task InitializeDatabaseAsync()
    {
        try
        {
            await _databaseService.InitializeAsync();
            await LoadSongsAsync();
            if (MusicLibraryViewModel != null)
            {
                await MusicLibraryViewModel.LoadLibraryAsync();
            }
            StatusMessage = $"Ready - {TotalSongs} songs in library";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error initializing database: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ImportMusicFolderAsync(IStorageProvider? storageProvider)
    {
        if (storageProvider == null)
        {
            StatusMessage = "Storage provider not available";
            return;
        }

        try
        {
            var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Music Folder to Import",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                var folder = folders[0];
                var folderPath = folder.Path.LocalPath;
                await ScanFolderAsync(folderPath);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error opening folder picker: {ex.Message}";
        }
    }

    private async Task ScanFolderAsync(string folderPath)
    {
        IsScanning = true;
        StatusMessage = $"Scanning {folderPath}...";
        ScanProgress = "Starting scan...";

        try
        {
            var progress = new Progress<ScanProgress>(p =>
            {
                ScanProgress = $"Processing {p.ProcessedFiles} of {p.TotalFiles} files ({p.PercentComplete:F1}%)";
                StatusMessage = $"Scanning: {Path.GetFileName(p.CurrentFile)}";
            });

            var result = await _scannerService.ScanDirectoryAsync(folderPath, progress);

            // Reload songs from database
            await LoadSongsAsync();
            if (MusicLibraryViewModel != null)
            {
                await MusicLibraryViewModel.LoadLibraryAsync();
            }

            StatusMessage = $"Scan complete: {result.SuccessCount} songs imported, {result.FailureCount} failed";
            
            if (result.Errors.Count > 0)
            {
                var firstErrors = string.Join(", ", result.Errors.Take(3));
                // TODO: Replace with proper logging framework or user notification
                Console.WriteLine($"Errors during scan: {firstErrors}");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error scanning folder: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
            ScanProgress = string.Empty;
        }
    }

    private async Task LoadSongsAsync()
    {
        try
        {
            var songs = await _databaseService.GetAllSongsAsync();
            Songs = new ObservableCollection<Song>(songs);
            TotalSongs = songs.Count;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading songs: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RefreshSelectedSongMetadataAsync()
    {
        if (SelectedSong == null)
        {
            StatusMessage = "No song selected";
            return;
        }

        await RefreshSongMetadataAsync(SelectedSong);
    }

    private async Task RefreshSongMetadataAsync(Song song)
    {
        IsScanning = true;
        StatusMessage = $"Refreshing metadata for: {song.DisplayName}";

        try
        {
            var result = await _scannerService.RefreshSongAsync(song);

            // Reload songs from database to update UI
            await LoadSongsAsync();

            if (result.SuccessCount > 0)
            {
                StatusMessage = $"Metadata refreshed for: {song.DisplayName}";
            }
            else
            {
                StatusMessage = $"Failed to refresh metadata: {song.DisplayName}";
                if (result.Errors.Count > 0)
                {
                    Console.WriteLine($"Refresh error: {result.Errors[0]}");
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error refreshing metadata: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAllMetadataAsync()
    {
        if (Songs.Count == 0)
        {
            StatusMessage = "No songs in library to refresh";
            return;
        }

        IsScanning = true;
        StatusMessage = "Refreshing metadata for all songs...";
        ScanProgress = "Starting refresh...";

        try
        {
            var progress = new Progress<ScanProgress>(p =>
            {
                ScanProgress = $"Processing {p.ProcessedFiles} of {p.TotalFiles} files ({p.PercentComplete:F1}%)";
                StatusMessage = $"Refreshing: {Path.GetFileName(p.CurrentFile)}";
            });

            var result = await _scannerService.RefreshAllSongsAsync(progress);

            // Reload songs from database
            await LoadSongsAsync();

            StatusMessage = $"Refresh complete: {result.SuccessCount} songs updated, {result.FailureCount} failed";
            
            if (result.Errors.Count > 0)
            {
                var firstErrors = string.Join(", ", result.Errors.Take(3));
                Console.WriteLine($"Errors during refresh: {firstErrors}");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error refreshing metadata: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
            ScanProgress = string.Empty;
        }
    }

    [RelayCommand]
    private async Task AddMusicFilesAsync(IStorageProvider? storageProvider)
    {
        if (storageProvider == null)
        {
            StatusMessage = "Storage provider not available";
            return;
        }

        try
        {
            var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select Music Files to Import",
                AllowMultiple = true,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Audio Files")
                    {
                        Patterns = new[] { "*.mp3", "*.ogg", "*.flac", "*.m4a", "*.aac", "*.wma", "*.wav", "*.opus" }
                    }
                }
            });

            if (files.Count > 0)
            {
                IsScanning = true;
                StatusMessage = "Importing files...";
                
                int successCount = 0;
                int failureCount = 0;

                foreach (var file in files)
                {
                    try
                    {
                        var filePath = file.Path.LocalPath;
                        var song = _metadataService.ReadSongMetadata(filePath);
                        
                        if (song != null)
                        {
                            _coverArtService.UpdateSongCoverArt(song);
                            await _databaseService.SaveSongAsync(song);
                            successCount++;
                        }
                        else
                        {
                            failureCount++;
                        }
                    }
                    catch
                    {
                        failureCount++;
                    }
                }

                await LoadSongsAsync();
                StatusMessage = $"Import complete: {successCount} songs added, {failureCount} failed";
                IsScanning = false;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error importing files: {ex.Message}";
            IsScanning = false;
        }
    }

    [RelayCommand]
    private async Task PlaySelectedSongAsync()
    {
        if (SelectedSong == null)
        {
            StatusMessage = "No song selected";
            return;
        }

        await PlaySongAsync(SelectedSong);
    }

    private async Task PlaySongAsync(Song song)
    {
        try
        {
            await _playbackService.PlayAsync(song);
            StatusMessage = $"Playing: {song.DisplayName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error playing song: {ex.Message}";
        }
    }

    [RelayCommand]
    private void TogglePlayPause()
    {
        try
        {
            if (_playbackService.CurrentSong == null && SelectedSong != null)
            {
                // If no song is playing, play the selected song
                _ = PlaySongAsync(SelectedSong);
            }
            else
            {
                _playbackService.TogglePlayPause();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Stop()
    {
        try
        {
            _playbackService.Stop();
            StatusMessage = "Playback stopped";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error stopping playback: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Seek(double position)
    {
        try
        {
            var duration = _playbackService.Duration;
            if (duration.TotalSeconds > 0)
            {
                var seekPosition = TimeSpan.FromSeconds(position);
                _playbackService.Seek(seekPosition);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error seeking: {ex.Message}";
        }
    }

    partial void OnVolumeChanged(float value)
    {
        _playbackService.Volume = value;
    }

    private void OnPlaybackStateChanged(object? sender, PlaybackState state)
    {
        IsPlaying = state == PlaybackState.Playing;
        IsPaused = state == PlaybackState.Paused;
    }

    private void OnPlaybackPositionChanged(object? sender, TimeSpan position)
    {
        var duration = _playbackService.Duration;
        
        if (duration.TotalSeconds > 0)
        {
            CurrentPosition = position.TotalSeconds;
            MaxPosition = duration.TotalSeconds;
            TimeDisplay = $"{FormatTime(position)} / {FormatTime(duration)}";
        }
        else
        {
            CurrentPosition = 0;
            MaxPosition = 100;
            TimeDisplay = "0:00 / 0:00";
        }
    }

    private void OnCurrentSongChanged(object? sender, Song? song)
    {
        if (song != null)
        {
            CurrentSongDisplay = $"{song.DisplayName} - {song.ArtistsString}";
        }
        else
        {
            CurrentSongDisplay = "No song playing";
        }
    }

    public void AddSongToPlaylist(Song song)
    {
        PlaylistViewModel.AddSong(song);
        StatusMessage = $"Added '{song.DisplayName}' to playlist";
    }

    public void AddAlbumToPlaylist(IEnumerable<Song> songs)
    {
        PlaylistViewModel.AddSongs(songs);
        StatusMessage = $"Added album to playlist";
    }

    [RelayCommand]
    private async Task PlayNextAsync()
    {
        var nextSong = PlaylistViewModel.GetNextSong();
        if (nextSong != null)
        {
            await PlaySongAsync(nextSong);
        }
        else
        {
            StatusMessage = "No more songs in playlist";
        }
    }

    [RelayCommand]
    private async Task PlayPreviousAsync()
    {
        var prevSong = PlaylistViewModel.GetPreviousSong();
        if (prevSong != null)
        {
            await PlaySongAsync(prevSong);
        }
        else
        {
            StatusMessage = "No previous song in playlist";
        }
    }

    private static string FormatTime(TimeSpan time)
    {
        if (time.TotalHours >= 1)
        {
            return time.ToString(@"h\:mm\:ss");
        }
        return time.ToString(@"m\:ss");
    }

    public void Dispose()
    {
        _playbackService?.Dispose();
        _databaseService?.Dispose();
    }
}

