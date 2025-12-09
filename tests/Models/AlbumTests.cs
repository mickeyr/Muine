using Muine.Core.Models;

namespace Muine.Tests.Models;

public class AlbumTests
{
    [Fact]
    public void Album_ShouldInitializeWithDefaultValues()
    {
        var album = new Album();
        
        Assert.Equal(0, album.Id);
        Assert.Equal(string.Empty, album.Name);
        Assert.Empty(album.Songs);
        Assert.Empty(album.Artists);
        Assert.Equal(0, album.TrackCount);
        Assert.Equal(0, album.TotalDuration);
    }

    [Fact]
    public void Album_TrackCount_ShouldReturnNumberOfSongs()
    {
        var album = new Album();
        album.Songs.Add(new Song { Title = "Song 1" });
        album.Songs.Add(new Song { Title = "Song 2" });
        
        Assert.Equal(2, album.TrackCount);
    }

    [Fact]
    public void Album_TotalDuration_ShouldSumAllSongDurations()
    {
        var album = new Album();
        album.Songs.Add(new Song { Duration = 180 });
        album.Songs.Add(new Song { Duration = 240 });
        album.Songs.Add(new Song { Duration = 200 });
        
        Assert.Equal(620, album.TotalDuration);
    }

    [Fact]
    public void Album_IsComplete_ShouldReturnTrueWhenAllTracksPresent()
    {
        var album = new Album { TotalTracks = 10 };
        for (int i = 0; i < 10; i++)
        {
            album.Songs.Add(new Song { TrackNumber = i + 1 });
        }
        
        Assert.True(album.IsComplete);
    }

    [Fact]
    public void Album_IsComplete_ShouldReturnFalseWhenTracksMissing()
    {
        var album = new Album { TotalTracks = 10 };
        for (int i = 0; i < 5; i++)
        {
            album.Songs.Add(new Song { TrackNumber = i + 1 });
        }
        
        Assert.False(album.IsComplete);
    }

    [Fact]
    public void Album_IsComplete_ShouldReturnFalseWhenTotalTracksIsZero()
    {
        var album = new Album { TotalTracks = 0 };
        album.Songs.Add(new Song());
        
        Assert.False(album.IsComplete);
    }
}
