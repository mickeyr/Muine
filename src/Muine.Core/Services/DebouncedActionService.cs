namespace Muine.Core.Services;

/// <summary>
/// Service for executing actions with debouncing to avoid excessive updates
/// </summary>
public class DebouncedActionService : IDisposable
{
    private readonly Dictionary<string, Timer> _timers = new();
    private readonly Dictionary<string, object> _locks = new();
    private readonly object _dictionaryLock = new();
    private bool _disposed;

    /// <summary>
    /// Execute an action after a delay, canceling any previous pending execution
    /// </summary>
    /// <param name="key">Unique key to identify this debounced action</param>
    /// <param name="action">Action to execute</param>
    /// <param name="delayMilliseconds">Delay in milliseconds (default: 2000ms = 2 seconds)</param>
    public void Debounce(string key, Action action, int delayMilliseconds = 2000)
    {
        if (_disposed)
        {
            return;
        }

        lock (_dictionaryLock)
        {
            // Cancel existing timer if present
            if (_timers.TryGetValue(key, out var existingTimer))
            {
                existingTimer.Dispose();
                _timers.Remove(key);
            }

            // Get or create lock for this key
            if (!_locks.ContainsKey(key))
            {
                _locks[key] = new object();
            }

            // Create new timer
            var timer = new Timer(_ =>
            {
                try
                {
                    // Execute the action
                    action();
                }
                catch (Exception ex)
                {
                    LoggingService.Error($"Error executing debounced action for key: {key}", ex, "DebouncedActionService");
                }
                finally
                {
                    // Clean up timer
                    lock (_dictionaryLock)
                    {
                        if (_timers.TryGetValue(key, out var t))
                        {
                            t.Dispose();
                            _timers.Remove(key);
                        }
                        _locks.Remove(key);
                    }
                }
            }, null, delayMilliseconds, Timeout.Infinite);

            _timers[key] = timer;
        }
    }

    /// <summary>
    /// Execute an async action after a delay, canceling any previous pending execution
    /// </summary>
    /// <param name="key">Unique key to identify this debounced action</param>
    /// <param name="action">Async action to execute</param>
    /// <param name="delayMilliseconds">Delay in milliseconds (default: 2000ms = 2 seconds)</param>
    public void DebounceAsync(string key, Func<Task> action, int delayMilliseconds = 2000)
    {
        Debounce(key, () =>
        {
            // Execute async action synchronously in timer callback
            // Use GetAwaiter().GetResult() to avoid deadlocks
            action().GetAwaiter().GetResult();
        }, delayMilliseconds);
    }

    /// <summary>
    /// Flush a specific debounced action immediately
    /// </summary>
    /// <param name="key">Key of the action to flush</param>
    public void Flush(string key)
    {
        lock (_dictionaryLock)
        {
            if (_timers.TryGetValue(key, out var timer))
            {
                // Change timer to fire immediately
                timer.Change(0, Timeout.Infinite);
            }
        }
    }

    /// <summary>
    /// Flush all pending debounced actions immediately
    /// </summary>
    public void FlushAll()
    {
        lock (_dictionaryLock)
        {
            foreach (var timer in _timers.Values)
            {
                timer.Change(0, Timeout.Infinite);
            }
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            lock (_dictionaryLock)
            {
                foreach (var timer in _timers.Values)
                {
                    timer.Dispose();
                }
                _timers.Clear();
                _locks.Clear();
            }
            _disposed = true;
        }
    }
}
