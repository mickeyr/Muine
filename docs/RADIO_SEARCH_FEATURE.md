# Radio Station Online Search Feature

## Overview

Muine now includes the ability to search for radio stations online using the [Radio-Browser.info](https://www.radio-browser.info/) API. This feature allows users to discover thousands of internet radio stations from around the world and add them to their local library.

## Features

### Single Search Box
- Search by **station name**, **city**, or **genre** using one unified search box
- No need for complicated multi-field search forms
- Results are sorted by popularity (most voted stations first)

### Clean User Interface
- **Two-tab design** in the Radio section:
  - **My Stations**: Manage your local radio station library
  - **Search Online**: Discover new stations from Radio-Browser.info
- Search results only appear after user searches (no default flood of results)
- Easy to navigate between local and online stations

### Adding Stations to Library
Multiple ways to add a discovered station:
1. **Double-click** the station in search results
2. **Right-click** and select "Add to Library" from context menu
3. **Right-click** and select "Play" to test before adding

### Station Information
Search results include:
- Station name
- Genre/tags
- Country/location
- Bitrate (audio quality)

## Usage

### How to Search for Stations

1. Open Muine and click on the **Radio** tab
2. Click on the **Search Online** sub-tab
3. Enter your search term in the search box:
   - Station name: `"BBC"`, `"NPR"`, `"KEXP"`
   - Genre: `"jazz"`, `"rock"`, `"classical"`
   - City: `"London"`, `"Paris"`, `"Tokyo"`
4. Click **Search Online** button
5. Browse through the results

### Adding Stations to Your Library

1. Find a station you like in the search results
2. **Double-click** the station (or right-click and select "Add to Library")
3. The station is now saved to your local library
4. Switch to the **My Stations** tab to see all your stations
5. Optionally, categorize the station using the "Edit" function

### Playing Stations

- Stations can be played directly from search results (right-click → Play)
- Added stations can be played from the My Stations tab
- Double-click any station to play it

## Technical Details

### Dependencies
- **RadioBrowser.NET** (v0.7.0): Third-party package for accessing Radio-Browser.info API
- Data source: [https://www.radio-browser.info/](https://www.radio-browser.info/)

### API Features Used
- Advanced search across multiple fields
- Popularity-based sorting
- Limit results to prevent overwhelming the user

### Data Storage
- Found stations are converted to Muine's internal RadioStation format
- When added to library, stations are stored in the local SQLite database
- Stations can be categorized, edited, and managed like manually-added stations

## Limitations

- Requires internet connection to search for stations
- Search results limited to 100 stations per query (configurable in code)
- Some stations may have broken URLs (depends on Radio-Browser.info data quality)
- Station metadata accuracy depends on Radio-Browser.info community

## Future Enhancements

Possible improvements for future versions:
- Filter search results by country, language, or bitrate
- Preview/sample station before adding
- Import multiple stations at once
- Browse by categories/genres without searching
- Show station logos/icons if available

## Troubleshooting

### No Search Results
- Check your internet connection
- Try a different search term
- Radio-Browser.info API might be temporarily unavailable

### Station Won't Play
- The station URL might be broken or outdated
- Try finding an alternative station with similar content
- Report broken stations to Radio-Browser.info community

### Duplicate Station Warning
- If you try to add a station that's already in your library, you'll see a message
- Check the My Stations tab to manage existing stations

## Credits

- Radio station data provided by [Radio-Browser.info](https://www.radio-browser.info/)
- RadioBrowser.NET library: [https://git.sr.ht/~youkai/RadioBrowser.NET](https://git.sr.ht/~youkai/RadioBrowser.NET)
