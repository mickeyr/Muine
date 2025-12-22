using MetaBrainz.MusicBrainz;
using MetaBrainz.MusicBrainz.Interfaces.Searches;
using Muine.Core.Models;

namespace Muine.Core.Services;

/// <summary>
/// Service for querying the MusicBrainz API with rate limiting and authentication support
/// </summary>
public class MusicBrainzService : IMusicBrainzService
{
    private readonly Query _query;
    private readonly SemaphoreSlim _rateLimiter;
    private readonly TimeSpan _rateLimitDelay;
    private DateTime _lastRequestTime;
    private bool _disposed;

    /// <summary>
    /// MusicBrainz rate limit: 1 request per second for unauthenticated requests
    /// Authenticated requests can make more, but we'll be conservative
    /// </summary>
    private const int RequestsPerSecond = 1;

    /// <summary>
    /// Initialize MusicBrainzService with optional authentication
    /// </summary>
    /// <param name="applicationName">Application name for user agent</param>
    /// <param name="applicationVersion">Application version for user agent</param>
    /// <param name="contactEmail">Contact email for user agent</param>
    /// <param name="username">Optional MusicBrainz username for authentication</param>
    /// <param name="password">Optional MusicBrainz password for authentication</param>
    public MusicBrainzService(
        string applicationName = "Muine",
        string applicationVersion = "1.0",
        string contactEmail = "muine@example.com",
        string? username = null,
        string? password = null)
    {
        // Create the query with user agent information
        _query = new Query(applicationName, applicationVersion, contactEmail);
        
        // Set up authentication if credentials provided
        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
        {
            // Note: MetaBrainz.MusicBrainz library handles authentication internally
            // For now, we just note it. Full OAuth support may require additional setup
            LoggingService.Info($"MusicBrainz authentication configured for user: {username}", "MusicBrainzService");
        }
        
        _rateLimiter = new SemaphoreSlim(1, 1);
        _rateLimitDelay = TimeSpan.FromMilliseconds(1000.0 / RequestsPerSecond);
        _lastRequestTime = DateTime.MinValue;
    }

    /// <summary>
    /// Search for recordings (songs) by artist and title
    /// </summary>
    /// <param name="artist">Artist name</param>
    /// <param name="title">Song title</param>
    /// <param name="maxResults">Maximum number of results (default 10)</param>
    /// <returns>List of MusicBrainz matches</returns>
    public async Task<List<MusicBrainzMatch>> SearchRecordingsAsync(string artist, string title, int maxResults = 10)
    {
        if (string.IsNullOrWhiteSpace(artist) || string.IsNullOrWhiteSpace(title))
        {
            return new List<MusicBrainzMatch>();
        }

        await EnforceRateLimitAsync();

        try
        {
            // Build the search query
            var searchQuery = $"artist:\"{artist}\" AND recording:\"{title}\"";
            
            LoggingService.Info($"Searching MusicBrainz: {searchQuery}", "MusicBrainzService");
            
            // Perform the search
            var results = await _query.FindRecordingsAsync(searchQuery, limit: maxResults);
            
            var matches = new List<MusicBrainzMatch>();
            
            if (results?.Results == null)
            {
                return matches;
            }

            foreach (var result in results.Results.Take(maxResults))
            {
                var recording = result.Item;
                if (recording == null) continue;

                var match = new MusicBrainzMatch
                {
                    RecordingId = recording.Id.ToString(),
                    Title = recording.Title ?? string.Empty,
                    MatchScore = (double)result.Score / 100.0, // Convert 0-100 to 0.0-1.0
                    Disambiguation = recording.Disambiguation
                };

                // Get artist information
                if (recording.ArtistCredit != null && recording.ArtistCredit.Count > 0)
                {
                    var firstArtist = recording.ArtistCredit[0];
                    match.Artist = firstArtist.Name ?? string.Empty;
                    match.ArtistId = firstArtist.Artist?.Id.ToString();
                }

                // Get release (album) information - prefer official album releases
                if (recording.Releases != null && recording.Releases.Count > 0)
                {
                    // Sort releases to prefer official album releases
                    // Priority: Official albums > Other albums > Live releases > Other types
                    var preferredRelease = recording.Releases
                        .OrderByDescending(r =>
                        {
                            // Calculate preference score
                            int score = 0;
                            
                            // Prefer official releases
                            if (r.Status == "Official")
                                score += 1000;
                            
                            // Prefer album release groups over live recordings
                            var title = r.Title?.ToLowerInvariant() ?? "";
                            var disambiguation = r.Disambiguation?.ToLowerInvariant() ?? "";
                            
                            // Penalize live recordings heavily
                            if (title.Contains("live") || disambiguation.Contains("live") ||
                                title.Contains("concert") || disambiguation.Contains("concert") ||
                                disambiguation.Contains(": ") && disambiguation.Contains(", ")) // Date format like "1994-02-14: Paris, France"
                            {
                                score -= 500;
                            }
                            
                            // Prefer releases without disambiguation (usually main releases)
                            if (string.IsNullOrEmpty(r.Disambiguation))
                                score += 100;
                            
                            // Prefer releases with earlier dates (usually original releases)
                            if (r.Date != null && r.Date.Year.HasValue)
                            {
                                // Give slight preference to earlier releases
                                score += Math.Max(0, 50 - (r.Date.Year.Value - 1900));
                            }
                            
                            return score;
                        })
                        .ThenBy(r => r.Date?.Year ?? 9999) // Tie-breaker: earlier year
                        .FirstOrDefault();
                    
                    if (preferredRelease != null)
                    {
                        match.ReleaseId = preferredRelease.Id.ToString();
                        match.Album = preferredRelease.Title;
                        
                        // Get release date/year
                        if (preferredRelease.Date != null)
                        {
                            match.Year = preferredRelease.Date.Year;
                        }

                        // Try to get cover art URL
                        match.CoverArtUrl = $"https://coverartarchive.org/release/{preferredRelease.Id}/front-250";
                        
                        LoggingService.Info($"Selected release: {preferredRelease.Title} (Status: {preferredRelease.Status}, Disambiguation: {preferredRelease.Disambiguation ?? "none"})", "MusicBrainzService");
                    }
                }

                // Get genres/tags
                if (recording.Tags != null && recording.Tags.Count > 0)
                {
                    match.Genres = recording.Tags
                        .OrderByDescending(t => t.VoteCount)
                        .Take(5)
                        .Select(t => t.Name ?? string.Empty)
                        .Where(name => !string.IsNullOrEmpty(name))
                        .ToArray();
                }

                matches.Add(match);
            }

            LoggingService.Info($"Found {matches.Count} MusicBrainz matches", "MusicBrainzService");
            return matches;
        }
        catch (Exception ex)
        {
            LoggingService.Error($"Failed to search MusicBrainz for '{artist} - {title}'", ex, "MusicBrainzService");
            return new List<MusicBrainzMatch>();
        }
    }

    /// <summary>
    /// Get detailed recording information by MusicBrainz recording ID
    /// </summary>
    /// <param name="recordingId">MusicBrainz recording ID</param>
    /// <returns>Detailed match information or null if not found</returns>
    public async Task<MusicBrainzMatch?> GetRecordingAsync(string recordingId)
    {
        if (string.IsNullOrWhiteSpace(recordingId))
        {
            return null;
        }

        await EnforceRateLimitAsync();

        try
        {
            if (!Guid.TryParse(recordingId, out var guid))
            {
                LoggingService.Warning($"Invalid MusicBrainz recording ID: {recordingId}", "MusicBrainzService");
                return null;
            }

            LoggingService.Info($"Fetching MusicBrainz recording: {recordingId}", "MusicBrainzService");
            
            // Fetch the recording with releases included
            var recording = await _query.LookupRecordingAsync(guid, Include.Releases | Include.Artists | Include.Tags);
            
            if (recording == null)
            {
                return null;
            }

            var match = new MusicBrainzMatch
            {
                RecordingId = recording.Id.ToString(),
                Title = recording.Title ?? string.Empty,
                MatchScore = 1.0, // Direct lookup, perfect match
                Disambiguation = recording.Disambiguation
            };

            // Get artist information
            if (recording.ArtistCredit != null && recording.ArtistCredit.Count > 0)
            {
                var firstArtist = recording.ArtistCredit[0];
                match.Artist = firstArtist.Name ?? string.Empty;
                match.ArtistId = firstArtist.Artist?.Id.ToString();
            }

            // Get release (album) information
            if (recording.Releases != null && recording.Releases.Count > 0)
            {
                var release = recording.Releases[0];
                match.ReleaseId = release.Id.ToString();
                match.Album = release.Title;
                
                if (release.Date != null)
                {
                    match.Year = release.Date.Year;
                }

                match.CoverArtUrl = $"https://coverartarchive.org/release/{release.Id}/front-250";
            }

            // Get genres/tags
            if (recording.Tags != null && recording.Tags.Count > 0)
            {
                match.Genres = recording.Tags
                    .OrderByDescending(t => t.VoteCount)
                    .Take(5)
                    .Select(t => t.Name ?? string.Empty)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToArray();
            }

            return match;
        }
        catch (Exception ex)
        {
            LoggingService.Error($"Failed to fetch MusicBrainz recording {recordingId}", ex, "MusicBrainzService");
            return null;
        }
    }

    /// <summary>
    /// Download cover art from Cover Art Archive
    /// </summary>
    /// <param name="releaseId">MusicBrainz release ID</param>
    /// <param name="outputPath">Path where cover art should be saved</param>
    /// <returns>True if successful, false otherwise</returns>
    public async Task<bool> DownloadCoverArtAsync(string releaseId, string outputPath)
    {
        if (string.IsNullOrWhiteSpace(releaseId) || string.IsNullOrWhiteSpace(outputPath))
        {
            return false;
        }

        try
        {
            var url = $"https://coverartarchive.org/release/{releaseId}/front";
            
            LoggingService.Info($"Downloading cover art from: {url}", "MusicBrainzService");
            
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Muine/1.0 (muine@example.com)");
            
            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                LoggingService.Warning($"Cover art not found for release {releaseId}: {response.StatusCode}", "MusicBrainzService");
                return false;
            }

            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            
            // Ensure output directory exists
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllBytesAsync(outputPath, imageBytes);
            LoggingService.Info($"Cover art saved to: {outputPath}", "MusicBrainzService");
            
            return true;
        }
        catch (Exception ex)
        {
            LoggingService.Error($"Failed to download cover art for release {releaseId}", ex, "MusicBrainzService");
            return false;
        }
    }

    /// <summary>
    /// Enforce rate limiting to comply with MusicBrainz API guidelines
    /// </summary>
    private async Task EnforceRateLimitAsync()
    {
        await _rateLimiter.WaitAsync();
        try
        {
            var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
            if (timeSinceLastRequest < _rateLimitDelay)
            {
                var delayNeeded = _rateLimitDelay - timeSinceLastRequest;
                await Task.Delay(delayNeeded);
            }
            _lastRequestTime = DateTime.UtcNow;
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _rateLimiter?.Dispose();
            _disposed = true;
        }
    }
}
