using Muine.Core.Models;
using Muine.Core.Services;
using Xunit;

namespace Muine.Tests.Services;

public class RadioStationServiceTests : IDisposable
{
    private readonly RadioStationService _service;
    private readonly string _dbPath;

    public RadioStationServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"test_radio_{Guid.NewGuid()}.db");
        _service = new RadioStationService(_dbPath);
        _service.InitializeAsync().Wait();
    }

    [Fact]
    public async Task SaveStationAsync_ShouldSaveNewStation()
    {
        // Arrange
        var station = new RadioStation
        {
            Name = "Test Radio",
            Url = "http://example.com/stream",
            Genre = "Rock",
            Location = "Test City"
        };

        // Act
        var id = await _service.SaveStationAsync(station);

        // Assert
        Assert.True(id > 0);
        
        var retrieved = await _service.GetStationByIdAsync(id);
        Assert.NotNull(retrieved);
        Assert.Equal("Test Radio", retrieved!.Name);
        Assert.Equal("http://example.com/stream", retrieved.Url);
        Assert.Equal("Rock", retrieved.Genre);
        Assert.Equal("Test City", retrieved.Location);
    }

    [Fact]
    public async Task SaveStationAsync_ShouldUpdateExistingStation()
    {
        // Arrange
        var station = new RadioStation
        {
            Name = "Test Radio",
            Url = "http://example.com/stream",
            Genre = "Rock"
        };
        var id = await _service.SaveStationAsync(station);

        // Act - Save again with same URL but different name
        station.Name = "Updated Radio";
        station.Genre = "Jazz";
        var updatedId = await _service.SaveStationAsync(station);

        // Assert
        var retrieved = await _service.GetStationByIdAsync(updatedId);
        Assert.NotNull(retrieved);
        Assert.Equal("Updated Radio", retrieved!.Name);
        Assert.Equal("Jazz", retrieved.Genre);
    }

    [Fact]
    public async Task GetAllStationsAsync_ShouldReturnAllStations()
    {
        // Arrange
        await _service.SaveStationAsync(new RadioStation { Name = "Station 1", Url = "http://example.com/1" });
        await _service.SaveStationAsync(new RadioStation { Name = "Station 2", Url = "http://example.com/2" });
        await _service.SaveStationAsync(new RadioStation { Name = "Station 3", Url = "http://example.com/3" });

        // Act
        var stations = await _service.GetAllStationsAsync();

        // Assert
        Assert.Equal(3, stations.Count);
    }

    [Fact]
    public async Task GetStationByUrlAsync_ShouldReturnStation()
    {
        // Arrange
        var url = "http://example.com/stream";
        await _service.SaveStationAsync(new RadioStation { Name = "Test Radio", Url = url });

        // Act
        var retrieved = await _service.GetStationByUrlAsync(url);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("Test Radio", retrieved!.Name);
    }

    [Fact]
    public async Task DeleteStationAsync_ShouldRemoveStation()
    {
        // Arrange
        var station = new RadioStation { Name = "Test Radio", Url = "http://example.com/stream" };
        var id = await _service.SaveStationAsync(station);

        // Act
        await _service.DeleteStationAsync(id);

        // Assert
        var retrieved = await _service.GetStationByIdAsync(id);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task GetStationsGroupedByCategoryAsync_ShouldGroupCorrectly()
    {
        // Arrange
        await _service.SaveStationAsync(new RadioStation 
        { 
            Name = "Rock Station 1", 
            Url = "http://example.com/rock1",
            ParentCategory = "Music",
            Category = "Rock"
        });
        await _service.SaveStationAsync(new RadioStation 
        { 
            Name = "Rock Station 2", 
            Url = "http://example.com/rock2",
            ParentCategory = "Music",
            Category = "Rock"
        });
        await _service.SaveStationAsync(new RadioStation 
        { 
            Name = "Jazz Station", 
            Url = "http://example.com/jazz",
            ParentCategory = "Music",
            Category = "Jazz"
        });

        // Act
        var grouped = await _service.GetStationsGroupedByCategoryAsync();

        // Assert
        Assert.Equal(2, grouped.Count);
        Assert.True(grouped.ContainsKey("Music > Rock"));
        Assert.True(grouped.ContainsKey("Music > Jazz"));
        Assert.Equal(2, grouped["Music > Rock"].Count);
        Assert.Single(grouped["Music > Jazz"]);
    }

    [Fact]
    public async Task SearchStationsAsync_ShouldFindMatchingStations()
    {
        // Arrange
        await _service.SaveStationAsync(new RadioStation { Name = "Rock Radio", Url = "http://example.com/1", Genre = "Rock" });
        await _service.SaveStationAsync(new RadioStation { Name = "Jazz Station", Url = "http://example.com/2", Genre = "Jazz" });
        await _service.SaveStationAsync(new RadioStation { Name = "Classic Rock", Url = "http://example.com/3", Genre = "Rock" });

        // Act
        var results = await _service.SearchStationsAsync("Rock");

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, s => Assert.Contains("Rock", s.Name + s.Genre));
    }

    [Fact]
    public async Task SaveCategoryAsync_ShouldSaveCategory()
    {
        // Arrange
        var category = new RadioCategory
        {
            Name = "Rock",
            ParentCategory = "Music",
            DisplayOrder = 1
        };

        // Act
        var id = await _service.SaveCategoryAsync(category);

        // Assert
        Assert.True(id > 0);
        var categories = await _service.GetAllCategoriesAsync();
        Assert.Contains(categories, c => c.Name == "Rock" && c.ParentCategory == "Music");
    }

    [Fact]
    public async Task GetRootCategoriesAsync_ShouldReturnOnlyRootCategories()
    {
        // Arrange
        await _service.SaveCategoryAsync(new RadioCategory { Name = "Music", ParentCategory = "" });
        await _service.SaveCategoryAsync(new RadioCategory { Name = "Rock", ParentCategory = "Music" });
        await _service.SaveCategoryAsync(new RadioCategory { Name = "Sports", ParentCategory = "" });

        // Act
        var rootCategories = await _service.GetRootCategoriesAsync();

        // Assert
        Assert.Equal(2, rootCategories.Count);
        Assert.All(rootCategories, c => Assert.True(c.IsRootCategory));
    }

    [Fact]
    public async Task GetSubCategoriesAsync_ShouldReturnChildCategories()
    {
        // Arrange
        await _service.SaveCategoryAsync(new RadioCategory { Name = "Music", ParentCategory = "" });
        await _service.SaveCategoryAsync(new RadioCategory { Name = "Rock", ParentCategory = "Music" });
        await _service.SaveCategoryAsync(new RadioCategory { Name = "Jazz", ParentCategory = "Music" });
        await _service.SaveCategoryAsync(new RadioCategory { Name = "Sports", ParentCategory = "" });

        // Act
        var musicSubCategories = await _service.GetSubCategoriesAsync("Music");

        // Assert
        Assert.Equal(2, musicSubCategories.Count);
        Assert.All(musicSubCategories, c => Assert.Equal("Music", c.ParentCategory));
    }

    [Fact]
    public async Task UpdateLastPlayedAsync_ShouldUpdateTimestamp()
    {
        // Arrange
        var station = new RadioStation { Name = "Test Radio", Url = "http://example.com/stream" };
        var id = await _service.SaveStationAsync(station);
        var before = DateTime.UtcNow;

        // Act
        await Task.Delay(100); // Ensure some time passes
        await _service.UpdateLastPlayedAsync(id);

        // Assert
        var retrieved = await _service.GetStationByIdAsync(id);
        Assert.NotNull(retrieved);
        Assert.NotNull(retrieved!.LastPlayed);
        Assert.True(retrieved.LastPlayed > before);
    }

    public void Dispose()
    {
        _service.Dispose();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
}
