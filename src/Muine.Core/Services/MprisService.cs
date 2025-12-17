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
    
    // Seeked signal
    Task<IDisposable> WatchSeekedAsync(Action<long> handler);
}

// Combined MPRIS object implementing both interfaces
internal class MprisObject : IMediaPlayer2, IMediaPlayer2Player
{
    private readonly PlaybackService _playbackService;
    private readonly MprisService _service;
    private readonly List<Action<PropertyChanges>> _mediaPlayer2PropertyWatchers = new();
    private readonly List<Action<PropertyChanges>> _playerPropertyWatchers = new();
    private readonly List<Action<long>> _seekedWatchers = new();
    
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
            
            // CRITICAL INSIGHT from stack trace:
            // Tmds.DBus generated MprisObjectAdapter.Emitorg_mpris_MediaPlayer2_Player_Properties(PropertyChanges)
            // The signal emission mechanism IS working, we just need a valid PropertyChanges struct.
            //
            // PropertyChanges struct must have:
            // - Changed: IDictionary<string, object>  
            // - Invalidated: string[]
            //
            // Let's use RuntimeHelpers.GetUninitializedObject (not obsolete) to create the struct
            // and then set the backing fields via reflection
            
            try
            {
                var propertyChangesType = typeof(PropertyChanges);
                
                // Create uninitialized instance
                var changes = (PropertyChanges)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(propertyChangesType);
                
                // Find ALL fields (public, private, instance)
                var allFields = propertyChangesType.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                
                Console.WriteLine($"[MPRIS] PropertyChanges has {allFields.Length} fields");
                foreach (var field in allFields)
                {
                    Console.WriteLine($"[MPRIS]   Field: {field.Name}, Type: {field.FieldType.Name}, IsPublic: {field.IsPublic}");
                }
                
                // Also check for properties
                var allProperties = propertyChangesType.GetProperties(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                Console.WriteLine($"[MPRIS] PropertyChanges has {allProperties.Length} properties");
                foreach (var prop in allProperties)
                {
                    Console.WriteLine($"[MPRIS]   Property: {prop.Name}, Type: {prop.PropertyType.Name}, CanRead: {prop.CanRead}, CanWrite: {prop.CanWrite}");
                }
                
                // The actual field names are _changed and _invalidated (lowercase with underscore)
                var changedField = allFields.FirstOrDefault(f => f.Name == "_changed");
                var invalidatedField = allFields.FirstOrDefault(f => f.Name == "_invalidated");
                
                if (changedField != null)
                {
                    // _changed is KeyValuePair<string, object>[], not IDictionary!
                    // Convert our dictionary to KeyValuePair array
                    var changedArray = changedProperties.Select(kvp => new KeyValuePair<string, object>(kvp.Key, kvp.Value)).ToArray();
                    changedField.SetValue(changes, changedArray);
                    Console.WriteLine($"[MPRIS] ✓ Set _changed field with {changedArray.Length} properties");
                }
                else
                {
                    Console.WriteLine("[MPRIS] ✗ Could not find _changed field");
                }
                
                if (invalidatedField != null)
                {
                    invalidatedField.SetValue(changes, Array.Empty<string>());
                    Console.WriteLine("[MPRIS] ✓ Set _invalidated field");
                }
                else
                {
                    Console.WriteLine("[MPRIS] ✗ Could not find _invalidated field");
                }
                
                // Invoke watchers with the constructed PropertyChanges
                foreach (var watcher in watchers.ToArray())
                {
                    try
                    {
                        Console.WriteLine($"[MPRIS] About to invoke watcher with PropertyChanges...");
                        Console.WriteLine($"[MPRIS]   _changed: {changedField?.GetValue(changes) != null} ({(changedField?.GetValue(changes) as Array)?.Length ?? 0} items)");
                        Console.WriteLine($"[MPRIS]   _invalidated: {invalidatedField?.GetValue(changes) != null} ({(invalidatedField?.GetValue(changes) as Array)?.Length ?? 0} items)");
                        
                        watcher(changes);
                        Console.WriteLine($"[MPRIS] ✓ Successfully emitted PropertyChanged signal for {interfaceName}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[MPRIS] ✗ Error invoking watcher: {ex.GetType().Name}: {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            Console.WriteLine($"[MPRIS]   Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                            Console.WriteLine($"[MPRIS]   Inner Stack: {ex.InnerException.StackTrace}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MPRIS] ✗ Error creating PropertyChanges: {ex.GetType().Name}: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine($"[MPRIS] No watchers registered for {interfaceName}, signal will not be emitted");
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
        // D-Bus clients register watchers here to receive PropertiesChanged signals
        _mediaPlayer2PropertyWatchers.Add(handler);
        Console.WriteLine($"[MPRIS] MediaPlayer2 property watcher registered (total: {_mediaPlayer2PropertyWatchers.Count})");
        return Task.FromResult<IDisposable>(new PropertyWatcherDisposable(() => {
            _mediaPlayer2PropertyWatchers.Remove(handler);
            Console.WriteLine($"[MPRIS] MediaPlayer2 property watcher removed (total: {_mediaPlayer2PropertyWatchers.Count})");
        }));
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
        // D-Bus clients register watchers here to receive PropertiesChanged signals
        _playerPropertyWatchers.Add(handler);
        Console.WriteLine($"[MPRIS] MediaPlayer2.Player property watcher registered (total: {_playerPropertyWatchers.Count})");
        return Task.FromResult<IDisposable>(new PropertyWatcherDisposable(() => {
            _playerPropertyWatchers.Remove(handler);
            Console.WriteLine($"[MPRIS] MediaPlayer2.Player property watcher removed (total: {_playerPropertyWatchers.Count})");
        }));
    }

    Task<IDisposable> IMediaPlayer2Player.WatchSeekedAsync(Action<long> handler)
    {
        // For Seeked signal
        _seekedWatchers.Add(handler);
        Console.WriteLine($"[MPRIS] Seeked signal watcher registered (total: {_seekedWatchers.Count})");
        return Task.FromResult<IDisposable>(new PropertyWatcherDisposable(() => {
            _seekedWatchers.Remove(handler);
            Console.WriteLine($"[MPRIS] Seeked signal watcher removed (total: {_seekedWatchers.Count})");
        }));
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
