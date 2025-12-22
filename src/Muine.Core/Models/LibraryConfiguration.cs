namespace Muine.Core.Models;

/// <summary>
/// Configuration for the managed music library
/// </summary>
public class LibraryConfiguration
{
    /// <summary>
    /// Root directory for the managed music library
    /// </summary>
    public string LibraryPath { get; set; } = GetDefaultLibraryPath();

    /// <summary>
    /// Whether to copy files instead of moving them (default: false = move)
    /// </summary>
    public bool CopyInsteadOfMove { get; set; } = false;

    /// <summary>
    /// Directory structure pattern for organizing files
    /// Format: {Artist}/{Year} - {Album}/{TrackNumber} - {Title}.ext
    /// </summary>
    public string OrganizationPattern { get; set; } = "{Artist}/{Year} - {Album}/{TrackNumber} - {Title}";

    /// <summary>
    /// Whether to automatically enhance metadata during import
    /// </summary>
    public bool AutoEnhanceMetadata { get; set; } = true;

    /// <summary>
    /// Whether library has been initialized and scanned at least once
    /// </summary>
    public bool HasBeenInitialized { get; set; } = false;

    /// <summary>
    /// Get the default library path based on the user's Music folder
    /// </summary>
    public static string GetDefaultLibraryPath()
    {
        var musicFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        
        // If MyMusic is not available, fallback to user profile
        if (string.IsNullOrEmpty(musicFolder) || musicFolder == Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))
        {
            musicFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Music");
        }

        return Path.Combine(musicFolder, "Muine");
    }

    /// <summary>
    /// Ensure the library directory exists
    /// </summary>
    public void EnsureLibraryDirectoryExists()
    {
        if (!Directory.Exists(LibraryPath))
        {
            Directory.CreateDirectory(LibraryPath);
        }
    }
}
