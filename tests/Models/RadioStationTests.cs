using Muine.Core.Models;
using Xunit;

namespace Muine.Tests.Models;

public class RadioStationTests
{
    [Fact]
    public void RadioStation_DisplayName_ReturnsName_WhenNameIsSet()
    {
        // Arrange
        var station = new RadioStation
        {
            Name = "Test Radio",
            Url = "http://example.com/stream"
        };

        // Act & Assert
        Assert.Equal("Test Radio", station.DisplayName);
    }

    [Fact]
    public void RadioStation_DisplayName_ReturnsUrl_WhenNameIsEmpty()
    {
        // Arrange
        var station = new RadioStation
        {
            Name = "",
            Url = "http://example.com/stream"
        };

        // Act & Assert
        Assert.Equal("http://example.com/stream", station.DisplayName);
    }

    [Fact]
    public void RadioStation_FullCategory_ReturnsParentAndChild()
    {
        // Arrange
        var station = new RadioStation
        {
            ParentCategory = "Music",
            Category = "Rock"
        };

        // Act & Assert
        Assert.Equal("Music > Rock", station.FullCategory);
    }

    [Fact]
    public void RadioStation_FullCategory_ReturnsOnlyParent_WhenChildIsEmpty()
    {
        // Arrange
        var station = new RadioStation
        {
            ParentCategory = "Music",
            Category = ""
        };

        // Act & Assert
        Assert.Equal("Music", station.FullCategory);
    }

    [Fact]
    public void RadioStation_FullCategory_ReturnsOnlyChild_WhenParentIsEmpty()
    {
        // Arrange
        var station = new RadioStation
        {
            ParentCategory = "",
            Category = "Rock"
        };

        // Act & Assert
        Assert.Equal("Rock", station.FullCategory);
    }

    [Fact]
    public void RadioStation_HasCategory_ReturnsTrue_WhenCategoryIsSet()
    {
        // Arrange
        var station = new RadioStation
        {
            Category = "Rock"
        };

        // Act & Assert
        Assert.True(station.HasCategory);
    }

    [Fact]
    public void RadioStation_HasCategory_ReturnsFalse_WhenNoCategoryIsSet()
    {
        // Arrange
        var station = new RadioStation();

        // Act & Assert
        Assert.False(station.HasCategory);
    }
}
