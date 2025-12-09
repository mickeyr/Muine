using Muine.Core.Models;

namespace Muine.Tests.Models;

public class SongTests
{
    [Fact]
    public void Song_ShouldInitializeWithDefaultValues()
    {
        var song = new Song();
        
        Assert.Equal(0, song.Id);
        Assert.Equal(string.Empty, song.Filename);
        Assert.Equal(string.Empty, song.Title);
        Assert.Empty(song.Artists);
        Assert.Empty(song.Performers);
        Assert.Equal(string.Empty, song.Album);
        Assert.Equal(0, song.TrackNumber);
        Assert.Equal(0, song.Duration);
    }

    [Fact]
    public void Song_HasAlbum_ShouldReturnTrueWhenAlbumIsSet()
    {
        var song = new Song { Album = "Test Album" };
        
        Assert.True(song.HasAlbum);
    }

    [Fact]
    public void Song_HasAlbum_ShouldReturnFalseWhenAlbumIsEmpty()
    {
        var song = new Song { Album = "" };
        
        Assert.False(song.HasAlbum);
    }

    [Fact]
    public void Song_Folder_ShouldReturnDirectoryPath()
    {
        var song = new Song { Filename = "/home/music/artist/album/song.mp3" };
        
        Assert.Equal("/home/music/artist/album", song.Folder);
    }

    [Fact]
    public void Song_AlbumKey_ShouldCombineFolderAndAlbum()
    {
        var song = new Song 
        { 
            Filename = "/home/music/artist/album/song.mp3",
            Album = "Test Album"
        };
        
        Assert.Equal("/home/music/artist/album|Test Album", song.AlbumKey);
    }

    [Fact]
    public void Song_ArtistsString_ShouldJoinMultipleArtists()
    {
        var song = new Song 
        { 
            Artists = new[] { "Artist 1", "Artist 2", "Artist 3" }
        };
        
        Assert.Equal("Artist 1, Artist 2, Artist 3", song.ArtistsString);
    }

    [Fact]
    public void Song_DisplayName_ShouldUseTitleWhenAvailable()
    {
        var song = new Song 
        { 
            Filename = "/home/music/song.mp3",
            Title = "My Song"
        };
        
        Assert.Equal("My Song", song.DisplayName);
    }

    [Fact]
    public void Song_DisplayName_ShouldUseFilenameWhenTitleIsEmpty()
    {
        var song = new Song 
        { 
            Filename = "/home/music/song.mp3",
            Title = ""
        };
        
        Assert.Equal("song", song.DisplayName);
    }
}
