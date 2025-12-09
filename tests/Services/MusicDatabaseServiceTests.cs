using Muine.Core.Models;
using Muine.Core.Services;

namespace Muine.Tests.Services;

public class MusicDatabaseServiceTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly MusicDatabaseService _service;

    public MusicDatabaseServiceTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_muine_{Guid.NewGuid()}.db");
        _service = new MusicDatabaseService(_testDbPath);
        _service.InitializeAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task SaveSongAsync_ShouldSaveAndRetrieveSong()
    {
        var song = new Song
        {
            Filename = "/music/test.mp3",
            Title = "Test Song",
            Artists = new[] { "Test Artist" },
            Performers = new[] { "Test Performer" },
            Album = "Test Album",
            TrackNumber = 1,
            Duration = 180
        };

        var id = await _service.SaveSongAsync(song);
        Assert.True(id > 0);

        var songs = await _service.GetAllSongsAsync();
        Assert.Single(songs);
        
        var retrieved = songs[0];
        Assert.Equal(song.Filename, retrieved.Filename);
        Assert.Equal(song.Title, retrieved.Title);
        Assert.Equal(song.Artists[0], retrieved.Artists[0]);
        Assert.Equal(song.Album, retrieved.Album);
        Assert.Equal(song.Duration, retrieved.Duration);
    }

    [Fact]
    public async Task SaveSongAsync_ShouldUpdateExistingSong()
    {
        var song = new Song
        {
            Filename = "/music/update.mp3",
            Title = "Original Title",
            Artists = new[] { "Artist" },
            Duration = 200
        };

        await _service.SaveSongAsync(song);
        
        song.Title = "Updated Title";
        song.Duration = 250;
        await _service.SaveSongAsync(song);

        var songs = await _service.GetAllSongsAsync();
        Assert.Single(songs);
        Assert.Equal("Updated Title", songs[0].Title);
        Assert.Equal(250, songs[0].Duration);
    }

    [Fact]
    public async Task GetAllSongsAsync_ShouldReturnEmptyListWhenNoSongs()
    {
        var songs = await _service.GetAllSongsAsync();
        Assert.Empty(songs);
    }

    public void Dispose()
    {
        _service.Dispose();
        if (System.IO.File.Exists(_testDbPath))
        {
            System.IO.File.Delete(_testDbPath);
        }
    }
}
