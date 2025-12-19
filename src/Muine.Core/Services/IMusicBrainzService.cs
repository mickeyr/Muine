using Muine.Core.Models;

namespace Muine.Core.Services;

/// <summary>
/// Interface for MusicBrainz service operations
/// </summary>
public interface IMusicBrainzService : IDisposable
{
    /// <summary>
    /// Search for recordings (songs) by artist and title
    /// </summary>
    Task<List<MusicBrainzMatch>> SearchRecordingsAsync(string artist, string title, int maxResults = 10);

    /// <summary>
    /// Get detailed recording information by MusicBrainz recording ID
    /// </summary>
    Task<MusicBrainzMatch?> GetRecordingAsync(string recordingId);

    /// <summary>
    /// Download cover art from Cover Art Archive
    /// </summary>
    Task<bool> DownloadCoverArtAsync(string releaseId, string outputPath);
}
