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
    private Connection? _connection;
    private MprisObject? _mprisObject;
    private bool _isInitialized;
    private bool _disposed;

    // MPRIS constants
    private const string BusName = "org.mpris.MediaPlayer2.muine";

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
        Console.WriteLine($"[MPRIS] InitializeAsync called");
        Console.WriteLine($"[MPRIS] Is Linux: {RuntimeInformation.IsOSPlatform(OSPlatform.Linux)}");
        Console.WriteLine($"[MPRIS] Already initialized: {_isInitialized}");
        
        if (_isInitialized || !RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Console.WriteLine("[MPRIS] Skipping initialization");
            return;
        }

        try
        {
            Console.WriteLine("[MPRIS] Creating connection to session bus...");
            _connection = new Connection(Address.Session!);
            await _connection.ConnectAsync();
            Console.WriteLine("[MPRIS] Connected to session bus");
            
            Console.WriteLine("[MPRIS] Creating MPRIS object...");
            _mprisObject = new MprisObject(_playbackService, this);
            Console.WriteLine("[MPRIS] MPRIS object created");
            
            Console.WriteLine($"[MPRIS] Registering object at {MprisObject.Path}...");
            await _connection.RegisterObjectAsync(_mprisObject);
            Console.WriteLine("[MPRIS] Object registered");
            
            Console.WriteLine($"[MPRIS] Registering service name: {BusName}...");
            await _connection.RegisterServiceAsync(BusName);
            Console.WriteLine("[MPRIS] Service name registered");
            
            _isInitialized = true;
            Console.WriteLine($"[MPRIS] ✓ Service initialized successfully as {BusName}");
            Console.WriteLine("[MPRIS] Property change signals will be emitted when playback state or song changes");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MPRIS] ✗ Initialization failed: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"[MPRIS] Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"[MPRIS] Inner exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
        }
    }

    internal void OnNext() => NextRequested?.Invoke(this, EventArgs.Empty);
    internal void OnPrevious() => PreviousRequested?.Invoke(this, EventArgs.Empty);
    internal void OnRaise() => RaiseRequested?.Invoke(this, EventArgs.Empty);
    internal void OnQuit() => QuitRequested?.Invoke(this, EventArgs.Empty);

    private void OnPlaybackStateChanged(object? sender, PlaybackState state)
    {
        if (_mprisObject != null && _isInitialized)
        {
            var changes = new Dictionary<string, object>
            {
                ["PlaybackStatus"] = _mprisObject.GetPlaybackStatus()
            };
            _mprisObject.EmitPropertyChanged("org.mpris.MediaPlayer2.Player", changes);
        }
    }

    private void OnCurrentSongChanged(object? sender, Song? song)
    {
        if (_mprisObject != null && _isInitialized)
        {
            var changes = new Dictionary<string, object>
            {
                ["Metadata"] = _mprisObject.GetMetadata()
            };
            _mprisObject.EmitPropertyChanged("org.mpris.MediaPlayer2.Player", changes);
        }
    }

    private void OnCurrentRadioStationChanged(object? sender, RadioStation? station)
    {
        if (_mprisObject != null && _isInitialized)
        {
            var changes = new Dictionary<string, object>
            {
                ["Metadata"] = _mprisObject.GetMetadata()
            };
            _mprisObject.EmitPropertyChanged("org.mpris.MediaPlayer2.Player", changes);
        }
    }

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
public interface IMediaPlayer2 : IDBusObject
{
    Task RaiseAsync();
    Task QuitAsync();
    Task<object> GetAsync(string prop);
    Task<IDictionary<string, object>> GetAllAsync();
    Task SetAsync(string prop, object val);
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
}

// MPRIS Player Interface (org.mpris.MediaPlayer2.Player)
[DBusInterface("org.mpris.MediaPlayer2.Player")]
public interface IMediaPlayer2Player : IDBusObject
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
    Task<object> GetAsync(string prop);
    Task<IDictionary<string, object>> GetAllAsync();
    Task SetAsync(string prop, object val);
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
}

// Combined MPRIS object implementing both interfaces
internal class MprisObject : IMediaPlayer2, IMediaPlayer2Player
{
    private readonly PlaybackService _playbackService;
    private readonly MprisService _service;
    private readonly List<Action<PropertyChanges>> _mediaPlayer2PropertyWatchers = new();
    private readonly List<Action<PropertyChanges>> _playerPropertyWatchers = new();
    
    public static readonly ObjectPath Path = new ObjectPath("/org/mpris/MediaPlayer2");
    public ObjectPath ObjectPath => Path;

    public MprisObject(PlaybackService playbackService, MprisService service)
    {
        _playbackService = playbackService;
        _service = service;
    }

    // Method to emit property change signals
    internal void EmitPropertyChanged(string interfaceName, IDictionary<string, object> changedProperties)
    {
        var watchers = interfaceName switch
        {
            "org.mpris.MediaPlayer2" => _mediaPlayer2PropertyWatchers,
            "org.mpris.MediaPlayer2.Player" => _playerPropertyWatchers,
            _ => null
        };

        if (watchers != null && watchers.Count > 0)
        {
            Console.WriteLine($"[MPRIS] Emitting PropertyChanged signal for {interfaceName} with {changedProperties.Count} properties to {watchers.Count} watchers");
            
            // PropertyChanges is from Tmds.DBus - try default construction
            var changes = default(PropertyChanges);
            
            foreach (var watcher in watchers.ToArray())
            {
                try
                {
                    watcher(changes);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MPRIS] Error invoking property watcher: {ex.Message}");
                }
            }
        }
    }

    // IMediaPlayer2 implementation
    Task IMediaPlayer2.RaiseAsync()
    {
        _service.OnRaise();
        return Task.CompletedTask;
    }

    Task IMediaPlayer2.QuitAsync()
    {
        _service.OnQuit();
        return Task.CompletedTask;
    }

    Task<object> IMediaPlayer2.GetAsync(string prop)
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
        return Task.FromResult(value);
    }

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
            ["SupportedMimeTypes"] = new string[] { "audio/mpeg", "audio/ogg", "audio/flac", "audio/x-flac", "audio/mp4" }
        };
        return Task.FromResult<IDictionary<string, object>>(properties);
    }

    Task IMediaPlayer2.SetAsync(string prop, object val) => Task.CompletedTask;

    Task<IDisposable> IMediaPlayer2.WatchPropertiesAsync(Action<PropertyChanges> handler)
    {
        _mediaPlayer2PropertyWatchers.Add(handler);
        Console.WriteLine("[MPRIS] Added MediaPlayer2 property watcher");
        return Task.FromResult<IDisposable>(new PropertyWatcherDisposable(() => _mediaPlayer2PropertyWatchers.Remove(handler)));
    }

    // IMediaPlayer2Player implementation
    Task IMediaPlayer2Player.PlayAsync()
    {
        if (_playbackService.State != PlaybackState.Playing)
        {
            _playbackService.TogglePlayPause();
        }
        return Task.CompletedTask;
    }

    Task IMediaPlayer2Player.PauseAsync()
    {
        if (_playbackService.State == PlaybackState.Playing)
        {
            _playbackService.TogglePlayPause();
        }
        return Task.CompletedTask;
    }

    Task IMediaPlayer2Player.PlayPauseAsync()
    {
        _playbackService.TogglePlayPause();
        return Task.CompletedTask;
    }

    Task IMediaPlayer2Player.StopAsync()
    {
        _playbackService.Stop();
        return Task.CompletedTask;
    }

    Task IMediaPlayer2Player.NextAsync()
    {
        _service.OnNext();
        return Task.CompletedTask;
    }

    Task IMediaPlayer2Player.PreviousAsync()
    {
        _service.OnPrevious();
        return Task.CompletedTask;
    }

    Task IMediaPlayer2Player.SeekAsync(long offset)
    {
        var currentPosition = _playbackService.Position;
        var newPosition = currentPosition + TimeSpan.FromMicroseconds(offset);
        if (newPosition >= TimeSpan.Zero)
        {
            _playbackService.Seek(newPosition);
        }
        return Task.CompletedTask;
    }

    Task IMediaPlayer2Player.SetPositionAsync(ObjectPath trackId, long position)
    {
        var newPosition = TimeSpan.FromMicroseconds(position);
        if (newPosition >= TimeSpan.Zero)
        {
            _playbackService.Seek(newPosition);
        }
        return Task.CompletedTask;
    }

    Task IMediaPlayer2Player.OpenUriAsync(string uri)
    {
        // Not implemented
        return Task.CompletedTask;
    }

    Task<object> IMediaPlayer2Player.GetAsync(string prop)
    {
        Console.WriteLine($"[MPRIS] GetAsync called for property: {prop}");
        
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
        
        if (prop == "Metadata")
        {
            var metadata = (IDictionary<string, object>)value;
            Console.WriteLine($"[MPRIS] Returning Metadata with {metadata.Count} keys:");
            foreach (var kvp in metadata)
            {
                Console.WriteLine($"[MPRIS]   {kvp.Key} = {kvp.Value}");
            }
        }
        
        return Task.FromResult(value);
    }

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
            ["CanGoNext"] = false,
            ["CanGoPrevious"] = false,
            ["CanPlay"] = true,
            ["CanPause"] = true,
            ["CanSeek"] = !_playbackService.IsPlayingRadio,
            ["CanControl"] = true
        };
        return Task.FromResult<IDictionary<string, object>>(properties);
    }

    Task IMediaPlayer2Player.SetAsync(string prop, object val)
    {
        if (prop == "Volume")
        {
            _playbackService.Volume = (float)((double)val * 100.0);
        }
        return Task.CompletedTask;
    }

    Task<IDisposable> IMediaPlayer2Player.WatchPropertiesAsync(Action<PropertyChanges> handler)
    {
        _playerPropertyWatchers.Add(handler);
        Console.WriteLine("[MPRIS] Added MediaPlayer2.Player property watcher");
        return Task.FromResult<IDisposable>(new PropertyWatcherDisposable(() => _playerPropertyWatchers.Remove(handler)));
    }

    // Make this method accessible to MprisService for property change signals
    internal string GetPlaybackStatus() => _playbackService.State switch
    {
        PlaybackState.Playing => "Playing",
        PlaybackState.Paused => "Paused",
        _ => "Stopped"
    };

    // Make this method accessible to MprisService for property change signals
    internal IDictionary<string, object> GetMetadata()
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

// Disposable that removes property watcher on disposal
internal class PropertyWatcherDisposable : IDisposable
{
    private readonly Action _onDispose;
    private bool _disposed;

    public PropertyWatcherDisposable(Action onDispose)
    {
        _onDispose = onDispose;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _onDispose();
            _disposed = true;
        }
    }
}
