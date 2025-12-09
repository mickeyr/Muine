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

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly MetadataService _metadataService;
    private readonly MusicDatabaseService _databaseService;
    private readonly LibraryScannerService _scannerService;
    private readonly CoverArtService _coverArtService;

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

    public MainWindowViewModel()
    {
        // Initialize services
        _metadataService = new MetadataService();
        _coverArtService = new CoverArtService();
        
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
        
        // Initialize database asynchronously
        _ = InitializeDatabaseAsync();
    }

    private async Task InitializeDatabaseAsync()
    {
        try
        {
            await _databaseService.InitializeAsync();
            await LoadSongsAsync();
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
}

