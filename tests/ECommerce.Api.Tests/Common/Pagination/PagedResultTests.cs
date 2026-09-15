using ECommerce.Application.Common.Pagination;

namespace ECommerce.Api.Tests.Common.Pagination;

public sealed class PagedResultTests
{
    [Fact]
    public void TotalPages_WhenCountIsNotDivisible_RoundsUp()
    {
        // Arrange
        var items = Array.Empty<string>();

        var result = new PagedResult<string>(
            items,
            Page: 1,
            PageSize: 2,
            TotalCount: 3);

        // Act
        var totalPages = result.TotalPages;

        // Assert
        Assert.Equal(2, totalPages);
    }

    [Fact]
    public void TotalPages_WhenCountIsDivisible_ReturnsExactPageCount()
    {
        // Arrange
        var result = new PagedResult<string>(
            Items: Array.Empty<string>(),
            Page: 1,
            PageSize: 2,
            TotalCount: 4);

        // Act
        var totalPages = result.TotalPages;

        // Assert
        Assert.Equal(2, totalPages);
    }

    [Fact]
    public void TotalPages_WhenThereAreNoItems_ReturnsZero()
    {
        // Arrange
        var result = new PagedResult<string>(
            Items: Array.Empty<string>(),
            Page: 1,
            PageSize: 20,
            TotalCount: 0);

        // Act
        var totalPages = result.TotalPages;

        // Assert
        Assert.Equal(0, totalPages);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(5, true)]
    public void HasPreviousPage_GivenPage_ReturnsExpectedResult(
        int page,
        bool expected)
    {
        // Arrange
        var result = new PagedResult<string>(
            Items: Array.Empty<string>(),
            Page: page,
            PageSize: 2,
            TotalCount: 10);

        // Act
        var hasPreviousPage = result.HasPreviousPage;

        // Assert
        Assert.Equal(expected, hasPreviousPage);
    }

    [Theory]
    [InlineData(1, 2, 3, true)]
    [InlineData(2, 2, 3, false)]
    [InlineData(1, 20, 0, false)]
    public void HasNextPage_GivenPaginationData_ReturnsExpectedResult(
    int page,
    int pageSize,
    int totalCount,
    bool expected)
    {
        // Arrange
        var result = new PagedResult<string>(
            Items: Array.Empty<string>(),
            Page: page,
            PageSize: pageSize,
            TotalCount: totalCount);

        // Act
        var hasNextPage = result.HasNextPage;

        // Assert
        Assert.Equal(expected, hasNextPage);
    }
}
