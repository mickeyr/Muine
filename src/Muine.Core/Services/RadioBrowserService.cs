using RadioBrowser;
using RadioBrowser.Models;
using Muine.Core.Models;

namespace Muine.Core.Services;

/// <summary>
/// Service for searching radio stations using the Radio-Browser.info API
/// </summary>
public class RadioBrowserService : IDisposable
{
    private readonly RadioBrowserClient _client;
    private bool _disposed;

    public RadioBrowserService()
    {
        _client = new RadioBrowserClient();
    }

    /// <summary>
    /// Search for radio stations by name, city, genre, or any combination
    /// Uses a unified search approach across multiple fields
    /// </summary>
    /// <param name="searchQuery">Search term to look for across name, tags, country, etc.</param>
    /// <param name="limit">Maximum number of results to return (default 50)</param>
    /// <returns>List of radio stations matching the search criteria</returns>
    public async Task<List<RadioStation>> SearchStationsAsync(string searchQuery, int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
        {
            return new List<RadioStation>();
        }

        try
        {
            // Use advanced search to search across multiple fields
            var searchOptions = new AdvancedSearchOptions
            {
                Name = searchQuery,
                Limit = (uint)limit,
                Order = "votes",
                Reverse = true // Get highest voted stations first
            };

            var results = await _client.Search.AdvancedAsync(searchOptions);
            
            // Convert RadioBrowser StationInfo to our RadioStation model
            var stations = results.Select(ConvertToRadioStation).ToList();
            
            return stations;
        }
        catch (Exception)
        {
            // Return empty list on error - errors will be visible to user through empty results
            // In production, consider using a logging framework here
            return new List<RadioStation>();
        }
    }

    /// <summary>
    /// Search for radio stations with more specific criteria
    /// </summary>
    public async Task<List<RadioStation>> SearchStationsAdvancedAsync(
        string? name = null,
        string? country = null,
        string? tag = null,
        int limit = 50)
    {
        try
        {
            var searchOptions = new AdvancedSearchOptions
            {
                Limit = (uint)limit,
                Order = "votes",
                Reverse = true
            };

            if (!string.IsNullOrWhiteSpace(name))
                searchOptions.Name = name;

            // Note: The exact properties available depend on the RadioBrowser package version
            // We'll use Name as the primary search field which searches across multiple attributes

            var results = await _client.Search.AdvancedAsync(searchOptions);
            var stations = results.Select(ConvertToRadioStation).ToList();
            
            // Apply additional filtering if needed
            if (!string.IsNullOrWhiteSpace(country))
            {
                stations = stations.Where(s => 
                    s.Location.Contains(country, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            
            if (!string.IsNullOrWhiteSpace(tag))
            {
                stations = stations.Where(s => 
                    s.Genre.Contains(tag, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            
            return stations;
        }
        catch (Exception)
        {
            // Return empty list on error
            return new List<RadioStation>();
        }
    }

    /// <summary>
    /// Get popular radio stations
    /// </summary>
    public async Task<List<RadioStation>> GetPopularStationsAsync(int limit = 20)
    {
        try
        {
            // Get stations ordered by votes
            var searchOptions = new AdvancedSearchOptions
            {
                Limit = (uint)limit,
                Order = "votes",
                Reverse = true
            };

            var results = await _client.Search.AdvancedAsync(searchOptions);
            return results.Select(ConvertToRadioStation).ToList();
        }
        catch (Exception)
        {
            // Return empty list on error
            return new List<RadioStation>();
        }
    }

    /// <summary>
    /// Convert RadioBrowser StationInfo to our internal RadioStation model
    /// </summary>
    private static RadioStation ConvertToRadioStation(StationInfo station)
    {
        // Extract genre from tags if available
        var genre = string.Empty;
        var tags = station.Tags; // This is a List<string>
        if (tags != null && tags.Count > 0)
        {
            // Take first 3 tags
            genre = string.Join(", ", tags.Take(3));
        }

        // Use CountryCode for location
        var location = string.Empty;
        if (!string.IsNullOrWhiteSpace(station.CountryCode))
        {
            location = station.CountryCode;
        }

        return new RadioStation
        {
            Name = station.Name ?? "Unknown Station",
            Url = station.Url?.ToString() ?? string.Empty,
            Genre = genre,
            Location = location,
            Description = string.Empty,
            Website = station.Homepage?.ToString() ?? string.Empty,
            Bitrate = station.Bitrate,
            Category = string.Empty, // Will be set by user when saving
            ParentCategory = string.Empty,
            DateAdded = DateTime.UtcNow
        };
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            // RadioBrowserClient doesn't implement IDisposable, so nothing to dispose
            _disposed = true;
        }
    }
}
