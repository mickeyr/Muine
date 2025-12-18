namespace Muine.Core.Models;

/// <summary>
/// Represents a MusicBrainz match result for a song
/// </summary>
public class MusicBrainzMatch
{
    /// <summary>
    /// MusicBrainz Recording ID (song ID)
    /// </summary>
    public string? RecordingId { get; set; }
    
    /// <summary>
    /// MusicBrainz Release ID (album ID)
    /// </summary>
    public string? ReleaseId { get; set; }
    
    /// <summary>
    /// MusicBrainz Artist ID
    /// </summary>
    public string? ArtistId { get; set; }
    
    /// <summary>
    /// Title from MusicBrainz
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Artist name from MusicBrainz
    /// </summary>
    public string Artist { get; set; } = string.Empty;
    
    /// <summary>
    /// Album name from MusicBrainz (if available)
    /// </summary>
    public string? Album { get; set; }
    
    /// <summary>
    /// Release year (if available)
    /// </summary>
    public int? Year { get; set; }
    
    /// <summary>
    /// Track number on the album (if available)
    /// </summary>
    public int? TrackNumber { get; set; }
    
    /// <summary>
    /// Total tracks on the album (if available)
    /// </summary>
    public int? TotalTracks { get; set; }
    
    /// <summary>
    /// Genre/tags from MusicBrainz
    /// </summary>
    public string[] Genres { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Match confidence score (0.0 to 1.0)
    /// Higher values indicate better matches
    /// </summary>
    public double MatchScore { get; set; }
    
    /// <summary>
    /// Album artwork URL from Cover Art Archive
    /// </summary>
    public string? CoverArtUrl { get; set; }
    
    /// <summary>
    /// Disambiguation comment (e.g., "live", "acoustic", etc.)
    /// </summary>
    public string? Disambiguation { get; set; }
}
