using System.Collections.Concurrent;
using Muine.Core.Models;

namespace Muine.Core.Services;

/// <summary>
/// Background queue for tagging music with MusicBrainz metadata
/// Respects rate limits and allows processing to continue across app restarts
/// </summary>
public class BackgroundTaggingQueue : IDisposable
{
    private readonly MetadataEnhancementService _enhancementService;
    private readonly ConcurrentQueue<TaggingWorkItem> _queue;
    private readonly SemaphoreSlim _queueSignal;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task _workerTask;
    private bool _disposed;

    /// <summary>
    /// Event raised when a work item is completed
    /// </summary>
    public event EventHandler<TaggingCompletedEventArgs>? WorkCompleted;

    /// <summary>
    /// Event raised when a work item fails
    /// </summary>
    public event EventHandler<TaggingFailedEventArgs>? WorkFailed;

    /// <summary>
    /// Get the current queue size
    /// </summary>
    public int QueueSize => _queue.Count;

    /// <summary>
    /// Check if the queue is currently processing
    /// </summary>
    public bool IsProcessing { get; private set; }

    public BackgroundTaggingQueue(MetadataEnhancementService? enhancementService = null)
    {
        _enhancementService = enhancementService ?? new MetadataEnhancementService();
        _queue = new ConcurrentQueue<TaggingWorkItem>();
        _queueSignal = new SemaphoreSlim(0);
        _cancellationTokenSource = new CancellationTokenSource();

        // Start the background worker
        _workerTask = Task.Run(ProcessQueueAsync);

        LoggingService.Info("Background tagging queue started", "BackgroundTaggingQueue");
    }

    /// <summary>
    /// Add a song to the tagging queue
    /// </summary>
    /// <param name="song">Song to tag</param>
    /// <param name="downloadCoverArt">Whether to download cover art</param>
    public void EnqueueSong(Song song, bool downloadCoverArt = true)
    {
        if (song == null)
        {
            throw new ArgumentNullException(nameof(song));
        }

        var workItem = new TaggingWorkItem
        {
            Id = Guid.NewGuid(),
            Song = song,
            DownloadCoverArt = downloadCoverArt,
            EnqueuedAt = DateTime.UtcNow
        };

        _queue.Enqueue(workItem);
        _queueSignal.Release();

        LoggingService.Info($"Song added to tagging queue: {song.DisplayName} (Queue size: {_queue.Count})", "BackgroundTaggingQueue");
    }

    /// <summary>
    /// Add multiple songs to the tagging queue
    /// </summary>
    /// <param name="songs">Songs to tag</param>
    /// <param name="downloadCoverArt">Whether to download cover art</param>
    public void EnqueueSongs(IEnumerable<Song> songs, bool downloadCoverArt = true)
    {
        if (songs == null)
        {
            throw new ArgumentNullException(nameof(songs));
        }

        var count = 0;
        foreach (var song in songs)
        {
            var workItem = new TaggingWorkItem
            {
                Id = Guid.NewGuid(),
                Song = song,
                DownloadCoverArt = downloadCoverArt,
                EnqueuedAt = DateTime.UtcNow
            };

            _queue.Enqueue(workItem);
            _queueSignal.Release();
            count++;
        }

        LoggingService.Info($"{count} songs added to tagging queue (Queue size: {_queue.Count})", "BackgroundTaggingQueue");
    }

    /// <summary>
    /// Clear all pending work items from the queue
    /// </summary>
    public void Clear()
    {
        while (_queue.TryDequeue(out _))
        {
            // Drain the queue
        }

        LoggingService.Info("Tagging queue cleared", "BackgroundTaggingQueue");
    }

    /// <summary>
    /// Background worker that processes the queue
    /// </summary>
    private async Task ProcessQueueAsync()
    {
        var cancellationToken = _cancellationTokenSource.Token;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Wait for work to be available
                await _queueSignal.WaitAsync(cancellationToken);

                // Try to dequeue a work item
                if (!_queue.TryDequeue(out var workItem))
                {
                    continue;
                }

                IsProcessing = true;

                try
                {
                    LoggingService.Info($"Processing tagging work: {workItem.Song.DisplayName}", "BackgroundTaggingQueue");

                    // Process the work item
                    var (enhancedSong, match) = await _enhancementService.EnhanceSongAsync(
                        workItem.Song,
                        writeToFile: true,
                        downloadCoverArt: workItem.DownloadCoverArt);

                    if (enhancedSong != null && match != null)
                    {
                        // Raise completed event
                        WorkCompleted?.Invoke(this, new TaggingCompletedEventArgs
                        {
                            WorkItemId = workItem.Id,
                            OriginalSong = workItem.Song,
                            EnhancedSong = enhancedSong,
                            Match = match,
                            ProcessedAt = DateTime.UtcNow
                        });

                        LoggingService.Info($"Successfully tagged: {workItem.Song.DisplayName} -> {match.Artist} - {match.Title}", "BackgroundTaggingQueue");
                    }
                    else
                    {
                        // Raise failed event (no match found)
                        WorkFailed?.Invoke(this, new TaggingFailedEventArgs
                        {
                            WorkItemId = workItem.Id,
                            Song = workItem.Song,
                            ErrorMessage = "No MusicBrainz match found",
                            FailedAt = DateTime.UtcNow
                        });

                        LoggingService.Info($"No match found for: {workItem.Song.DisplayName}", "BackgroundTaggingQueue");
                    }
                }
                catch (Exception ex)
                {
                    // Raise failed event
                    WorkFailed?.Invoke(this, new TaggingFailedEventArgs
                    {
                        WorkItemId = workItem.Id,
                        Song = workItem.Song,
                        ErrorMessage = ex.Message,
                        Exception = ex,
                        FailedAt = DateTime.UtcNow
                    });

                    LoggingService.Error($"Failed to process tagging work for: {workItem.Song.DisplayName}", ex, "BackgroundTaggingQueue");
                }

                IsProcessing = false;

                // Rate limiting is handled by MusicBrainzService
                // No additional delay needed here
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation, exit the loop
                break;
            }
            catch (Exception ex)
            {
                // Log unexpected errors but continue processing
                LoggingService.Error("Unexpected error in tagging queue worker", ex, "BackgroundTaggingQueue");
            }
        }

        LoggingService.Info("Background tagging queue worker stopped", "BackgroundTaggingQueue");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            LoggingService.Info("Stopping background tagging queue", "BackgroundTaggingQueue");

            // Signal cancellation
            _cancellationTokenSource.Cancel();

            // Give the worker task time to complete gracefully
            // Use Task.Run to avoid potential deadlocks
            Task.Run(async () =>
            {
                try
                {
                    await Task.WhenAny(_workerTask, Task.Delay(TimeSpan.FromSeconds(5)));
                }
                catch
                {
                    // Ignore errors during shutdown
                }
            }).GetAwaiter().GetResult();

            // Dispose resources
            _queueSignal?.Dispose();
            _cancellationTokenSource?.Dispose();
            _enhancementService?.Dispose();

            _disposed = true;

            LoggingService.Info("Background tagging queue stopped", "BackgroundTaggingQueue");
        }
    }
}

/// <summary>
/// Represents a work item in the tagging queue
/// </summary>
public class TaggingWorkItem
{
    public Guid Id { get; set; }
    public Song Song { get; set; } = null!;
    public bool DownloadCoverArt { get; set; }
    public DateTime EnqueuedAt { get; set; }
}

/// <summary>
/// Event args for completed tagging work
/// </summary>
public class TaggingCompletedEventArgs : EventArgs
{
    public Guid WorkItemId { get; set; }
    public Song OriginalSong { get; set; } = null!;
    public Song EnhancedSong { get; set; } = null!;
    public MusicBrainzMatch Match { get; set; } = null!;
    public DateTime ProcessedAt { get; set; }
}

/// <summary>
/// Event args for failed tagging work
/// </summary>
public class TaggingFailedEventArgs : EventArgs
{
    public Guid WorkItemId { get; set; }
    public Song Song { get; set; } = null!;
    public string ErrorMessage { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
    public DateTime FailedAt { get; set; }
}
