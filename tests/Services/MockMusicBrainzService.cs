using Muine.Core.Models;
using Muine.Core.Services;

namespace Muine.Tests.Services;

/// <summary>
/// Mock implementation of IMusicBrainzService for testing
/// </summary>
public class MockMusicBrainzService : IMusicBrainzService
{
    private bool _disposed;

    // Properties to control mock behavior
    public List<MusicBrainzMatch> SearchResults { get; set; } = new();
    public MusicBrainzMatch? GetRecordingResult { get; set; }
    public bool DownloadCoverArtResult { get; set; } = true;
    public bool ThrowExceptionOnSearch { get; set; }
    public bool ThrowExceptionOnGetRecording { get; set; }
    public bool ThrowExceptionOnDownload { get; set; }

    // Track method calls for verification
    public int SearchCallCount { get; private set; }
    public int GetRecordingCallCount { get; private set; }
    public int DownloadCallCount { get; private set; }
    public string? LastSearchArtist { get; private set; }
    public string? LastSearchTitle { get; private set; }
    public string? LastRecordingId { get; private set; }

    public Task<List<MusicBrainzMatch>> SearchRecordingsAsync(string artist, string title, int maxResults = 10)
    {
        SearchCallCount++;
        LastSearchArtist = artist;
        LastSearchTitle = title;

        if (ThrowExceptionOnSearch)
        {
            throw new InvalidOperationException("Mock exception on search");
        }

        // Return a copy of results, limited by maxResults
        return Task.FromResult(SearchResults.Take(maxResults).ToList());
    }

    public Task<MusicBrainzMatch?> GetRecordingAsync(string recordingId)
    {
        GetRecordingCallCount++;
        LastRecordingId = recordingId;

        if (ThrowExceptionOnGetRecording)
        {
            throw new InvalidOperationException("Mock exception on get recording");
        }

        return Task.FromResult(GetRecordingResult);
    }

    public Task<bool> DownloadCoverArtAsync(string releaseId, string outputPath)
    {
        DownloadCallCount++;

        if (ThrowExceptionOnDownload)
        {
            throw new InvalidOperationException("Mock exception on download");
        }

        // Optionally create a dummy file
        if (DownloadCoverArtResult && !string.IsNullOrEmpty(outputPath))
        {
            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllBytes(outputPath, new byte[] { 0xFF, 0xD8, 0xFF }); // Minimal JPEG header
        }

        return Task.FromResult(DownloadCoverArtResult);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }

    /// <summary>
    /// Helper method to create a default mock match
    /// </summary>
    public static MusicBrainzMatch CreateMockMatch(
        string title = "Test Title",
        string artist = "Test Artist",
        string? album = "Test Album",
        double matchScore = 0.95)
    {
        return new MusicBrainzMatch
        {
            RecordingId = Guid.NewGuid().ToString(),
            ReleaseId = Guid.NewGuid().ToString(),
            ArtistId = Guid.NewGuid().ToString(),
            Title = title,
            Artist = artist,
            Album = album,
            Year = 2020,
            TrackNumber = 1,
            TotalTracks = 10,
            Genres = new[] { "Rock", "Pop" },
            MatchScore = matchScore,
            CoverArtUrl = $"https://coverartarchive.org/release/{Guid.NewGuid()}/front-250"
        };
    }
}
