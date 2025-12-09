using Muine.Core.Models;
using Muine.Core.Services;
using Xunit;

namespace Muine.Tests.Services;

public class CoverArtServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly CoverArtService _coverArtService;

    public CoverArtServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"muine_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        _coverArtService = new CoverArtService();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void FindCoverArt_WithCoverJpg_ShouldReturnPath()
    {
        // Arrange
        var coverPath = Path.Combine(_testDirectory, "cover.jpg");
        File.WriteAllText(coverPath, "fake image");
        
        var song = new Song { Filename = Path.Combine(_testDirectory, "song.mp3") };

        // Act
        var result = _coverArtService.FindCoverArt(song);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(coverPath, result);
    }

    [Fact]
    public void FindCoverArt_WithFolderJpg_ShouldReturnPath()
    {
        // Arrange
        var coverPath = Path.Combine(_testDirectory, "folder.jpg");
        File.WriteAllText(coverPath, "fake image");
        
        var song = new Song { Filename = Path.Combine(_testDirectory, "song.mp3") };

        // Act
        var result = _coverArtService.FindCoverArt(song);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(coverPath, result);
    }

    [Fact]
    public void FindCoverArt_WithMultipleCoverFiles_ShouldReturnFirstMatch()
    {
        // Arrange
        var coverJpgPath = Path.Combine(_testDirectory, "cover.jpg");
        var folderJpgPath = Path.Combine(_testDirectory, "folder.jpg");
        File.WriteAllText(coverJpgPath, "fake image 1");
        File.WriteAllText(folderJpgPath, "fake image 2");
        
        var song = new Song { Filename = Path.Combine(_testDirectory, "song.mp3") };

        // Act
        var result = _coverArtService.FindCoverArt(song);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(coverJpgPath, result); // cover.jpg comes first in the search list
    }

    [Fact]
    public void FindCoverArt_WithNoCoverFiles_ShouldReturnNull()
    {
        // Arrange
        var song = new Song { Filename = Path.Combine(_testDirectory, "song.mp3") };

        // Act
        var result = _coverArtService.FindCoverArt(song);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FindCoverArt_WithRandomImageFile_ShouldReturnPath()
    {
        // Arrange
        var imagePath = Path.Combine(_testDirectory, "random-image.png");
        File.WriteAllText(imagePath, "fake image");
        
        var song = new Song { Filename = Path.Combine(_testDirectory, "song.mp3") };

        // Act
        var result = _coverArtService.FindCoverArt(song);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(imagePath, result);
    }

    [Fact]
    public void FindCoverArt_ForAlbum_WithCoverJpg_ShouldReturnPath()
    {
        // Arrange
        var coverPath = Path.Combine(_testDirectory, "cover.jpg");
        File.WriteAllText(coverPath, "fake image");
        
        var album = new Album { Folder = _testDirectory };

        // Act
        var result = _coverArtService.FindCoverArt(album);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(coverPath, result);
    }

    [Fact]
    public void UpdateSongCoverArt_ShouldSetCoverImagePath()
    {
        // Arrange
        var coverPath = Path.Combine(_testDirectory, "cover.jpg");
        File.WriteAllText(coverPath, "fake image");
        
        var song = new Song { Filename = Path.Combine(_testDirectory, "song.mp3") };

        // Act
        _coverArtService.UpdateSongCoverArt(song);

        // Assert
        Assert.NotNull(song.CoverImagePath);
        Assert.Equal(coverPath, song.CoverImagePath);
    }

    [Fact]
    public void UpdateAlbumCoverArt_ShouldSetCoverImagePath()
    {
        // Arrange
        var coverPath = Path.Combine(_testDirectory, "cover.jpg");
        File.WriteAllText(coverPath, "fake image");
        
        var album = new Album { Folder = _testDirectory };

        // Act
        _coverArtService.UpdateAlbumCoverArt(album);

        // Assert
        Assert.NotNull(album.CoverImagePath);
        Assert.Equal(coverPath, album.CoverImagePath);
    }

    [Fact]
    public void FindCoverArt_WithEmptyFolder_ShouldReturnNull()
    {
        // Arrange
        var song = new Song { Filename = "" };

        // Act
        var result = _coverArtService.FindCoverArt(song);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FindCoverArt_WithNonExistentDirectory_ShouldReturnNull()
    {
        // Arrange
        var song = new Song { Filename = "/non/existent/path/song.mp3" };

        // Act
        var result = _coverArtService.FindCoverArt(song);

        // Assert
        Assert.Null(result);
    }
}
