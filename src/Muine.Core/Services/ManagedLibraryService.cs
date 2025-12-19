using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Muine.Core.Models;

namespace Muine.Core.Services;

/// <summary>
/// Service for managing the organized music library
/// </summary>
public class ManagedLibraryService
{
    private readonly LibraryConfiguration _config;
    private readonly MetadataService _metadataService;
    private readonly MusicDatabaseService _databaseService;

    public ManagedLibraryService(
        LibraryConfiguration config,
        MetadataService metadataService,
        MusicDatabaseService databaseService)
    {
        _config = config;
        _metadataService = metadataService;
        _databaseService = databaseService;
    }

    /// <summary>
    /// Generate the target path for a song in the managed library
    /// Format: {Artist}/{Year} - {Album}/{TrackNumber} - {Title}.ext
    /// </summary>
    public string GenerateLibraryPath(Song song)
    {
        // Get artist (first artist if multiple, or "Unknown Artist")
        var artist = song.Artists.Length > 0 && !string.IsNullOrWhiteSpace(song.Artists[0])
            ? song.Artists[0]
            : "Unknown Artist";

        // Clean up multi-artist format (e.g., "Artist1 feat. Artist2" -> "Artist1")
        artist = ExtractPrimaryArtist(artist);

        // Get album or use "Unknown Album"
        var album = !string.IsNullOrWhiteSpace(song.Album)
            ? song.Album
            : "Unknown Album";

        // Get year or use empty string
        var year = !string.IsNullOrWhiteSpace(song.Year)
            ? song.Year
            : "";

        // Get title or use filename
        var title = !string.IsNullOrWhiteSpace(song.Title)
            ? song.Title
            : Path.GetFileNameWithoutExtension(song.Filename);

        // Get track number with padding
        var trackNumber = song.TrackNumber > 0
            ? song.TrackNumber.ToString("D2")
            : "00";

        // Get file extension
        var extension = Path.GetExtension(song.Filename);

        // Sanitize all components
        artist = SanitizeFilesystemName(artist);
        album = SanitizeFilesystemName(album);
        year = SanitizeFilesystemName(year);
        title = SanitizeFilesystemName(title);

        // Build path: {Artist}/{Year} - {Album}/{TrackNumber} - {Title}.ext
        string albumFolder;
        if (!string.IsNullOrWhiteSpace(year))
        {
            albumFolder = $"{year} - {album}";
        }
        else
        {
            albumFolder = album;
        }

        var filename = $"{trackNumber} - {title}{extension}";
        
        return Path.Combine(_config.LibraryPath, artist, albumFolder, filename);
    }

    /// <summary>
    /// Extract the primary artist from a multi-artist string
    /// e.g., "Artist1 feat. Artist2" -> "Artist1"
    /// </summary>
    private string ExtractPrimaryArtist(string artist)
    {
        var patterns = new[]
        {
            " feat. ",
            " feat ",
            " ft. ",
            " ft ",
            " featuring ",
            " with ",
            " & ",
            " and "
        };

        foreach (var pattern in patterns)
        {
            var index = artist.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (index > 0)
            {
                return artist.Substring(0, index).Trim();
            }
        }

        return artist;
    }

    /// <summary>
    /// Sanitize a filename component by replacing invalid characters
    /// </summary>
    private string SanitizeFilesystemName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        // Invalid characters for both Windows and Unix filesystems
        var invalidChars = new char[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|' };
        
        var result = name;
        foreach (var c in invalidChars)
        {
            result = result.Replace(c, '_');
        }

        // Remove leading/trailing dots and spaces (Windows issue)
        result = result.Trim('.', ' ');

        // Limit length to avoid filesystem issues (keep it reasonable)
        if (result.Length > 200)
        {
            result = result.Substring(0, 200);
        }

        return result;
    }

    /// <summary>
    /// Import a file into the managed library
    /// </summary>
    /// <param name="sourcePath">Source file path</param>
    /// <param name="copyInsteadOfMove">Whether to copy instead of move (null uses config default)</param>
    /// <param name="skipDuplicateCheck">Skip expensive duplicate checking for faster imports</param>
    /// <returns>ImportResult with the imported song or error information</returns>
    public async Task<ImportResult> ImportFileAsync(string sourcePath, bool? copyInsteadOfMove = null, bool skipDuplicateCheck = false)
    {
        var result = new ImportResult { SourcePath = sourcePath };

        try
        {
            // Check if file exists
            if (!File.Exists(sourcePath))
            {
                result.Success = false;
                result.ErrorMessage = "File not found";
                return result;
            }

            // Read metadata
            var song = _metadataService.ReadSongMetadata(sourcePath);
            if (song == null)
            {
                result.Success = false;
                result.ErrorMessage = "Failed to read metadata";
                result.NeedsManualMetadata = true;
                return result;
            }

            // Check for duplicates (skip if requested for faster imports)
            if (!skipDuplicateCheck)
            {
                var duplicateCheck = await CheckForDuplicateAsync(sourcePath, song);
                if (duplicateCheck.IsDuplicate)
                {
                    result.Success = false;
                    result.ErrorMessage = "Duplicate file found";
                    result.IsDuplicate = true;
                    result.DuplicateSong = duplicateCheck.ExistingSong;
                    return result;
                }
            }

            // Check if metadata needs enhancement
            if (NeedsMetadataEnhancement(song))
            {
                result.NeedsMetadataEnhancement = true;
            }

            // Generate target path
            var targetPath = GenerateLibraryPath(song);

            // Ensure directory exists
            var targetDir = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            // Check for file conflicts
            if (File.Exists(targetPath))
            {
                var conflict = await HandleFileConflictAsync(sourcePath, targetPath);
                if (conflict.NeedsUserInput)
                {
                    result.Success = false;
                    result.ErrorMessage = "File already exists at target location";
                    result.NeedsUserInput = true;
                    result.ConflictInfo = conflict;
                    return result;
                }
                
                targetPath = conflict.ResolvedPath ?? targetPath;
            }

            // Move or copy file
            var shouldCopy = copyInsteadOfMove ?? _config.CopyInsteadOfMove;
            if (shouldCopy)
            {
                File.Copy(sourcePath, targetPath, overwrite: false);
            }
            else
            {
                File.Move(sourcePath, targetPath);
                
                // Clean up empty directories
                CleanupEmptyDirectories(Path.GetDirectoryName(sourcePath));
            }

            // Update song with new filename
            song.Filename = targetPath;

            // Save to database
            await _databaseService.SaveSongAsync(song);

            result.Success = true;
            result.ImportedSong = song;
            result.TargetPath = targetPath;

            LoggingService.Info($"Imported file: {sourcePath} -> {targetPath}", "ManagedLibraryService");

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            LoggingService.Error($"Failed to import file: {sourcePath}", ex, "ManagedLibraryService");
            return result;
        }
    }

    /// <summary>
    /// Check if a song needs metadata enhancement
    /// </summary>
    private bool NeedsMetadataEnhancement(Song song)
    {
        // Check if essential metadata is missing
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

    /// <summary>
    /// Check if a file is a duplicate of an existing song in the database
    /// Optimized to use fast metadata matching instead of expensive file hashing
    /// </summary>
    private async Task<DuplicateCheckResult> CheckForDuplicateAsync(string filePath, Song song)
    {
        var result = new DuplicateCheckResult();

        try
        {
            // Get all songs from database (cached by DB service)
            var allSongs = await _databaseService.GetAllSongsAsync();
            
            // Fast metadata-based duplicate check (no file hashing)
            var metadataMatches = allSongs.Where(s =>
                s.Artists.Length > 0 &&
                song.Artists.Length > 0 &&
                string.Equals(s.Artists[0], song.Artists[0], StringComparison.OrdinalIgnoreCase) &&
                string.Equals(s.Title, song.Title, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(s.Album, song.Album, StringComparison.OrdinalIgnoreCase)
            ).ToList();

            if (metadataMatches.Any())
            {
                result.IsDuplicate = true;
                result.ExistingSong = metadataMatches.First();
                result.MatchType = "Metadata match (artist/title/album)";
                return result;
            }
            
            // Optional: Only do expensive hash check if metadata suggests possible duplicate
            // This is now skipped by default for performance
        }
        catch (Exception ex)
        {
            LoggingService.Warning($"Error checking for duplicates: {ex.Message}", "ManagedLibraryService");
        }

        return result;
    }

    /// <summary>
    /// Check if two files are exact duplicates using SHA256 hash
    /// This is expensive and should only be used when necessary
    /// </summary>
    private bool AreFilesIdentical(string filePath1, string filePath2)
    {
        try
        {
            var hash1 = CalculateFileHash(filePath1);
            var hash2 = CalculateFileHash(filePath2);
            return hash1 == hash2;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Calculate SHA256 hash of a file
    /// </summary>
    private string CalculateFileHash(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha256.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// Handle file conflict at target location
    /// </summary>
    private async Task<FileConflictInfo> HandleFileConflictAsync(string sourcePath, string targetPath)
    {
        var conflict = new FileConflictInfo
        {
            SourcePath = sourcePath,
            TargetPath = targetPath
        };

        try
        {
            // Check if files are identical
            var sourceHash = CalculateFileHash(sourcePath);
            var targetHash = CalculateFileHash(targetPath);

            if (sourceHash == targetHash)
            {
                // Files are identical, no conflict
                conflict.NeedsUserInput = false;
                conflict.AreIdentical = true;
                conflict.ResolvedPath = targetPath; // Keep existing file
                return conflict;
            }

            // Files are different, need user input
            conflict.NeedsUserInput = true;
            conflict.AreIdentical = false;
            
            return conflict;
        }
        catch (Exception ex)
        {
            LoggingService.Warning($"Error handling file conflict: {ex.Message}", "ManagedLibraryService");
            conflict.NeedsUserInput = true;
            return conflict;
        }
    }

    /// <summary>
    /// Clean up empty directories after moving files
    /// </summary>
    private void CleanupEmptyDirectories(string? directoryPath)
    {
        if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
        {
            return;
        }

        try
        {
            // Don't delete if it contains files
            if (Directory.GetFiles(directoryPath).Length > 0)
            {
                return;
            }

            // Don't delete if it contains subdirectories with files
            if (Directory.GetDirectories(directoryPath).Length > 0)
            {
                return;
            }

            // Delete the empty directory
            Directory.Delete(directoryPath, recursive: false);
            
            LoggingService.Info($"Cleaned up empty directory: {directoryPath}", "ManagedLibraryService");

            // Recursively clean up parent directories
            var parentDir = Path.GetDirectoryName(directoryPath);
            if (!string.IsNullOrEmpty(parentDir))
            {
                CleanupEmptyDirectories(parentDir);
            }
        }
        catch (Exception ex)
        {
            // Ignore errors during cleanup
            LoggingService.Warning($"Failed to cleanup directory {directoryPath}: {ex.Message}", "ManagedLibraryService");
        }
    }

    /// <summary>
    /// Move a song file to match the correct organization pattern
    /// </summary>
    public async Task<bool> ReorganizeSongAsync(Song song)
    {
        try
        {
            if (!File.Exists(song.Filename))
            {
                LoggingService.Warning($"Cannot reorganize - file not found: {song.Filename}", "ManagedLibraryService");
                return false;
            }

            var targetPath = GenerateLibraryPath(song);
            
            // If already in correct location, nothing to do
            if (string.Equals(song.Filename, targetPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Ensure target directory exists
            var targetDir = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            // Move file
            var oldPath = song.Filename;
            File.Move(oldPath, targetPath);
            
            // Update song
            song.Filename = targetPath;
            await _databaseService.SaveSongAsync(song);

            // Clean up old directories
            CleanupEmptyDirectories(Path.GetDirectoryName(oldPath));

            LoggingService.Info($"Reorganized: {oldPath} -> {targetPath}", "ManagedLibraryService");
            
            return true;
        }
        catch (Exception ex)
        {
            LoggingService.Error($"Failed to reorganize song: {song.Filename}", ex, "ManagedLibraryService");
            return false;
        }
    }
}

/// <summary>
/// Result of importing a file
/// </summary>
public class ImportResult
{
    public bool Success { get; set; }
    public string SourcePath { get; set; } = string.Empty;
    public string? TargetPath { get; set; }
    public Song? ImportedSong { get; set; }
    public string? ErrorMessage { get; set; }
    public bool NeedsMetadataEnhancement { get; set; }
    public bool NeedsManualMetadata { get; set; }
    public bool IsDuplicate { get; set; }
    public Song? DuplicateSong { get; set; }
    public bool NeedsUserInput { get; set; }
    public FileConflictInfo? ConflictInfo { get; set; }
}

/// <summary>
/// Result of checking for duplicates
/// </summary>
public class DuplicateCheckResult
{
    public bool IsDuplicate { get; set; }
    public Song? ExistingSong { get; set; }
    public string MatchType { get; set; } = string.Empty;
}

/// <summary>
/// Information about a file conflict
/// </summary>
public class FileConflictInfo
{
    public string SourcePath { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
    public bool NeedsUserInput { get; set; }
    public bool AreIdentical { get; set; }
    public string? ResolvedPath { get; set; }
}
