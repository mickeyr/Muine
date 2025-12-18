using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Search;
using Muine.Core.Models;
using FFMpegCore;
using FFMpegCore.Enums;

namespace Muine.Core.Services;

/// <summary>
/// Service for searching and retrieving metadata from YouTube
/// </summary>
public class YouTubeService : IDisposable
{
    private readonly YoutubeClient _youtube;
    private readonly Dictionary<string, SemaphoreSlim> _downloadLocks;
    private readonly SemaphoreSlim _lockDictionaryLock;
    private bool _disposed;

    public YouTubeService()
    {
        _youtube = new YoutubeClient();
        _downloadLocks = new Dictionary<string, SemaphoreSlim>();
        _lockDictionaryLock = new SemaphoreSlim(1, 1);
    }

    /// <summary>
    /// Search for videos on YouTube
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="maxResults">Maximum number of results (default 20)</param>
    /// <returns>List of songs representing YouTube videos</returns>
    public async Task<List<Song>> SearchAsync(string query, int maxResults = 20)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new List<Song>();
        }

        try
        {
            var results = new List<Song>();
            
            // Search for videos
            await foreach (var result in _youtube.Search.GetVideosAsync(query))
            {
                if (results.Count >= maxResults)
                    break;

                var song = ConvertToSong(result);
                results.Add(song);
            }

            return results;
        }
        catch (YoutubeExplode.Exceptions.VideoUnavailableException ex)
        {
            // Video is unavailable - log and continue
            System.Diagnostics.Debug.WriteLine($"[YouTubeService] Video unavailable: {ex.Message}");
            return new List<Song>();
        }
        catch (System.Net.Http.HttpRequestException ex)
        {
            // Network error
            System.Diagnostics.Debug.WriteLine($"[YouTubeService] Network error during search: {ex.Message}");
            return new List<Song>();
        }
        catch (Exception ex)
        {
            // Unexpected error - log for debugging
            System.Diagnostics.Debug.WriteLine($"[YouTubeService] Search failed: {ex.GetType().Name} - {ex.Message}");
            return new List<Song>();
        }
    }

    /// <summary>
    /// Get detailed metadata for a YouTube video by ID
    /// </summary>
    /// <param name="videoId">YouTube video ID</param>
    /// <returns>Song with detailed metadata</returns>
    public async Task<Song?> GetVideoMetadataAsync(string videoId)
    {
        if (string.IsNullOrWhiteSpace(videoId))
        {
            return null;
        }

        try
        {
            var video = await _youtube.Videos.GetAsync(videoId);
            return ConvertToSong(video);
        }
        catch (YoutubeExplode.Exceptions.VideoUnavailableException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[YouTubeService] Video unavailable: {videoId} - {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[YouTubeService] Failed to get metadata for {videoId}: {ex.GetType().Name} - {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Download audio from a YouTube video to a local file and convert to OGG format for better compatibility
    /// </summary>
    /// <param name="videoId">YouTube video ID</param>
    /// <param name="outputPath">Path where the audio file should be saved (should end in .ogg)</param>
    /// <returns>True if successful, false otherwise</returns>
    public async Task<bool> DownloadAudioAsync(string videoId, string outputPath)
    {
        if (string.IsNullOrWhiteSpace(videoId) || string.IsNullOrWhiteSpace(outputPath))
        {
            return false;
        }

        // Get or create a lock for this specific video ID to prevent concurrent downloads
        await _lockDictionaryLock.WaitAsync();
        if (!_downloadLocks.TryGetValue(videoId, out var downloadLock))
        {
            downloadLock = new SemaphoreSlim(1, 1);
            _downloadLocks[videoId] = downloadLock;
        }
        _lockDictionaryLock.Release();

        // Acquire the download lock for this video
        await downloadLock.WaitAsync();
        try
        {
            // Check if file was created by another thread while we were waiting
            if (File.Exists(outputPath))
            {
                LoggingService.Info($"Audio file already exists (created by concurrent request): {outputPath}", "YouTubeService");
                return true;
            }

            var streamManifest = await _youtube.Videos.Streams.GetManifestAsync(videoId);
            
            // Get the best audio-only stream
            var audioStream = streamManifest
                .GetAudioOnlyStreams()
                .OrderByDescending(s => s.Bitrate)
                .FirstOrDefault();

            if (audioStream == null)
            {
                LoggingService.Error($"No audio stream found for video: {videoId}", null, "YouTubeService");
                return false;
            }

            // Ensure output directory exists
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Download to temporary WebM file first
            var tempWebmPath = Path.ChangeExtension(outputPath, ".webm.tmp");
            
            LoggingService.Info($"Downloading audio for {videoId} to temporary file", "YouTubeService");
            
            // Download the audio stream (WebM format)
            await _youtube.Videos.Streams.DownloadAsync(audioStream, tempWebmPath);
            
            LoggingService.Info($"Converting WebM to OGG for better compatibility: {videoId}", "YouTubeService");
            
            // Convert WebM to OGG using FFmpeg for better LibVLC compatibility
            // OGG Vorbis is well-supported by VLC and maintains good quality
            // Explicitly set sample rate to 48000 Hz to ensure correct playback speed
            var success = await FFMpegArguments
                .FromFileInput(tempWebmPath)
                .OutputToFile(outputPath, true, options => options
                    .WithAudioCodec(AudioCodec.LibVorbis)
                    .WithAudioBitrate(192)  // 192kbps for good quality
                    .WithAudioSamplingRate(48000))  // Explicitly set sample rate to prevent speed issues
                .ProcessAsynchronously();
            
            // Clean up temporary WebM file
            if (File.Exists(tempWebmPath))
            {
                try
                {
                    File.Delete(tempWebmPath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
            
            if (!success)
            {
                LoggingService.Error($"Failed to convert audio for {videoId}", null, "YouTubeService");
                return false;
            }
            
            LoggingService.Info($"Successfully downloaded and converted audio for {videoId}", "YouTubeService");
            return true;
        }
        catch (YoutubeExplode.Exceptions.VideoUnavailableException ex)
        {
            LoggingService.Error($"Video unavailable: {videoId}", ex, "YouTubeService");
            return false;
        }
        catch (Exception ex)
        {
            LoggingService.Error($"Failed to download audio for {videoId}", ex, "YouTubeService");
            return false;
        }
        finally
        {
            downloadLock.Release();
        }
    }

    /// <summary>
    /// Get the best audio stream URL for a YouTube video
    /// This URL can be played directly with LibVLC
    /// </summary>
    /// <param name="videoId">YouTube video ID</param>
    /// <returns>Audio stream URL or null if unavailable</returns>
    public async Task<string?> GetAudioStreamUrlAsync(string videoId)
    {
        if (string.IsNullOrWhiteSpace(videoId))
        {
            return null;
        }

        try
        {
            var streamManifest = await _youtube.Videos.Streams.GetManifestAsync(videoId);
            
            // Get the best audio-only stream
            var audioStream = streamManifest
                .GetAudioOnlyStreams()
                .OrderByDescending(s => s.Bitrate)
                .FirstOrDefault();

            return audioStream?.Url;
        }
        catch (YoutubeExplode.Exceptions.VideoUnavailableException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[YouTubeService] Video unavailable: {videoId} - {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[YouTubeService] Failed to get stream URL for {videoId}: {ex.GetType().Name} - {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Convert YouTube search result to Song model
    /// </summary>
    private static Song ConvertToSong(IVideo video)
    {
        // Extract artist and title from video title
        // Common patterns: "Artist - Title", "Title by Artist", "Title (Artist)"
        var (artist, title) = ParseVideoTitle(video.Title);

        return new Song
        {
            Title = title,
            Artists = new[] { artist },
            Duration = (int)video.Duration.GetValueOrDefault().TotalSeconds,
            SourceType = SongSourceType.YouTube,
            YouTubeId = video.Id,
            YouTubeUrl = video.Url,
            Filename = video.Url, // Use URL as filename for YouTube songs
            Album = string.Empty,
            Year = string.Empty, // Year information not readily available from search results
            // Store thumbnail URL as cover image path
            CoverImagePath = video.Thumbnails.OrderByDescending(t => t.Resolution.Area).FirstOrDefault()?.Url
        };
    }

    /// <summary>
    /// Parse video title to extract artist and title
    /// Handles common formats like "Artist - Title", "Title by Artist", etc.
    /// </summary>
    private static (string artist, string title) ParseVideoTitle(string videoTitle)
    {
        // Try "Artist - Title" format (most common)
        if (videoTitle.Contains(" - "))
        {
            var parts = videoTitle.Split(new[] { " - " }, 2, StringSplitOptions.None);
            if (parts.Length == 2)
            {
                return (parts[0].Trim(), parts[1].Trim());
            }
        }

        // Try "Title by Artist" format
        if (videoTitle.Contains(" by ", StringComparison.OrdinalIgnoreCase))
        {
            var byIndex = videoTitle.IndexOf(" by ", StringComparison.OrdinalIgnoreCase);
            var title = videoTitle.Substring(0, byIndex).Trim();
            var artist = videoTitle.Substring(byIndex + 4).Trim();
            
            // Remove common suffixes like "(Official Video)", "[Official Audio]", etc.
            artist = RemoveCommonSuffixes(artist);
            
            return (artist, title);
        }

        // If no pattern matches, use video author as artist and title as-is
        // Remove common suffixes from title
        var cleanTitle = RemoveCommonSuffixes(videoTitle);
        return ("Unknown Artist", cleanTitle);
    }

    /// <summary>
    /// Remove common suffixes from video titles like "(Official Video)", "[HD]", etc.
    /// </summary>
    private static string RemoveCommonSuffixes(string text)
    {
        var suffixes = new[]
        {
            "(Official Video)",
            "(Official Music Video)",
            "[Official Video]",
            "(Official Audio)",
            "[Official Audio]",
            "(Lyric Video)",
            "[Lyric Video]",
            "(Audio)",
            "[Audio]",
            "(HD)",
            "[HD]",
            "(4K)",
            "[4K]",
            "(Official)",
            "[Official]"
        };

        foreach (var suffix in suffixes)
        {
            if (text.Contains(suffix, StringComparison.OrdinalIgnoreCase))
            {
                text = text.Replace(suffix, "", StringComparison.OrdinalIgnoreCase).Trim();
            }
        }

        return text;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            // Clean up download locks
            foreach (var lockObj in _downloadLocks.Values)
            {
                lockObj?.Dispose();
            }
            _downloadLocks.Clear();
            _lockDictionaryLock?.Dispose();
            
            // YoutubeClient doesn't implement IDisposable in current version
            // but we keep this for future-proofing
            _disposed = true;
        }
    }
}
