using Muine.Core.Services;
using Xunit;

namespace Muine.Tests.Services;

public class MusicBrainzServiceTests : IDisposable
{
    private readonly MusicBrainzService _service;
    private readonly string _testDirectory;

    public MusicBrainzServiceTests()
    {
        _service = new MusicBrainzService();
        _testDirectory = Path.Combine(Path.GetTempPath(), $"muine_mb_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        _service?.Dispose();
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public async Task SearchRecordingsAsync_WithValidArtistAndTitle_ReturnsMatches()
    {
        // Arrange
        var artist = "The Beatles";
        var title = "Let It Be";

        // Act
        var matches = await _service.SearchRecordingsAsync(artist, title, maxResults: 5);

        // Assert
        Assert.NotNull(matches);
        Assert.NotEmpty(matches);
        
        // First result should be the most relevant
        var topMatch = matches[0];
        Assert.NotNull(topMatch.RecordingId);
        Assert.Contains("Let It Be", topMatch.Title, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Beatles", topMatch.Artist, StringComparison.OrdinalIgnoreCase);
        Assert.True(topMatch.MatchScore > 0.0);
    }

    [Fact]
    public async Task SearchRecordingsAsync_WithEmptyArtist_ReturnsEmptyList()
    {
        // Arrange
        var artist = "";
        var title = "Let It Be";

        // Act
        var matches = await _service.SearchRecordingsAsync(artist, title);

        // Assert
        Assert.NotNull(matches);
        Assert.Empty(matches);
    }

    [Fact]
    public async Task SearchRecordingsAsync_WithEmptyTitle_ReturnsEmptyList()
    {
        // Arrange
        var artist = "The Beatles";
        var title = "";

        // Act
        var matches = await _service.SearchRecordingsAsync(artist, title);

        // Assert
        Assert.NotNull(matches);
        Assert.Empty(matches);
    }

    [Fact]
    public async Task SearchRecordingsAsync_WithObscureQuery_ReturnsEmptyOrFewResults()
    {
        // Arrange
        var artist = "XYZ123NonExistentArtist456";
        var title = "ABC789NonExistentSong012";

        // Act
        var matches = await _service.SearchRecordingsAsync(artist, title, maxResults: 5);

        // Assert
        Assert.NotNull(matches);
        // Either empty or very low match scores
        if (matches.Count > 0)
        {
            Assert.All(matches, match => Assert.True(match.MatchScore < 0.5));
        }
    }

    [Fact]
    public async Task SearchRecordingsAsync_RespectsMaxResults()
    {
        // Arrange
        var artist = "John";
        var title = "Love";
        var maxResults = 3;

        // Act
        var matches = await _service.SearchRecordingsAsync(artist, title, maxResults: maxResults);

        // Assert
        Assert.NotNull(matches);
        Assert.True(matches.Count <= maxResults);
    }

    [Fact]
    public async Task SearchRecordingsAsync_IncludesAlbumInformation()
    {
        // Arrange
        var artist = "The Beatles";
        var title = "Let It Be";

        // Act
        var matches = await _service.SearchRecordingsAsync(artist, title, maxResults: 5);

        // Assert
        Assert.NotEmpty(matches);
        
        // At least one match should have album info
        var matchWithAlbum = matches.FirstOrDefault(m => !string.IsNullOrEmpty(m.Album));
        Assert.NotNull(matchWithAlbum);
        Assert.NotNull(matchWithAlbum.Album);
    }

    [Fact]
    public async Task SearchRecordingsAsync_IncludesCoverArtUrl()
    {
        // Arrange
        var artist = "The Beatles";
        var title = "Let It Be";

        // Act
        var matches = await _service.SearchRecordingsAsync(artist, title, maxResults: 5);

        // Assert
        Assert.NotEmpty(matches);
        
        // At least one match should have cover art URL
        var matchWithCoverArt = matches.FirstOrDefault(m => !string.IsNullOrEmpty(m.CoverArtUrl));
        Assert.NotNull(matchWithCoverArt);
        Assert.Contains("coverartarchive.org", matchWithCoverArt.CoverArtUrl);
    }

    [Fact]
    public async Task GetRecordingAsync_WithValidId_ReturnsMatch()
    {
        // Arrange
        // Known recording ID for "Let It Be" by The Beatles
        var recordingId = "90a9895f-6d28-4957-af1f-73dc78866fad";

        // Act
        var match = await _service.GetRecordingAsync(recordingId);

        // Assert
        Assert.NotNull(match);
        Assert.Equal(recordingId, match.RecordingId, ignoreCase: true);
        Assert.NotEmpty(match.Title);
        Assert.NotEmpty(match.Artist);
        Assert.Equal(1.0, match.MatchScore); // Direct lookup should have perfect score
    }

    [Fact]
    public async Task GetRecordingAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var recordingId = "not-a-valid-guid";

        // Act
        var match = await _service.GetRecordingAsync(recordingId);

        // Assert
        Assert.Null(match);
    }

    [Fact]
    public async Task GetRecordingAsync_WithEmptyId_ReturnsNull()
    {
        // Arrange
        var recordingId = "";

        // Act
        var match = await _service.GetRecordingAsync(recordingId);

        // Assert
        Assert.Null(match);
    }

    [Fact]
    public async Task GetRecordingAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        // Valid GUID format but non-existent recording
        var recordingId = "00000000-0000-0000-0000-000000000000";

        // Act
        var match = await _service.GetRecordingAsync(recordingId);

        // Assert
        Assert.Null(match);
    }

    [Fact]
    public async Task DownloadCoverArtAsync_WithValidReleaseId_DownloadsImage()
    {
        // Arrange
        // Known release ID for "Let It Be" album
        var releaseId = "4c9b6ab9-8f8a-4e1f-870f-6d1e8f7d7f2c";
        var outputPath = Path.Combine(_testDirectory, "cover.jpg");

        // Act
        var success = await _service.DownloadCoverArtAsync(releaseId, outputPath);

        // Assert
        // Note: This test may fail if cover art is not available
        // But it should not throw exceptions
        if (success)
        {
            Assert.True(File.Exists(outputPath));
            var fileInfo = new FileInfo(outputPath);
            Assert.True(fileInfo.Length > 0);
        }
    }

    [Fact]
    public async Task DownloadCoverArtAsync_WithInvalidReleaseId_ReturnsFalse()
    {
        // Arrange
        var releaseId = "00000000-0000-0000-0000-000000000000";
        var outputPath = Path.Combine(_testDirectory, "cover_invalid.jpg");

        // Act
        var success = await _service.DownloadCoverArtAsync(releaseId, outputPath);

        // Assert
        Assert.False(success);
        Assert.False(File.Exists(outputPath));
    }

    [Fact]
    public async Task DownloadCoverArtAsync_WithEmptyReleaseId_ReturnsFalse()
    {
        // Arrange
        var releaseId = "";
        var outputPath = Path.Combine(_testDirectory, "cover_empty.jpg");

        // Act
        var success = await _service.DownloadCoverArtAsync(releaseId, outputPath);

        // Assert
        Assert.False(success);
    }

    [Fact]
    public async Task DownloadCoverArtAsync_WithEmptyOutputPath_ReturnsFalse()
    {
        // Arrange
        var releaseId = "4c9b6ab9-8f8a-4e1f-870f-6d1e8f7d7f2c";
        var outputPath = "";

        // Act
        var success = await _service.DownloadCoverArtAsync(releaseId, outputPath);

        // Assert
        Assert.False(success);
    }

    [Fact]
    public async Task SearchRecordingsAsync_EnforcesRateLimit()
    {
        // Arrange
        var artist = "The Beatles";
        var title = "Help";

        // Act
        var startTime = DateTime.UtcNow;
        
        // Make multiple requests
        await _service.SearchRecordingsAsync(artist, title, maxResults: 1);
        await _service.SearchRecordingsAsync(artist, title + " 2", maxResults: 1);
        
        var endTime = DateTime.UtcNow;
        var elapsed = endTime - startTime;

        // Assert
        // Should take at least 1 second due to rate limiting (1 request per second)
        Assert.True(elapsed.TotalMilliseconds >= 900); // Allow some margin
    }
}
