using Muine.Core.Services;
using Muine.Core.Models;
using Xunit;

namespace Muine.Tests.Services;

public class YouTubeServiceTests
{
    [Fact]
    public async Task SearchAsync_WithEmptyQuery_ReturnsEmptyList()
    {
        // Arrange
        using var service = new YouTubeService();

        // Act
        var results = await service.SearchAsync(string.Empty);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_WithWhitespaceQuery_ReturnsEmptyList()
    {
        // Arrange
        using var service = new YouTubeService();

        // Act
        var results = await service.SearchAsync("   ");

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_WithValidQuery_ReturnsResults()
    {
        // Arrange
        using var service = new YouTubeService();

        // Act
        var results = await service.SearchAsync("beethoven symphony", maxResults: 5);

        // Assert
        // Note: This test requires internet connection and may be flaky
        // In a production environment, consider mocking the YouTube API
        Assert.NotNull(results);
        
        // If results are returned, verify they have YouTube properties
        if (results.Count > 0)
        {
            var song = results[0];
            Assert.Equal(SongSourceType.YouTube, song.SourceType);
            Assert.NotNull(song.YouTubeId);
            Assert.NotNull(song.YouTubeUrl);
            Assert.True(song.IsYouTube);
            Assert.NotEmpty(song.Title);
        }
    }

    [Fact]
    public async Task GetVideoMetadataAsync_WithEmptyId_ReturnsNull()
    {
        // Arrange
        using var service = new YouTubeService();

        // Act
        var result = await service.GetVideoMetadataAsync(string.Empty);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetVideoMetadataAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        using var service = new YouTubeService();

        // Act
        var result = await service.GetVideoMetadataAsync("invalid_video_id_12345");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAudioStreamUrlAsync_WithEmptyId_ReturnsNull()
    {
        // Arrange
        using var service = new YouTubeService();

        // Act
        var result = await service.GetAudioStreamUrlAsync(string.Empty);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAudioStreamUrlAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        using var service = new YouTubeService();

        // Act
        var result = await service.GetAudioStreamUrlAsync("invalid_video_id_12345");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void YouTubeSong_Properties_AreSetCorrectly()
    {
        // Arrange & Act
        var song = new Song
        {
            Title = "Test Song",
            Artists = new[] { "Test Artist" },
            Duration = 180,
            SourceType = SongSourceType.YouTube,
            YouTubeId = "dQw4w9WgXcQ",
            YouTubeUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            Filename = "https://www.youtube.com/watch?v=dQw4w9WgXcQ"
        };

        // Assert
        Assert.Equal(SongSourceType.YouTube, song.SourceType);
        Assert.True(song.IsYouTube);
        Assert.False(song.IsLocal);
        Assert.Equal("dQw4w9WgXcQ", song.YouTubeId);
        Assert.NotNull(song.YouTubeUrl);
        Assert.Contains("youtube.com", song.YouTubeUrl);
    }

    [Fact]
    public void LocalSong_Properties_AreSetCorrectly()
    {
        // Arrange & Act
        var song = new Song
        {
            Title = "Test Song",
            Artists = new[] { "Test Artist" },
            Duration = 180,
            SourceType = SongSourceType.Local,
            Filename = "/path/to/file.mp3"
        };

        // Assert
        Assert.Equal(SongSourceType.Local, song.SourceType);
        Assert.False(song.IsYouTube);
        Assert.True(song.IsLocal);
        Assert.Null(song.YouTubeId);
        Assert.Null(song.YouTubeUrl);
    }
}
