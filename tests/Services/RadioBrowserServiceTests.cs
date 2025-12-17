using Muine.Core.Services;
using Xunit;

namespace Muine.Tests.Services;

public class RadioBrowserServiceTests : IDisposable
{
    private RadioBrowserService? _service;

    // Note: RadioBrowserClient requires network access during construction
    // So we mark all tests as requiring network access or skip them

    [Fact(Skip = "Requires network access - RadioBrowserClient connects on construction")]
    public async Task SearchStationsAsync_WithEmptyQuery_ReturnsEmptyList()
    {
        // Arrange
        _service = new RadioBrowserService();
        var query = "";

        // Act
        var results = await _service.SearchStationsAsync(query);

        // Assert
        Assert.Empty(results);
    }

    [Fact(Skip = "Requires network access - RadioBrowserClient connects on construction")]
    public async Task SearchStationsAsync_WithWhitespaceQuery_ReturnsEmptyList()
    {
        // Arrange
        _service = new RadioBrowserService();
        var query = "   ";

        // Act
        var results = await _service.SearchStationsAsync(query);

        // Assert
        Assert.Empty(results);
    }

    [Fact(Skip = "Requires network access - RadioBrowserClient connects on construction")]
    public async Task SearchStationsAsync_WithValidQuery_ReturnsResults()
    {
        // Arrange
        _service = new RadioBrowserService();
        var query = "BBC";

        // Act
        var results = await _service.SearchStationsAsync(query, limit: 5);

        // Assert
        Assert.NotNull(results);
        // We can't assert on count as it depends on API availability
    }

    [Fact(Skip = "Requires network access - RadioBrowserClient connects on construction")]
    public async Task GetPopularStationsAsync_ReturnsResults()
    {
        // Arrange
        _service = new RadioBrowserService();
        
        // Act
        var results = await _service.GetPopularStationsAsync(limit: 5);

        // Assert
        Assert.NotNull(results);
    }

    public void Dispose()
    {
        _service?.Dispose();
    }
}
