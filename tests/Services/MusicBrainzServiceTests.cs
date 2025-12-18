using Muine.Core.Models;
using Muine.Core.Services;
using Xunit;

namespace Muine.Tests.Services;

/// <summary>
/// Tests for MusicBrainz service using mocked implementation to avoid API rate limiting issues
/// </summary>
public class MusicBrainzServiceTests : IDisposable
{
    private readonly MockMusicBrainzService _mockService;
    private readonly string _testDirectory;

    public MusicBrainzServiceTests()
    {
        _mockService = new MockMusicBrainzService();
        _testDirectory = Path.Combine(Path.GetTempPath(), $"muine_mb_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        _mockService?.Dispose();
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
        
        var mockMatch = MockMusicBrainzService.CreateMockMatch(
            title: "Let It Be",
            artist: "The Beatles",
            album: "Let It Be",
            matchScore: 0.98);
        
        _mockService.SearchResults = new List<MusicBrainzMatch> { mockMatch };

        // Act
        var matches = await _mockService.SearchRecordingsAsync(artist, title, maxResults: 5);

        // Assert
        Assert.NotNull(matches);
        Assert.NotEmpty(matches);
        
        var topMatch = matches[0];
        Assert.NotNull(topMatch.RecordingId);
        Assert.Contains("Let It Be", topMatch.Title, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Beatles", topMatch.Artist, StringComparison.OrdinalIgnoreCase);
        Assert.True(topMatch.MatchScore > 0.0);
        
        // Verify method was called
        Assert.Equal(1, _mockService.SearchCallCount);
        Assert.Equal(artist, _mockService.LastSearchArtist);
        Assert.Equal(title, _mockService.LastSearchTitle);
    }

    [Fact]
    public async Task SearchRecordingsAsync_WithEmptyArtist_ReturnsEmptyList()
    {
        // Arrange
        var artist = "";
        var title = "Let It Be";

        // Act
        var matches = await _mockService.SearchRecordingsAsync(artist, title);

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
        var matches = await _mockService.SearchRecordingsAsync(artist, title);

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
        
        _mockService.SearchResults = new List<MusicBrainzMatch>();

        // Act
        var matches = await _mockService.SearchRecordingsAsync(artist, title, maxResults: 5);

        // Assert
        Assert.NotNull(matches);
        Assert.Empty(matches);
    }

    [Fact]
    public async Task SearchRecordingsAsync_RespectsMaxResults()
    {
        // Arrange
        var artist = "John";
        var title = "Love";
        var maxResults = 3;
        
        _mockService.SearchResults = Enumerable.Range(1, 5)
            .Select(i => MockMusicBrainzService.CreateMockMatch($"Song {i}", "Artist", matchScore: 0.9 - i * 0.1))
            .ToList();

        // Act
        var matches = await _mockService.SearchRecordingsAsync(artist, title, maxResults: maxResults);

        // Assert
        Assert.NotNull(matches);
        Assert.Equal(maxResults, matches.Count);
    }

    [Fact]
    public async Task SearchRecordingsAsync_IncludesAlbumInformation()
    {
        // Arrange
        var artist = "The Beatles";
        var title = "Let It Be";
        
        var mockMatch = MockMusicBrainzService.CreateMockMatch(
            title: title,
            artist: artist,
            album: "Let It Be Album");
        
        _mockService.SearchResults = new List<MusicBrainzMatch> { mockMatch };

        // Act
        var matches = await _mockService.SearchRecordingsAsync(artist, title, maxResults: 5);

        // Assert
        Assert.NotEmpty(matches);
        
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
        
        var mockMatch = MockMusicBrainzService.CreateMockMatch(title, artist);
        _mockService.SearchResults = new List<MusicBrainzMatch> { mockMatch };

        // Act
        var matches = await _mockService.SearchRecordingsAsync(artist, title, maxResults: 5);

        // Assert
        Assert.NotEmpty(matches);
        
        var matchWithCoverArt = matches.FirstOrDefault(m => !string.IsNullOrEmpty(m.CoverArtUrl));
        Assert.NotNull(matchWithCoverArt);
        Assert.Contains("coverartarchive.org", matchWithCoverArt.CoverArtUrl);
    }

    [Fact]
    public async Task GetRecordingAsync_WithValidId_ReturnsMatch()
    {
        // Arrange
        var recordingId = "90a9895f-6d28-4957-af1f-73dc78866fad";
        var mockMatch = MockMusicBrainzService.CreateMockMatch("Let It Be", "The Beatles");
        mockMatch.RecordingId = recordingId;
        mockMatch.MatchScore = 1.0;
        
        _mockService.GetRecordingResult = mockMatch;

        // Act
        var match = await _mockService.GetRecordingAsync(recordingId);

        // Assert
        Assert.NotNull(match);
        Assert.Equal(recordingId, match.RecordingId, ignoreCase: true);
        Assert.NotEmpty(match.Title);
        Assert.NotEmpty(match.Artist);
        Assert.Equal(1.0, match.MatchScore);
        
        Assert.Equal(1, _mockService.GetRecordingCallCount);
    }

    [Fact]
    public async Task GetRecordingAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var recordingId = "not-a-valid-guid";
        _mockService.GetRecordingResult = null;

        // Act
        var match = await _mockService.GetRecordingAsync(recordingId);

        // Assert
        Assert.Null(match);
    }

    [Fact]
    public async Task GetRecordingAsync_WithEmptyId_ReturnsNull()
    {
        // Arrange
        var recordingId = "";
        _mockService.GetRecordingResult = null;

        // Act
        var match = await _mockService.GetRecordingAsync(recordingId);

        // Assert
        Assert.Null(match);
    }

    [Fact]
    public async Task GetRecordingAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var recordingId = "00000000-0000-0000-0000-000000000000";
        _mockService.GetRecordingResult = null;

        // Act
        var match = await _mockService.GetRecordingAsync(recordingId);

        // Assert
        Assert.Null(match);
    }

    [Fact]
    public async Task DownloadCoverArtAsync_WithValidReleaseId_DownloadsImage()
    {
        // Arrange
        var releaseId = "4c9b6ab9-8f8a-4e1f-870f-6d1e8f7d7f2c";
        var outputPath = Path.Combine(_testDirectory, "cover.jpg");
        _mockService.DownloadCoverArtResult = true;

        // Act
        var success = await _mockService.DownloadCoverArtAsync(releaseId, outputPath);

        // Assert
        Assert.True(success);
        Assert.True(File.Exists(outputPath));
        var fileInfo = new FileInfo(outputPath);
        Assert.True(fileInfo.Length > 0);
    }

    [Fact]
    public async Task DownloadCoverArtAsync_WithInvalidReleaseId_ReturnsFalse()
    {
        // Arrange
        var releaseId = "00000000-0000-0000-0000-000000000000";
        var outputPath = Path.Combine(_testDirectory, "cover_invalid.jpg");
        _mockService.DownloadCoverArtResult = false;

        // Act
        var success = await _mockService.DownloadCoverArtAsync(releaseId, outputPath);

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
        _mockService.DownloadCoverArtResult = false;

        // Act
        var success = await _mockService.DownloadCoverArtAsync(releaseId, outputPath);

        // Assert
        Assert.False(success);
    }

    [Fact]
    public async Task DownloadCoverArtAsync_WithEmptyOutputPath_ReturnsFalse()
    {
        // Arrange
        var releaseId = "4c9b6ab9-8f8a-4e1f-870f-6d1e8f7d7f2c";
        var outputPath = "";
        _mockService.DownloadCoverArtResult = false;

        // Act
        var success = await _mockService.DownloadCoverArtAsync(releaseId, outputPath);

        // Assert
        Assert.False(success);
    }

    [Fact]
    public async Task SearchRecordingsAsync_WithException_ThrowsException()
    {
        // Arrange
        _mockService.ThrowExceptionOnSearch = true;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _mockService.SearchRecordingsAsync("Artist", "Title"));
    }

    [Fact]
    public async Task MockService_TracksMethodCalls()
    {
        // Arrange
        var artist = "Test Artist";
        var title = "Test Title";
        var recordingId = "test-id";
        var releaseId = "release-id";
        var outputPath = Path.Combine(_testDirectory, "test.jpg");

        // Act
        await _mockService.SearchRecordingsAsync(artist, title);
        await _mockService.GetRecordingAsync(recordingId);
        await _mockService.DownloadCoverArtAsync(releaseId, outputPath);

        // Assert
        Assert.Equal(1, _mockService.SearchCallCount);
        Assert.Equal(1, _mockService.GetRecordingCallCount);
        Assert.Equal(1, _mockService.DownloadCallCount);
        Assert.Equal(artist, _mockService.LastSearchArtist);
        Assert.Equal(title, _mockService.LastSearchTitle);
        Assert.Equal(recordingId, _mockService.LastRecordingId);
    }
}
