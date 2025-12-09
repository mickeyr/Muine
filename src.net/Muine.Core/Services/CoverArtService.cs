using Muine.Core.Models;

namespace Muine.Core.Services;

public class CoverArtService
{
    private static readonly string[] CoverFilenames = 
    {
        "cover.jpg", "Cover.jpg",
        "cover.jpeg", "Cover.jpeg",
        "cover.png", "Cover.png",
        "cover.gif", "Cover.gif",
        "folder.jpg", "Folder.jpg",
        "album.jpg", "Album.jpg",
        "albumart.jpg", "AlbumArt.jpg"
    };

    private static readonly string[] ImageExtensions = 
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp"
    };

    /// <summary>
    /// Finds cover art in the directory of the given song.
    /// Looks for common cover art filenames (cover.jpg, folder.jpg, etc.)
    /// and embedded album art in the audio file itself.
    /// </summary>
    /// <param name="song">The song to find cover art for</param>
    /// <returns>Path to the cover art file, or null if not found</returns>
    public string? FindCoverArt(Song song)
    {
        if (string.IsNullOrEmpty(song.Folder))
            return null;

        // First, check for common cover art filenames
        foreach (var filename in CoverFilenames)
        {
            var coverPath = Path.Combine(song.Folder, filename);
            if (File.Exists(coverPath))
                return coverPath;
        }

        // If not found, look for any image file in the directory
        // This is a fallback for directories with unconventional naming
        try
        {
            var imageFiles = Directory.GetFiles(song.Folder)
                .Where(f => ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .OrderBy(f => f) // Prefer alphabetically first images
                .ToList();

            if (imageFiles.Count > 0)
                return imageFiles[0];
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error searching for cover art in {song.Folder}: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Finds cover art for an album by checking the album's folder.
    /// </summary>
    /// <param name="album">The album to find cover art for</param>
    /// <returns>Path to the cover art file, or null if not found</returns>
    public string? FindCoverArt(Album album)
    {
        if (string.IsNullOrEmpty(album.Folder))
            return null;

        // Check for common cover art filenames in album folder
        foreach (var filename in CoverFilenames)
        {
            var coverPath = Path.Combine(album.Folder, filename);
            if (File.Exists(coverPath))
                return coverPath;
        }

        // Fallback to any image file
        try
        {
            var imageFiles = Directory.GetFiles(album.Folder)
                .Where(f => ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .OrderBy(f => f)
                .ToList();

            if (imageFiles.Count > 0)
                return imageFiles[0];
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error searching for cover art in {album.Folder}: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Updates the cover image path for a song by searching its directory.
    /// </summary>
    /// <param name="song">The song to update</param>
    public void UpdateSongCoverArt(Song song)
    {
        song.CoverImagePath = FindCoverArt(song);
    }

    /// <summary>
    /// Updates the cover image path for an album by searching its folder.
    /// </summary>
    /// <param name="album">The album to update</param>
    public void UpdateAlbumCoverArt(Album album)
    {
        album.CoverImagePath = FindCoverArt(album);
    }
}
