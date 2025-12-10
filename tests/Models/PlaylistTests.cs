using Muine.Core.Models;
using Xunit;

namespace Muine.Tests.Models;

public class PlaylistTests
{
    private static Song CreateTestSong(int id, string title)
    {
        return new Song
        {
            Id = id,
            Title = title,
            Filename = $"/path/to/{title}.mp3"
        };
    }

    [Fact]
    public void Playlist_InitialState_ShouldBeEmpty()
    {
        var playlist = new Playlist();

        Assert.Empty(playlist.Songs);
        Assert.Equal(-1, playlist.CurrentIndex);
        Assert.Null(playlist.CurrentSong);
        Assert.Equal(0, playlist.Count);
        Assert.False(playlist.HasNext);
        Assert.False(playlist.HasPrevious);
    }

    [Fact]
    public void Add_SingleSong_ShouldAddToPlaylist()
    {
        var playlist = new Playlist();
        var song = CreateTestSong(1, "Song1");

        playlist.Add(song);

        Assert.Single(playlist.Songs);
        Assert.Equal(song, playlist.Songs[0]);
        Assert.Equal(1, playlist.Count);
    }

    [Fact]
    public void AddRange_MultipleSongs_ShouldAddAllToPlaylist()
    {
        var playlist = new Playlist();
        var songs = new[]
        {
            CreateTestSong(1, "Song1"),
            CreateTestSong(2, "Song2"),
            CreateTestSong(3, "Song3")
        };

        playlist.AddRange(songs);

        Assert.Equal(3, playlist.Count);
        Assert.Equal(songs[0], playlist.Songs[0]);
        Assert.Equal(songs[1], playlist.Songs[1]);
        Assert.Equal(songs[2], playlist.Songs[2]);
    }

    [Fact]
    public void MoveTo_ValidIndex_ShouldSetCurrentIndex()
    {
        var playlist = new Playlist();
        playlist.AddRange(new[]
        {
            CreateTestSong(1, "Song1"),
            CreateTestSong(2, "Song2"),
            CreateTestSong(3, "Song3")
        });

        playlist.MoveTo(1);

        Assert.Equal(1, playlist.CurrentIndex);
        Assert.Equal("Song2", playlist.CurrentSong?.Title);
    }

    [Fact]
    public void Next_WhenHasNext_ShouldMoveToNextSong()
    {
        var playlist = new Playlist();
        playlist.AddRange(new[]
        {
            CreateTestSong(1, "Song1"),
            CreateTestSong(2, "Song2"),
            CreateTestSong(3, "Song3")
        });
        playlist.MoveTo(0);

        var nextSong = playlist.Next();

        Assert.NotNull(nextSong);
        Assert.Equal("Song2", nextSong.Title);
        Assert.Equal(1, playlist.CurrentIndex);
    }

    [Fact]
    public void Next_WhenAtEnd_ShouldReturnNull()
    {
        var playlist = new Playlist();
        playlist.AddRange(new[]
        {
            CreateTestSong(1, "Song1"),
            CreateTestSong(2, "Song2")
        });
        playlist.MoveTo(1); // Last song

        var nextSong = playlist.Next();

        Assert.Null(nextSong);
        Assert.Equal(1, playlist.CurrentIndex); // Index should not change
    }

    [Fact]
    public void Previous_WhenHasPrevious_ShouldMoveToPreviousSong()
    {
        var playlist = new Playlist();
        playlist.AddRange(new[]
        {
            CreateTestSong(1, "Song1"),
            CreateTestSong(2, "Song2"),
            CreateTestSong(3, "Song3")
        });
        playlist.MoveTo(2);

        var prevSong = playlist.Previous();

        Assert.NotNull(prevSong);
        Assert.Equal("Song2", prevSong.Title);
        Assert.Equal(1, playlist.CurrentIndex);
    }

    [Fact]
    public void Previous_WhenAtStart_ShouldReturnNull()
    {
        var playlist = new Playlist();
        playlist.AddRange(new[]
        {
            CreateTestSong(1, "Song1"),
            CreateTestSong(2, "Song2")
        });
        playlist.MoveTo(0); // First song

        var prevSong = playlist.Previous();

        Assert.Null(prevSong);
        Assert.Equal(0, playlist.CurrentIndex); // Index should not change
    }

    [Fact]
    public void Remove_ExistingSong_ShouldRemoveAndAdjustIndex()
    {
        var playlist = new Playlist();
        var songs = new[]
        {
            CreateTestSong(1, "Song1"),
            CreateTestSong(2, "Song2"),
            CreateTestSong(3, "Song3")
        };
        playlist.AddRange(songs);
        playlist.MoveTo(2); // Set to Song3

        playlist.Remove(songs[1]); // Remove Song2

        Assert.Equal(2, playlist.Count);
        Assert.Equal(1, playlist.CurrentIndex); // Should adjust from 2 to 1
        Assert.Equal("Song3", playlist.CurrentSong?.Title);
    }

    [Fact]
    public void RemoveAt_ValidIndex_ShouldRemoveAndAdjustCurrentIndex()
    {
        var playlist = new Playlist();
        playlist.AddRange(new[]
        {
            CreateTestSong(1, "Song1"),
            CreateTestSong(2, "Song2"),
            CreateTestSong(3, "Song3")
        });
        playlist.MoveTo(2);

        playlist.RemoveAt(0); // Remove first song

        Assert.Equal(2, playlist.Count);
        Assert.Equal(1, playlist.CurrentIndex); // Adjusted from 2 to 1
    }

    [Fact]
    public void Clear_ShouldResetPlaylist()
    {
        var playlist = new Playlist();
        playlist.AddRange(new[]
        {
            CreateTestSong(1, "Song1"),
            CreateTestSong(2, "Song2")
        });
        playlist.MoveTo(1);

        playlist.Clear();

        Assert.Empty(playlist.Songs);
        Assert.Equal(-1, playlist.CurrentIndex);
        Assert.Null(playlist.CurrentSong);
    }

    [Fact]
    public void Move_ValidIndices_ShouldReorderAndAdjustCurrentIndex()
    {
        var playlist = new Playlist();
        var songs = new[]
        {
            CreateTestSong(1, "Song1"),
            CreateTestSong(2, "Song2"),
            CreateTestSong(3, "Song3")
        };
        playlist.AddRange(songs);
        playlist.MoveTo(1); // Current is Song2

        playlist.Move(1, 2); // Move Song2 to end

        Assert.Equal("Song1", playlist.Songs[0].Title);
        Assert.Equal("Song3", playlist.Songs[1].Title);
        Assert.Equal("Song2", playlist.Songs[2].Title);
        Assert.Equal(2, playlist.CurrentIndex); // Current index adjusted
        Assert.Equal("Song2", playlist.CurrentSong?.Title);
    }

    [Fact]
    public void HasNext_WhenNotAtEnd_ShouldReturnTrue()
    {
        var playlist = new Playlist();
        playlist.AddRange(new[]
        {
            CreateTestSong(1, "Song1"),
            CreateTestSong(2, "Song2")
        });
        playlist.MoveTo(0);

        Assert.True(playlist.HasNext);
    }

    [Fact]
    public void HasPrevious_WhenNotAtStart_ShouldReturnTrue()
    {
        var playlist = new Playlist();
        playlist.AddRange(new[]
        {
            CreateTestSong(1, "Song1"),
            CreateTestSong(2, "Song2")
        });
        playlist.MoveTo(1);

        Assert.True(playlist.HasPrevious);
    }

    [Fact]
    public void CurrentSong_AfterMoveTo_ShouldReturnCorrectSong()
    {
        var playlist = new Playlist();
        var songs = new[]
        {
            CreateTestSong(1, "Song1"),
            CreateTestSong(2, "Song2"),
            CreateTestSong(3, "Song3")
        };
        playlist.AddRange(songs);

        playlist.MoveTo(1);
        Assert.Equal("Song2", playlist.CurrentSong?.Title);

        playlist.MoveTo(0);
        Assert.Equal("Song1", playlist.CurrentSong?.Title);

        playlist.MoveTo(2);
        Assert.Equal("Song3", playlist.CurrentSong?.Title);
    }

    [Fact]
    public void CurrentSong_WhenIndexNegative_ShouldReturnNull()
    {
        var playlist = new Playlist();
        playlist.AddRange(new[]
        {
            CreateTestSong(1, "Song1"),
            CreateTestSong(2, "Song2")
        });

        Assert.Null(playlist.CurrentSong);
    }

    [Fact]
    public void PlaybackScenario_PlayStopPlayAgain_ShouldResumeFromCurrentPosition()
    {
        // This test simulates the user scenario:
        // 1. Add songs to playlist
        // 2. Start playing (current index set)
        // 3. Stop playback
        // 4. Resume playback - should continue from current position
        
        var playlist = new Playlist();
        playlist.AddRange(new[]
        {
            CreateTestSong(1, "Song1"),
            CreateTestSong(2, "Song2"),
            CreateTestSong(3, "Song3")
        });

        // Start playback from beginning
        playlist.MoveTo(0);
        Assert.Equal("Song1", playlist.CurrentSong?.Title);

        // Play next song
        var song2 = playlist.Next();
        Assert.Equal("Song2", song2?.Title);

        // Simulate stop - index should remain at current position
        Assert.Equal(1, playlist.CurrentIndex);
        Assert.Equal("Song2", playlist.CurrentSong?.Title);

        // Resume playback - should get current song (Song2)
        var resumedSong = playlist.CurrentSong;
        Assert.Equal("Song2", resumedSong?.Title);
    }

    [Fact]
    public void PlaybackScenario_EmptyPlaylistStartFromBeginning_ShouldWorkCorrectly()
    {
        var playlist = new Playlist();
        
        // Initially empty
        Assert.Equal(-1, playlist.CurrentIndex);
        Assert.Null(playlist.CurrentSong);

        // Add songs
        playlist.AddRange(new[]
        {
            CreateTestSong(1, "Song1"),
            CreateTestSong(2, "Song2")
        });

        // Start from beginning when current index is -1
        if (playlist.CurrentSong == null && playlist.Count > 0)
        {
            playlist.MoveTo(0);
        }

        Assert.Equal(0, playlist.CurrentIndex);
        Assert.Equal("Song1", playlist.CurrentSong?.Title);
    }
}
