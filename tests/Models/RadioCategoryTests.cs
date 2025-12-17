using Muine.Core.Models;
using Xunit;

namespace Muine.Tests.Models;

public class RadioCategoryTests
{
    [Fact]
    public void RadioCategory_FullPath_ReturnsName_WhenNoParent()
    {
        // Arrange
        var category = new RadioCategory
        {
            Name = "Music"
        };

        // Act & Assert
        Assert.Equal("Music", category.FullPath);
    }

    [Fact]
    public void RadioCategory_FullPath_ReturnsParentAndName()
    {
        // Arrange
        var category = new RadioCategory
        {
            Name = "Rock",
            ParentCategory = "Music"
        };

        // Act & Assert
        Assert.Equal("Music > Rock", category.FullPath);
    }

    [Fact]
    public void RadioCategory_IsRootCategory_ReturnsTrue_WhenNoParent()
    {
        // Arrange
        var category = new RadioCategory
        {
            Name = "Music"
        };

        // Act & Assert
        Assert.True(category.IsRootCategory);
    }

    [Fact]
    public void RadioCategory_IsRootCategory_ReturnsFalse_WhenHasParent()
    {
        // Arrange
        var category = new RadioCategory
        {
            Name = "Rock",
            ParentCategory = "Music"
        };

        // Act & Assert
        Assert.False(category.IsRootCategory);
    }
}
