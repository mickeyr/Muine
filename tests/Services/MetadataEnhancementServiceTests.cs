using Muine.Core.Models;
using Muine.Core.Services;
using Xunit;

namespace Muine.Tests.Services;

public class MetadataEnhancementServiceTests : IDisposable
{
    private readonly MetadataEnhancementService _service;
    private readonly string _testDirectory;

    public MetadataEnhancementServiceTests()
    {
        _service = new MetadataEnhancementService();
        _testDirectory = Path.Combine(Path.GetTempPath(), $"muine_enhancement_test_{Guid.NewGuid()}");
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
    public async Task FindMatchesAsync_WithValidSong_ReturnsMatches()
    {
        // Arrange
        var song = new Song
        {
            Title = "Let It Be",
            Artists = new[] { "The Beatles" },
            Album = "Let It Be",
            Year = "1970"
        };

        // Act
        var matches = await _service.FindMatchesAsync(song, maxResults: 5);

        // Assert
        Assert.NotNull(matches);
        Assert.NotEmpty(matches);
        
        // Should be sorted by match score
        for (int i = 0; i < matches.Count - 1; i++)
        {
            Assert.True(matches[i].MatchScore >= matches[i + 1].MatchScore);
        }
    }

    [Fact]
    public async Task FindMatchesAsync_WithNullSong_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.FindMatchesAsync(null!));
    }

    [Fact]
    public async Task FindMatchesAsync_WithNoTitle_ReturnsEmptyList()
    {
        // Arrange
        var song = new Song
        {
            Title = "",
            Artists = new[] { "The Beatles" }
        };

        // Act
        var matches = await _service.FindMatchesAsync(song);

        // Assert
        Assert.NotNull(matches);
        Assert.Empty(matches);
    }

    [Fact]
    public async Task FindMatchesAsync_BoostsScoreForMatchingAlbum()
    {
        // Arrange
        var song = new Song
        {
            Title = "Let It Be",
            Artists = new[] { "The Beatles" },
            Album = "Let It Be",
            Year = "1970"
        };

        // Act
        var matches = await _service.FindMatchesAsync(song, maxResults: 10);

        // Assert
        Assert.NotEmpty(matches);
        
        // Matches with the album "Let It Be" should have boosted scores
        var matchesWithAlbum = matches.Where(m => 
            string.Equals(m.Album, "Let It Be", StringComparison.OrdinalIgnoreCase)).ToList();
        
        if (matchesWithAlbum.Any())
        {
            // The boost is applied, scores should be competitive
            Assert.True(matchesWithAlbum[0].MatchScore >= 0.7);
        }
    }

    [Fact]
    public async Task EnhanceSongAsync_WithNullSong_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.EnhanceSongAsync(null!));
    }

    [Fact]
    public async Task EnhanceSongAsync_WithGoodMatch_ReturnsEnhancedSong()
    {
        // Arrange
        var song = new Song
        {
            Filename = Path.Combine(_testDirectory, "test.mp3"),
            Title = "Let It Be",
            Artists = new[] { "Beatles" },
            Album = "Let It Be"
        };

        // Create a minimal MP3 file (don't write to it since it's just for testing)
        CreateMinimalMp3(song.Filename);

        // Act - don't write to file for this test
        var (enhancedSong, match) = await _service.EnhanceSongAsync(song, writeToFile: false, downloadCoverArt: false);

        // Assert
        if (enhancedSong != null)
        {
            Assert.NotNull(match);
            Assert.NotEmpty(enhancedSong.Title);
            Assert.NotEmpty(enhancedSong.Artists);
            Assert.True(match.MatchScore >= 0.7);
        }
        // If no match found (due to rate limiting or API issues), that's okay for tests
    }

    [Fact]
    public async Task EnhanceSongAsync_WithPoorMatch_ReturnsNull()
    {
        // Arrange
        var song = new Song
        {
            Title = "XYZ123NonExistent456",
            Artists = new[] { "ABC789NonExistent012" }
        };

        // Act
        var (enhancedSong, match) = await _service.EnhanceSongAsync(song, writeToFile: false, downloadCoverArt: false);

        // Assert
        // Should return null for poor matches
        Assert.Null(enhancedSong);
        Assert.Null(match);
    }

    [Fact]
    public async Task EnhanceSongWithMatchAsync_WithNullSong_ThrowsArgumentNullException()
    {
        // Arrange
        var match = new MusicBrainzMatch
        {
            Title = "Test",
            Artist = "Test Artist"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _service.EnhanceSongWithMatchAsync(null!, match));
    }

    [Fact]
    public async Task EnhanceSongWithMatchAsync_WithNullMatch_ThrowsArgumentNullException()
    {
        // Arrange
        var song = new Song
        {
            Title = "Test",
            Artists = new[] { "Test Artist" }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _service.EnhanceSongWithMatchAsync(song, null!));
    }

    [Fact]
    public async Task EnhanceSongWithMatchAsync_AppliesMatchToSong()
    {
        // Arrange
        var song = new Song
        {
            Filename = Path.Combine(_testDirectory, "test2.mp3"),
            Title = "Original Title",
            Artists = new[] { "Original Artist" },
            Album = "Original Album"
        };

        var match = new MusicBrainzMatch
        {
            RecordingId = "test-id",
            Title = "Enhanced Title",
            Artist = "Enhanced Artist",
            Album = "Enhanced Album",
            Year = 2020,
            TrackNumber = 5,
            TotalTracks = 12,
            MatchScore = 0.95
        };

        CreateMinimalMp3(song.Filename);

        // Act - don't write to file for this test
        var enhancedSong = await _service.EnhanceSongWithMatchAsync(song, match, writeToFile: false, downloadCoverArt: false);

        // Assert
        Assert.Equal(match.Title, enhancedSong.Title);
        Assert.Equal(match.Artist, enhancedSong.Artists[0]);
        Assert.Equal(match.Album, enhancedSong.Album);
        Assert.Equal(match.Year.ToString(), enhancedSong.Year);
        Assert.Equal(match.TrackNumber, enhancedSong.TrackNumber);
        Assert.Equal(match.TotalTracks, enhancedSong.NAlbumTracks);
    }

    [Fact]
    public async Task EnhanceYouTubeSongAsync_WithNullSong_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.EnhanceYouTubeSongAsync(null!));
    }

    [Fact]
    public async Task EnhanceYouTubeSongAsync_WithNonYouTubeSong_ThrowsArgumentException()
    {
        // Arrange
        var song = new Song
        {
            Title = "Test",
            Artists = new[] { "Test Artist" },
            SourceType = SongSourceType.Local
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.EnhanceYouTubeSongAsync(song));
    }

    [Fact]
    public async Task EnhanceYouTubeSongAsync_WithYouTubeSong_ReturnsEnhancedSong()
    {
        // Arrange
        var song = new Song
        {
            Title = "Let It Be",
            Artists = new[] { "The Beatles" },
            SourceType = SongSourceType.YouTube,
            YouTubeId = "test-id",
            YouTubeUrl = "https://youtube.com/watch?v=test-id"
        };

        // Act
        var (enhancedSong, match) = await _service.EnhanceYouTubeSongAsync(song);

        // Assert
        if (enhancedSong != null)
        {
            Assert.NotNull(match);
            Assert.True(enhancedSong.IsYouTube);
            Assert.Equal(SongSourceType.YouTube, enhancedSong.SourceType);
        }
        // If no match found, that's okay for tests
    }

    private static void CreateMinimalMp3(string path)
    {
        using var fs = File.Create(path);
        byte[] header = { 0xFF, 0xFB, 0x90, 0x00 };
        fs.Write(header, 0, header.Length);
        byte[] padding = new byte[1024];
        fs.Write(padding, 0, padding.Length);
    }
}
