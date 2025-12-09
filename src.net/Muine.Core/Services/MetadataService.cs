using Muine.Core.Models;
using TagLib;

namespace Muine.Core.Services;

public class MetadataService
{
    public Song? ReadSongMetadata(string filename)
    {
        if (!System.IO.File.Exists(filename))
            return null;

        try
        {
            using var file = TagLib.File.Create(filename);
            var tag = file.Tag;
            var properties = file.Properties;

            var song = new Song
            {
                Filename = filename,
                Title = tag.Title ?? Path.GetFileNameWithoutExtension(filename),
                Artists = tag.AlbumArtists.Length > 0 ? tag.AlbumArtists : tag.Performers,
                Performers = tag.Performers,
                Album = tag.Album ?? string.Empty,
                TrackNumber = (int)tag.Track,
                NAlbumTracks = (int)tag.TrackCount,
                DiscNumber = (int)tag.Disc,
                Year = tag.Year > 0 ? tag.Year.ToString() : string.Empty,
                Duration = (int)properties.Duration.TotalSeconds
            };

            var fileInfo = new FileInfo(filename);
            song.MTime = (int)(fileInfo.LastWriteTimeUtc - DateTime.UnixEpoch).TotalSeconds;

            return song;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error reading metadata from {filename}: {ex.Message}");
            return null;
        }
    }

    public bool IsSupportedFormat(string filename)
    {
        var extension = Path.GetExtension(filename).ToLowerInvariant();
        return extension switch
        {
            ".mp3" => true,
            ".ogg" => true,
            ".flac" => true,
            ".m4a" => true,
            ".aac" => true,
            ".wma" => true,
            ".wav" => true,
            ".opus" => true,
            _ => false
        };
    }
}
