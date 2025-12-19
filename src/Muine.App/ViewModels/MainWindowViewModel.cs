using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
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
    private readonly RadioStationService _radioStationService;
    private readonly RadioMetadataService _radioMetadataService;
    private readonly RadioBrowserService? _radioBrowserService;
    private readonly YouTubeService _youtubeService;
    private readonly MprisService? _mprisService;
    private readonly BackgroundTaggingQueue _taggingQueue;
    private readonly DebouncedActionService _debouncer;

    [ObservableProperty]
    private string _statusMessage = "Ready - Muine Music Player";

    [ObservableProperty]
    private string _operationStatus = string.Empty;

    [ObservableProperty]
    private bool _hasOperationStatus;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private string _scanProgress = string.Empty;

    [ObservableProperty]
    private double _scanProgressPercentage;

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
    [NotifyPropertyChangedFor(nameof(CanSeek))]
    private double _currentPosition;

    [ObservableProperty]
    private double _maxPosition = 100;

    [ObservableProperty]
    private string _timeDisplay = "0:00 / 0:00";

    [ObservableProperty]
    private float _volume = 50;

    public bool CanSeek => _playbackService.CurrentSong != null;

    [ObservableProperty]
    private MusicLibraryViewModel? _musicLibraryViewModel;

    [ObservableProperty]
    private PlaylistViewModel _playlistViewModel = new();

    [ObservableProperty]
    private RadioViewModel? _radioViewModel;

    [ObservableProperty]
    private YouTubeSearchViewModel? _youTubeSearchViewModel;

    [ObservableProperty]
    private int _selectedTabIndex = 0;

    private System.Diagnostics.Stopwatch? _operationStopwatch;
    private bool _isUserSeeking;

    public MainWindowViewModel()
    {
        // Initialize services
        _metadataService = new MetadataService();
        _coverArtService = new CoverArtService();
        _playbackService = new PlaybackService();
        _radioMetadataService = new RadioMetadataService();
        _youtubeService = new YouTubeService();
        _debouncer = new DebouncedActionService();
        
        // Initialize RadioBrowserService with error handling
        // The RadioBrowser library requires DNS resolution which may fail in some environments
        try
        {
            _radioBrowserService = new RadioBrowserService();
        }
        catch (Exception ex)
        {
            // Log the error but continue - radio browser search won't work but the app can still function
            System.Diagnostics.Debug.WriteLine($"[RadioBrowser] Failed to initialize: {ex.Message}");
            // Create a null reference - we'll need to check for null before using
            _radioBrowserService = null!;
        }
        
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
        _radioStationService = new RadioStationService(databasePath);
        
        // Initialize library configuration
        var libraryConfigService = new LibraryConfigurationService();
        var libraryConfig = libraryConfigService.LoadConfiguration();
        libraryConfig.EnsureLibraryDirectoryExists();
        
        // Initialize managed library service
        var managedLibraryService = new ManagedLibraryService(libraryConfig, _metadataService, _databaseService);
        
        // Initialize metadata enhancement services
        var mbService = new MusicBrainzService();
        var enhancementService = new MetadataEnhancementService(mbService, _metadataService);
        _taggingQueue = new BackgroundTaggingQueue(enhancementService);
        
        // Subscribe to tagging queue events
        _taggingQueue.WorkCompleted += OnTaggingWorkCompleted;
        _taggingQueue.WorkFailed += OnTaggingWorkFailed;
        
        _scannerService = new LibraryScannerService(_metadataService, _databaseService, _coverArtService, _taggingQueue, managedLibraryService);
        
        // Initialize MPRIS service (Linux media key support)
        _mprisService = new MprisService(_playbackService);
        _mprisService.NextRequested += (s, e) => _ = PlayNextCommand.ExecuteAsync(null);
        _mprisService.PreviousRequested += (s, e) => _ = PlayPreviousCommand.ExecuteAsync(null);
        
        // Initialize view models
        MusicLibraryViewModel = new MusicLibraryViewModel(_databaseService);
        PlaylistViewModel = new PlaylistViewModel();
        RadioViewModel = new RadioViewModel(_radioStationService, _radioMetadataService, _radioBrowserService);
        YouTubeSearchViewModel = new YouTubeSearchViewModel(_youtubeService, _databaseService, _taggingQueue, managedLibraryService, _metadataService);
        
        // Subscribe to YouTube events
        YouTubeSearchViewModel.SongsAddedToLibrary += OnYouTubeSongsAddedToLibrary;
        YouTubeSearchViewModel.YouTubeSongNeedsMetadataReview += OnYouTubeSongNeedsMetadataReview;
        
        // Subscribe to playback events
        _playbackService.StateChanged += OnPlaybackStateChanged;
        _playbackService.PositionChanged += OnPlaybackPositionChanged;
        _playbackService.CurrentSongChanged += OnCurrentSongChanged;
        _playbackService.CurrentRadioStationChanged += OnCurrentRadioStationChanged;
        
        // Initialize database asynchronously
        _ = InitializeDatabaseAsync();
    }

    private async Task InitializeDatabaseAsync()
    {
        try
        {
            await _databaseService.InitializeAsync();
            await _radioStationService.InitializeAsync();
            
            // Initialize MPRIS service (Linux media key support)
            if (_mprisService != null)
            {
                await _mprisService.InitializeAsync();
            }
            
            await LoadSongsAsync();
            if (MusicLibraryViewModel != null)
            {
                await MusicLibraryViewModel.LoadLibraryAsync();
            }
            if (RadioViewModel != null)
            {
                await RadioViewModel.LoadStationsAsync();
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
        SetOperationStatus($"Importing {folderPath}...");
        ScanProgress = "Starting import...";

        try
        {
            int lastRefreshCount = 0;
            const int refreshInterval = 10; // Refresh UI every 10 files
            
            var progress = new Progress<ScanProgress>(async p =>
            {
                ScanProgress = $"Processing {p.ProcessedFiles} of {p.TotalFiles} files ({p.PercentComplete:F1}%)";
                ScanProgressPercentage = p.PercentComplete;
                SetOperationStatus($"Importing: {Path.GetFileName(p.CurrentFile)} ({p.PercentComplete:F1}%)");
                
                // Refresh UI periodically during import for immediate feedback
                if (p.ProcessedFiles - lastRefreshCount >= refreshInterval)
                {
                    lastRefreshCount = p.ProcessedFiles;
                    await Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        try
                        {
                            await LoadSongsAsync();
                            if (MusicLibraryViewModel != null)
                            {
                                await MusicLibraryViewModel.LoadLibraryAsync();
                            }
                        }
                        catch
                        {
                            // Ignore errors during intermediate refreshes
                        }
                    });
                }
            });

            // Run the import in a background thread to avoid blocking the UI
            // This will move/copy files to the managed library and organize them
            var result = await Task.Run(() => _scannerService.ImportDirectoryAsync(
                folderPath, 
                copyInsteadOfMove: false, // Use default (move) - TODO: make this configurable in UI
                progress, 
                autoEnhanceMetadata: true,
                skipDuplicateCheck: true)); // Skip expensive duplicate checking for faster imports

            // Final reload after import completes
            await LoadSongsAsync();
            if (MusicLibraryViewModel != null)
            {
                await MusicLibraryViewModel.LoadLibraryAsync();
            }

            // Build status message
            var statusParts = new List<string>();
            statusParts.Add($"{result.SuccessCount} songs imported");
            
            if (result.FailureCount > 0)
                statusParts.Add($"{result.FailureCount} failed");
            if (result.Duplicates.Count > 0)
                statusParts.Add($"{result.Duplicates.Count} duplicates skipped");
            if (result.FilesNeedingManualMetadata.Count > 0)
                statusParts.Add($"{result.FilesNeedingManualMetadata.Count} need manual metadata");
            if (result.Conflicts.Count > 0)
                statusParts.Add($"{result.Conflicts.Count} conflicts");
            if (result.SongsWithMissingCriticalMetadata.Count > 0)
                statusParts.Add($"{result.SongsWithMissingCriticalMetadata.Count} need metadata review");
            
            SetOperationStatus($"Import complete: {string.Join(", ", statusParts)}", autoHideAfter: 5000);
            
            // Store songs needing review for later access
            if (result.SongsWithMissingCriticalMetadata.Count > 0)
            {
                // TODO: Show notification or dialog to review these songs
                LoggingService.Info($"{result.SongsWithMissingCriticalMetadata.Count} songs need metadata review", "MainWindowViewModel");
            }
            
            // Log any errors
            foreach (var error in result.Errors)
            {
                LoggingService.Warning(error, "MainWindowViewModel");
            }
        }
        catch (Exception ex)
        {
            SetOperationStatus($"Error importing folder: {ex.Message}", autoHideAfter: 5000);
            LoggingService.Error($"Failed to import folder: {folderPath}", ex, "MainWindowViewModel");
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
        SetOperationStatus($"Refreshing metadata for: {song.DisplayName}");

        try
        {
            // Run the refresh in a background thread to avoid blocking the UI
            var result = await Task.Run(() => _scannerService.RefreshSongAsync(song));

            // Reload songs from database to update UI
            await LoadSongsAsync();
            if (MusicLibraryViewModel != null)
            {
                await MusicLibraryViewModel.LoadLibraryAsync();
            }

            if (result.SuccessCount > 0)
            {
                SetOperationStatus($"Metadata refreshed for: {song.DisplayName}", autoHideAfter: 3000);
            }
            else
            {
                SetOperationStatus($"Failed to refresh metadata: {song.DisplayName}", autoHideAfter: 5000);
                // Errors are already tracked in result.Errors for logging if needed
            }
        }
        catch (Exception ex)
        {
            SetOperationStatus($"Error refreshing metadata: {ex.Message}", autoHideAfter: 5000);
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
            SetOperationStatus("No songs in library to refresh", autoHideAfter: 3000);
            return;
        }

        IsScanning = true;
        _operationStopwatch = System.Diagnostics.Stopwatch.StartNew();
        SetOperationStatus("Refreshing metadata...");
        ScanProgress = "Starting refresh...";

        try
        {
            var progress = new Progress<ScanProgress>(p =>
            {
                ScanProgress = $"Processing {p.ProcessedFiles} of {p.TotalFiles} files ({p.PercentComplete:F1}%)";
                ScanProgressPercentage = p.PercentComplete;
                SetOperationStatus($"Refreshing: {p.PercentComplete:F0}%");
            });

            // Run the metadata refresh in a background thread to avoid blocking the UI
            var result = await Task.Run(() => _scannerService.RefreshAllSongsAsync(progress));

            // Reload songs from database
            await LoadSongsAsync();
            if (MusicLibraryViewModel != null)
            {
                await MusicLibraryViewModel.LoadLibraryAsync();
            }

            _operationStopwatch.Stop();
            var elapsed = _operationStopwatch.Elapsed;
            var timeString = elapsed.TotalMinutes >= 1 
                ? $"{elapsed.Minutes}m {elapsed.Seconds}s"
                : $"{elapsed.TotalSeconds:F1}s";
            
            SetOperationStatus($"Refresh complete in {timeString}", autoHideAfter: 3000);
            
            // Errors are already tracked in result.Errors for logging if needed
        }
        catch (Exception ex)
        {
            SetOperationStatus($"Error: {ex.Message}", autoHideAfter: 5000);
        }
        finally
        {
            IsScanning = false;
            ScanProgress = string.Empty;
            _operationStopwatch = null;
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
                var importedSongs = new List<Song>();

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
                            importedSongs.Add(song);
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

                // Queue imported songs for metadata enhancement
                if (importedSongs.Count > 0)
                {
                    _taggingQueue.EnqueueSongs(importedSongs, downloadCoverArt: true);
                    LoggingService.Info($"Queued {importedSongs.Count} songs for metadata enhancement", "MainWindowViewModel");
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
            
            // Queue for metadata enhancement if it appears to need it
            // This handles songs already in the library that haven't been enhanced
            if (ShouldEnhanceMetadata(song))
            {
                _taggingQueue.EnqueueSong(song, downloadCoverArt: true);
                LoggingService.Info($"Queued existing song for metadata enhancement: {song.DisplayName}", "MainWindowViewModel");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error playing song: {ex.Message}";
        }
    }
    
    /// <summary>
    /// Determine if a song should be queued for metadata enhancement
    /// </summary>
    private bool ShouldEnhanceMetadata(Song song)
    {
        // Skip if it's a radio station or not a trackable song
        if (string.IsNullOrEmpty(song.Title))
            return false;
        
        // YouTube songs with "Unknown Artist" definitely need enhancement
        if (song.IsYouTube && (song.Artists.Length == 0 || song.Artists[0] == "Unknown Artist"))
            return true;
        
        // YouTube songs missing album/year info could benefit from enhancement
        if (song.IsYouTube && string.IsNullOrEmpty(song.Album))
            return true;
        
        // Local files with "Unknown Artist" or missing basic metadata
        if (song.IsLocal && (song.Artists.Length == 0 || song.Artists[0] == "Unknown Artist"))
            return true;
        
        return false;
    }

    [RelayCommand]
    private void TogglePlayPause()
    {
        try
        {
            if (_playbackService.CurrentSong == null || _playbackService.State == PlaybackState.Stopped)
            {
                // If no song is playing or playback is stopped, play from playlist
                _ = PlayFromPlaylistAsync();
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
        
        // Always update MaxPosition and Duration even during seeking, as we need this for the seek operation
        if (duration.TotalSeconds > 0)
        {
            MaxPosition = duration.TotalSeconds;
            
            // Only update CurrentPosition if not seeking (to avoid fighting with user interaction)
            if (!_isUserSeeking)
            {
                CurrentPosition = position.TotalSeconds;
                TimeDisplay = $"{FormatTime(position)} / {FormatTime(duration)}";
            }
        }
        else
        {
            MaxPosition = 100;
            
            if (!_isUserSeeking)
            {
                CurrentPosition = 0;
                TimeDisplay = "0:00 / 0:00";
            }
        }
    }

    private void OnCurrentSongChanged(object? sender, Song? song)
    {
        if (song != null)
        {
            CurrentSongDisplay = $"{song.DisplayName} - {song.ArtistsString}";
        }
        else if (_playbackService.CurrentRadioStation == null)
        {
            CurrentSongDisplay = "No song playing";
        }
        
        // Notify that CanSeek property has changed
        OnPropertyChanged(nameof(CanSeek));
    }

    private void OnCurrentRadioStationChanged(object? sender, RadioStation? station)
    {
        if (station != null)
        {
            CurrentSongDisplay = $"📻 {station.DisplayName}";
            if (!string.IsNullOrEmpty(station.Genre))
            {
                CurrentSongDisplay += $" - {station.Genre}";
            }
            
            // Update last played time
            _ = Task.Run(async () =>
            {
                try
                {
                    await _radioStationService.UpdateLastPlayedAsync(station.Id);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error updating last played: {ex.Message}");
                }
            });
        }
        
        // Notify that CanSeek property has changed (radio streams are not seekable)
        OnPropertyChanged(nameof(CanSeek));
    }

    private async void OnYouTubeSongsAddedToLibrary(object? sender, EventArgs e)
    {
        // Debounce UI updates for YouTube songs (immediate but debounced)
        _debouncer.DebounceAsync("library-refresh", async () =>
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                try
                {
                    // Reload songs from database
                    await LoadSongsAsync();
                    
                    // Refresh the music library view
                    if (MusicLibraryViewModel != null)
                    {
                        await MusicLibraryViewModel.LoadLibraryAsync();
                    }
                    
                    StatusMessage = $"Library updated - {TotalSongs} songs";
                }
                catch (Exception ex)
                {
                    LoggingService.Error("Failed to refresh UI after YouTube import", ex, "MainWindowViewModel");
                }
            });
        }, delayMilliseconds: 2500); // 2.5 seconds debounce
    }
    
    private void OnYouTubeSongNeedsMetadataReview(object? sender, YouTubeSongEventArgs e)
    {
        // This event is fired when user clicks to add YouTube song
        // We show the metadata dialog immediately while download can happen in background
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                // Open MusicBrainz search dialog for YouTube song
                var searchDialog = new Views.MusicBrainzSearchWindow
                {
                    DataContext = App.CreateMusicBrainzSearchViewModel()
                };

                if (searchDialog.DataContext is MusicBrainzSearchViewModel searchViewModel)
                {
                    // Initialize with the YouTube song metadata
                    searchViewModel.Initialize(e.Song);
                    
                    // Show dialog - this will block until user completes or cancels
                    var mainWindow = (Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow;
                    if (mainWindow != null)
                    {
                        await searchDialog.ShowDialog(mainWindow);
                        
                        // If user applied metadata, download and complete the import
                        if (searchViewModel.DialogResult)
                        {
                            // Download happens here, AFTER user has selected metadata
                            // This provides responsive UI - user sees results immediately
                            await YouTubeSearchViewModel!.CompleteYouTubeImportAsync(e.Song, e.YouTubeId);
                        }
                        else
                        {
                            // User skipped - no download needed, nothing to clean up
                            YouTubeSearchViewModel!.StatusMessage = "Import cancelled";
                            YouTubeSearchViewModel.IsSearching = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Error("Failed to open metadata review dialog for YouTube song", ex, "MainWindowViewModel");
                StatusMessage = "Error reviewing YouTube song metadata";
                
                if (YouTubeSearchViewModel != null)
                {
                    YouTubeSearchViewModel.IsSearching = false;
                }
            }
        });
    }
    
    private async void OnTaggingWorkCompleted(object? sender, TaggingCompletedEventArgs e)
    {
        // Update the song in the database with enhanced metadata
        try
        {
            await _databaseService.SaveSongAsync(e.EnhancedSong);
            
            // Debounce UI updates to avoid excessive refreshes (every 2-3 seconds)
            _debouncer.DebounceAsync("library-refresh", async () =>
            {
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    try
                    {
                        // Reload all songs from database
                        await LoadSongsAsync();
                        
                        // Refresh the music library view
                        if (MusicLibraryViewModel != null)
                        {
                            await MusicLibraryViewModel.LoadLibraryAsync();
                        }
                        
                        StatusMessage = $"Library updated with enhanced metadata";
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Error("Failed to refresh UI after metadata enhancement", ex, "MainWindowViewModel");
                    }
                });
            }, delayMilliseconds: 2500); // 2.5 seconds debounce
        }
        catch (Exception ex)
        {
            LoggingService.Error($"Failed to save enhanced song metadata", ex, "MainWindowViewModel");
        }
    }
    
    private void OnTaggingWorkFailed(object? sender, TaggingFailedEventArgs e)
    {
        LoggingService.Warning($"Failed to enhance metadata for {e.Song.DisplayName}: {e.ErrorMessage}", "MainWindowViewModel");
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
    private async Task PlayFromPlaylistAsync()
    {
        // Check if we should start from the beginning
        // This happens when stopped or when there's no current song
        var currentSong = PlaylistViewModel.GetCurrentSong();
        
        if (currentSong != null && _playbackService.State != PlaybackState.Stopped)
        {
            // Play the current song in the playlist (resume case)
            await PlaySongAsync(currentSong);
        }
        else if (PlaylistViewModel.Songs.Count > 0)
        {
            // If stopped or no current song is set, start from the beginning
            PlaylistViewModel.MoveTo(0);
            currentSong = PlaylistViewModel.GetCurrentSong();
            if (currentSong != null)
            {
                await PlaySongAsync(currentSong);
            }
        }
        else
        {
            StatusMessage = "Playlist is empty";
        }
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

    public MetadataEditorViewModel CreateMetadataEditor(Song song)
    {
        var editorVm = new MetadataEditorViewModel(_metadataService, _databaseService, _coverArtService);
        editorVm.LoadSong(song);
        return editorVm;
    }

    public async Task RefreshAfterMetadataEdit()
    {
        await LoadSongsAsync();
        if (MusicLibraryViewModel != null)
        {
            await MusicLibraryViewModel.LoadLibraryAsync();
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

    private void SetOperationStatus(string message, int autoHideAfter = 0)
    {
        // Ensure we're on the UI thread
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => SetOperationStatus(message, autoHideAfter));
            return;
        }

        OperationStatus = message;
        HasOperationStatus = !string.IsNullOrEmpty(message);

        if (autoHideAfter > 0)
        {
            Task.Delay(autoHideAfter).ContinueWith(_ =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (OperationStatus == message) // Only hide if it's still the same message
                    {
                        OperationStatus = string.Empty;
                        HasOperationStatus = false;
                    }
                });
            });
        }
    }

    [RelayCommand]
    private void SelectTab(string tabIndex)
    {
        if (int.TryParse(tabIndex, out int index))
        {
            SelectedTabIndex = index;
        }
    }

    public void BeginSeeking()
    {
        _isUserSeeking = true;
    }

    public void UpdateSeekPreview(double position)
    {
        // Update the time display to show where we'll seek to
        if (_isUserSeeking)
        {
            var duration = _playbackService.Duration;
            if (duration.TotalSeconds > 0)
            {
                var previewPosition = TimeSpan.FromSeconds(position);
                TimeDisplay = $"{FormatTime(previewPosition)} / {FormatTime(duration)}";
            }
        }
    }

    public void EndSeeking(double position)
    {
        try
        {
            var duration = _playbackService.Duration;
            
            if (duration.TotalSeconds > 0)
            {
                var seekPosition = TimeSpan.FromSeconds(position);
                _playbackService.Seek(seekPosition);
                
                // Update UI immediately after seek
                _isUserSeeking = false;
                CurrentPosition = position;
                TimeDisplay = $"{FormatTime(seekPosition)} / {FormatTime(duration)}";
                StatusMessage = $"Seeked to {FormatTime(seekPosition)}";
            }
            else
            {
                _isUserSeeking = false;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error seeking: {ex.Message}";
            _isUserSeeking = false;
        }
    }

    public async Task PlayRadioStationAsync(RadioStation station)
    {
        try
        {
            await _playbackService.PlayRadioAsync(station);
            StatusMessage = $"Playing radio: {station.DisplayName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error playing radio station: {ex.Message}";
        }
    }

    public async Task PlayYouTubeSongAsync(Song song)
    {
        try
        {
            await _playbackService.PlayAsync(song);
            PlaylistViewModel.AddSong(song);
            StatusMessage = $"Playing YouTube song: {song.DisplayName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error playing YouTube song: {ex.Message}";
        }
    }

    public AddRadioStationViewModel CreateRadioStationEditor(RadioStation? station = null)
    {
        var editorVm = new AddRadioStationViewModel(_radioStationService, _radioMetadataService);
        if (station != null)
        {
            editorVm.LoadStation(station);
        }
        return editorVm;
    }

    public async Task RefreshRadioStationsAsync()
    {
        if (RadioViewModel != null)
        {
            await RadioViewModel.LoadStationsAsync();
        }
    }

    public void Dispose()
    {
        // Flush any pending debounced actions before disposing
        _debouncer?.FlushAll();
        _debouncer?.Dispose();
        
        _taggingQueue?.Dispose();
        _mprisService?.Dispose();
        _playbackService?.Dispose();
        _databaseService?.Dispose();
        _radioStationService?.Dispose();
        _radioBrowserService?.Dispose();
        _youtubeService?.Dispose();
    }
}

