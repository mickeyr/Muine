namespace Muine.Core.Models;

public class Album
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Song> Songs { get; set; } = new();
    public List<string> Artists { get; set; } = new();
    public string Year { get; set; } = string.Empty;
    public string Folder { get; set; } = string.Empty;
    public int TrackCount => Songs.Count;
    public int TotalTracks { get; set; }
    public bool IsComplete => TotalTracks > 0 && TrackCount >= TotalTracks;
    public string? CoverImagePath { get; set; }
    public int TotalDuration => Songs.Sum(s => s.Duration);
}
