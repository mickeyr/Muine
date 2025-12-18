# MusicBrainz Metadata Enhancement

This document describes the MusicBrainz integration features added to Muine.

## Overview

Muine now includes comprehensive MusicBrainz integration for automatic metadata enhancement. This allows you to:
- Match local music files to MusicBrainz database entries
- Enhance YouTube song metadata with accurate artist/album information
- Write ID3 tags to MP3/FLAC/OGG files
- Download and embed album artwork from Cover Art Archive
- Process metadata updates in the background with rate limiting

## Features

### 1. MusicBrainz Service
The `MusicBrainzService` provides rate-limited access to the MusicBrainz API:

```csharp
using var service = new MusicBrainzService();

// Search for recordings
var matches = await service.SearchRecordingsAsync("The Beatles", "Let It Be", maxResults: 10);

// Get detailed recording information
var recording = await service.GetRecordingAsync(recordingId);

// Download cover art
await service.DownloadCoverArtAsync(releaseId, outputPath);
```

**Rate Limiting:** The service automatically enforces MusicBrainz's 1 request per second limit for unauthenticated access.

### 2. Metadata Enhancement Service
The `MetadataEnhancementService` orchestrates the matching and enhancement process:

```csharp
using var enhancer = new MetadataEnhancementService();

// Find matches for a song
var matches = await enhancer.FindMatchesAsync(song);

// Enhance with best match (writes to file)
var (enhancedSong, match) = await enhancer.EnhanceSongAsync(song, 
    writeToFile: true, 
    downloadCoverArt: true);

// Enhance with specific match (manual disambiguation)
var enhancedSong = await enhancer.EnhanceSongWithMatchAsync(song, match);

// Enhance YouTube song (no file writing)
var (ytEnhanced, ytMatch) = await enhancer.EnhanceYouTubeSongAsync(youtubeSong);
```

**Match Scoring:** Songs are matched based on artist, title, album, and year. Matches with confidence scores below 70% are rejected automatically.

### 3. Background Tagging Queue
The `BackgroundTaggingQueue` processes metadata updates in the background:

```csharp
using var queue = new BackgroundTaggingQueue();

// Subscribe to events
queue.WorkCompleted += (sender, args) =>
{
    Console.WriteLine($"Tagged: {args.EnhancedSong.Title}");
};

queue.WorkFailed += (sender, args) =>
{
    Console.WriteLine($"Failed: {args.Song.Title} - {args.ErrorMessage}");
};

// Queue single song
queue.EnqueueSong(song, downloadCoverArt: true);

// Queue multiple songs
queue.EnqueueSongs(songs, downloadCoverArt: true);

// Check status
Console.WriteLine($"Queue size: {queue.QueueSize}");
Console.WriteLine($"Processing: {queue.IsProcessing}");
```

**Background Processing:** The queue automatically respects rate limits and processes items one at a time. Events are raised when work completes or fails.

### 4. Library Scanner Integration
The library scanner now supports automatic metadata enhancement:

```csharp
var scanner = new LibraryScannerService(
    metadataService, 
    databaseService, 
    coverArtService, 
    taggingQueue);

// Scan with auto-enhancement enabled
await scanner.ScanDirectoryAsync(
    directory, 
    progress, 
    autoEnhanceMetadata: true);
```

When enabled, newly imported songs are automatically queued for metadata enhancement in the background.

## Metadata Written

The following metadata fields are written to audio files:

### Basic Tags
- Title
- Artist(s)
- Album
- Year
- Track Number
- Total Tracks
- Genres/Tags

### MusicBrainz IDs
For MP3 files (ID3v2):
- MusicBrainz Recording Id (TXXX frame)
- MusicBrainz Release Id (TXXX frame)
- MusicBrainz Artist Id (TXXX frame)

For FLAC/OGG files (Xiph comments):
- MUSICBRAINZ_TRACKID
- MUSICBRAINZ_ALBUMID
- MUSICBRAINZ_ARTISTID

### Album Artwork
Cover art is embedded as front cover picture with appropriate MIME type.

## Rate Limiting

MusicBrainz enforces rate limits:
- **Unauthenticated**: 1 request per second
- **Authenticated**: Higher limits (requires MusicBrainz account)

The services automatically enforce these limits. For large libraries, use the `BackgroundTaggingQueue` to process songs over time without blocking.

## Authentication (Optional)

While not currently exposed in the UI, MusicBrainz authentication can be configured programmatically:

```csharp
var service = new MusicBrainzService(
    applicationName: "Muine",
    applicationVersion: "1.0",
    contactEmail: "your-email@example.com",
    username: "your-username",  // Optional
    password: "your-password"   // Optional
);
```

Authentication increases rate limits and provides access to additional features.

## API Documentation

For more information on the MusicBrainz API:
- [MusicBrainz API Documentation](https://musicbrainz.org/doc/MusicBrainz_API)
- [Cover Art Archive](https://coverartarchive.org/)
- [MusicBrainz Identifier Guidelines](https://musicbrainz.org/doc/MusicBrainz_Identifier)

## Error Handling

All services handle errors gracefully:
- API failures return empty results or null
- Network errors are logged but don't crash the application
- Rate limiting is enforced automatically
- Background queue continues processing even if individual items fail

## Performance Considerations

- MusicBrainz queries are rate-limited, so bulk operations take time
- Use the background queue for large libraries
- Cover art is downloaded once and cached
- Existing metadata is preserved if enhancement fails

## Examples

### Example 1: Enhance a Single Song
```csharp
using var enhancer = new MetadataEnhancementService();

var song = new Song 
{ 
    Title = "Hey Jude", 
    Artists = new[] { "Beatles" },
    Filename = "/path/to/song.mp3"
};

var (enhanced, match) = await enhancer.EnhanceSongAsync(song);

if (enhanced != null)
{
    Console.WriteLine($"Enhanced: {enhanced.Artist} - {enhanced.Title}");
    Console.WriteLine($"Album: {enhanced.Album} ({enhanced.Year})");
}
```

### Example 2: Batch Process Library
```csharp
using var queue = new BackgroundTaggingQueue();

int completedCount = 0;
queue.WorkCompleted += (s, e) => completedCount++;

var allSongs = await database.GetAllSongsAsync();
queue.EnqueueSongs(allSongs);

Console.WriteLine($"Queued {allSongs.Count} songs for processing...");
// Processing continues in background
```

### Example 3: Manual Disambiguation
```csharp
using var enhancer = new MetadataEnhancementService();

var song = new Song { Title = "Time", Artists = new[] { "Pink Floyd" } };

// Get multiple matches
var matches = await enhancer.FindMatchesAsync(song, maxResults: 10);

// Display to user and let them choose
foreach (var match in matches)
{
    Console.WriteLine($"{match.Title} - {match.Album} ({match.Year}) [Score: {match.MatchScore:P0}]");
}

// User selects match at index 2
var selectedMatch = matches[2];
var enhanced = await enhancer.EnhanceSongWithMatchAsync(song, selectedMatch);
```

## Future Enhancements

Planned improvements include:
- UI for manual metadata enhancement
- UI for disambiguation when multiple matches exist
- Settings page for MusicBrainz credentials
- Queue status indicator in main window
- Context menu "Enhance Metadata" option
- Bulk enhancement for entire albums
