using Microsoft.EntityFrameworkCore;
using RentalApp.Contracts.Requests;
using RentalApp.Database.Data;
using RentalApp.Database.Repositories;
using RentalApp.Http;
using RentalApp.Services;
using RentalApp.Test.Fixtures;

namespace RentalApp.Test.Services;

public class LocalItemServiceTests
    : IClassFixture<DatabaseFixture<LocalItemServiceTests>>,
        IAsyncLifetime
{
    private readonly DatabaseFixture<LocalItemServiceTests> _fixture;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly AuthTokenState _tokenState = new();

    public LocalItemServiceTests(DatabaseFixture<LocalItemServiceTests> fixture)
    {
        _fixture = fixture;
        _contextFactory = fixture.ContextFactory;
    }

    public async Task InitializeAsync()
    {
        _tokenState.CurrentToken = null;
        await _fixture.ResetItemsAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

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
    public async Task GetItemsAsync_WithSearchFilter_ReturnsMatchingItems()
    {
        var result = await CreateSut().GetItemsAsync(new GetItemsRequest(1, 20, null, "drill"));
        Assert.All(
            result.Items,
            i => Assert.Contains("drill", i.Title, StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public async Task GetItemAsync_ExistingItem_ReturnsItemDetail()
    {
        var result = await CreateSut().GetItemAsync(1);

        Assert.Equal(1, result.Id);
        Assert.Equal("Test Drill", result.Title);
    }

    [Fact]
    public async Task GetItemAsync_NonExistentItem_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSut().GetItemAsync(999));
    }

    [Fact]
    public async Task GetNearbyItemsAsync_WithRadiusThatExcludesFarItems_OnlyReturnsNearbyItems()
    {
        // Item 1 is at the origin (~0 km), Item 2 is ~1.5 km, Item 3 is ~20 km
        var result = await CreateSut()
            .GetNearbyItemsAsync(new GetNearbyItemsRequest(Lat: 55.9533, Lon: -3.1883, Radius: 2));

        var ids = result.Items.Select(i => i.Id).ToList();
        Assert.Contains(1, ids);
        Assert.Contains(2, ids);
        Assert.DoesNotContain(3, ids);
    }

    [Fact]
    public async Task GetNearbyItemsAsync_ReturnsItemsOrderedByDistance()
    {
        var result = await CreateSut()
            .GetNearbyItemsAsync(new GetNearbyItemsRequest(Lat: 55.9533, Lon: -3.1883, Radius: 5));

        // Item 1 is at the search origin, Item 2 is ~1.5 km away
        Assert.Equal(1, result.Items[0].Id);
        Assert.Equal(2, result.Items[1].Id);
        Assert.True(result.Items[0].Distance < result.Items[1].Distance);
    }

    [Fact]
    public async Task UpdateItemAsync_ValidRequest_ReturnsUpdatedItem()
    {
        var request = new UpdateItemRequest("Updated Drill", "An updated description", 15.0, false);
        var result = await CreateSut().UpdateItemAsync(1, request);

        Assert.Equal(1, result.Id);
        Assert.Equal("Updated Drill", result.Title);
        Assert.Equal(15.0, result.DailyRate);
        Assert.False(result.IsAvailable);
    }

    [Fact]
    public async Task CreateItemAsync_NoSession_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateSut()
                .CreateItemAsync(new CreateItemRequest("Title", "Desc", 10.0, 1, 55.0, -3.0))
        );
    }

    [Fact]
    public async Task CreateItemAsync_WithValidSession_ReturnsCreatedItem()
    {
        _tokenState.CurrentToken = "1";
        var request = new CreateItemRequest("New Hammer", "A hammer", 5.0, 1, 55.9533, -3.1883);

        var result = await CreateSut().CreateItemAsync(request);

        Assert.Equal("New Hammer", result.Title);
        Assert.Equal(1, result.OwnerId);
        Assert.Equal(55.9533, result.Latitude);
        Assert.Equal(-3.1883, result.Longitude);
    }
}
