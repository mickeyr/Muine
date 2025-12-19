using Muine.Core.Models;

namespace Muine.Core.Services;

public class LibraryScannerService
{
    private readonly MetadataService _metadataService;
    private readonly MusicDatabaseService _databaseService;
    private readonly CoverArtService _coverArtService;
    private readonly BackgroundTaggingQueue? _taggingQueue;
    private readonly ManagedLibraryService? _managedLibraryService;

    public LibraryScannerService(
        MetadataService metadataService, 
        MusicDatabaseService databaseService,
        CoverArtService coverArtService,
        BackgroundTaggingQueue? taggingQueue = null,
        ManagedLibraryService? managedLibraryService = null)
    {
        _metadataService = metadataService;
        _databaseService = databaseService;
        _coverArtService = coverArtService;
        _taggingQueue = taggingQueue;
        _managedLibraryService = managedLibraryService;
    }

    public async Task<ScanResult> ScanDirectoryAsync(string directory, IProgress<ScanProgress>? progress = null, bool autoEnhanceMetadata = false)
    {
        var result = new ScanResult();
        
        if (!Directory.Exists(directory))
        {
            result.Errors.Add($"Directory not found: {directory}");
            return result;
        }

        var files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories)
            .Where(f => _metadataService.IsSupportedFormat(f))
            .ToList();

        result.TotalFiles = files.Count;

        for (int i = 0; i < files.Count; i++)
        {
            var file = files[i];
            try
            {
                var song = _metadataService.ReadSongMetadata(file);
                if (song != null)
                {
                    // Find and set cover art for the song
                    _coverArtService.UpdateSongCoverArt(song);
                    
                    await _databaseService.SaveSongAsync(song);
                    result.SuccessCount++;
                    
                    // Queue for metadata enhancement if requested and tagging queue is available
                    if (autoEnhanceMetadata && _taggingQueue != null)
                    {
                        _taggingQueue.EnqueueSong(song, downloadCoverArt: true);
                    }
                }
                else
                {
                    result.Errors.Add($"Failed to read metadata: {file}");
                    result.FailureCount++;
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Error processing {file}: {ex.Message}");
                result.FailureCount++;
            }

            progress?.Report(new ScanProgress
            {
                CurrentFile = file,
                ProcessedFiles = i + 1,
                TotalFiles = files.Count
            });
        }

        return result;
    }

    public async Task<RefreshResult> RefreshSongAsync(Song song)
    {
        var result = new RefreshResult();
        await RefreshSingleSongInternalAsync(song, result);
        return result;
    }

    public async Task<RefreshResult> RefreshAllSongsAsync(IProgress<ScanProgress>? progress = null)
    {
        var result = new RefreshResult();
        
        var songs = await _databaseService.GetAllSongsAsync();
        result.TotalFiles = songs.Count;

        for (int i = 0; i < songs.Count; i++)
        {
            var song = songs[i];
            await RefreshSingleSongInternalAsync(song, result);

            progress?.Report(new ScanProgress
            {
                CurrentFile = song.Filename,
                ProcessedFiles = i + 1,
                TotalFiles = songs.Count
            });
        }

        return result;
    }

    private async Task RefreshSingleSongInternalAsync(Song song, RefreshResult result)
    {
        if (!System.IO.File.Exists(song.Filename))
        {
            result.Errors.Add($"File not found: {song.Filename}");
            result.FailureCount++;
            return;
        }

        try
        {
            var refreshedSong = _metadataService.ReadSongMetadata(song.Filename);
            if (refreshedSong != null)
            {
                // Preserve the original ID
                refreshedSong.Id = song.Id;
                
                // Find and set cover art for the song
                _coverArtService.UpdateSongCoverArt(refreshedSong);
                
                await _databaseService.SaveSongAsync(refreshedSong);
                result.SuccessCount++;
            }
            else
            {
                result.Errors.Add($"Failed to read metadata: {song.Filename}");
                result.FailureCount++;
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Error refreshing {song.Filename}: {ex.Message}");
            result.FailureCount++;
        }
    }

    /// <summary>
    /// Scan the managed library directory for new/changed files
    /// </summary>
    public async Task<ManagedScanResult> ScanManagedLibraryAsync(
        string libraryPath,
        IProgress<ScanProgress>? progress = null,
        bool reorganizeFiles = true,
        bool autoEnhanceMetadata = true)
    {
        var result = new ManagedScanResult();
        
        if (!Directory.Exists(libraryPath))
        {
            result.Errors.Add($"Library directory not found: {libraryPath}");
            return result;
        }

        // Get all audio files in library
        var files = Directory.GetFiles(libraryPath, "*.*", SearchOption.AllDirectories)
            .Where(f => _metadataService.IsSupportedFormat(f))
            .ToList();

        result.TotalFiles = files.Count;

        // Get all songs currently in database
        var databaseSongs = await _databaseService.GetAllSongsAsync();
        var databasePaths = new HashSet<string>(
            databaseSongs.Select(s => s.Filename),
            StringComparer.OrdinalIgnoreCase
        );

        // Clean up orphaned database entries (files that no longer exist)
        var orphanedSongs = databaseSongs.Where(s => !File.Exists(s.Filename)).ToList();
        foreach (var orphan in orphanedSongs)
        {
            try
            {
                await _databaseService.DeleteSongAsync(orphan.Id);
                result.OrphanedEntries.Add(orphan.Filename);
                LoggingService.Info($"Removed orphaned database entry: {orphan.Filename}", "LibraryScannerService");
            }
            catch (Exception ex)
            {
                LoggingService.Warning($"Failed to remove orphaned entry: {orphan.Filename} - {ex.Message}", "LibraryScannerService");
            }
        }

        // Process each file
        for (int i = 0; i < files.Count; i++)
        {
            var file = files[i];
            
            try
            {
                // Check if file is already in database
                var isNew = !databasePaths.Contains(file);
                
                if (isNew)
                {
                    // New file - read metadata and add to database
                    var song = _metadataService.ReadSongMetadata(file);
                    if (song != null)
                    {
                        // Find and set cover art
                        _coverArtService.UpdateSongCoverArt(song);
                        
                        // Check if metadata needs enhancement
                        if (NeedsMetadataEnhancement(song))
                        {
                            result.FilesNeedingEnhancement.Add(song);
                            
                            if (autoEnhanceMetadata && _taggingQueue != null)
                            {
                                _taggingQueue.EnqueueSong(song, downloadCoverArt: true);
                            }
                        }
                        
                        // Save to database
                        await _databaseService.SaveSongAsync(song);
                        result.NewFilesAdded++;
                        
                        // Reorganize if needed and managed library service is available
                        if (reorganizeFiles && _managedLibraryService != null)
                        {
                            var targetPath = _managedLibraryService.GenerateLibraryPath(song);
                            if (!string.Equals(song.Filename, targetPath, StringComparison.OrdinalIgnoreCase))
                            {
                                result.FilesNeedingReorganization.Add(song);
                            }
                        }
                    }
                    else
                    {
                        result.Errors.Add($"Failed to read metadata: {file}");
                        result.FailureCount++;
                    }
                }
                else
                {
                    // Existing file - check if it needs reorganization
                    var existingSong = databaseSongs.FirstOrDefault(
                        s => string.Equals(s.Filename, file, StringComparison.OrdinalIgnoreCase)
                    );
                    
                    if (existingSong != null && reorganizeFiles && _managedLibraryService != null)
                    {
                        var targetPath = _managedLibraryService.GenerateLibraryPath(existingSong);
                        if (!string.Equals(existingSong.Filename, targetPath, StringComparison.OrdinalIgnoreCase))
                        {
                            result.FilesNeedingReorganization.Add(existingSong);
                        }
                    }
                }
                
                result.ProcessedFiles++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Error processing {file}: {ex.Message}");
                result.FailureCount++;
            }

            progress?.Report(new ScanProgress
            {
                CurrentFile = file,
                ProcessedFiles = i + 1,
                TotalFiles = files.Count
            });
        }

        return result;
    }

    /// <summary>
    /// Import a directory of music files into the managed library
    /// </summary>
    public async Task<ImportDirectoryResult> ImportDirectoryAsync(
        string sourceDirectory,
        bool copyInsteadOfMove,
        IProgress<ScanProgress>? progress = null,
        bool autoEnhanceMetadata = true)
    {
        var result = new ImportDirectoryResult();
        
        if (!Directory.Exists(sourceDirectory))
        {
            result.Errors.Add($"Directory not found: {sourceDirectory}");
            return result;
        }

        if (_managedLibraryService == null)
        {
            result.Errors.Add("Managed library service not available");
            return result;
        }

        // Get all audio files recursively
        var files = Directory.GetFiles(sourceDirectory, "*.*", SearchOption.AllDirectories)
            .Where(f => _metadataService.IsSupportedFormat(f))
            .ToList();

        result.TotalFiles = files.Count;

        for (int i = 0; i < files.Count; i++)
        {
            var file = files[i];
            
            try
            {
                var importResult = await _managedLibraryService.ImportFileAsync(file, copyInsteadOfMove);
                
                if (importResult.Success)
                {
                    result.SuccessCount++;
                    
                    // Queue for enhancement if needed
                    if (importResult.NeedsMetadataEnhancement && autoEnhanceMetadata && _taggingQueue != null)
                    {
                        _taggingQueue.EnqueueSong(importResult.ImportedSong!, downloadCoverArt: true);
                    }
                }
                else
                {
                    result.FailureCount++;
                    
                    // Queue issues for user review
                    if (importResult.IsDuplicate)
                    {
                        result.Duplicates.Add(new DuplicateInfo
                        {
                            SourcePath = file,
                            ExistingSong = importResult.DuplicateSong!
                        });
                    }
                    else if (importResult.NeedsManualMetadata)
                    {
                        result.FilesNeedingManualMetadata.Add(file);
                    }
                    else if (importResult.NeedsUserInput && importResult.ConflictInfo != null)
                    {
                        result.Conflicts.Add(importResult.ConflictInfo);
                    }
                    else
                    {
                        result.Errors.Add($"{file}: {importResult.ErrorMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Error importing {file}: {ex.Message}");
                result.FailureCount++;
            }

            progress?.Report(new ScanProgress
            {
                CurrentFile = file,
                ProcessedFiles = i + 1,
                TotalFiles = files.Count
            });
        }

        return result;
    }

    private bool NeedsMetadataEnhancement(Song song)
    {
        if (song.Artists.Length == 0 || string.IsNullOrWhiteSpace(song.Artists[0]))
            return true;
        if (string.IsNullOrWhiteSpace(song.Title))
            return true;
        if (string.IsNullOrWhiteSpace(song.Album))
            return true;
        if (string.IsNullOrWhiteSpace(song.Year))
            return true;
        if (string.IsNullOrWhiteSpace(song.CoverImagePath))
            return true;
        return false;
    }
}

public class ScanResult
{
    public int TotalFiles { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class ManagedScanResult
{
    public int TotalFiles { get; set; }
    public int ProcessedFiles { get; set; }
    public int NewFilesAdded { get; set; }
    public int FailureCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> OrphanedEntries { get; set; } = new();
    public List<Song> FilesNeedingEnhancement { get; set; } = new();
    public List<Song> FilesNeedingReorganization { get; set; } = new();
}

public class ImportDirectoryResult
{
    public int TotalFiles { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<DuplicateInfo> Duplicates { get; set; } = new();
    public List<string> FilesNeedingManualMetadata { get; set; } = new();
    public List<FileConflictInfo> Conflicts { get; set; } = new();
}

public class DuplicateInfo
{
    public string SourcePath { get; set; } = string.Empty;
    public Song ExistingSong { get; set; } = null!;
}

public class ScanProgress
{
    public string CurrentFile { get; set; } = string.Empty;
    public int ProcessedFiles { get; set; }
    public int TotalFiles { get; set; }
    public double PercentComplete => TotalFiles > 0 ? (ProcessedFiles * 100.0 / TotalFiles) : 0;
}

public class RefreshResult
{
    public int TotalFiles { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> Errors { get; set; } = new();
}
