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
    private readonly YouTubeService? _youtubeService;
    private Song? _currentSong;
    private RadioStation? _currentRadioStation;
    private Timer? _positionTimer;
    private bool _disposed;
    private float _volume = 50f;

    public event EventHandler<PlaybackState>? StateChanged;
    public event EventHandler<TimeSpan>? PositionChanged;
    public event EventHandler<Song?>? CurrentSongChanged;
    public event EventHandler<RadioStation?>? CurrentRadioStationChanged;

    public PlaybackState State { get; private set; } = PlaybackState.Stopped;
    public Song? CurrentSong => _currentSong;
    public RadioStation? CurrentRadioStation => _currentRadioStation;
    public bool IsPlayingRadio => _currentRadioStation != null;
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
            
            // Initialize YouTube service for streaming
            _youtubeService = new YouTubeService();
        }
        catch (Exception)
        {
            // LibVLC not available - playback functionality will not work
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

        // For YouTube songs, we need to download the audio first
        if (song.IsYouTube && !string.IsNullOrEmpty(song.YouTubeId))
        {
            if (_youtubeService == null)
            {
                var error = "YouTube service not initialized.";
                LoggingService.Error(error, null, "Playback");
                throw new InvalidOperationException(error);
            }
            
            // Check if we already have a downloaded file
            var youtubeAudioDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Muine",
                "YouTubeAudio"
            );
            
            var audioFilePath = Path.Combine(youtubeAudioDir, $"{song.YouTubeId}.ogg");
            
            // Download if not already cached
            if (!File.Exists(audioFilePath))
            {
                LoggingService.Info($"Downloading audio for YouTube video: {song.YouTubeId} - {song.Title}", "Playback");
                
                var downloadSuccess = await _youtubeService.DownloadAudioAsync(song.YouTubeId, audioFilePath);
                if (!downloadSuccess || !File.Exists(audioFilePath))
                {
                    var error = $"Failed to download audio for YouTube video: {song.YouTubeId}";
                    LoggingService.Error(error, null, "Playback");
                    throw new InvalidOperationException(error);
                }
                
                LoggingService.Info($"Successfully downloaded audio to: {audioFilePath}", "Playback");
            }
            else
            {
                LoggingService.Info($"Using cached audio file: {audioFilePath}", "Playback");
            }

            // Play the downloaded file like a local file
            await Task.Run(() =>
            {
                Stop();

                var media = new Media(_libVLC!, audioFilePath, FromType.FromPath);
                _mediaPlayer.Media = media;
                _mediaPlayer.Volume = (int)_volume;

                _currentSong = song;
                _currentRadioStation = null;
                CurrentSongChanged?.Invoke(this, _currentSong);
                CurrentRadioStationChanged?.Invoke(this, null);

                var playResult = _mediaPlayer.Play();
                LoggingService.Info($"MediaPlayer.Play() returned: {playResult} for YouTube song: {song.Title}", "Playback");
                
                // Log media state after delays to see progression
                Task.Delay(1000).ContinueWith(_ =>
                {
                    try
                    {
                        LoggingService.Info($"[1s] Media state: {_mediaPlayer.Media?.State}, Player state: {_mediaPlayer.State}", "Playback");
                        LoggingService.Info($"[1s] Duration: {_mediaPlayer.Media?.Duration} ms, Position: {_mediaPlayer.Position}, Volume: {_mediaPlayer.Volume}", "Playback");
                        LoggingService.Info($"[1s] IsPlaying: {_mediaPlayer.IsPlaying}", "Playback");
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Error($"Error checking media state at 1s", ex, "Playback");
                    }
                });
            });
        }
        else
        {
            // Local file playback
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
                else
                {
                    _mediaPlayer.Volume = (int)_volume;
                }

                _currentSong = song;
                _currentRadioStation = null;
                CurrentSongChanged?.Invoke(this, _currentSong);
                CurrentRadioStationChanged?.Invoke(this, null);

                _mediaPlayer.Play();
            });
        }
    }

    public async Task PlayRadioAsync(RadioStation station)
    {
        if (_mediaPlayer == null)
        {
            throw new InvalidOperationException("Media player not initialized. LibVLC may not be available on this system.");
        }

        ArgumentNullException.ThrowIfNull(station);

        if (string.IsNullOrEmpty(station.Url))
        {
            throw new ArgumentException("Radio station URL cannot be empty.");
        }

        await Task.Run(() =>
        {
            Stop();

            var media = new Media(_libVLC!, station.Url, FromType.FromLocation);
            _mediaPlayer.Media = media;
            _mediaPlayer.Volume = (int)_volume;

            _currentRadioStation = station;
            _currentSong = null;
            CurrentRadioStationChanged?.Invoke(this, _currentRadioStation);
            CurrentSongChanged?.Invoke(this, null);

            _mediaPlayer.Play();
        });
    }

    public void Play()
    {
        if (_mediaPlayer == null)
        {
            throw new InvalidOperationException("Media player not initialized.");
        }

        if (_currentSong == null && _currentRadioStation == null)
        {
            throw new InvalidOperationException("No media loaded. Call PlayAsync or PlayRadioAsync first.");
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

        if (_currentSong == null && _currentRadioStation == null)
        {
            throw new InvalidOperationException("No media loaded. Call PlayAsync or PlayRadioAsync first.");
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

        if (_currentSong == null && _currentRadioStation == null)
        {
            throw new InvalidOperationException("No media loaded. Call PlayAsync or PlayRadioAsync first.");
        }

        // Radio streams are typically not seekable
        if (_currentRadioStation != null)
        {
            throw new InvalidOperationException("Cannot seek in radio streams");
        }

        // Check if media is seekable
        if (!_mediaPlayer.IsSeekable)
        {
            throw new InvalidOperationException("Media is not seekable");
        }
        
        var targetMs = (long)position.TotalMilliseconds;
        _mediaPlayer.Time = targetMs;
        
        // Read back the actual position set
        var actualMs = _mediaPlayer.Time;
        
        Position = TimeSpan.FromMilliseconds(actualMs);
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
        _youtubeService?.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
