using Microsoft.EntityFrameworkCore;
using RentalApp.Contracts.Requests;
using RentalApp.Database.Data;
using RentalApp.Database.Repositories;
using RentalApp.Http;
using RentalApp.Services;
using RentalApp.Test.Fixtures;

namespace RentalApp.Test.Services;

public class LocalItemServiceTests : IClassFixture<DatabaseFixture<LocalItemServiceTests>>
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly AuthTokenState _tokenState = new();

    public LocalItemServiceTests(DatabaseFixture<LocalItemServiceTests> fixture)
    {
        _contextFactory = fixture.ContextFactory;
    }

    private LocalItemService CreateSut()
    {
        var itemRepo = new ItemRepository(_contextFactory);
        var catRepo = new CategoryRepository(_contextFactory);
        return new LocalItemService(itemRepo, catRepo, _tokenState);
    }

    [Fact]
    public async Task GetCategoriesAsync_ReturnsCategories()
    {
        var result = await CreateSut().GetCategoriesAsync();
        Assert.NotNull(result.Categories);
    }

    [Fact]
    public async Task GetCategoriesAsync_ItemCountMatchesSeededData()
    {
        var result = await CreateSut().GetCategoriesAsync();

        var tools = result.Categories.Single(c => c.Slug == "tools");
        var electronics = result.Categories.Single(c => c.Slug == "electronics");
        Assert.Equal(2, tools.ItemCount);
        Assert.Equal(1, electronics.ItemCount);
    }

    [Fact]
    public async Task GetItemsAsync_ReturnsItemsResponse()
    {
        var result = await CreateSut().GetItemsAsync(new GetItemsRequest(1, 20, null, null));
        Assert.NotNull(result);
    }

    [Fact]
    public async Task CreateItemAsync_NoSession_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateSut().CreateItemAsync(new CreateItemRequest("Title", "Desc", 10.0, 1, 55.0, -3.0))
        );
    }
}
