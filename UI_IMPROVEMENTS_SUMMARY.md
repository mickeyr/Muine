# UI Improvements Implementation Summary

This document provides a comprehensive overview of the UI improvements implemented for Muine Music Player.

## Overview

The implementation successfully addresses all requirements from the issue:

1. ✅ Music Library view with Artist → Album → Song organization
2. ✅ Search functionality across artists, albums, and songs
3. ✅ Playlist view showing queued songs with metadata
4. ✅ Cover art display in both views with fallback image
5. ✅ Metadata editing capability including cover art

## Architecture

### New Models

#### `Playlist.cs`
- Manages the playback queue
- Tracks current playing index
- Supports add, remove, move, next, previous operations
- Handles index adjustment when items are removed

### New ViewModels

#### `ArtistViewModel.cs`
- Represents an artist in the hierarchical library view
- Contains collection of albums by the artist
- Tracks total songs and album count

#### `AlbumViewModel.cs`
- Represents an album in the hierarchical library view
- Contains collection of songs in the album
- Displays album name, year, and cover art

#### `MusicLibraryViewModel.cs`
- Main view model for the music library tab
- Loads and organizes songs by artist and album
- Implements search functionality
- Supports two view modes: grouped and list

#### `PlaylistViewModel.cs`
- Manages the playlist queue
- Handles add/remove operations
- Tracks current playing position
- Supports song reordering

#### `MetadataEditorViewModel.cs`
- Manages metadata editing for individual songs
- Supports editing title, artist(s), album, year, track number
- Handles cover art selection
- Saves changes to database

### New Views

#### `MusicLibraryView.axaml`
- Tab-based layout with two modes:
  - **Grouped View**: Expandable Artist → Album → Song hierarchy
  - **List View**: Flat list with search filtering
- Cover art displayed for albums and songs
- Context menu for adding to playlist and editing metadata
- Double-click to add song to playlist
- Search bar with real-time filtering

#### `PlaylistView.axaml`
- Shows queued songs with metadata
- Cover art display for each song
- Remove button for each song
- Double-click to play song
- Clear playlist button

#### `MetadataEditorWindow.axaml`
- Modal dialog for editing song metadata
- Fields for title, artist(s), album, year, track number
- Cover art preview with selection button
- Save/Cancel buttons
- Live validation (Save enabled only when changes made)

### Enhanced Existing Components

#### `MainWindow.axaml`
Updated to use `TabControl` with two tabs:
- Music Library
- Playlist

Improved player controls:
- Previous/Play/Stop/Next buttons
- Progress slider
- Time display
- Volume control
- Current song display

#### `MainWindowViewModel.cs`
Added:
- `MusicLibraryViewModel` and `PlaylistViewModel` properties
- `SelectedTabIndex` for tab navigation
- `AddSongToPlaylist()` method
- `AddAlbumToPlaylist()` method
- `PlayNext()` and `PlayPrevious()` commands
- `CreateMetadataEditor()` method
- `RefreshAfterMetadataEdit()` method

#### `MusicDatabaseService.cs`
Added:
- `GetSongsGroupedByArtistAndAlbumAsync()` - Groups songs by artist and album
- `SearchSongsAsync()` - Searches across title, artist, album fields

### Assets

#### `default-cover.svg`
- SVG fallback image for songs/albums without cover art
- Simple, professional design with music note icon
- Used throughout the UI when `CoverImagePath` is null

## Features Implemented

### 1. Music Library Organization

The library view organizes songs in a hierarchical structure:

```
Artist Name (5 albums)
  └─ Album Name (2024) [cover art] (12 tracks)
      ├─ 1. Song Title
      ├─ 2. Another Song
      └─ ...
```

**Features:**
- Expandable/collapsible sections
- Cover art at album level
- Track numbers displayed
- Album metadata (year, track count)
- Sort by artist name, then album year

### 2. Search Functionality

Real-time search across:
- Song titles
- Artist names
- Album names
- Performers

**Implementation:**
- Database query with LIKE operator
- Async search with debouncing via `OnSearchQueryChanged`
- Updates filtered results in real-time
- Works in both grouped and list views

### 3. Playlist Management

**Features:**
- Add songs via double-click in library
- Remove songs via button or command
- Clear entire playlist
- View current queue with metadata
- Navigate with Previous/Next buttons
- Visual indication of current song (framework ready)

**Queue Management:**
```csharp
playlist.Add(song);           // Add to end
playlist.Next();              // Move to next song
playlist.Previous();          // Move to previous song
playlist.Remove(song);        // Remove specific song
playlist.Clear();             // Clear all
```

### 4. Cover Art Display

**Sources (in priority order):**
1. Song's `CoverImagePath` (from database/embedded art)
2. Default fallback image (`/Assets/default-cover.svg`)

**Display locations:**
- Album headers in grouped view
- Song items in list view
- Playlist items
- Metadata editor

### 5. Metadata Editing

**Editable fields:**
- Title
- Artist(s) - comma or semicolon separated
- Album
- Year
- Track Number
- Cover Art Image Path

**Workflow:**
1. Right-click song in library → "Edit Metadata..."
2. Edit fields in modal dialog
3. Click "Save" to persist to database
4. Library view refreshes automatically

**Limitations:**
- Currently saves to database only
- Does not write tags back to audio files
- This is documented as a future enhancement

## User Workflows

### Adding Music to Library
1. File → Import Music Folder
2. Select folder containing music files
3. Wait for scanning to complete
4. Songs appear organized in Music Library tab

### Playing Music
1. Navigate to Music Library tab
2. Double-click a song to add to playlist
3. Switch to Playlist tab (automatic)
4. Use Play/Pause/Stop/Next/Previous controls

### Searching for Music
1. Go to Music Library tab
2. Type in search box at top
3. Results filter in real-time
4. Clear search to show all songs

### Editing Metadata
1. Find song in Music Library
2. Right-click → "Edit Metadata..."
3. Modify fields as needed
4. Click "Save" to persist changes

### Managing Playlist
1. Add songs by double-clicking in Library
2. View queue in Playlist tab
3. Remove unwanted songs with X button
4. Clear all with "Clear Playlist" button

## Technical Details

### View Modes

**Grouped View:**
- Hierarchical: Artist → Album → Song
- Best for browsing by artist
- Shows album art and metadata
- Collapsible sections for organization

**List View:**
- Flat list of all songs
- Best for searching
- Shows individual song cover art
- Faster scrolling through large libraries

### Data Flow

```
Database
   ↓
MusicDatabaseService.GetSongsGroupedByArtistAndAlbumAsync()
   ↓
MusicLibraryViewModel.LoadLibraryAsync()
   ↓
MusicLibraryView (displays in UI)
   ↓
User interaction (double-click, etc.)
   ↓
MainWindowViewModel (coordinates)
   ↓
PlaylistViewModel (manages queue)
   ↓
PlaylistView (displays queue)
```

### Search Implementation

```csharp
// Database query with wildcards
SELECT * FROM Songs 
WHERE Title LIKE '%query%' 
   OR Artists LIKE '%query%' 
   OR Album LIKE '%query%'
   OR Performers LIKE '%query%'
ORDER BY Artists, Album, DiscNumber, TrackNumber
```

### Playlist Index Management

The playlist maintains a `CurrentIndex` that:
- Points to the currently playing song
- Adjusts automatically when songs are removed
- Enables Previous/Next navigation
- Handles edge cases (empty playlist, last song, etc.)

## Testing

### Existing Tests
All 55 existing tests pass:
- Model tests (Song, Album)
- Service tests (Database, Metadata, Scanner, Playback, CoverArt)
- Integration tests

### Security Scan
CodeQL security scan: **0 alerts found**

### Manual Testing Recommended
1. Import a music folder with various artists/albums
2. Test search with different queries
3. Add songs to playlist and play them
4. Edit metadata for a song
5. Test playlist navigation (next/previous)
6. Remove songs from playlist
7. Test both grouped and list views
8. Verify cover art display and fallback

## Future Enhancements

### Metadata File Writing
Currently, metadata edits only save to the database. To write back to audio files:

```csharp
public async Task WriteMetadataAsync(Song song)
{
    var file = TagLib.File.Create(song.Filename);
    file.Tag.Title = song.Title;
    file.Tag.Performers = song.Artists;
    file.Tag.Album = song.Album;
    file.Tag.Year = uint.Parse(song.Year);
    file.Tag.Track = (uint)song.TrackNumber;
    
    // Handle cover art
    if (!string.IsNullOrEmpty(song.CoverImagePath))
    {
        var imageData = File.ReadAllBytes(song.CoverImagePath);
        var picture = new TagLib.Picture(imageData);
        file.Tag.Pictures = new[] { picture };
    }
    
    await Task.Run(() => file.Save());
}
```

### Additional Features to Consider
- Playlist persistence (save/load playlists)
- Smart playlists (auto-generated based on criteria)
- Album art download from online services
- Bulk metadata editing
- Song ratings and play counts
- Advanced search filters
- Keyboard shortcuts
- Drag-and-drop reordering in playlist

## File Summary

### Created Files (16)
- `src.net/Muine.Core/Models/Playlist.cs`
- `src.net/Muine.App/ViewModels/ArtistViewModel.cs`
- `src.net/Muine.App/ViewModels/AlbumViewModel.cs`
- `src.net/Muine.App/ViewModels/MusicLibraryViewModel.cs`
- `src.net/Muine.App/ViewModels/PlaylistViewModel.cs`
- `src.net/Muine.App/ViewModels/MetadataEditorViewModel.cs`
- `src.net/Muine.App/Views/MusicLibraryView.axaml`
- `src.net/Muine.App/Views/MusicLibraryView.axaml.cs`
- `src.net/Muine.App/Views/PlaylistView.axaml`
- `src.net/Muine.App/Views/PlaylistView.axaml.cs`
- `src.net/Muine.App/Views/MetadataEditorWindow.axaml`
- `src.net/Muine.App/Views/MetadataEditorWindow.axaml.cs`
- `src.net/Muine.App/Assets/default-cover.svg`

### Modified Files (3)
- `src.net/Muine.Core/Services/MusicDatabaseService.cs` - Added grouping and search
- `src.net/Muine.App/ViewModels/MainWindowViewModel.cs` - Added playlist integration
- `src.net/Muine.App/Views/MainWindow.axaml` - Added tab navigation

## Conclusion

This implementation successfully delivers all requested features:
- ✅ Music Library with Artist/Album organization
- ✅ Search functionality
- ✅ Playlist view with queue management
- ✅ Cover art display with fallbacks
- ✅ Metadata editing

The code follows existing patterns, passes all tests, has no security issues, and provides a solid foundation for future enhancements.
