using Muine.Core.Models;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Muine.Core.Services;

public class RadioMetadataService
{
    private readonly HttpClient _httpClient;

    public RadioMetadataService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Muine Music Player");
        _httpClient.DefaultRequestHeaders.Add("Icy-MetaData", "1");
    }

    /// <summary>
    /// Attempts to extract metadata from a radio stream URL
    /// </summary>
    public async Task<RadioStation> ExtractMetadataAsync(string url)
    {
        var station = new RadioStation
        {
            Url = url,
            Name = ExtractNameFromUrl(url)
        };

        try
        {
            // Try to parse as PLS playlist
            if (url.EndsWith(".pls", StringComparison.OrdinalIgnoreCase))
            {
                await ParsePlsPlaylistAsync(url, station);
                return station;
            }

            // Try to parse as M3U playlist
            if (url.EndsWith(".m3u", StringComparison.OrdinalIgnoreCase) || 
                url.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase))
            {
                await ParseM3uPlaylistAsync(url, station);
                return station;
            }

            // Try to get ICY metadata from the stream
            await ExtractIcyMetadataAsync(url, station);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: Could not extract metadata from {url}: {ex.Message}");
        }

        return station;
    }

    private async Task ParsePlsPlaylistAsync(string url, RadioStation station)
    {
        try
        {
            var content = await _httpClient.GetStringAsync(url);
            var lines = content.Split('\n');

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                if (trimmedLine.StartsWith("File1=", StringComparison.OrdinalIgnoreCase))
                {
                    station.Url = trimmedLine.Substring(6);
                }
                else if (trimmedLine.StartsWith("Title1=", StringComparison.OrdinalIgnoreCase))
                {
                    station.Name = trimmedLine.Substring(7);
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error parsing PLS playlist: {ex.Message}");
        }
    }

    private async Task ParseM3uPlaylistAsync(string url, RadioStation station)
    {
        try
        {
            var content = await _httpClient.GetStringAsync(url);
            var lines = content.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                
                // Extended M3U format: #EXTINF:duration,Artist - Title
                if (line.StartsWith("#EXTINF:", StringComparison.OrdinalIgnoreCase))
                {
                    var commaIndex = line.IndexOf(',');
                    if (commaIndex > 0 && commaIndex < line.Length - 1)
                    {
                        station.Name = line.Substring(commaIndex + 1).Trim();
                    }
                }
                // The actual stream URL (first non-comment line)
                else if (!line.StartsWith("#") && !string.IsNullOrWhiteSpace(line))
                {
                    station.Url = line;
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error parsing M3U playlist: {ex.Message}");
        }
    }

    private async Task ExtractIcyMetadataAsync(string url, RadioStation station)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Icy-MetaData", "1");

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            // Try to extract ICY headers
            if (response.Headers.TryGetValues("icy-name", out var nameValues))
            {
                station.Name = nameValues.FirstOrDefault() ?? station.Name;
            }

            if (response.Headers.TryGetValues("icy-genre", out var genreValues))
            {
                station.Genre = genreValues.FirstOrDefault() ?? string.Empty;
            }

            if (response.Headers.TryGetValues("icy-br", out var bitrateValues))
            {
                var bitrateStr = bitrateValues.FirstOrDefault();
                if (!string.IsNullOrEmpty(bitrateStr) && int.TryParse(bitrateStr, out int bitrate))
                {
                    station.Bitrate = bitrate;
                }
            }

            if (response.Headers.TryGetValues("icy-url", out var urlValues))
            {
                station.Website = urlValues.FirstOrDefault() ?? string.Empty;
            }

            if (response.Headers.TryGetValues("icy-description", out var descValues))
            {
                station.Description = descValues.FirstOrDefault() ?? string.Empty;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error extracting ICY metadata: {ex.Message}");
        }
    }

    private string ExtractNameFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            
            // Try to extract a meaningful name from the URL
            // Remove common streaming paths
            var path = uri.AbsolutePath.TrimEnd('/');
            if (!string.IsNullOrEmpty(path))
            {
                var segments = path.Split('/');
                var lastSegment = segments[^1];
                
                // Remove common file extensions
                lastSegment = Regex.Replace(lastSegment, @"\.(pls|m3u|m3u8)$", "", RegexOptions.IgnoreCase);
                
                if (!string.IsNullOrEmpty(lastSegment))
                {
                    // Convert underscores and hyphens to spaces, and capitalize
                    return Regex.Replace(lastSegment, @"[_-]", " ");
                }
            }

            // Fall back to hostname
            return uri.Host;
        }
        catch
        {
            return "Unknown Station";
        }
    }

    /// <summary>
    /// Validates if a URL is likely a valid radio stream
    /// </summary>
    public async Task<bool> ValidateStreamUrlAsync(string url)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            using var response = await _httpClient.SendAsync(request);

            // Check if the response is successful and has audio content
            if (response.IsSuccessStatusCode)
            {
                if (response.Content.Headers.ContentType?.MediaType != null)
                {
                    var mediaType = response.Content.Headers.ContentType.MediaType.ToLowerInvariant();
                    return mediaType.StartsWith("audio/") || 
                           mediaType == "application/ogg" || 
                           mediaType == "video/mp2t";
                }
                
                // If HEAD request doesn't work, check if ICY headers are present
                if (response.Headers.Contains("icy-name") || 
                    response.Headers.Contains("icy-genre"))
                {
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
