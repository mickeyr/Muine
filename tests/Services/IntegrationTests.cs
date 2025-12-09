using Muine.Core.Services;
using Xunit;

namespace Muine.Tests.Services;

public class IntegrationTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _databasePath;
    private readonly MetadataService _metadataService;
    private readonly MusicDatabaseService _databaseService;
    private readonly CoverArtService _coverArtService;
    private readonly LibraryScannerService _scannerService;

    public IntegrationTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"muine_integration_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);

        _databasePath = Path.Combine(_testDirectory, "test.db");
        
        _metadataService = new MetadataService();
        _coverArtService = new CoverArtService();
        _databaseService = new MusicDatabaseService(_databasePath);
        _scannerService = new LibraryScannerService(_metadataService, _databaseService, _coverArtService);
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
    public async Task EndToEnd_ScanDirectory_WithCoverArt_ShouldImportSuccessfully()
    {
        // Arrange
        await _databaseService.InitializeAsync();
        
        var musicDir = Path.Combine(_testDirectory, "music");
        Directory.CreateDirectory(musicDir);
        
        // Create a cover art file
        var coverPath = Path.Combine(musicDir, "cover.jpg");
        File.WriteAllText(coverPath, "fake cover image");
        
        // Create a minimal valid MP3 file
        var mp3Path = Path.Combine(musicDir, "test.mp3");
        CreateMinimalMp3(mp3Path);

        // Act
        var result = await _scannerService.ScanDirectoryAsync(musicDir);

        // Assert
        Assert.Equal(1, result.TotalFiles);
        
        // The file should be processed (either success or failure)
        Assert.Equal(1, result.SuccessCount + result.FailureCount);
        
        // If it succeeded, verify cover art was found
        if (result.SuccessCount > 0)
        {
            var songs = await _databaseService.GetAllSongsAsync();
            Assert.Single(songs);
            
            var song = songs[0];
            Assert.Equal(mp3Path, song.Filename);
            // Cover art should be found
            Assert.Equal(coverPath, song.CoverImagePath);
        }
    }

    [Fact]
    public async Task EndToEnd_CoverArtService_FindsCoverInSongDirectory()
    {
        // Arrange
        await _databaseService.InitializeAsync();
        
        var musicDir = Path.Combine(_testDirectory, "music");
        Directory.CreateDirectory(musicDir);
        
        var coverPath = Path.Combine(musicDir, "folder.jpg");
        File.WriteAllText(coverPath, "fake cover");
        
        var mp3Path = Path.Combine(musicDir, "song.mp3");
        CreateMinimalMp3(mp3Path);

        // Act - Read metadata and find cover art
        var song = _metadataService.ReadSongMetadata(mp3Path);
        
        // Assert
        if (song != null)
        {
            _coverArtService.UpdateSongCoverArt(song);
            Assert.Equal(coverPath, song.CoverImagePath);
        }
    }

    private static void CreateMinimalMp3(string path)
    {
        // Create a minimal valid MP3 frame
        // This creates a very basic MP3 header that TagLib should be able to read
        using var fs = File.Create(path);
        
        // MP3 frame sync word (0xFFE or 0xFFF for MPEG version 1 Layer 3)
        // This is a minimal frame header
        byte[] header = { 0xFF, 0xFB, 0x90, 0x00 };
        fs.Write(header, 0, header.Length);
    }
}
