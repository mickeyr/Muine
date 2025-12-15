using System;
using System.Threading;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using Muine.Core.Models;

namespace Muine.Core.Services;

public enum PlaybackState
{
    Stopped,
    Playing,
    Paused
}

public class PlaybackService : IDisposable
{
    private readonly LibVLC? _libVLC;
    private readonly MediaPlayer? _mediaPlayer;
    private Song? _currentSong;
    private Timer? _positionTimer;
    private bool _disposed;
    private float _volume = 50f;

    public event EventHandler<PlaybackState>? StateChanged;
    public event EventHandler<TimeSpan>? PositionChanged;
    public event EventHandler<Song?>? CurrentSongChanged;

    public PlaybackState State { get; private set; } = PlaybackState.Stopped;
    public Song? CurrentSong => _currentSong;
    public TimeSpan Position { get; private set; }
    public TimeSpan Duration { get; private set; }
    public float Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0, 100);
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Volume = (int)_volume;
            }
        }
    }

    public bool IsLibVLCAvailable => _mediaPlayer != null;

    public PlaybackService()
    {
        try
        {
            LibVLCSharp.Shared.Core.Initialize();
            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC);
            _mediaPlayer.Playing += OnMediaPlayerPlaying;
            _mediaPlayer.Paused += OnMediaPlayerPaused;
            _mediaPlayer.Stopped += OnMediaPlayerStopped;
            _mediaPlayer.EndReached += OnMediaPlayerEndReached;

            // Start position update timer
            _positionTimer = new Timer(UpdatePosition, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not initialize LibVLC: {ex.Message}");
            Console.WriteLine("Playback functionality will not be available.");
        }
    }

    private void UpdatePosition(object? state)
    {
        if (_mediaPlayer == null || State != PlaybackState.Playing)
            return;

        try
        {
            var positionMs = _mediaPlayer.Time;
            var durationMs = _mediaPlayer.Length;

            if (durationMs > 0)
            {
                Position = TimeSpan.FromMilliseconds(positionMs);
                Duration = TimeSpan.FromMilliseconds(durationMs);
                PositionChanged?.Invoke(this, Position);
            }
        }
        catch
        {
            // Ignore errors during position updates
        }
    }

    public async Task PlayAsync(Song song)
    {
        if (_mediaPlayer == null)
        {
            throw new InvalidOperationException("Media player not initialized. LibVLC may not be available on this system.");
        }

        ArgumentNullException.ThrowIfNull(song);

        if (!File.Exists(song.Filename))
        {
            throw new FileNotFoundException($"Audio file not found: {song.Filename}");
        }

        await Task.Run(() =>
        {
            Stop();

            var media = new Media(_libVLC!, song.Filename, FromType.FromPath);
            _mediaPlayer.Media = media;

            // Apply ReplayGain if available
            if (song.Gain != 0.0)
            {
                var gainDb = song.Gain;
                // LibVLC volume is 0-200, where 100 is normal
                // ReplayGain is in dB, typical range is -15 to +15 dB
                // Convert dB to linear: 10^(dB/20)
                var linearGain = Math.Pow(10, gainDb / 20.0);
                var newVolume = (int)(100 * linearGain);
                _mediaPlayer.Volume = Math.Clamp(newVolume, 0, 200);
            }

            _currentSong = song;
            CurrentSongChanged?.Invoke(this, _currentSong);

            _mediaPlayer.Play();
        });
    }

    public void Play()
    {
        if (_mediaPlayer == null)
        {
            throw new InvalidOperationException("Media player not initialized.");
        }

        if (_currentSong == null)
        {
            throw new InvalidOperationException("No song loaded. Call PlayAsync first.");
        }

        _mediaPlayer.Play();
    }

    public void Pause()
    {
        if (_mediaPlayer == null)
        {
            throw new InvalidOperationException("Media player not initialized.");
        }

        _mediaPlayer.Pause();
    }

    public void Stop()
    {
        if (_mediaPlayer == null)
        {
            throw new InvalidOperationException("Media player not initialized.");
        }

        _mediaPlayer.Stop();
        Position = TimeSpan.Zero;
        Duration = TimeSpan.Zero;
        PositionChanged?.Invoke(this, Position);
    }

    public void TogglePlayPause()
    {
        if (_mediaPlayer == null)
        {
            throw new InvalidOperationException("Media player not initialized.");
        }

        if (_currentSong == null)
        {
            throw new InvalidOperationException("No song loaded.");
        }

        if (State == PlaybackState.Playing)
        {
            Pause();
        }
        else if (State == PlaybackState.Paused)
        {
            Play();
        }
    }

    public void Seek(TimeSpan position)
    {
        if (_mediaPlayer == null)
        {
            throw new InvalidOperationException("Media player not initialized.");
        }

        if (_currentSong == null)
        {
            throw new InvalidOperationException("No song loaded.");
        }

        _mediaPlayer.Time = (long)position.TotalMilliseconds;
        Position = position;
        PositionChanged?.Invoke(this, Position);
    }

    private void OnMediaPlayerPlaying(object? sender, EventArgs e)
    {
        State = PlaybackState.Playing;
        StateChanged?.Invoke(this, State);
    }

    private void OnMediaPlayerPaused(object? sender, EventArgs e)
    {
        State = PlaybackState.Paused;
        StateChanged?.Invoke(this, State);
    }

    private void OnMediaPlayerStopped(object? sender, EventArgs e)
    {
        State = PlaybackState.Stopped;
        StateChanged?.Invoke(this, State);
    }

    private void OnMediaPlayerEndReached(object? sender, EventArgs e)
    {
        State = PlaybackState.Stopped;
        Position = TimeSpan.Zero;
        StateChanged?.Invoke(this, State);
        PositionChanged?.Invoke(this, Position);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _positionTimer?.Dispose();
        _mediaPlayer?.Dispose();
        _libVLC?.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
