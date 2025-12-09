using Muine.Core.Models;
using TagLib;
using TagLib.Id3v2;
using TagLib.Ogg;
using System.Security.Cryptography;
using System.Text;

namespace Muine.Core.Services;

public class MetadataService
{
    // ReplayGain field names for Vorbis/Xiph comments (FLAC, OGG)
    private static readonly string[] ReplayGainGainFields = 
        { "replaygain_track_gain", "replaygain_album_gain", "rg_audiophile", "rg_radio" };
    
    private static readonly string[] ReplayGainPeakFields = 
        { "replaygain_track_peak", "replaygain_album_peak", "rg_peak" };

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

            // Read ReplayGain tags (Gain and Peak)
            ReadReplayGainTags(file, song);

            // Extract embedded album art
            ExtractEmbeddedAlbumArt(file, song);

            return song;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error reading metadata from {filename}: {ex.Message}");
            return null;
        }
    }

    private void ReadReplayGainTags(TagLib.File file, Song song)
    {
        try
        {
            // Try reading from ID3v2 RVA2 frames (MP3)
            var id3v2Tag = file.GetTag(TagTypes.Id3v2) as TagLib.Id3v2.Tag;
            if (id3v2Tag != null)
            {
                var rva2Frames = id3v2Tag.GetFrames<RelativeVolumeFrame>();
                foreach (var frame in rva2Frames)
                {
                    song.Gain = frame.GetVolumeAdjustment(ChannelType.MasterVolume);
                    song.Peak = frame.GetPeakVolume(ChannelType.MasterVolume);
                    return; // Found RVA2, use it
                }
            }

            // Try reading from Vorbis comments (FLAC, OGG)
            var xiphComment = file.GetTag(TagTypes.Xiph) as XiphComment;
            if (xiphComment != null)
            {
                // Try different ReplayGain field names (in order of preference)
                foreach (var fieldName in ReplayGainGainFields)
                {
                    var values = xiphComment.GetField(fieldName);
                    if (values != null && values.Length > 0)
                    {
                        if (double.TryParse(values[0], out double gain))
                        {
                            song.Gain = gain;
                            break;
                        }
                    }
                }

                foreach (var fieldName in ReplayGainPeakFields)
                {
                    var values = xiphComment.GetField(fieldName);
                    if (values != null && values.Length > 0)
                    {
                        if (double.TryParse(values[0], out double peak))
                        {
                            song.Peak = peak;
                            break;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // ReplayGain is optional, don't fail if we can't read it
            Console.Error.WriteLine($"Warning: Could not read ReplayGain tags from {file.Name}: {ex.Message}");
        }
    }

    private void ExtractEmbeddedAlbumArt(TagLib.File file, Song song)
    {
        try
        {
            // Try to get pictures from the tag
            var pictures = file.Tag.Pictures;
            if (pictures == null || pictures.Length == 0)
                return;

            // First, try to find a front cover
            foreach (var picture in pictures)
            {
                if (picture.Type == PictureType.FrontCover)
                {
                    SaveEmbeddedAlbumArt(picture, song);
                    return;
                }
            }

            // If no front cover, use the first available picture
            if (pictures.Length > 0)
            {
                SaveEmbeddedAlbumArt(pictures[0], song);
            }
        }
        catch (Exception ex)
        {
            // Embedded album art is optional, don't fail if we can't extract it
            Console.Error.WriteLine($"Warning: Could not extract embedded album art from {file.Name}: {ex.Message}");
        }
    }

    private void SaveEmbeddedAlbumArt(IPicture picture, Song song)
    {
        try
        {
            // Create a cache directory for embedded album art
            var cacheDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Muine",
                "AlbumArtCache");

            if (!Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }

            // Create a unique filename based on the song's album and folder using SHA256
            // This ensures unique cache files without collisions
            var hashInput = song.AlbumKey;
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(hashInput));
            var hashString = Convert.ToHexString(hashBytes)[..16]; // Use first 16 chars for reasonable length
            
            var extension = GetImageExtension(picture.MimeType);
            var artPath = Path.Combine(cacheDir, $"embedded_{hashString}{extension}");

            // Only write if file doesn't exist or content is different (optimization)
            if (!System.IO.File.Exists(artPath) || !AreFilesEqual(artPath, picture.Data.Data))
            {
                System.IO.File.WriteAllBytes(artPath, picture.Data.Data);
            }
            
            song.CoverImagePath = artPath;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: Could not save embedded album art: {ex.Message}");
        }
    }

    private static bool AreFilesEqual(string filePath, byte[] newData)
    {
        try
        {
            var existingData = System.IO.File.ReadAllBytes(filePath);
            return existingData.SequenceEqual(newData);
        }
        catch
        {
            // If we can't read the file, assume they're different
            return false;
        }
    }

    private string GetImageExtension(string mimeType)
    {
        return mimeType?.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/jpg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/bmp" => ".bmp",
            _ => ".jpg" // Default to jpg
        };
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
