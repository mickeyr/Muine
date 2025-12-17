namespace Muine.Core.Models;

/// <summary>
/// Source type for a song
/// </summary>
public enum SongSourceType
{
    Local = 0,
    YouTube = 1,
    // Future: Spotify, SoundCloud, etc.
}

public class Song
{
    public int Id { get; set; }
    public string Filename { get; set; } = string.Empty;
    public string Folder => Path.GetDirectoryName(Filename) ?? string.Empty;
    public string Title { get; set; } = string.Empty;
    public string[] Artists { get; set; } = Array.Empty<string>();
    public string[] Performers { get; set; } = Array.Empty<string>();
    public string Album { get; set; } = string.Empty;
    public int TrackNumber { get; set; }
    public int NAlbumTracks { get; set; }
    public int DiscNumber { get; set; }
    public string Year { get; set; } = string.Empty;
    public int Duration { get; set; }
    public double Gain { get; set; }
    public double Peak { get; set; }
    public int MTime { get; set; }
    public string? CoverImagePath { get; set; }
    
    /// <summary>
    /// Source type of the song (Local file, YouTube, etc.)
    /// </summary>
    public SongSourceType SourceType { get; set; } = SongSourceType.Local;
    
    /// <summary>
    /// YouTube video ID (only for YouTube songs)
    /// </summary>
    public string? YouTubeId { get; set; }
    
    /// <summary>
    /// YouTube video URL (only for YouTube songs)
    /// </summary>
    public string? YouTubeUrl { get; set; }
    
    // Computed properties
    public bool HasAlbum => !string.IsNullOrEmpty(Album);
    public string AlbumKey => $"{Folder}|{Album}";
    public string ArtistsString => Artists.Length > 0 ? string.Join(", ", Artists) : string.Empty;
    public string DisplayName => !string.IsNullOrEmpty(Title) ? Title : Path.GetFileNameWithoutExtension(Filename);
    public bool IsYouTube => SourceType == SongSourceType.YouTube;
    public bool IsLocal => SourceType == SongSourceType.Local;
}
