using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Search;
using Muine.Core.Models;

namespace Muine.Core.Services;

/// <summary>
/// Service for searching and retrieving metadata from YouTube
/// </summary>
public class YouTubeService : IDisposable
{
    private readonly YoutubeClient _youtube;
    private bool _disposed;

    public YouTubeService()
    {
        _youtube = new YoutubeClient();
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
        catch (Exception)
        {
            // Return empty list on error
            // In production, consider using a logging framework
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
        catch (Exception)
        {
            // Return null on error
            return null;
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
        catch (Exception)
        {
            // Return null on error
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
            // YoutubeClient doesn't implement IDisposable in current version
            // but we keep this for future-proofing
            _disposed = true;
        }
    }
}
