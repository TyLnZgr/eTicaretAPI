using ECommerce.Domain.Catalog;

namespace ECommerce.Api.Tests.Domain.Catalog;

public sealed class CategoryTests
{
    [Fact]
    public void Constructor_WhenValuesAreValid_CreatesNormalizedCategory()
    {
        var category = new Category(
            "  Computer Accessories  ",
            isActive: true);

        Assert.Equal("Computer Accessories", category.Name);
        Assert.True(category.IsActive);
        Assert.Empty(category.Products);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenNameIsBlank_Throws(string name)
    {
        Assert.Throws<ArgumentException>(() =>
            new Category(name, isActive: true));
    }

    [Fact]
    public void Constructor_WhenNameExceedsMaximumLength_Throws()
    {
        var name = new string('A', Category.MaxNameLength + 1);

        Assert.Throws<ArgumentException>(() =>
            new Category(name, isActive: true));
    }

    [Fact]
    public void UpdateDetails_WhenValuesAreValid_ChangesCategory()
    {
        var category = new Category("Old Name", isActive: false);

        category.UpdateDetails("  Updated Name  ", isActive: true);

        Assert.Equal("Updated Name", category.Name);
        Assert.True(category.IsActive);
    }

    [Fact]
    public void UpdateDetails_WhenNameIsInvalid_DoesNotChangeCategory()
    {
        var category = new Category("Original Name", isActive: false);

        Assert.Throws<ArgumentException>(() =>
            category.UpdateDetails("   ", isActive: true));

        Assert.Equal("Original Name", category.Name);
        Assert.False(category.IsActive);
    }
}
