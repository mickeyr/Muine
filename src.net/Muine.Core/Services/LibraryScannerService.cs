using Muine.Core.Models;

namespace Muine.Core.Services;

public class LibraryScannerService
{
    private readonly MetadataService _metadataService;
    private readonly MusicDatabaseService _databaseService;
    private readonly CoverArtService _coverArtService;

    public LibraryScannerService(
        MetadataService metadataService, 
        MusicDatabaseService databaseService,
        CoverArtService coverArtService)
    {
        _metadataService = metadataService;
        _databaseService = databaseService;
        _coverArtService = coverArtService;
    }

    public async Task<ScanResult> ScanDirectoryAsync(string directory, IProgress<ScanProgress>? progress = null)
    {
        var result = new ScanResult();
        
        if (!Directory.Exists(directory))
        {
            result.Errors.Add($"Directory not found: {directory}");
            return result;
        }

        var files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories)
            .Where(f => _metadataService.IsSupportedFormat(f))
            .ToList();

        result.TotalFiles = files.Count;

        for (int i = 0; i < files.Count; i++)
        {
            var file = files[i];
            try
            {
                var song = _metadataService.ReadSongMetadata(file);
                if (song != null)
                {
                    // Find and set cover art for the song
                    _coverArtService.UpdateSongCoverArt(song);
                    
                    await _databaseService.SaveSongAsync(song);
                    result.SuccessCount++;
                }
                else
                {
                    result.Errors.Add($"Failed to read metadata: {file}");
                    result.FailureCount++;
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Error processing {file}: {ex.Message}");
                result.FailureCount++;
            }

            progress?.Report(new ScanProgress
            {
                CurrentFile = file,
                ProcessedFiles = i + 1,
                TotalFiles = files.Count
            });
        }

        return result;
    }

    public async Task<RefreshResult> RefreshSongAsync(Song song)
    {
        var result = new RefreshResult();
        await RefreshSingleSongInternalAsync(song, result);
        return result;
    }

    public async Task<RefreshResult> RefreshAllSongsAsync(IProgress<ScanProgress>? progress = null)
    {
        var result = new RefreshResult();
        
        var songs = await _databaseService.GetAllSongsAsync();
        result.TotalFiles = songs.Count;

        for (int i = 0; i < songs.Count; i++)
        {
            var song = songs[i];
            await RefreshSingleSongInternalAsync(song, result);

            progress?.Report(new ScanProgress
            {
                CurrentFile = song.Filename,
                ProcessedFiles = i + 1,
                TotalFiles = songs.Count
            });
        }

        return result;
    }

    private async Task RefreshSingleSongInternalAsync(Song song, RefreshResult result)
    {
        if (!System.IO.File.Exists(song.Filename))
        {
            result.Errors.Add($"File not found: {song.Filename}");
            result.FailureCount++;
            return;
        }

        try
        {
            var refreshedSong = _metadataService.ReadSongMetadata(song.Filename);
            if (refreshedSong != null)
            {
                // Preserve the original ID
                refreshedSong.Id = song.Id;
                
                // Find and set cover art for the song
                _coverArtService.UpdateSongCoverArt(refreshedSong);
                
                await _databaseService.SaveSongAsync(refreshedSong);
                result.SuccessCount++;
            }
            else
            {
                result.Errors.Add($"Failed to read metadata: {song.Filename}");
                result.FailureCount++;
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Error refreshing {song.Filename}: {ex.Message}");
            result.FailureCount++;
        }
    }
}

public class ScanResult
{
    public int TotalFiles { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class ScanProgress
{
    public string CurrentFile { get; set; } = string.Empty;
    public int ProcessedFiles { get; set; }
    public int TotalFiles { get; set; }
    public double PercentComplete => TotalFiles > 0 ? (ProcessedFiles * 100.0 / TotalFiles) : 0;
}

public class RefreshResult
{
    public int TotalFiles { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> Errors { get; set; } = new();
}
