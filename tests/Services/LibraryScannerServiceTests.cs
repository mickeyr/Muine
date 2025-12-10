using Muine.Core.Models;
using Muine.Core.Services;
using Xunit;

namespace Muine.Tests.Services;

public class LibraryScannerServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _databasePath;
    private readonly MetadataService _metadataService;
    private readonly MusicDatabaseService _databaseService;
    private readonly CoverArtService _coverArtService;
    private readonly LibraryScannerService _scannerService;

    public LibraryScannerServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"muine_scanner_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        
        _databasePath = Path.Combine(_testDirectory, "test.db");
        
        _metadataService = new MetadataService();
        _databaseService = new MusicDatabaseService(_databasePath);
        _coverArtService = new CoverArtService();
        _scannerService = new LibraryScannerService(_metadataService, _databaseService, _coverArtService);
        
        // Initialize database
        _databaseService.InitializeAsync().Wait();
    }

    public void Dispose()
    {
        _databaseService?.Dispose();
        
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public async Task RefreshSongAsync_WithValidSong_UpdatesMetadata()
    {
        // Arrange
        var mp3Path = Path.Combine(_testDirectory, "test.mp3");
        CreateMinimalMp3(mp3Path);
        
        // Import the song first
        var originalSong = _metadataService.ReadSongMetadata(mp3Path);
        Assert.NotNull(originalSong);
        var originalMTime = originalSong.MTime;
        await _databaseService.SaveSongAsync(originalSong);
        
        // Get the song from database to have the ID
        var dbSong = await _databaseService.GetSongByFilenameAsync(mp3Path);
        Assert.NotNull(dbSong);
        
        // Simulate file modification by waiting and touching the file
        await Task.Delay(1100); // Wait to ensure mtime changes
        File.SetLastWriteTimeUtc(mp3Path, DateTime.UtcNow);
        
        // Act
        var result = await _scannerService.RefreshSongAsync(dbSong);
        
        // Assert
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
        
        // Verify the database was updated (mtime should be different)
        var refreshedSong = await _databaseService.GetSongByFilenameAsync(mp3Path);
        Assert.NotNull(refreshedSong);
        Assert.NotEqual(originalMTime, refreshedSong.MTime);
    }

    [Fact]
    public async Task RefreshSongAsync_WithMissingFile_ReturnsError()
    {
        // Arrange
        var song = new Song
        {
            Id = 1,
            Filename = Path.Combine(_testDirectory, "nonexistent.mp3"),
            Title = "Test Song"
        };
        
        // Act
        var result = await _scannerService.RefreshSongAsync(song);
        
        // Assert
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailureCount);
        Assert.Single(result.Errors);
        Assert.Contains("not found", result.Errors[0]);
    }

    [Fact]
    public async Task RefreshAllSongsAsync_WithMultipleSongs_UpdatesAll()
    {
        // Arrange
        var mp3Path1 = Path.Combine(_testDirectory, "test1.mp3");
        var mp3Path2 = Path.Combine(_testDirectory, "test2.mp3");
        
        CreateMinimalMp3(mp3Path1);
        CreateMinimalMp3(mp3Path2);
        
        // Import songs
        var song1 = _metadataService.ReadSongMetadata(mp3Path1);
        var song2 = _metadataService.ReadSongMetadata(mp3Path2);
        Assert.NotNull(song1);
        Assert.NotNull(song2);
        
        await _databaseService.SaveSongAsync(song1);
        await _databaseService.SaveSongAsync(song2);
        
        // Simulate file modification
        await Task.Delay(1100);
        File.SetLastWriteTimeUtc(mp3Path1, DateTime.UtcNow);
        File.SetLastWriteTimeUtc(mp3Path2, DateTime.UtcNow);
        
        // Act
        var result = await _scannerService.RefreshAllSongsAsync();
        
        // Assert
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
        Assert.Equal(2, result.TotalFiles);
    }

    [Fact]
    public async Task RefreshAllSongsAsync_WithMixedResults_ReportsCorrectly()
    {
        // Arrange
        var existingMp3 = Path.Combine(_testDirectory, "existing.mp3");
        var missingMp3 = Path.Combine(_testDirectory, "missing.mp3");
        
        CreateMinimalMp3(existingMp3);
        
        // Import both songs (but delete one after import)
        var song1 = _metadataService.ReadSongMetadata(existingMp3);
        Assert.NotNull(song1);
        await _databaseService.SaveSongAsync(song1);
        
        // Create a dummy entry for the missing file
        var song2 = new Song
        {
            Filename = missingMp3,
            Title = "Missing Song"
        };
        await _databaseService.SaveSongAsync(song2);
        
        // Act
        var result = await _scannerService.RefreshAllSongsAsync();
        
        // Assert
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(1, result.FailureCount);
        Assert.Equal(2, result.TotalFiles);
        Assert.Single(result.Errors);
    }

    [Fact]
    public async Task RefreshAllSongsAsync_WithProgressCallback_CompletesSuccessfully()
    {
        // Arrange
        var mp3Path = Path.Combine(_testDirectory, "test.mp3");
        CreateMinimalMp3(mp3Path);
        
        var song = _metadataService.ReadSongMetadata(mp3Path);
        Assert.NotNull(song);
        await _databaseService.SaveSongAsync(song);
        
        int progressReports = 0;
        var progress = new Progress<ScanProgress>(p =>
        {
            progressReports++;
            Assert.True(p.ProcessedFiles <= p.TotalFiles);
            Assert.True(p.PercentComplete >= 0 && p.PercentComplete <= 100);
        });
        
        // Act
        var result = await _scannerService.RefreshAllSongsAsync(progress);
        
        // Assert - verify operation completed successfully
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
        // Note: Progress callback may or may not fire in test context depending on SynchronizationContext
    }

    private static void CreateMinimalMp3(string path)
    {
        // Create a minimal valid MP3 file
        using var fs = File.Create(path);
        
        // Create ID3v2 header
        byte[] id3Header = {
            0x49, 0x44, 0x33, // "ID3"
            0x03, 0x00,       // Version 2.3.0
            0x00,             // Flags
            0x00, 0x00, 0x00, 0x0A // Size (minimal)
        };
        fs.Write(id3Header, 0, id3Header.Length);
        
        // Minimal ID3 tag content
        byte[] tagPadding = new byte[10];
        fs.Write(tagPadding, 0, tagPadding.Length);
        
        // MP3 frame
        byte[] mp3Header = { 0xFF, 0xFB, 0x90, 0x00 };
        fs.Write(mp3Header, 0, mp3Header.Length);
        
        // Padding
        byte[] padding = new byte[1024];
        fs.Write(padding, 0, padding.Length);
    }
}
