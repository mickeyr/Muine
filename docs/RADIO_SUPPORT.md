# Internet Radio Support

Muine now includes comprehensive internet radio station support, allowing you to listen to streaming radio stations alongside your local music library.

## Features

- **Stream Playback**: Play HTTP audio streams (MP3, OGG, AAC, etc.)
- **Playlist Support**: Automatically parse PLS and M3U playlist files
- **Metadata Extraction**: Extract station info from ICY headers and playlists
- **Hierarchical Organization**: Organize stations by genre/location (e.g., "Music > Rock", "Sports > Atlanta")
- **Station Management**: Add, edit, delete stations via UI
- **Search**: Search stations by name, genre, or location
- **Persistent Storage**: Stations stored in SQLite database
- **Last Played Tracking**: Remember when each station was last played

## Quick Start

### Adding a Radio Station

1. Click the **"Radio"** tab in Muine
2. Click **"Add Station"** button
3. Enter the stream URL (examples below)
4. Click **"Extract Metadata"** to automatically fill in station details
5. Optionally categorize:
   - **Parent Category**: e.g., "Music", "Sports", "News"
   - **Sub Category**: e.g., "Rock", "Atlanta", "Local"
6. Click **"Save"**

### Playing a Station

1. Navigate to the **"Radio"** tab
2. Browse by category in the tree view (left side) OR search in the station list
3. Double-click a station to start playing

### Organizing Stations

Stations can be organized in a two-level hierarchy:

- **Parent Category** > **Sub Category**
  - Music > Rock
  - Music > Jazz
  - Sports > Atlanta
  - Sports > Nashville
  - News > Local
  - News > International

The category tree in the UI automatically shows the hierarchy based on how you categorize your stations.

## Supported Stream Formats

### Direct Stream URLs
```
http://example.com/stream.mp3
http://example.com:8000/stream
```

### Playlist Files
- **PLS Format**: `http://example.com/station.pls`
- **M3U Format**: `http://example.com/station.m3u`
- **M3U8 Format**: `http://example.com/station.m3u8`

Muine will automatically parse these playlist files and extract the actual stream URL.

### ICY/Shoutcast Streams
Streams that support Icecast/Shoutcast metadata (ICY headers) will have their metadata automatically extracted, including:
- Station name
- Genre
- Bitrate
- Website
- Description

## Example Radio Stations

Here are some example URLs you can try (note: these are examples and may not be active):

```
# Direct MP3 streams
http://stream.example.com:8000/stream

# PLS playlist
http://example.com/radio.pls

# M3U playlist
http://example.com/station.m3u

# Shoutcast/Icecast
http://icecast.example.com:8000/radio.mp3
```

## Searching Stations

Use the search box at the top of the Radio tab to search across:
- Station names
- Genres
- Locations
- Descriptions

## Tips

1. **Use Categories**: Organize your stations with categories to make them easier to find
2. **Extract Metadata**: Always try the "Extract Metadata" button first - it will save you time filling in station details
3. **Test URLs**: If a station doesn't play, check that the URL is correct and the stream is active
4. **Stream Quality**: Higher bitrate stations (128kbps+) will sound better but use more bandwidth
5. **Radio Icon**: When playing a radio station, you'll see a 📻 icon in the player display

## Troubleshooting

### Station Won't Play
- Verify the stream URL is correct and active (try opening it in a web browser)
- Check your internet connection
- Some streams may be geographically restricted

### No Metadata Extracted
- Not all streams support metadata extraction
- You can manually fill in station details

### Categories Not Showing
- Make sure you've assigned at least one category to your stations
- The category tree shows categories that have stations assigned to them

## Technical Details

- **Database**: Radio stations are stored in SQLite alongside your music library
- **Playback**: Uses LibVLC for streaming (same engine as local music playback)
- **Seeking**: Radio streams cannot be seeked (no progress bar when playing radio)
- **Format Support**: Any format supported by LibVLC (MP3, OGG, AAC, etc.)

## Need Help?

If you encounter issues with internet radio support, please check:
1. That the stream URL is valid and accessible
2. That LibVLC is properly installed on your system
3. That you have an active internet connection

For bugs or feature requests, please file an issue on the GitHub repository.
