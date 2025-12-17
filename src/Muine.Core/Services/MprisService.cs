using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Muine.Core.Models;
using Tmds.DBus;

namespace Muine.Core.Services;

/// <summary>
/// MPRIS (Media Player Remote Interfacing Specification) service for Linux.
/// Provides D-Bus integration for media key support and "now playing" information.
/// </summary>
public class MprisService : IDisposable
{
    private readonly PlaybackService _playbackService;
    private IConnection? _connection;
    private MprisRoot? _root;
    private MprisPlayer? _player;
    private bool _isInitialized;
    private bool _disposed;

    // MPRIS constants
    private const string BusName = "org.mpris.MediaPlayer2.muine";
    private const string ObjectPath = "/org/mpris/MediaPlayer2";

    // Callbacks for UI actions
    public event EventHandler? NextRequested;
    public event EventHandler? PreviousRequested;
    public event EventHandler? RaiseRequested;
    public event EventHandler? QuitRequested;

    public MprisService(PlaybackService playbackService)
    {
        _playbackService = playbackService ?? throw new ArgumentNullException(nameof(playbackService));
        
        // Subscribe to playback events
        _playbackService.StateChanged += OnPlaybackStateChanged;
        _playbackService.CurrentSongChanged += OnCurrentSongChanged;
        _playbackService.CurrentRadioStationChanged += OnCurrentRadioStationChanged;
    }

    /// <summary>
    /// Initialize MPRIS service on Linux. Does nothing on other platforms.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized || !RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return;

        try
        {
            _connection = new Connection(Address.Session!);
            await _connection.ConnectAsync();
            
            _root = new MprisRoot(this);
            _player = new MprisPlayer(_playbackService, this);
            
            await _connection.RegisterObjectAsync(_root);
            await _connection.RegisterObjectAsync(_player);
            await _connection.RegisterServiceAsync(BusName);
            
            _isInitialized = true;
            System.Diagnostics.Debug.WriteLine("MPRIS service initialized successfully");
        }
        catch (Exception ex)
        {
            // MPRIS initialization failed - this is non-critical
            // App will still work without MPRIS
            System.Diagnostics.Debug.WriteLine($"MPRIS initialization failed: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private void OnPlaybackStateChanged(object? sender, PlaybackState state)
    {
        // Notify property change via D-Bus signal if needed
    }

    private void OnCurrentSongChanged(object? sender, Song? song)
    {
        // Notify metadata change via D-Bus signal if needed
    }

    private void OnCurrentRadioStationChanged(object? sender, RadioStation? station)
    {
        // Notify metadata change via D-Bus signal if needed
    }

    internal void OnNext() => NextRequested?.Invoke(this, EventArgs.Empty);
    internal void OnPrevious() => PreviousRequested?.Invoke(this, EventArgs.Empty);
    internal void OnRaise() => RaiseRequested?.Invoke(this, EventArgs.Empty);
    internal void OnQuit() => QuitRequested?.Invoke(this, EventArgs.Empty);

    public void Dispose()
    {
        if (_disposed)
            return;

        _playbackService.StateChanged -= OnPlaybackStateChanged;
        _playbackService.CurrentSongChanged -= OnCurrentSongChanged;
        _playbackService.CurrentRadioStationChanged -= OnCurrentRadioStationChanged;

        _connection?.Dispose();
        _disposed = true;
    }
}

// MPRIS Root Interface (org.mpris.MediaPlayer2)
[DBusInterface("org.mpris.MediaPlayer2")]
internal class MprisRoot : IDBusObject
{
    private readonly MprisService _service;
    
    public ObjectPath ObjectPath => new ObjectPath("/org/mpris/MediaPlayer2");

    public MprisRoot(MprisService service)
    {
        _service = service;
    }

    public Task RaiseAsync()
    {
        _service.OnRaise();
        return Task.CompletedTask;
    }

    public Task QuitAsync()
    {
        _service.OnQuit();
        return Task.CompletedTask;
    }

    public Task<T> GetAsync<T>(string prop)
    {
        object value = prop switch
        {
            "Identity" => "Muine",
            "DesktopEntry" => "muine",
            "CanQuit" => true,
            "CanRaise" => true,
            "HasTrackList" => false,
            "SupportedUriSchemes" => new string[] { "file" },
            "SupportedMimeTypes" => new string[] { "audio/mpeg", "audio/ogg", "audio/flac", "audio/x-flac", "audio/mp4" },
            _ => throw new ArgumentException($"Unknown property: {prop}")
        };
        return Task.FromResult((T)value);
    }

    public Task<IDictionary<string, object>> GetAllAsync()
    {
        var properties = new Dictionary<string, object>
        {
            ["Identity"] = "Muine",
            ["DesktopEntry"] = "muine",
            ["CanQuit"] = true,
            ["CanRaise"] = true,
            ["HasTrackList"] = false,
            ["SupportedUriSchemes"] = new string[] { "file" },
            ["SupportedMimeTypes"] = new string[] { "audio/mpeg", "audio/ogg", "audio/flac", "audio/x-flac", "audio/mp4" }
        };
        return Task.FromResult<IDictionary<string, object>>(properties);
    }

    public Task SetAsync(string prop, object val) => Task.CompletedTask;

    public Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler) =>
        Task.FromResult<IDisposable>(null!);
}

// MPRIS Player Interface (org.mpris.MediaPlayer2.Player)
[DBusInterface("org.mpris.MediaPlayer2.Player")]
internal class MprisPlayer : IDBusObject
{
    private readonly PlaybackService _playbackService;
    private readonly MprisService _service;
    
    public ObjectPath ObjectPath => new ObjectPath("/org/mpris/MediaPlayer2");

    public MprisPlayer(PlaybackService playbackService, MprisService service)
    {
        _playbackService = playbackService;
        _service = service;
    }

    public Task PlayAsync()
    {
        if (_playbackService.State != PlaybackState.Playing)
        {
            _playbackService.TogglePlayPause();
        }
        return Task.CompletedTask;
    }

    public Task PauseAsync()
    {
        if (_playbackService.State == PlaybackState.Playing)
        {
            _playbackService.TogglePlayPause();
        }
        return Task.CompletedTask;
    }

    public Task PlayPauseAsync()
    {
        _playbackService.TogglePlayPause();
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        _playbackService.Stop();
        return Task.CompletedTask;
    }

    public Task NextAsync()
    {
        _service.OnNext();
        return Task.CompletedTask;
    }

    public Task PreviousAsync()
    {
        _service.OnPrevious();
        return Task.CompletedTask;
    }

    public Task SeekAsync(long offset)
    {
        var currentPosition = _playbackService.Position;
        var newPosition = currentPosition + TimeSpan.FromMicroseconds(offset);
        if (newPosition >= TimeSpan.Zero)
        {
            _playbackService.Seek(newPosition);
        }
        return Task.CompletedTask;
    }

    public Task SetPositionAsync(ObjectPath trackId, long position)
    {
        var newPosition = TimeSpan.FromMicroseconds(position);
        if (newPosition >= TimeSpan.Zero)
        {
            _playbackService.Seek(newPosition);
        }
        return Task.CompletedTask;
    }

    public Task OpenUriAsync(string uri)
    {
        // Not implemented
        return Task.CompletedTask;
    }

    public Task<T> GetAsync<T>(string prop)
    {
        object value = prop switch
        {
            "PlaybackStatus" => GetPlaybackStatus(),
            "LoopStatus" => "None",
            "Rate" => 1.0,
            "Shuffle" => false,
            "Metadata" => GetMetadata(),
            "Volume" => _playbackService.Volume / 100.0,
            "Position" => (long)_playbackService.Position.TotalMicroseconds,
            "MinimumRate" => 1.0,
            "MaximumRate" => 1.0,
            "CanGoNext" => false, // TODO: Connect to playlist
            "CanGoPrevious" => false, // TODO: Connect to playlist
            "CanPlay" => true,
            "CanPause" => true,
            "CanSeek" => !_playbackService.IsPlayingRadio,
            "CanControl" => true,
            _ => throw new ArgumentException($"Unknown property: {prop}")
        };
        return Task.FromResult((T)value);
    }

    public Task<IDictionary<string, object>> GetAllAsync()
    {
        var properties = new Dictionary<string, object>
        {
            ["PlaybackStatus"] = GetPlaybackStatus(),
            ["LoopStatus"] = "None",
            ["Rate"] = 1.0,
            ["Shuffle"] = false,
            ["Metadata"] = GetMetadata(),
            ["Volume"] = _playbackService.Volume / 100.0,
            ["Position"] = (long)_playbackService.Position.TotalMicroseconds,
            ["MinimumRate"] = 1.0,
            ["MaximumRate"] = 1.0,
            ["CanGoNext"] = false,
            ["CanGoPrevious"] = false,
            ["CanPlay"] = true,
            ["CanPause"] = true,
            ["CanSeek"] = !_playbackService.IsPlayingRadio,
            ["CanControl"] = true
        };
        return Task.FromResult<IDictionary<string, object>>(properties);
    }

    public Task SetAsync(string prop, object val)
    {
        if (prop == "Volume")
        {
            _playbackService.Volume = (float)((double)val * 100.0);
        }
        else if (prop == "LoopStatus" || prop == "Rate" || prop == "Shuffle")
        {
            // These are not supported, but we don't throw an error
        }
        return Task.CompletedTask;
    }

    public Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler) =>
        Task.FromResult<IDisposable>(null!);

    private string GetPlaybackStatus()
    {
        return _playbackService.State switch
        {
            PlaybackState.Playing => "Playing",
            PlaybackState.Paused => "Paused",
            _ => "Stopped"
        };
    }

    private IDictionary<string, object> GetMetadata()
    {
        var metadata = new Dictionary<string, object>();

        if (_playbackService.CurrentSong != null)
        {
            var song = _playbackService.CurrentSong;
            
            metadata["mpris:trackid"] = new ObjectPath($"/org/mpris/MediaPlayer2/Track/{song.Id}");
            
            if (!string.IsNullOrEmpty(song.Title))
                metadata["xesam:title"] = song.Title;
            
            if (song.Artists.Length > 0)
                metadata["xesam:artist"] = song.Artists;
            
            if (!string.IsNullOrEmpty(song.Album))
                metadata["xesam:album"] = song.Album;
            
            if (song.Duration > 0)
                metadata["mpris:length"] = (long)(song.Duration * 1000000);
            
            if (!string.IsNullOrEmpty(song.CoverImagePath))
                metadata["mpris:artUrl"] = $"file://{song.CoverImagePath}";
        }
        else if (_playbackService.CurrentRadioStation != null)
        {
            var station = _playbackService.CurrentRadioStation;
            
            metadata["mpris:trackid"] = new ObjectPath($"/org/mpris/MediaPlayer2/Station/{station.Id}");
            metadata["xesam:title"] = station.Name;
            
            if (!string.IsNullOrEmpty(station.Genre))
                metadata["xesam:genre"] = new[] { station.Genre };
        }
        else
        {
            // Provide a default trackid even when nothing is playing
            metadata["mpris:trackid"] = new ObjectPath("/org/mpris/MediaPlayer2/TrackList/NoTrack");
        }

        return metadata;
    }
}
