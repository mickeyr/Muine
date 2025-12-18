using Muine.Core.Models;

namespace Muine.Core.Services;

/// <summary>
/// Service for enhancing song metadata using MusicBrainz
/// </summary>
public class MetadataEnhancementService : IDisposable
{
    private readonly IMusicBrainzService _musicBrainzService;
    private readonly MetadataService _metadataService;
    private bool _disposed;

    public MetadataEnhancementService(
        IMusicBrainzService? musicBrainzService = null,
        MetadataService? metadataService = null)
    {
        _musicBrainzService = musicBrainzService ?? new MusicBrainzService();
        _metadataService = metadataService ?? new MetadataService();
    }

    /// <summary>
    /// Find MusicBrainz matches for a song
    /// </summary>
    /// <param name="song">Song to match</param>
    /// <param name="maxResults">Maximum number of results</param>
    /// <returns>List of MusicBrainz matches</returns>
    public async Task<List<MusicBrainzMatch>> FindMatchesAsync(Song song, int maxResults = 10)
    {
        if (song == null)
        {
            throw new ArgumentNullException(nameof(song));
        }

        // Extract artist and title
        var artist = song.Artists.Length > 0 ? song.Artists[0] : "Unknown Artist";
        var title = song.Title;

        if (string.IsNullOrWhiteSpace(title))
        {
            LoggingService.Warning($"Song has no title: {song.Filename}", "MetadataEnhancementService");
            return new List<MusicBrainzMatch>();
        }

        LoggingService.Info($"Finding MusicBrainz matches for: {artist} - {title}", "MetadataEnhancementService");

        var matches = await _musicBrainzService.SearchRecordingsAsync(artist, title, maxResults);
        
        // Calculate additional match scores based on existing metadata
        foreach (var match in matches)
        {
            // Boost score if album matches
            if (!string.IsNullOrEmpty(song.Album) && !string.IsNullOrEmpty(match.Album))
            {
                if (string.Equals(song.Album, match.Album, StringComparison.OrdinalIgnoreCase))
                {
                    match.MatchScore += 0.1;
                }
            }

            // Boost score if year matches
            if (!string.IsNullOrEmpty(song.Year) && match.Year.HasValue)
            {
                if (int.TryParse(song.Year, out var songYear) && songYear == match.Year.Value)
                {
                    match.MatchScore += 0.05;
                }
            }

            // Cap at 1.0
            match.MatchScore = Math.Min(1.0, match.MatchScore);
        }

        // Sort by match score (highest first)
        matches.Sort((a, b) => b.MatchScore.CompareTo(a.MatchScore));

        return matches;
    }

    /// <summary>
    /// Enhance a song with metadata from the best MusicBrainz match
    /// </summary>
    /// <param name="song">Song to enhance</param>
    /// <param name="writeToFile">Whether to write the enhanced metadata to the file</param>
    /// <param name="downloadCoverArt">Whether to download and embed cover art</param>
    /// <returns>Enhanced song with MusicBrainz match, or null if no good match found</returns>
    public async Task<(Song? enhancedSong, MusicBrainzMatch? match)> EnhanceSongAsync(
        Song song,
        bool writeToFile = true,
        bool downloadCoverArt = true)
    {
        if (song == null)
        {
            throw new ArgumentNullException(nameof(song));
        }

        // Find matches
        var matches = await FindMatchesAsync(song, maxResults: 5);

        if (matches.Count == 0)
        {
            LoggingService.Info($"No MusicBrainz matches found for: {song.DisplayName}", "MetadataEnhancementService");
            return (null, null);
        }

        // Use the best match (highest score)
        var bestMatch = matches[0];

        // Only use matches with reasonable confidence (> 70%)
        if (bestMatch.MatchScore < 0.7)
        {
            LoggingService.Info($"Best match score too low ({bestMatch.MatchScore:P0}) for: {song.DisplayName}", "MetadataEnhancementService");
            return (null, null);
        }

        LoggingService.Info($"Using MusicBrainz match (score: {bestMatch.MatchScore:P0}): {bestMatch.Artist} - {bestMatch.Title}", "MetadataEnhancementService");

        // Create enhanced song
        var enhancedSong = new Song
        {
            Id = song.Id,
            Filename = song.Filename,
            Title = bestMatch.Title,
            Artists = new[] { bestMatch.Artist },
            Performers = new[] { bestMatch.Artist },
            Album = bestMatch.Album ?? song.Album,
            Year = bestMatch.Year?.ToString() ?? song.Year,
            TrackNumber = bestMatch.TrackNumber ?? song.TrackNumber,
            NAlbumTracks = bestMatch.TotalTracks ?? song.NAlbumTracks,
            DiscNumber = song.DiscNumber,
            Duration = song.Duration,
            Gain = song.Gain,
            Peak = song.Peak,
            MTime = song.MTime,
            CoverImagePath = song.CoverImagePath,
            SourceType = song.SourceType,
            YouTubeId = song.YouTubeId,
            YouTubeUrl = song.YouTubeUrl
        };

        // Write to file if requested and if it's a local file
        if (writeToFile && song.IsLocal && !string.IsNullOrEmpty(song.Filename))
        {
            var writeSuccess = _metadataService.WriteMusicBrainzMetadata(song.Filename, bestMatch);
            if (!writeSuccess)
            {
                LoggingService.Warning($"Failed to write metadata to file: {song.Filename}", "MetadataEnhancementService");
            }
        }

        // Download and embed cover art if requested
        if (downloadCoverArt && !string.IsNullOrEmpty(bestMatch.CoverArtUrl))
        {
            await DownloadAndEmbedCoverArtAsync(enhancedSong, bestMatch);
        }

        return (enhancedSong, bestMatch);
    }

    /// <summary>
    /// Enhance a song with a specific MusicBrainz match (for manual disambiguation)
    /// </summary>
    /// <param name="song">Song to enhance</param>
    /// <param name="match">The specific MusicBrainz match to use</param>
    /// <param name="writeToFile">Whether to write the enhanced metadata to the file</param>
    /// <param name="downloadCoverArt">Whether to download and embed cover art</param>
    /// <returns>Enhanced song</returns>
    public async Task<Song> EnhanceSongWithMatchAsync(
        Song song,
        MusicBrainzMatch match,
        bool writeToFile = true,
        bool downloadCoverArt = true)
    {
        if (song == null)
        {
            throw new ArgumentNullException(nameof(song));
        }

        if (match == null)
        {
            throw new ArgumentNullException(nameof(match));
        }

        LoggingService.Info($"Enhancing song with match: {match.Artist} - {match.Title}", "MetadataEnhancementService");

        // Create enhanced song
        var enhancedSong = new Song
        {
            Id = song.Id,
            Filename = song.Filename,
            Title = match.Title,
            Artists = new[] { match.Artist },
            Performers = new[] { match.Artist },
            Album = match.Album ?? song.Album,
            Year = match.Year?.ToString() ?? song.Year,
            TrackNumber = match.TrackNumber ?? song.TrackNumber,
            NAlbumTracks = match.TotalTracks ?? song.NAlbumTracks,
            DiscNumber = song.DiscNumber,
            Duration = song.Duration,
            Gain = song.Gain,
            Peak = song.Peak,
            MTime = song.MTime,
            CoverImagePath = song.CoverImagePath,
            SourceType = song.SourceType,
            YouTubeId = song.YouTubeId,
            YouTubeUrl = song.YouTubeUrl
        };

        // Write to file if requested and if it's a local file
        if (writeToFile && song.IsLocal && !string.IsNullOrEmpty(song.Filename))
        {
            var writeSuccess = _metadataService.WriteMusicBrainzMetadata(song.Filename, match);
            if (!writeSuccess)
            {
                LoggingService.Warning($"Failed to write metadata to file: {song.Filename}", "MetadataEnhancementService");
            }
        }

        // Download and embed cover art if requested
        if (downloadCoverArt && !string.IsNullOrEmpty(match.CoverArtUrl))
        {
            await DownloadAndEmbedCoverArtAsync(enhancedSong, match);
        }

        return enhancedSong;
    }

    /// <summary>
    /// Download and embed cover art for a song
    /// </summary>
    private async Task DownloadAndEmbedCoverArtAsync(Song song, MusicBrainzMatch match)
    {
        if (string.IsNullOrEmpty(match.CoverArtUrl))
        {
            return;
        }

        // For local files, embed the artwork
        if (song.IsLocal && !string.IsNullOrEmpty(song.Filename))
        {
            try
            {
                var success = await _metadataService.EmbedAlbumArtFromUrlAsync(song.Filename, match.CoverArtUrl);
                if (success)
                {
                    LoggingService.Info($"Cover art embedded in: {song.Filename}", "MetadataEnhancementService");
                    
                    // Update the song's cover image path
                    // Re-read metadata to get the embedded cover path
                    var updatedSong = _metadataService.ReadSongMetadata(song.Filename);
                    if (updatedSong?.CoverImagePath != null)
                    {
                        song.CoverImagePath = updatedSong.CoverImagePath;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Error($"Failed to download and embed cover art", ex, "MetadataEnhancementService");
            }
        }
        // For YouTube songs, just store the URL
        else if (song.IsYouTube)
        {
            song.CoverImagePath = match.CoverArtUrl;
        }
    }

    /// <summary>
    /// Enhance a YouTube song by matching it to MusicBrainz
    /// YouTube songs typically have less metadata, so this is especially useful
    /// </summary>
    /// <param name="youTubeSong">YouTube song to enhance</param>
    /// <returns>Enhanced song with MusicBrainz match, or null if no good match found</returns>
    public async Task<(Song? enhancedSong, MusicBrainzMatch? match)> EnhanceYouTubeSongAsync(Song youTubeSong)
    {
        if (youTubeSong == null)
        {
            throw new ArgumentNullException(nameof(youTubeSong));
        }

        if (!youTubeSong.IsYouTube)
        {
            throw new ArgumentException("Song is not a YouTube song", nameof(youTubeSong));
        }

        LoggingService.Info($"Enhancing YouTube song: {youTubeSong.DisplayName}", "MetadataEnhancementService");

        // YouTube songs can't have metadata written to file (they're streams)
        // But we can enhance the in-memory Song object
        return await EnhanceSongAsync(youTubeSong, writeToFile: false, downloadCoverArt: false);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _musicBrainzService?.Dispose();
            _disposed = true;
        }
    }
}
