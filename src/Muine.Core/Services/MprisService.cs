using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Muine.Core.Models;
using Tmds.DBus;

[assembly: InternalsVisibleTo(Tmds.DBus.Connection.DynamicAssemblyName)]

namespace Muine.Core.Services;

/// <summary>
/// MPRIS (Media Player Remote Interfacing Specification) service for Linux.
/// Provides D-Bus integration for media key support and "now playing" information.
/// </summary>
public class MprisService : IDisposable
{
    private IConnection? _connection;
    private readonly PlaybackService _playbackService;
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
            _connection = Connection.Session;
            _player = new MprisPlayer(_playbackService, this);
            
            await _connection.RegisterObjectAsync(_player);
            await _connection.RegisterServiceAsync(BusName);
            
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            // MPRIS initialization failed - this is non-critical
            // App will still work without MPRIS
            System.Diagnostics.Debug.WriteLine($"MPRIS initialization failed: {ex.Message}");
        }
    }

    private void OnPlaybackStateChanged(object? sender, PlaybackState state)
    {
        if (!_isInitialized || _player == null)
            return;

        _player.OnPlaybackStateChanged();
    }

    private void OnCurrentSongChanged(object? sender, Song? song)
    {
        if (!_isInitialized || _player == null)
            return;

        _player.OnMetadataChanged();
    }

    private void OnCurrentRadioStationChanged(object? sender, RadioStation? station)
    {
        if (!_isInitialized || _player == null)
            return;

        _player.OnMetadataChanged();
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

[DBusInterface("org.mpris.MediaPlayer2")]
internal interface IMediaPlayer2 : IDBusObject
{
    Task RaiseAsync();
    Task QuitAsync();
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    Task<IDictionary<string, object>> GetAllAsync();
    Task<object> GetAsync(string prop);
    Task SetAsync(string prop, object val);
}

[DBusInterface("org.mpris.MediaPlayer2.Player")]
internal interface IMediaPlayer2Player : IDBusObject
{
    Task PlayAsync();
    Task PauseAsync();
    Task PlayPauseAsync();
    Task StopAsync();
    Task NextAsync();
    Task PreviousAsync();
    Task SeekAsync(long offset);
    Task SetPositionAsync(ObjectPath trackId, long position);
    Task OpenUriAsync(string uri);
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    Task<IDictionary<string, object>> GetAllAsync();
    Task<object> GetAsync(string prop);
    Task SetAsync(string prop, object val);
}

internal class MprisPlayer : IMediaPlayer2, IMediaPlayer2Player
{
    private readonly PlaybackService _playbackService;
    private readonly MprisService _service;
    
    public ObjectPath ObjectPath => new ObjectPath("/org/mpris/MediaPlayer2");

    public MprisPlayer(PlaybackService playbackService, MprisService service)
    {
        _playbackService = playbackService;
        _service = service;
    }

    // IMediaPlayer2 implementation
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

    Task<IDisposable> IMediaPlayer2.WatchPropertiesAsync(Action<PropertyChanges> handler) =>
        Task.FromResult<IDisposable>(null!);

    Task<IDictionary<string, object>> IMediaPlayer2.GetAllAsync()
    {
        var properties = new Dictionary<string, object>
        {
            ["Identity"] = "Muine",
            ["DesktopEntry"] = "muine",
            ["CanQuit"] = true,
            ["CanRaise"] = true,
            ["HasTrackList"] = false,
            ["SupportedUriSchemes"] = new string[] { "file" },
            ["SupportedMimeTypes"] = new string[] { "audio/mpeg", "audio/ogg", "audio/flac" }
        };
        return Task.FromResult<IDictionary<string, object>>(properties);
    }

    Task<object> IMediaPlayer2.GetAsync(string prop)
    {
        return prop switch
        {
            "Identity" => Task.FromResult<object>("Muine"),
            "DesktopEntry" => Task.FromResult<object>("muine"),
            "CanQuit" => Task.FromResult<object>(true),
            "CanRaise" => Task.FromResult<object>(true),
            "HasTrackList" => Task.FromResult<object>(false),
            "SupportedUriSchemes" => Task.FromResult<object>(new string[] { "file" }),
            "SupportedMimeTypes" => Task.FromResult<object>(new string[] { "audio/mpeg", "audio/ogg", "audio/flac" }),
            _ => throw new ArgumentException($"Unknown property: {prop}")
        };
    }

    Task IMediaPlayer2.SetAsync(string prop, object val) => Task.CompletedTask;

    // IMediaPlayer2Player implementation
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
        _playbackService.Seek(newPosition);
        return Task.CompletedTask;
    }

    public Task SetPositionAsync(ObjectPath trackId, long position)
    {
        _playbackService.Seek(TimeSpan.FromMicroseconds(position));
        return Task.CompletedTask;
    }

    public Task OpenUriAsync(string uri)
    {
        // Not implemented
        return Task.CompletedTask;
    }

    Task<IDisposable> IMediaPlayer2Player.WatchPropertiesAsync(Action<PropertyChanges> handler) =>
        Task.FromResult<IDisposable>(null!);

    Task<IDictionary<string, object>> IMediaPlayer2Player.GetAllAsync()
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
            ["CanGoNext"] = false, // TODO: Connect to playlist
            ["CanGoPrevious"] = false, // TODO: Connect to playlist
            ["CanPlay"] = true,
            ["CanPause"] = true,
            ["CanSeek"] = !_playbackService.IsPlayingRadio,
            ["CanControl"] = true
        };
        return Task.FromResult<IDictionary<string, object>>(properties);
    }

    Task<object> IMediaPlayer2Player.GetAsync(string prop)
    {
        return prop switch
        {
            "PlaybackStatus" => Task.FromResult<object>(GetPlaybackStatus()),
            "LoopStatus" => Task.FromResult<object>("None"),
            "Rate" => Task.FromResult<object>(1.0),
            "Shuffle" => Task.FromResult<object>(false),
            "Metadata" => Task.FromResult<object>(GetMetadata()),
            "Volume" => Task.FromResult<object>(_playbackService.Volume / 100.0),
            "Position" => Task.FromResult<object>((long)_playbackService.Position.TotalMicroseconds),
            "MinimumRate" => Task.FromResult<object>(1.0),
            "MaximumRate" => Task.FromResult<object>(1.0),
            "CanGoNext" => Task.FromResult<object>(false),
            "CanGoPrevious" => Task.FromResult<object>(false),
            "CanPlay" => Task.FromResult<object>(true),
            "CanPause" => Task.FromResult<object>(true),
            "CanSeek" => Task.FromResult<object>(!_playbackService.IsPlayingRadio),
            "CanControl" => Task.FromResult<object>(true),
            _ => throw new ArgumentException($"Unknown property: {prop}")
        };
    }

    Task IMediaPlayer2Player.SetAsync(string prop, object val)
    {
        if (prop == "Volume")
        {
            _playbackService.Volume = (float)((double)val * 100.0);
        }
        return Task.CompletedTask;
    }

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

        return metadata;
    }

    public void OnPlaybackStateChanged()
    {
        // In a full implementation, would emit PropertiesChanged signal
        // Tmds.DBus 0.20 doesn't make this easy, so skipping for now
    }

    public void OnMetadataChanged()
    {
        // In a full implementation, would emit PropertiesChanged signal
        // Tmds.DBus 0.20 doesn't make this easy, so skipping for now
    }
}
