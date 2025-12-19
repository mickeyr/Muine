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
            // Use first 32 hex chars (128 bits) of the 256-bit hash for collision resistance
            var hashInput = song.AlbumKey;
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(hashInput));
            var hashString = Convert.ToHexString(hashBytes)[..32]; // 32 hex chars = 128 bits
            
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
            // Quick optimization: compare file sizes first
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length != newData.Length)
                return false;

            // If sizes match, compare content
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

    /// <summary>
    /// Write metadata to an audio file
    /// </summary>
    /// <param name="filename">Path to the audio file</param>
    /// <param name="song">Song metadata to write</param>
    /// <returns>True if successful, false otherwise</returns>
    public bool WriteSongMetadata(string filename, Song song)
    {
        if (!System.IO.File.Exists(filename))
        {
            LoggingService.Warning($"File not found: {filename}", "MetadataService");
            return false;
        }

        try
        {
            using var file = TagLib.File.Create(filename);
            var tag = file.Tag;

            // Write basic metadata
            tag.Title = song.Title;
            tag.Performers = song.Artists.Length > 0 ? song.Artists : song.Performers;
            tag.AlbumArtists = song.Artists;
            tag.Album = song.Album;
            tag.Track = (uint)song.TrackNumber;
            tag.TrackCount = (uint)song.NAlbumTracks;
            tag.Disc = (uint)song.DiscNumber;
            
            if (!string.IsNullOrEmpty(song.Year) && uint.TryParse(song.Year, out var year))
            {
                tag.Year = year;
            }

            // Save the file
            file.Save();
            
            LoggingService.Info($"Metadata written to: {filename}", "MetadataService");
            return true;
        }
        catch (Exception ex)
        {
            LoggingService.Error($"Failed to write metadata to {filename}", ex, "MetadataService");
            return false;
        }
    }

    /// <summary>
    /// Write metadata from a MusicBrainz match to an audio file
    /// </summary>
    /// <param name="filename">Path to the audio file</param>
    /// <param name="match">MusicBrainz match data</param>
    /// <returns>True if successful, false otherwise</returns>
    public bool WriteMusicBrainzMetadata(string filename, MusicBrainzMatch match)
    {
        if (!System.IO.File.Exists(filename))
        {
            LoggingService.Warning($"File not found: {filename}", "MetadataService");
            return false;
        }

        try
        {
            using var file = TagLib.File.Create(filename);
            var tag = file.Tag;

            // Write basic metadata
            tag.Title = match.Title;
            tag.Performers = new[] { match.Artist };
            tag.AlbumArtists = new[] { match.Artist };
            
            if (!string.IsNullOrEmpty(match.Album))
            {
                tag.Album = match.Album;
            }

            if (match.Year.HasValue)
            {
                tag.Year = (uint)match.Year.Value;
            }

            if (match.TrackNumber.HasValue)
            {
                tag.Track = (uint)match.TrackNumber.Value;
            }

            if (match.TotalTracks.HasValue)
            {
                tag.TrackCount = (uint)match.TotalTracks.Value;
            }

            if (match.Genres.Length > 0)
            {
                tag.Genres = match.Genres;
            }

            // Write MusicBrainz IDs (only for formats that support them)
            if (file.GetTag(TagTypes.Id3v2) is TagLib.Id3v2.Tag id3v2Tag)
            {
                WriteMusicBrainzIds(id3v2Tag, match);
            }
            else if (file.GetTag(TagTypes.Xiph) is XiphComment xiphComment)
            {
                WriteMusicBrainzIdsXiph(xiphComment, match);
            }

            // Save the file
            file.Save();
            
            LoggingService.Info($"MusicBrainz metadata written to: {filename}", "MetadataService");
            return true;
        }
        catch (Exception ex)
        {
            LoggingService.Error($"Failed to write MusicBrainz metadata to {filename}", ex, "MetadataService");
            return false;
        }
    }

    /// <summary>
    /// Write MusicBrainz IDs to ID3v2 tags (MP3)
    /// </summary>
    private void WriteMusicBrainzIds(TagLib.Id3v2.Tag tag, MusicBrainzMatch match)
    {
        // MusicBrainz uses TXXX frames for custom fields
        if (!string.IsNullOrEmpty(match.RecordingId))
        {
            SetUserTextInformationFrame(tag, "MusicBrainz Recording Id", match.RecordingId);
        }

        if (!string.IsNullOrEmpty(match.ReleaseId))
        {
            SetUserTextInformationFrame(tag, "MusicBrainz Release Id", match.ReleaseId);
        }

        if (!string.IsNullOrEmpty(match.ArtistId))
        {
            SetUserTextInformationFrame(tag, "MusicBrainz Artist Id", match.ArtistId);
        }
    }

    /// <summary>
    /// Write MusicBrainz IDs to Xiph comments (FLAC, OGG)
    /// </summary>
    private void WriteMusicBrainzIdsXiph(XiphComment comment, MusicBrainzMatch match)
    {
        if (!string.IsNullOrEmpty(match.RecordingId))
        {
            comment.SetField("MUSICBRAINZ_TRACKID", match.RecordingId);
        }

        if (!string.IsNullOrEmpty(match.ReleaseId))
        {
            comment.SetField("MUSICBRAINZ_ALBUMID", match.ReleaseId);
        }

        if (!string.IsNullOrEmpty(match.ArtistId))
        {
            comment.SetField("MUSICBRAINZ_ARTISTID", match.ArtistId);
        }
    }

    /// <summary>
    /// Set or update a TXXX (User Text Information) frame in ID3v2 tag
    /// </summary>
    private void SetUserTextInformationFrame(TagLib.Id3v2.Tag tag, string description, string value)
    {
        // Remove existing frame with this description
        var existingFrames = tag.GetFrames<UserTextInformationFrame>()
            .Where(f => f.Description == description)
            .ToList();

        foreach (var frame in existingFrames)
        {
            tag.RemoveFrame(frame);
        }

        // Add new frame
        var newFrame = UserTextInformationFrame.Get(tag, description, true);
        newFrame.Text = new[] { value };
    }

    /// <summary>
    /// Embed album artwork into an audio file
    /// </summary>
    /// <param name="filename">Path to the audio file</param>
    /// <param name="artworkPath">Path to the artwork image file</param>
    /// <returns>True if successful, false otherwise</returns>
    public bool EmbedAlbumArt(string filename, string artworkPath)
    {
        if (!System.IO.File.Exists(filename))
        {
            LoggingService.Warning($"Audio file not found: {filename}", "MetadataService");
            return false;
        }

        if (!System.IO.File.Exists(artworkPath))
        {
            LoggingService.Warning($"Artwork file not found: {artworkPath}", "MetadataService");
            return false;
        }

        try
        {
            using var file = TagLib.File.Create(filename);
            var tag = file.Tag;

            // Read the artwork file
            var artworkData = System.IO.File.ReadAllBytes(artworkPath);
            var mimeType = GetMimeType(artworkPath);

            // Create a picture
            var picture = new Picture
            {
                Type = PictureType.FrontCover,
                MimeType = mimeType,
                Description = "Cover",
                Data = artworkData
            };

            // Remove existing pictures
            tag.Pictures = new IPicture[] { picture };

            // Save the file
            file.Save();
            
            LoggingService.Info($"Album art embedded in: {filename}", "MetadataService");
            return true;
        }
        catch (Exception ex)
        {
            LoggingService.Error($"Failed to embed album art in {filename}", ex, "MetadataService");
            return false;
        }
    }

    /// <summary>
    /// Embed album artwork from a URL (downloads first, then embeds)
    /// </summary>
    /// <param name="filename">Path to the audio file</param>
    /// <param name="artworkUrl">URL of the artwork image</param>
    /// <returns>True if successful, false otherwise</returns>
    public async Task<bool> EmbedAlbumArtFromUrlAsync(string filename, string artworkUrl)
    {
        if (!System.IO.File.Exists(filename))
        {
            LoggingService.Warning($"Audio file not found: {filename}", "MetadataService");
            return false;
        }

        if (string.IsNullOrWhiteSpace(artworkUrl))
        {
            LoggingService.Warning("Artwork URL is empty", "MetadataService");
            return false;
        }

        try
        {
            // Download the artwork to a temporary file
            var tempPath = Path.Combine(Path.GetTempPath(), $"muine_artwork_{Guid.NewGuid()}.jpg");
            
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Muine/1.0");
            
            var response = await httpClient.GetAsync(artworkUrl);
            if (!response.IsSuccessStatusCode)
            {
                LoggingService.Warning($"Failed to download artwork from {artworkUrl}: {response.StatusCode}", "MetadataService");
                return false;
            }

            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            await System.IO.File.WriteAllBytesAsync(tempPath, imageBytes);

            // Embed the downloaded artwork
            var success = EmbedAlbumArt(filename, tempPath);

            // Clean up temporary file
            try
            {
                System.IO.File.Delete(tempPath);
            }
            catch
            {
                // Ignore cleanup errors
            }

            return success;
        }
        catch (Exception ex)
        {
            LoggingService.Error($"Failed to embed album art from URL {artworkUrl}", ex, "MetadataService");
            return false;
        }
    }

    /// <summary>
    /// Get MIME type for an image file
    /// </summary>
    private string GetMimeType(string filename)
    {
        var extension = Path.GetExtension(filename).ToLowerInvariant();
        return extension switch
        {
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            _ => "image/jpeg"
        };
    }
}
