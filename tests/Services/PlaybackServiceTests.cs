using Muine.Core.Models;
using Muine.Core.Services;
using Xunit;

namespace Muine.Tests.Services;

public class PlaybackServiceTests : IDisposable
{
    private readonly PlaybackService _playbackService;
    private readonly string _testDirectory;

    public PlaybackServiceTests()
    {
        _playbackService = new PlaybackService();
        _testDirectory = Path.Combine(Path.GetTempPath(), $"muine_playback_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        _playbackService?.Dispose();
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void PlaybackService_InitialState_IsStopped()
    {
        // Assert
        Assert.Equal(PlaybackState.Stopped, _playbackService.State);
        Assert.Null(_playbackService.CurrentSong);
    }

    [Fact]
    public void PlaybackService_VolumeProperty_CanBeSetAndRetrieved()
    {
        // Act
        _playbackService.Volume = 75f;

        // Assert
        Assert.Equal(75f, _playbackService.Volume);
    }

    [Fact]
    public void PlaybackService_Position_InitiallyZero()
    {
        // Assert
        Assert.Equal(TimeSpan.Zero, _playbackService.Position);
        Assert.Equal(TimeSpan.Zero, _playbackService.Duration);
    }

    [Fact]
    public async Task PlayAsync_WithNullSong_ThrowsArgumentNullException()
    {
        if (!_playbackService.IsLibVLCAvailable)
        {
            // If LibVLC is not available, we expect InvalidOperationException instead
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _playbackService.PlayAsync(null!);
            });
            return;
        }

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await _playbackService.PlayAsync(null!);
        });
    }

    [Fact]
    public async Task PlayAsync_WithNonExistentFile_ThrowsException()
    {
        // Arrange
        var song = new Song
        {
            Filename = Path.Combine(_testDirectory, "nonexistent.mp3"),
            Title = "Test Song"
        };

        if (!_playbackService.IsLibVLCAvailable)
        {
            // If LibVLC is not available, we expect InvalidOperationException
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _playbackService.PlayAsync(song);
            });
            return;
        }

        // Act & Assert - With LibVLC available, we expect FileNotFoundException
        await Assert.ThrowsAsync<FileNotFoundException>(async () =>
        {
            await _playbackService.PlayAsync(song);
        });
    }

    [Fact]
    public void TogglePlayPause_WithNoSongLoaded_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            _playbackService.TogglePlayPause();
        });
    }

    [Fact]
    public void Stop_WithNoSongLoaded_HandlesGracefully()
    {
        if (!_playbackService.IsLibVLCAvailable)
        {
            // If LibVLC is not available, Stop should throw
            Assert.Throws<InvalidOperationException>(() =>
            {
                _playbackService.Stop();
            });
            return;
        }

        // Act & Assert - Should not throw if LibVLC is available
        _playbackService.Stop();
        Assert.Equal(PlaybackState.Stopped, _playbackService.State);
    }

    [Fact]
    public void Seek_WithNoSongLoaded_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            _playbackService.Seek(TimeSpan.FromSeconds(10));
        });
    }

    private static void CreateMinimalMp3(string path)
    {
        // Create a minimal valid MP3 frame
        using var fs = File.Create(path);
        
        // MP3 frame sync word
        byte[] header = { 0xFF, 0xFB, 0x90, 0x00 };
        fs.Write(header, 0, header.Length);
        
        // Add some padding
        byte[] padding = new byte[1024];
        fs.Write(padding, 0, padding.Length);
    }
}
